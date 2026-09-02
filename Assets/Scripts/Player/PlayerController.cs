using MonsterMiner.Core;
using MonsterMiner.UI;
using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [DefaultExecutionOrder(100)]
    public class PlayerController : MonoBehaviour
    {
        public const float CharacterHeight = WorldScale.CharacterHeightUnits;

        [SerializeField] float moveSpeedMph = 11f;
        [SerializeField] float mouseSensitivity = 2f;
        [SerializeField] float landingBounce = 0.35f;
        [SerializeField] float groundCheckDistance = 0.2f;

        Rigidbody rb;
        Camera viewCamera;
        Transform head;
        CapsuleCollider bodyCollider;
        float pitch;
        bool groundedLastFrame;
        bool gameplayCursorLocked = true;
        bool jumpRequested;
        float plainsJumpVelocity;

        public Camera ViewCamera => viewCamera;
        public Transform Head => head;
        public bool IsGameplayCursorLocked => gameplayCursorLocked;
        public bool IsInPlainsJump => Mathf.Abs(plainsJumpVelocity) > 0.01f;

        float JumpVelocity => Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * CharacterHeight * 0.5f);

        public void Initialize(Vector3 spawnPoint)
        {
            rb = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<CapsuleCollider>();
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            transform.position = SnapToFloor(spawnPoint);
            transform.rotation = Quaternion.identity;
            pitch = 0f;

            head = new GameObject("Head").transform;
            head.SetParent(transform, false);
            head.localPosition = new Vector3(0f, 0.65f, 0f);
            head.localRotation = Quaternion.identity;

            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(head, false);
            camGo.transform.localRotation = Quaternion.identity;
            viewCamera = camGo.AddComponent<Camera>();
            viewCamera.nearClipPlane = 0.05f;
            viewCamera.farClipPlane = 6000f;
            RenderPipelineSetup.ConfigureCamera(viewCamera);
            var listener = camGo.AddComponent<AudioListener>();
            DisableExtraAudioListeners(listener);

            LockCursorToCenter();
        }

        static void DisableExtraAudioListeners(AudioListener keep)
        {
            foreach (var other in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (other != keep)
                    other.enabled = false;
            }
        }

        void Update()
        {
            HandleCursorToggle();

            if (!IsUiCursorMode()
                && gameplayCursorLocked
                && Cursor.lockState != CursorLockMode.Locked
                && Input.GetMouseButtonDown(0))
                LockCursorToCenter();

            ApplyCursorState();

            if (Input.GetKeyDown(KeyCode.Space) && !IsUiCursorMode() && !IsGameplayBlocked())
                jumpRequested = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.F10) && !IsUiCursorMode() && !IsGameplayBlocked())
                PlayerSpawnPersistence.SetSpawnToCurrentPlayerPosition();
#endif
        }

        void LateUpdate()
        {
            ApplyCursorState();
            HandleLook();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && gameplayCursorLocked && !IsUiCursorMode())
                LockCursorToCenter();
        }

        void HandleCursorToggle()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.Numlock))
                gameplayCursorLocked = !gameplayCursorLocked;
#else
            gameplayCursorLocked = true;
