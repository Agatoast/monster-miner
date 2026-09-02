using System.Collections.Generic;
using MonsterMiner.Core;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class DriveableBoat : MonoBehaviour, ICargoVehicle
    {
        const float MaxForwardSpeedMph = 50f;
        const float MaxReverseSpeedMph = 10f;
        const float SpeedChangeMphPerSecond = 5f;
        const float TurnSpeedDegreesPerSecond = 25f;
        const float DeckBoundaryInsetFeet = 1.1f;
        const float SolidImpactShakeDuration = 3f;
        const float SolidImpactShakeIntensity = 0.3f;
        const float SolidImpactCooldownSeconds = 3.1f;
        const float SolidImpactShakeMinSpeedMph = 10f;

        Transform deck;
        Transform helm;
        Rigidbody rb;
        PlayerVehicleMount driver;
        PlayerVehicleMount cargoOccupant;
        Vector3 cargoHalfExtents = new Vector3(2.4f, 0.2f, 5.5f);
        float cargoDeckTopLocalY;
        Vector3 cargoEntryLocalPosition;
        float waterVerticalOffsetFeet;
        float bottomOffset = 0.5f;
        Vector3 hullSampleCenterLocal;
        Vector3 hullHalfExtentsLocal = new Vector3(2.8f, 1.2f, 7f);
        Vector3 bowLocalDirection = Vector3.right;
        Vector3 beamLocalDirection = Vector3.forward;
        float hullHalfLengthLocal;
        float hullHalfBeamLocal;
        Vector3 lastValidPosition;
        Quaternion lastValidRotation;
        bool hasLastValidPose;
        float signedSpeedMph;
        Vector2[] deckBoundaryCargoLocal;

        Renderer[] bodyRenderers;
        HashSet<Renderer> hullVisibilityHidden;
        bool driverViewHidden;
        bool storedDetectCollisions = true;
        RigidbodyInterpolation storedInterpolation;
        Collider[] walkDeckColliders;
        Vector3 lastDriveSampleWorld;
        bool hasDriveSample;
        float measuredDriveSpeedMph;
        float lastSolidImpactTime = -999f;

        public Transform CargoBed => deck;
        public Transform HostTransform => transform;
        public Transform Helm => helm;
        public Vector3 CargoEntryLocalPosition => cargoEntryLocalPosition;
        public bool HasDriver => driver != null;
        public bool HasCargoOccupant => cargoOccupant != null;
        public bool IsNearDismountShore => SampleNearDismountShore();
        public bool CanDismountToLand => IsNearDismountShore;
        public bool CanDismount => CanDismountToLand;
        public bool CanReturnToDeck => true;

        public Vector3 BowForwardWorld => GetBowForwardWorld(transform.rotation);

        float CurrentSpeedMagnitude
        {
            get
            {
                if (!HasDriver)
                    return rb != null ? rb.linearVelocity.magnitude : 0f;

                return WorldScale.MilesPerHour(Mathf.Abs(signedSpeedMph));
            }
        }

        public float DisplaySpeedMph => HasDriver ? measuredDriveSpeedMph : GetPassiveSpeedMph();

        float GetPassiveSpeedMph()
        {
            if (rb == null)
                return 0f;

            float forwardUnits = Vector3.Dot(rb.linearVelocity, GetBowForwardWorld());
            float oneMph = WorldScale.MilesPerHour(1f);
            if (oneMph <= 0.0001f)
                return 0f;

            return Mathf.Abs(forwardUnits / oneMph);
        }

        public void Initialize(
            Transform boatDeck,
            Transform helmSeat,
            Vector3 deckHalfExtents,
            float verticalOffsetFeet,
            Vector3 bowLocalDir,
            IList<Vector2> deckBoundaryCargoLocalXZ = null,
            Vector3? cargoEntryLocal = null)
        {
            deck = boatDeck;
            helm = helmSeat;
            cargoHalfExtents = deckHalfExtents;
            deckBoundaryCargoLocal = CopyDeckBoundary(deckBoundaryCargoLocalXZ);
            waterVerticalOffsetFeet = verticalOffsetFeet;
            bowLocalDirection = bowLocalDir.sqrMagnitude > 0.0001f ? bowLocalDir.normalized : Vector3.right;
            beamLocalDirection = Vector3.Cross(Vector3.up, bowLocalDirection).normalized;
            if (beamLocalDirection.sqrMagnitude < 0.0001f)
                beamLocalDirection = Vector3.forward;
            CacheCargoDeckTopLocalY();
            float defaultStandY = cargoDeckTopLocalY + WorldScale.CharacterHeightUnits * 0.5f;
            cargoEntryLocalPosition = cargoEntryLocal ?? new Vector3(0f, defaultStandY, 0f);

            rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            CacheBottomOffset();
            CacheHullHalfExtentsLocal();
            PreserveSpawnPose(transform.position, transform.rotation);
            rb.WakeUp();
        }

        public void PreserveSpawnPose(Vector3 worldPosition, Quaternion worldRotation)
        {
            lastValidPosition = worldPosition;
            lastValidRotation = worldRotation;
            hasLastValidPose = true;
            rb.position = worldPosition;
            rb.rotation = worldRotation;
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            Physics.SyncTransforms();
        }

        public bool IsForwardHalfLocalPoint(Vector3 localPoint)
        {
            Vector3 fromCenter = localPoint - hullSampleCenterLocal;
            return Vector3.Dot(fromCenter, bowLocalDirection) >= 0f;
        }

        public Vector3 ClampCargoLocalPosition(Vector3 localPosition)
        {
            localPosition.y = CargoEntryLocalPosition.y;

            if (deckBoundaryCargoLocal == null || deckBoundaryCargoLocal.Length < 3)
            {
                localPosition.x = Mathf.Clamp(localPosition.x, -cargoHalfExtents.x, cargoHalfExtents.x);
                localPosition.z = Mathf.Clamp(localPosition.z, -cargoHalfExtents.z, cargoHalfExtents.z);
                return localPosition;
            }

            float inset = WorldScale.Feet(DeckBoundaryInsetFeet);
            if (deck != null)
            {
                float scale = Mathf.Max(deck.lossyScale.x, 0.0001f);
                inset /= scale;
            }

            Vector2 clamped = ClampPointToDeckBoundary(
                new Vector2(localPosition.x, localPosition.z),
                deckBoundaryCargoLocal,
                inset);
            localPosition.x = clamped.x;
            localPosition.z = clamped.y;
            return localPosition;
        }

        static Vector2[] CopyDeckBoundary(IList<Vector2> source)
        {
            if (source == null || source.Count < 3)
                return null;

            var copy = new Vector2[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];

            return copy;
        }

        static Vector2 ClampPointToDeckBoundary(Vector2 point, IReadOnlyList<Vector2> polygon, float inset)
        {
            if (IsPointInPolygon(point, polygon))
                return point;

            Vector2 onEdge = ClosestPointOnPolygonBoundary(point, polygon);
            Vector2 centroid = GetPolygonCentroid(polygon);
            Vector2 inward = centroid - onEdge;
            if (inward.sqrMagnitude < 0.000001f)
                return onEdge;

            inward.Normalize();
            for (float step = inset; step >= inset * 0.1f; step -= inset * 0.1f)
            {
                Vector2 candidate = onEdge + inward * step;
                if (IsPointInPolygon(candidate, polygon))
                    return candidate;
            }

            return onEdge;
        }

        static Vector2 ClosestPointOnPolygonBoundary(Vector2 point, IReadOnlyList<Vector2> polygon)
        {
            Vector2 best = polygon[0];
            float bestDistSq = float.PositiveInfinity;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 closest = ClosestPointOnSegment(point, polygon[j], polygon[i]);
                float distSq = (closest - point).sqrMagnitude;
                if (distSq >= bestDistSq)
                    continue;

                bestDistSq = distSq;
                best = closest;
            }

            return best;
        }

        static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;
            if (lengthSq < 0.000001f)
                return a;

            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSq);
            return a + ab * t;
        }

        static Vector2 GetPolygonCentroid(IReadOnlyList<Vector2> polygon)
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < polygon.Count; i++)
                sum += polygon[i];

            return sum / polygon.Count;
        }

        static bool IsPointInPolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                bool intersects = (a.y > point.y) != (b.y > point.y)
                    && point.x < (b.x - a.x) * (point.y - a.y) / ((b.y - a.y) + 0.000001f) + a.x;
                if (intersects)
                    inside = !inside;
            }

            return inside;
        }

        public void SetDriver(PlayerVehicleMount mount)
        {
            driver = mount;
            signedSpeedMph = 0f;
            measuredDriveSpeedMph = 0f;
            hasDriveSample = false;
            rb.isKinematic = true;
            storedDetectCollisions = rb.detectCollisions;
            storedInterpolation = rb.interpolation;
            rb.detectCollisions = false;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.WakeUp();
            SetWalkDeckCollidersEnabled(false);
            SetDriverSailView(true);
        }

        public void ClearDriver(PlayerVehicleMount mount, bool enableWalkDeckColliders = true)
        {
            if (driver == mount)
            {
                driver = null;
                signedSpeedMph = 0f;
                measuredDriveSpeedMph = 0f;
                hasDriveSample = false;
                rb.isKinematic = true;
                rb.detectCollisions = storedDetectCollisions;
                rb.interpolation = storedInterpolation;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                StickToSurface();
                PreserveSpawnPose(rb.position, rb.rotation);
                if (enableWalkDeckColliders)
                    SetWalkDeckCollidersEnabled(true);
                SetDriverSailView(false);
            }
        }

        public void EnableWalkDeckColliders() => SetWalkDeckCollidersEnabled(true);

        void SetWalkDeckCollidersEnabled(bool enabled)
        {
            if (walkDeckColliders == null)
            {
                var colliders = GetComponentsInChildren<Collider>(true);
                var deckColliders = new List<Collider>();
                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider collider = colliders[i];
                    if (collider == null || collider.isTrigger)
                        continue;

                    Transform current = collider.transform;
                    while (current != null && current != transform)
                    {
                        string objectName = current.gameObject.name;
                        if (objectName == "BoatFloorWalkCollider"
                            || objectName == "WalkDeckCollision"
                            || objectName == "BoatWalkDeck"
                            || objectName.StartsWith("WalkDeckCell"))
                        {
                            deckColliders.Add(collider);
                            break;
                        }

                        current = current.parent;
                    }
                }

                walkDeckColliders = deckColliders.ToArray();
            }

            for (int i = 0; i < walkDeckColliders.Length; i++)
            {
                if (walkDeckColliders[i] != null)
                    walkDeckColliders[i].enabled = enabled;
            }
        }

        public void RegisterHullVisibilityOverrides(IEnumerable<Renderer> hiddenRenderers)
        {
            hullVisibilityHidden = hiddenRenderers == null
                ? null
                : new HashSet<Renderer>(hiddenRenderers);
        }

        public void SetDriverViewHidden(bool hidden)
        {
            SetDriverSailView(hidden);
        }

        void SetDriverSailView(bool active)
        {
            if (driverViewHidden == active)
                return;

            bodyRenderers = GetComponentsInChildren<Renderer>(true);
            driverViewHidden = active;
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                Renderer renderer = bodyRenderers[i];
                if (renderer == null)
                    continue;

                if (hullVisibilityHidden != null && hullVisibilityHidden.Contains(renderer))
                {
                    renderer.enabled = false;
                    continue;
                }

                if (!active)
                {
                    if (hullVisibilityHidden != null && hullVisibilityHidden.Contains(renderer))
                        renderer.enabled = false;
                    else
                        renderer.enabled = true;
                    continue;
                }

                renderer.enabled = !ShouldHideRendererDuringSailView(renderer.gameObject.name);
            }
        }

        static bool ShouldHideRendererDuringSailView(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return false;

            return objectName.IndexOf("Sail", System.StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Mast", System.StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Rope", System.StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Oar", System.StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Anchor", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void StopForDismount()
        {
            signedSpeedMph = 0f;
            measuredDriveSpeedMph = 0f;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        public void SetCargoOccupant(PlayerVehicleMount mount)
        {
            cargoOccupant = mount;
        }

        public void ClearCargoOccupant(PlayerVehicleMount mount)
        {
            if (cargoOccupant == mount)
                cargoOccupant = null;
        }

        void FixedUpdate()
        {
            if (rb == null)
                return;

            if (HasDriver)
                DriveWithInput();
            else
            {
                StickToSurface();
                if (CanOccupyPose(rb.position, rb.rotation))
                {
                    lastValidPosition = rb.position;
                    lastValidRotation = rb.rotation;
                    hasLastValidPose = true;
                }
            }
        }

        void DriveWithInput()
        {
            if (PlayerController.IsGameplayBlocked())
                return;

            bool throttle = Input.GetKey(KeyCode.W);
            bool reverse = Input.GetKey(KeyCode.S);
            bool brake = Input.GetKey(KeyCode.Space);
            float steer = Input.GetAxisRaw("Horizontal");
            float speedDelta = SpeedChangeMphPerSecond * Time.fixedDeltaTime;

            if (brake)
                signedSpeedMph = Mathf.MoveTowards(signedSpeedMph, 0f, speedDelta);
            else if (throttle)
                signedSpeedMph = Mathf.MoveTowards(signedSpeedMph, MaxForwardSpeedMph, speedDelta);
            else if (reverse)
            {
                if (signedSpeedMph > 0f)
                    signedSpeedMph = Mathf.MoveTowards(signedSpeedMph, 0f, speedDelta);
                else
                    signedSpeedMph = Mathf.MoveTowards(signedSpeedMph, -MaxReverseSpeedMph, speedDelta);
            }

            Vector3 nextPosition = rb.position;
            Quaternion nextRotation = rb.rotation;

            if (Mathf.Abs(steer) > 0.01f)
                ApplyYawAroundHelm(ref nextPosition, ref nextRotation, steer * TurnSpeedDegreesPerSecond * Time.fixedDeltaTime);

            float speedUnits = WorldScale.MilesPerHour(signedSpeedMph);
            if (Mathf.Abs(speedUnits) > 0.0001f)
                nextPosition += GetBowForwardWorld(nextRotation) * speedUnits * Time.fixedDeltaTime;

            if (TryApplyPose(nextPosition, nextRotation))
            {
                lastValidPosition = nextPosition;
                lastValidRotation = nextRotation;
                hasLastValidPose = true;
            }
            else if (Mathf.Abs(signedSpeedMph) > 0.01f)
            {
                Vector3 forwardOnly = rb.position
                    + GetBowForwardWorld(rb.rotation) * speedUnits * Time.fixedDeltaTime;
                if (TryApplyPose(forwardOnly, rb.rotation))
                {
                    lastValidPosition = forwardOnly;
                    lastValidRotation = rb.rotation;
                    hasLastValidPose = true;
                }
                else
                {
                    float impactSpeedMph = Mathf.Abs(signedSpeedMph);
                    signedSpeedMph = 0f;
                    NotifySolidImpact(impactSpeedMph);
                }
            }
            else if (!TryApplyPose(nextPosition, nextRotation) && Mathf.Abs(steer) > 0.01f)
            {
                NotifySolidImpact(Mathf.Abs(signedSpeedMph));
            }

            StickToSurface();
            UpdateMeasuredDriveSpeed();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        void ApplyYawAroundHelm(ref Vector3 position, ref Quaternion rotation, float turnAmountDegrees)
        {
            if (helm == null || Mathf.Abs(turnAmountDegrees) <= 0.0001f)
                return;

            Vector3 pivotWorld = helm.position;
            Quaternion turn = Quaternion.Euler(0f, turnAmountDegrees, 0f);
            Vector3 offset = position - pivotWorld;
            offset.y = 0f;
            position = pivotWorld + turn * offset;
            rotation = turn * rotation;
        }

        void UpdateMeasuredDriveSpeed()
        {
            if (hasDriveSample && Time.fixedDeltaTime > 0.0001f)
            {
                Vector3 delta = rb.position - lastDriveSampleWorld;
                float forwardUnits = Vector3.Dot(delta, GetBowForwardWorld());
                float oneMph = WorldScale.MilesPerHour(1f);
                measuredDriveSpeedMph = oneMph > 0.0001f
                    ? Mathf.Abs(forwardUnits / oneMph / Time.fixedDeltaTime)
                    : 0f;
            }
            else
            {
                measuredDriveSpeedMph = 0f;
            }

            lastDriveSampleWorld = rb.position;
            hasDriveSample = true;
        }

        bool TryApplyPose(Vector3 worldPosition, Quaternion worldRotation)
        {
            if (!CanOccupyPose(worldPosition, worldRotation))
                return false;

            rb.position = worldPosition;
            rb.rotation = worldRotation;
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            Physics.SyncTransforms();
            return true;
        }

        void NotifySolidImpact(float impactSpeedMph)
        {
            if (impactSpeedMph <= SolidImpactShakeMinSpeedMph)
                return;

            if (Time.time - lastSolidImpactTime < SolidImpactCooldownSeconds)
                return;

            lastSolidImpactTime = Time.time;
            ShakeOccupants();
        }

        void ShakeOccupants()
        {
            ApplyShakeToMount(driver);
            ApplyShakeToMount(cargoOccupant);
        }

        static void ApplyShakeToMount(PlayerVehicleMount mount)
        {
            if (mount == null)
                return;

            mount.GetComponent<PlayerCameraShake>()?.BeginViolentShake(
                SolidImpactShakeDuration,
                SolidImpactShakeIntensity);
        }

        void StickToSurface()
        {
            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return;

            Vector3 local = bounds.transform.InverseTransformPoint(rb.position);
            float plainsBase = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(local.x, local.z, plainsBase);
            float waterLocalY = groundY + LakeCatalog.WaterSurfaceLocalYOffset + WorldScale.Feet(waterVerticalOffsetFeet);
            float waterWorldY = bounds.transform.TransformPoint(new Vector3(local.x, waterLocalY, local.z)).y;
            float groundWorldY = bounds.transform.TransformPoint(new Vector3(local.x, groundY, local.z)).y;

            Vector3 pos = rb.position;
            if (LakeCatalog.IsBoatNavigableLocal(local.x, local.z))
                pos.y = waterWorldY + bottomOffset;
            else
                pos.y = groundWorldY + bottomOffset;
            rb.position = pos;
            transform.position = pos;

            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
            rb.angularVelocity = Vector3.zero;
        }

        bool CanOccupyPose(Vector3 worldPosition, Quaternion worldRotation)
        {
            var cavernBounds = GameContext.Instance?.CavernBounds;
            if (cavernBounds == null)
                return false;

            foreach (Vector3 sampleLocal in GetHullSampleOffsetsLocal())
            {
                Vector3 sampleWorld = worldPosition + worldRotation * (hullSampleCenterLocal + sampleLocal);
                Vector3 contentLocal = cavernBounds.transform.InverseTransformPoint(sampleWorld);
                if (!LakeCatalog.IsBoatNavigableLocal(contentLocal.x, contentLocal.z))
                    return false;
            }

            return true;
        }

        Vector3[] GetHullSampleOffsetsLocal()
        {
            float length = hullHalfLengthLocal;
            float beam = hullHalfBeamLocal;
            return new[]
            {
                new Vector3(0f, 0f, 0f),
                bowLocalDirection * length,
                bowLocalDirection * length * 0.55f,
                -bowLocalDirection * length,
                -bowLocalDirection * length * 0.55f,
                beamLocalDirection * beam,
                -beamLocalDirection * beam,
                bowLocalDirection * length + beamLocalDirection * beam,
                bowLocalDirection * length - beamLocalDirection * beam,
                -bowLocalDirection * length + beamLocalDirection * beam,
                -bowLocalDirection * length - beamLocalDirection * beam,
            };
        }

        Vector3 GetBowForwardWorld()
        {
            return GetBowForwardWorld(transform.rotation);
        }

        Vector3 GetBowForwardWorld(Quaternion worldRotation)
        {
            Vector3 worldForward = worldRotation * bowLocalDirection;
            worldForward.y = 0f;
            return worldForward.sqrMagnitude > 0.0001f ? worldForward.normalized : worldForward;
        }

        void CacheCargoDeckTopLocalY()
        {
            if (deck == null)
                return;

            var bedCollider = deck.GetComponent<BoxCollider>();
            if (bedCollider == null)
                return;

            cargoDeckTopLocalY = bedCollider.center.y + bedCollider.size.y * 0.5f;
        }

        void CacheBottomOffset()
        {
            Physics.SyncTransforms();
            float bottomY = transform.position.y;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer == null)
                    continue;

                bottomY = Mathf.Min(bottomY, renderer.bounds.min.y);
            }

            bottomOffset = Mathf.Max(0.05f, transform.position.y - bottomY);
        }

        void CacheHullHalfExtentsLocal()
        {
            hullSampleCenterLocal = Vector3.zero;
            hullHalfLengthLocal = 1f;
            hullHalfBeamLocal = 0.5f;

            Renderer hullRenderer = FindHullWoodRenderer();
            if (hullRenderer != null)
            {
                Bounds hullBounds = hullRenderer.bounds;
                Vector3 centerLocal = transform.InverseTransformPoint(hullBounds.center);
                hullSampleCenterLocal = new Vector3(centerLocal.x, 0f, centerLocal.z);

                Vector3 extentsLocal = transform.InverseTransformVector(hullBounds.extents);
                bool bowAlongLocalX = Mathf.Abs(bowLocalDirection.x) > 0.5f;
                hullHalfLengthLocal = Mathf.Abs(bowAlongLocalX ? extentsLocal.x : extentsLocal.z);
                hullHalfBeamLocal = Mathf.Abs(bowAlongLocalX ? extentsLocal.z : extentsLocal.x);
                hullHalfExtentsLocal = new Vector3(hullHalfLengthLocal, Mathf.Abs(extentsLocal.y), hullHalfBeamLocal);
                return;
            }

            var box = GetComponent<BoxCollider>();
            if (box == null)
                return;

            hullSampleCenterLocal = new Vector3(box.center.x, 0f, box.center.z);
            Vector3 scaled = Vector3.Scale(box.size, transform.lossyScale);
            bool bowAlongX = Mathf.Abs(bowLocalDirection.x) > 0.5f;
            hullHalfLengthLocal = (bowAlongX ? scaled.x : scaled.z) * 0.28f;
            hullHalfBeamLocal = (bowAlongX ? scaled.z : scaled.x) * 0.28f;
            hullHalfExtentsLocal = new Vector3(hullHalfLengthLocal, scaled.y * 0.25f, hullHalfBeamLocal);
        }

        Renderer FindHullWoodRenderer()
        {
            Renderer best = null;
            int bestVertexCount = 0;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                string name = renderer.gameObject.name;
                if (name.IndexOf("Sail", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Oar", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("WalkDeck", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("BoatDeck", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                var meshFilter = renderer.GetComponent<MeshFilter>();
                int vertexCount = meshFilter != null && meshFilter.sharedMesh != null
                    ? meshFilter.sharedMesh.vertexCount
                    : 0;
                if (vertexCount > bestVertexCount)
                {
                    best = renderer;
                    bestVertexCount = vertexCount;
                }
            }

            return best;
        }

        bool SampleNearDismountShore()
        {
            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return false;

            Vector3 centerLocal = bounds.transform.InverseTransformPoint(transform.position);
            if (LakeCatalog.IsNearBoatDismountShoreLocal(
                    centerLocal.x,
                    centerLocal.z,
                    bounds.transform,
                    LakeCatalog.BoatDismountShoreProximityFeet))
                return true;

            foreach (Vector3 sampleLocal in GetHullSampleOffsetsLocal())
            {
                Vector3 sampleWorld = transform.TransformPoint(hullSampleCenterLocal + sampleLocal);
                Vector3 contentLocal = bounds.transform.InverseTransformPoint(sampleWorld);
                if (LakeCatalog.IsNearBoatDismountShoreLocal(
                        contentLocal.x,
                        contentLocal.z,
                        bounds.transform,
                        LakeCatalog.BoatDismountShoreProximityFeet))
                    return true;
            }

            return false;
        }

        public int FillStopSailingReferenceWorldPoints(Vector3[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
                return 0;

            int count = 0;
            buffer[count++] = transform.TransformPoint(hullSampleCenterLocal);
            foreach (Vector3 sampleLocal in GetHullSampleOffsetsLocal())
            {
                if (count >= buffer.Length)
                    break;

                buffer[count++] = transform.TransformPoint(hullSampleCenterLocal + sampleLocal);
            }

            return count;
        }

        public bool ContainsWorldPointInHullXZ(Vector3 worldPoint, float marginFeet = 2f)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            Vector3 fromCenter = local - hullSampleCenterLocal;
            float alongBow = Vector3.Dot(fromCenter, bowLocalDirection);
            float alongBeam = Vector3.Dot(fromCenter, beamLocalDirection);
            float margin = WorldScale.Feet(marginFeet);
            float scale = Mathf.Max(transform.lossyScale.x, 0.0001f);
            return Mathf.Abs(alongBow) <= hullHalfLengthLocal * scale + margin
                && Mathf.Abs(alongBeam) <= hullHalfBeamLocal * scale + margin;
        }

        public bool TryPushWorldPointClearOfHullXZ(ref Vector3 worldPoint, CavernBounds bounds, float maxExtraFeet = 48f)
        {
            if (!ContainsWorldPointInHullXZ(worldPoint))
                return true;
            if (bounds == null)
                return false;

            Vector3 pushDir = worldPoint - transform.position;
            pushDir.y = 0f;
            if (pushDir.sqrMagnitude < 0.0001f)
                pushDir = BowForwardWorld;
            pushDir.Normalize();

            for (float extra = WorldScale.Feet(2f); extra <= WorldScale.Feet(maxExtraFeet); extra += WorldScale.Feet(2f))
            {
                Vector3 candidate = worldPoint + pushDir * extra;
                Vector3 local = bounds.transform.InverseTransformPoint(candidate);
                if (!LakeCatalog.IsWalkableLandLocal(local.x, local.z, bounds.transform)
                    || LakeCatalog.IsOpenWaterLocal(local.x, local.z))
                    continue;

                if (!ContainsWorldPointInHullXZ(candidate))
                {
                    worldPoint = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