#endif
        }

        void ApplyCursorState()
        {
            if (IsUiCursorMode())
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (gameplayCursorLocked)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void LockCursorToCenter()
        {
            gameplayCursorLocked = true;
            ApplyCursorState();
        }

        static bool IsShopMenuOpen()
        {
            return GameContext.Instance?.Shop != null && GameContext.Instance.Shop.IsMenuOpen;
        }

        static bool IsUiCursorMode()
        {
            return IsShopMenuOpen()
                || SellConfirmationDisplay.IsActive
                || MinerTurnInPopupDisplay.IsActive
                || DeathScreenDisplay.IsActive;
        }

        public static bool IsGameplayBlocked()
        {
            var ctx = GameContext.Instance;
            return ctx != null
                && (ctx.IsPlayerDead
                    || DeathScreenDisplay.IsActive
                    || MinerTurnInPopupDisplay.IsActive);
        }

        void FixedUpdate()
        {
            if (IsGameplayBlocked() || IsUiCursorMode())
            {
                jumpRequested = false;
                return;
            }

            if (GetComponent<PlayerWingsFlight>()?.IsFlying == true)
            {
                jumpRequested = false;
                return;
            }

            if (GetComponent<PlayerVehicleMount>()?.IsMounted == true)
            {
                jumpRequested = false;
                return;
            }

            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            var move = transform.TransformDirection(input) * WorldScale.MilesPerHour(moveSpeedMph);
            var cavernBounds = GameContext.Instance?.CavernBounds;
            bool onPlains = cavernBounds != null && PlainsGroundSupport.IsOnPlains(cavernBounds, transform.position);

            if (onPlains)
            {
                rb.useGravity = false;

                if (jumpRequested && IsGrounded())
                {
                    plainsJumpVelocity = JumpVelocity;
                    jumpRequested = false;
                }

                plainsJumpVelocity += Physics.gravity.y * Time.fixedDeltaTime;
                if (plainsJumpVelocity < 0f && IsGrounded())
                    plainsJumpVelocity = 0f;

                Vector3 step = new Vector3(move.x, plainsJumpVelocity, move.z) * Time.fixedDeltaTime;
                if (step.sqrMagnitude > 0.0001f)
                    TryMoveOnPlains(step);

                rb.linearVelocity = Vector3.zero;
                groundedLastFrame = IsGrounded();
                return;
            }

            plainsJumpVelocity = 0f;
            rb.useGravity = true;
            var velocity = rb.linearVelocity;
            velocity.x = move.x;
            velocity.z = move.z;

            if (jumpRequested && IsGrounded())
            {
                if (input.sqrMagnitude < 0.01f)
                {
                    velocity.x = 0f;
                    velocity.z = 0f;
                }

                velocity.y = JumpVelocity;
                jumpRequested = false;
            }

            rb.linearVelocity = velocity;

            bool grounded = IsGrounded();
            if (grounded && !groundedLastFrame && rb.linearVelocity.y < -2f)
                rb.AddForce(Vector3.up * landingBounce, ForceMode.VelocityChange);
            groundedLastFrame = grounded;
        }

        void HandleLook()
        {
            if (head == null || IsUiCursorMode() || !gameplayCursorLocked || Cursor.lockState != CursorLockMode.Locked)
                return;

            var mount = GetComponent<PlayerVehicleMount>();
            if (mount != null && mount.IsDrivingBoat)
            {
                pitch = 0f;
                head.localRotation = Quaternion.identity;
                return;
            }

            if (mount != null && mount.IsDriving && mount.CurrentTruck != null)
            {
                head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                return;
            }

            if (mount != null && (mount.IsInCargo || mount.IsInBoatCargo))
            {
                float cargoYaw = Input.GetAxis("Mouse X") * mouseSensitivity;
                pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                pitch = Mathf.Clamp(pitch, -85f, 85f);
                transform.Rotate(0f, cargoYaw, 0f, Space.Self);
                head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                return;
            }

            float freeYaw = Input.GetAxis("Mouse X") * mouseSensitivity;
            if (GrenadeThrowController.UsesMouseYForRange)
            {
                transform.Rotate(0f, freeYaw, 0f);
                return;
            }

            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -85f, 85f);
            transform.Rotate(0f, freeYaw, 0f);
            head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        bool IsGrounded()
        {
            float probeDistance = groundCheckDistance + 0.2f;
            float bottomOffset = CharacterHeight * 0.5f;
            if (bodyCollider != null)
                bottomOffset = (bodyCollider.height * 0.5f - bodyCollider.center.y) * transform.lossyScale.y;

            var origin = transform.position - Vector3.up * bottomOffset + Vector3.up * 0.08f;
            if (RayHitsGround(origin, Vector3.down, probeDistance)
                || SphereHitsGround(origin, 0.18f, Vector3.down, probeDistance))
                return true;

            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return false;

            Vector3 local = bounds.transform.InverseTransformPoint(transform.position);
            if (!PlainsGroundSupport.ShouldSupportAt(local, bounds.Radius))
                return false;

            float groundY = PlainsGroundSupport.SampleSupportGroundWorldY(bounds, local.x, local.z);
            float feetY = transform.position.y - bottomOffset;
            return feetY <= groundY + bounds.SpawnRestHeight + 0.2f;
        }

        bool RayHitsGround(Vector3 origin, Vector3 direction, float distance)
        {
            var hits = Physics.RaycastAll(origin, direction, distance, ~0, QueryTriggerInteraction.Ignore);
            return HasExternalGroundHit(hits);
        }

        bool SphereHitsGround(Vector3 origin, float radius, Vector3 direction, float distance)
        {
            var hits = Physics.SphereCastAll(origin, radius, direction, distance, ~0, QueryTriggerInteraction.Ignore);
            return HasExternalGroundHit(hits);
        }

        bool HasExternalGroundHit(RaycastHit[] hits)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i].collider;
                if (col == null)
                    continue;
                if (col.transform == transform || col.transform.IsChildOf(transform))
                    continue;
                return true;
            }

            return false;
        }

        public void ResetViewPitch(float newPitch = 0f)
        {
            pitch = newPitch;
            if (head != null)
                head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public void ApplyViewRecoil(float degrees)
        {
            if (degrees <= 0f || head == null)
                return;

            pitch -= degrees;
            pitch = Mathf.Clamp(pitch, -85f, 85f);
            head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public void Respawn(Vector3 spawnPoint)
        {
            GetComponent<PlayerVehicleMount>()?.ForceDismount();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = SnapToFloor(spawnPoint);
            transform.rotation = Quaternion.identity;
            pitch = 0f;
            if (head != null)
                head.localRotation = Quaternion.identity;
            LockCursorToCenter();
        }

        public Vector3 SnapToFloorWorld(Vector3 worldPos) => SnapToFloor(worldPos);

        public void ResetPlainsMovementState()
        {
            plainsJumpVelocity = 0f;
            jumpRequested = false;
        }

        public float GetCapsuleBottomOffset()
        {
            float bottomOffset = CharacterHeight * 0.5f;
            if (bodyCollider != null)
                bottomOffset = (bodyCollider.height * 0.5f - bodyCollider.center.y) * Mathf.Abs(transform.lossyScale.y);
            return bottomOffset;
        }

        Vector3 SnapToFloor(Vector3 worldPos)
        {
            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return worldPos;

            var local = bounds.transform.InverseTransformPoint(worldPos);
            float halfHeight = GetCapsuleBottomOffset();

            if (bounds.TryResolveFloorWorldPoint(local.x, local.z, out Vector3 floorPoint))
            {
                worldPos.x = floorPoint.x;
                worldPos.z = floorPoint.z;
                worldPos.y = floorPoint.y + halfHeight + bounds.SpawnRestHeight;
                return worldPos;
            }

            if (FloorColliderUtility.TryResolveWalkFloorPoint(
                    worldPos,
                    WorldScale.Feet(250f),
                    WorldScale.Feet(600f),
                    bounds,
                    out Vector3 rayFloorPoint))
            {
                worldPos.y = rayFloorPoint.y + halfHeight + bounds.SpawnRestHeight;
                return worldPos;
            }

            float floorY = PlainsGroundSupport.SampleSupportGroundWorldY(bounds, local.x, local.z);
            worldPos.y = floorY + halfHeight + bounds.SpawnRestHeight;
            return worldPos;
        }

        void TryMoveOnPlains(Vector3 step)
        {
            Vector3 target = rb.position + step;
            if (bodyCollider == null)
            {
                rb.MovePosition(target);
                return;
            }

            float scaleY = Mathf.Abs(transform.lossyScale.y);
            float radius = bodyCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
            float height = bodyCollider.height * scaleY;
            float half = height * 0.5f;
            Vector3 up = transform.up;
            Vector3 point1 = rb.position + up * (half - radius);
            Vector3 point2 = rb.position - up * (half - radius);

            if (Physics.CapsuleCast(
                    point1,
                    point2,
                    radius,
                    step.normalized,
                    out var hit,
                    step.magnitude,
                    ~0,
                    QueryTriggerInteraction.Ignore)
                && !hit.collider.isTrigger)
            {
                target = rb.position + step.normalized * Mathf.Max(0f, hit.distance - 0.02f);
            }

            rb.MovePosition(target);
        }
    }
}
