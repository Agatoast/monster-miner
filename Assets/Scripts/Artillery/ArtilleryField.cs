using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.Artillery
{
    public class ArtilleryField : MonoBehaviour
    {
        public const string BackdropResourcePath = "Textures/Artillery/artillery_field";
        public const string SkyResourcePath = "Textures/Artillery/artillery_sky";
        public static readonly Vector3 Origin = new Vector3(0f, -4000f, 0f);

        static readonly Color SkyFallbackColor = new Color(0.82f, 0.68f, 0.38f);
        const float SkyDepth = 2f;
        const float PlainsDepth = 1f;
        const float MountainsDepth = 0f;
        const float BuildingsDepth = -0.1f;
        const float SoldiersDepth = -0.15f;
        const float CavalryDepth = -0.16f;
        const float CatapultDepth = -0.14f;
        const float WindFlagDepth = -0.17f;
        const float MountainScale = 1.5f;
        const float FieldAssetScale = 1.5f;
        const int LeftSideShiftPixels = -50;
        const int RightSideShiftPixels = 50;
        const int CavalryInwardShiftPixels = 50;
        const int CatapultSheetColumns = 8;
        const int CatapultSheetRows = 2;
        const float CatapultSizeScale = 2.2f;
        const int CatapultVerticalOffsetPixels = -62;
        static readonly Color CatapultWoodBright = new Color(0.890196f, 0.811765f, 0.690196f);
        const int SoldierSheetFrameCount = 4;
        const int CavalrySheetColumns = 6;
        const int CavalrySheetRows = 4;
        const int CavalryUnitCount = 2;
        const float CavalrySizeScale = 1.5f;
        const int SoldierSquadCount = 3;
        const float SoldierSizeScale = 0.5f;
        const int SoldierVerticalOffsetPixels = -17;

        Camera viewCamera;
        Texture2D backdropPixels;
        MeshRenderer backdropRenderer;
        AudioListener listener;
        ArtilleryCatapult leftCatapult;
        ArtilleryCatapult rightCatapult;
        ArtilleryWindFlag windFlag;
        float layoutScreenWidth;
        float cavalryUnitWidth;
        readonly List<ArtilleryHitTarget> hitTargets = new List<ArtilleryHitTarget>();
        readonly List<ArtilleryCavalryUnit> cavalryUnits = new List<ArtilleryCavalryUnit>();
        readonly ArtilleryHitTarget[] leftInfantryBySlot = new ArtilleryHitTarget[3];
        readonly ArtilleryHitTarget[] rightInfantryBySlot = new ArtilleryHitTarget[3];
        readonly Vector2[] leftInfantrySlotPositions = new Vector2[3];
        readonly Vector2[] rightInfantrySlotPositions = new Vector2[3];
        ArtilleryHitTarget leftFortress;
        ArtilleryHitTarget rightFortress;
        ArtilleryHitTarget leftPalace;
        ArtilleryHitTarget rightPalace;

        public Camera ViewCamera => viewCamera;
        public ArtilleryCatapult LeftCatapult => leftCatapult;
        public ArtilleryCatapult RightCatapult => rightCatapult;

        public ArtilleryCatapult GetCatapult(ArtillerySide side)
        {
            return side == ArtillerySide.Left ? leftCatapult : rightCatapult;
        }

        public static ArtilleryField Build()
        {
            var existing = FindFirstObjectByType<ArtilleryField>();
            if (existing != null)
                Destroy(existing.gameObject);

            var root = new GameObject("ArtilleryField");
            root.transform.position = Origin;
            var field = root.AddComponent<ArtilleryField>();
            field.BuildContents();
            return field;
        }

        public void TearDown()
        {
            if (gameObject != null)
                Destroy(gameObject);
        }

        public void Carve(Vector3 worldPoint, float radius)
        {
            if (backdropPixels == null)
                return;

            float localX = worldPoint.x - Origin.x;
            float localY = worldPoint.y - Origin.y;
            int px = Mathf.FloorToInt(localX / ArtilleryFieldProfile.Pixel);
            int py = Mathf.FloorToInt(localY / ArtilleryFieldProfile.Pixel);
            int r = Mathf.CeilToInt(radius / ArtilleryFieldProfile.Pixel);
            int w = backdropPixels.width;
            int h = backdropPixels.height;
            bool changed = false;

            for (int dy = -r; dy <= r; dy++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dy * dy > r * r)
                        continue;

                    int x = px + dx;
                    int y = py + dy;
                    if (x < 0 || x >= w || y < 0 || y >= h)
                        continue;

                    backdropPixels.SetPixel(x, y, Color.clear);
                    changed = true;
                }
            }

            if (changed)
                backdropPixels.Apply();
        }

        void BuildContents()
        {
            CreateCamera();
            GetScreenSize(out float screenWidth, out float screenHeight);
            layoutScreenWidth = screenWidth;
            CreateSky(screenWidth, screenHeight);
            CreatePlainsDistance(screenWidth);
            CreateBackdrop(screenWidth, screenHeight);
            CreateBuildings(screenWidth, screenHeight);
            CreateSoldierSquads(screenWidth, screenHeight);
            CreateCavalry(screenWidth, screenHeight);
            CreateCatapults(screenWidth, screenHeight);
            CreateWindFlag(screenWidth, screenHeight);
            float xScale = screenWidth / ArtilleryFieldProfile.DesignWidth;
            float yScale = screenHeight / ArtilleryFieldProfile.DesignHeight;
            CreateBuildingPads(ArtilleryFieldProfile.LeftPads, xScale, yScale);
            CreateBuildingPads(ArtilleryFieldProfile.RightPads, xScale, yScale);
            RefreshCatapultHitboxes();
            ApplyForcesTestSetup();
        }

        void ApplyForcesTestSetup()
        {
            if (ArtilleryFieldProfile.SpawnBlueForcesWithOnlyCatapultForTesting)
                StripSideToCatapultOnly(ArtillerySide.Left);
        }

        void StripSideToCatapultOnly(ArtillerySide side)
        {
            for (int i = 0; i < cavalryUnits.Count; i++)
            {
                var unit = cavalryUnits[i];
                if (unit?.HitTarget?.Side == side && unit.IsActive)
                    unit.DestroyUnit();
            }

            for (int i = 0; i < hitTargets.Count; i++)
            {
                var target = hitTargets[i];
                if (target.Side != side || target.Kind == ArtilleryTargetKind.Catapult || target.IsDestroyed)
                    continue;

                target.DestroyTarget();
            }

            RefreshCatapultHitboxes();
        }

        bool IsRightSideColumn(int column)
        {
            return column >= ArtilleryFieldProfile.RightStartColumn;
        }

        float MapImageXToWorld(int column, float xScale)
        {
            float pixelScale = ArtilleryFieldProfile.Pixel * xScale * MountainScale;
            bool rightSide = IsRightSideColumn(column);
            float sideShift = SideShiftX(rightSide, xScale);
            if (rightSide)
                return layoutScreenWidth - (ArtilleryFieldProfile.ImageWidth - column) * pixelScale + sideShift;

            return column * pixelScale + sideShift;
        }

        float SideShiftX(bool rightSide, float xScale)
        {
            int pixels = rightSide ? RightSideShiftPixels : LeftSideShiftPixels;
            return pixels * ArtilleryFieldProfile.Pixel * xScale * MountainScale;
        }

        float MapImageYToWorld(int row, float yScale)
        {
            return row * ArtilleryFieldProfile.Pixel * yScale * MountainScale;
        }

        float ScaledOffsetX(int pixels, float xScale)
        {
            return pixels * ArtilleryFieldProfile.Pixel * xScale * MountainScale;
        }

        float ScaledOffsetY(int pixels, float yScale)
        {
            return pixels * ArtilleryFieldProfile.Pixel * yScale * MountainScale;
        }

        float ComputeMiddleInfantryCenterX(
            float xScale,
            int mountainColumnCount,
            float soldierUnitWidth,
            int mountainStartColumn = 0)
        {
            float mountainWidth = mountainColumnCount * ArtilleryFieldProfile.Pixel * xScale * MountainScale;
            float rowStartX = MapImageXToWorld(mountainStartColumn, xScale);
            float totalWidth = soldierUnitWidth * SoldierSquadCount;
            return rowStartX + (mountainWidth - totalWidth) * 0.5f + soldierUnitWidth * 1.5f;
        }

        public void BindWindFlag(ArtilleryBattleController battle)
        {
            windFlag?.Bind(battle);
        }

        public bool TryGetWindLabelWorldPosition(out Vector3 worldPosition)
        {
            if (windFlag == null)
            {
                worldPosition = Vector3.zero;
                return false;
            }

            worldPosition = transform.TransformPoint(new Vector3(
                windFlag.WindLabelCenterX,
                windFlag.WindLabelCenterY,
                windFlag.WindLabelDepth));
            return true;
        }

        public bool TryGetYourTurnLabelWorldPosition(out Vector3 worldPosition)
        {
            if (windFlag == null)
            {
                worldPosition = Vector3.zero;
                return false;
            }

            worldPosition = transform.TransformPoint(new Vector3(
                windFlag.WindLabelCenterX,
                windFlag.YourTurnLabelCenterY,
                windFlag.WindLabelDepth));
            return true;
        }

        void CreateWindFlag(float screenWidth, float screenHeight)
        {
            float xScale = screenWidth / ArtilleryFieldProfile.DesignWidth;
            float yScale = screenHeight / ArtilleryFieldProfile.DesignHeight;

            var go = new GameObject("WindFlag");
            go.transform.SetParent(transform, false);
            windFlag = go.AddComponent<ArtilleryWindFlag>();
            windFlag.Build(screenWidth, xScale, yScale, WindFlagDepth);
        }

        public void GetScreenSize(out float width, out float height)
        {
            height = ArtilleryFieldProfile.DesignHeight;
            width = height * viewCamera.aspect;
        }

        public int HitTargetCount => hitTargets.Count;

        public ArtilleryHitTarget GetHitTarget(int index) => hitTargets[index];

        public Vector3 GetTargetWorldPoint(ArtilleryHitTarget target, float localX, float localY)
        {
            return transform.TransformPoint(new Vector3(localX, localY, target.Depth));
        }

        public bool IsSideDefeated(ArtillerySide side)
        {
            bool foundAny = false;
            for (int i = 0; i < hitTargets.Count; i++)
            {
                var target = hitTargets[i];
                if (target.Side != side)
                    continue;

                foundAny = true;
                if (!target.IsDestroyed)
                    return false;
            }

            return foundAny;
        }

        public ArtilleryHitTarget SelectFocusTarget(ArtillerySide defenderSide, ArtilleryHitTarget currentFocus)
        {
            if (currentFocus != null
                && !currentFocus.IsDestroyed
                && currentFocus.Side == defenderSide
                && currentFocus.Kind != ArtilleryTargetKind.Cavalry
                && !(currentFocus.Kind == ArtilleryTargetKind.Catapult && IsCatapultProtected(defenderSide)))
            {
                return currentFocus;
            }

            ArtilleryHitTarget bestTarget = null;
            int bestPriority = int.MaxValue;
            float bestCenterX = defenderSide == ArtillerySide.Left ? float.MaxValue : float.NegativeInfinity;

            for (int i = 0; i < hitTargets.Count; i++)
            {
                var target = hitTargets[i];
                if (target.Side != defenderSide || target.IsDestroyed)
                    continue;

                if (target.Kind == ArtilleryTargetKind.Cavalry)
                    continue;

                if (target.Kind == ArtilleryTargetKind.Catapult && IsCatapultProtected(defenderSide))
                    continue;

                int priority = GetTargetPriority(target.Kind);
                bool isBetter = priority < bestPriority;
                if (priority == bestPriority)
                {
                    isBetter = defenderSide == ArtillerySide.Left
                        ? target.CenterX < bestCenterX
                        : target.CenterX > bestCenterX;
                }

                if (!isBetter)
                    continue;

                bestPriority = priority;
                bestCenterX = target.CenterX;
                bestTarget = target;
            }

            return bestTarget;
        }

        static int GetTargetPriority(ArtilleryTargetKind kind)
        {
            switch (kind)
            {
                case ArtilleryTargetKind.Infantry:
                    return 0;
                case ArtilleryTargetKind.Palace:
                    return 1;
                case ArtilleryTargetKind.Fortress:
                    return 2;
                case ArtilleryTargetKind.Catapult:
                    return 3;
                default:
                    return 5;
            }
        }

        public bool TryGetTargetAimPoint(ArtilleryHitTarget target, out Vector2 aimPoint)
        {
            aimPoint = Vector2.zero;
            if (target == null || target.IsDestroyed)
                return false;

            aimPoint = new Vector2(target.CenterX, target.CenterY);
            return true;
        }

        public ArtilleryCavalryTurnResult ProcessEndOfTurnCavalry(ArtillerySide movingSide, ArtillerySide playerSide)
        {
            var result = ArtilleryCavalryTurnResult.Continue;

            for (int i = 0; i < cavalryUnits.Count; i++)
            {
                var unit = cavalryUnits[i];
                if (!unit.IsActive || unit.HitTarget.Side != movingSide)
                    continue;

                if (unit.AdvanceMode == ArtilleryCavalryAdvanceMode.PendingInfiltration)
                    ProcessCavalryInfiltration(unit);
            }

            for (int i = 0; i < cavalryUnits.Count; i++)
            {
                var unit = cavalryUnits[i];
                if (!unit.IsActive
                    || unit.HitTarget.Side != movingSide
                    || unit.AdvanceMode != ArtilleryCavalryAdvanceMode.Siege)
                    continue;

                var siegeResult = ProcessCavalrySiege(unit, playerSide);
                if (siegeResult != ArtilleryCavalryTurnResult.Continue)
                    result = siegeResult;
            }

            var advancing = new List<ArtilleryCavalryUnit>();
            for (int i = 0; i < cavalryUnits.Count; i++)
            {
                var unit = cavalryUnits[i];
                if (!unit.IsActive
                    || unit.HitTarget.Side != movingSide
                    || unit.AdvanceMode != ArtilleryCavalryAdvanceMode.Advancing)
                    continue;

                advancing.Add(unit);
            }

            advancing.Sort(CompareAdvancingCavalry);
            for (int i = 0; i < advancing.Count; i++)
                ProcessCavalryAdvance(advancing[i]);

            RefreshCatapultHitboxes();
            return result;
        }

        static int CompareAdvancingCavalry(ArtilleryCavalryUnit a, ArtilleryCavalryUnit b)
        {
            float ax = a.HitTarget.CenterX;
            float bx = b.HitTarget.CenterX;
            if (a.HitTarget.Side == ArtillerySide.Left)
                return bx.CompareTo(ax);

            return ax.CompareTo(bx);
        }

        void ProcessCavalryAdvance(ArtilleryCavalryUnit unit)
        {
            var target = unit.HitTarget;
            float step = cavalryUnitWidth;
            float direction = target.Side == ArtillerySide.Left ? step : -step;
            float newX = target.CenterX + direction;
            var enemySide = OppositeSide(target.Side);

            if (TryResolveCavalryCombat(unit, newX, enemySide))
                return;

            if (HasReachedOppositeEdge(newX, target.Side))
            {
                float edgeX = ClampToOppositeEdge(newX, target.Side);
                unit.MoveTo(edgeX, target.CenterY);
                unit.SetAdvanceMode(ArtilleryCavalryAdvanceMode.PendingInfiltration);
                return;
            }

            unit.MoveTo(newX, target.CenterY);
        }

        bool TryResolveCavalryCombat(ArtilleryCavalryUnit unit, float newX, ArtillerySide enemySide)
        {
            var target = unit.HitTarget;
            ArtilleryCavalryUnit enemyCavalry = null;
            ArtilleryHitTarget enemyInfantry = null;

            for (int i = 0; i < cavalryUnits.Count; i++)
            {
                var other = cavalryUnits[i];
                if (!other.IsActive || other.HitTarget.Side != enemySide)
                    continue;

                if (other.HitTarget.OverlapsHorizontally(newX, target.Width))
                {
                    enemyCavalry = other;
                    break;
                }
            }

            if (enemyCavalry != null)
            {
                unit.DestroyUnit();
                enemyCavalry.DestroyUnit();
                return true;
            }

            for (int i = 0; i < hitTargets.Count; i++)
            {
                var other = hitTargets[i];
                if (other.IsDestroyed || other.Side != enemySide || other.Kind != ArtilleryTargetKind.Infantry)
                    continue;

                if (!other.OverlapsHorizontally(newX, target.Width))
                    continue;

                enemyInfantry = other;
                break;
            }

            if (enemyInfantry != null)
            {
                enemyInfantry.DestroyTarget();
                if (!HasReachedOppositeEdge(newX, target.Side))
                {
                    unit.MoveTo(newX, target.CenterY);
                    return true;
                }

                float edgeX = ClampToOppositeEdge(newX, target.Side);
                unit.MoveTo(edgeX, target.CenterY);
                unit.SetAdvanceMode(ArtilleryCavalryAdvanceMode.PendingInfiltration);
                return true;
            }

            return false;
        }

        bool HasReachedOppositeEdge(float centerX, ArtillerySide side)
        {
            float halfWidth = cavalryUnitWidth * 0.5f;
            if (side == ArtillerySide.Left)
                return centerX + halfWidth >= layoutScreenWidth;

            return centerX - halfWidth <= 0f;
        }

        float ClampToOppositeEdge(float centerX, ArtillerySide side)
        {
            float halfWidth = cavalryUnitWidth * 0.5f;
            if (side == ArtillerySide.Left)
                return layoutScreenWidth - halfWidth;

            return halfWidth;
        }

        void ProcessCavalryInfiltration(ArtilleryCavalryUnit unit)
        {
            var attackerSide = unit.HitTarget.Side;
            var defenderSide = OppositeSide(attackerSide);
            int[] slotOrder = GetInfiltrationSlotOrder(defenderSide);
            var infantryBySlot = defenderSide == ArtillerySide.Left ? leftInfantryBySlot : rightInfantryBySlot;
            var slotPositions = defenderSide == ArtillerySide.Left ? leftInfantrySlotPositions : rightInfantrySlotPositions;

            Vector2 spawnPosition = slotPositions[slotOrder[0]];
            for (int i = 0; i < slotOrder.Length; i++)
            {
                int slot = slotOrder[i];
                spawnPosition = slotPositions[slot];
                var infantry = infantryBySlot[slot];
                if (infantry == null || infantry.IsDestroyed)
                    continue;

                infantry.DestroyTarget();
                break;
            }

            unit.MoveTo(spawnPosition.x, spawnPosition.y);
            unit.SetAdvanceMode(ArtilleryCavalryAdvanceMode.Siege);
        }

        ArtilleryCavalryTurnResult ProcessCavalrySiege(ArtilleryCavalryUnit unit, ArtillerySide playerSide)
        {
            var defenderSide = OppositeSide(unit.HitTarget.Side);

            while (unit.SiegeTargetIndex <= 2)
            {
                var siegeTarget = GetSiegeTarget(defenderSide, unit.SiegeTargetIndex);
                if (siegeTarget == null || siegeTarget.IsDestroyed)
                {
                    unit.AdvanceSiegeTargetIndex();
                    continue;
                }

                bool destroyed = siegeTarget.ApplyHit();
                if (siegeTarget.Kind == ArtilleryTargetKind.Catapult && destroyed)
                {
                    DisableCatapult(defenderSide);
                    return defenderSide == playerSide
                        ? ArtilleryCavalryTurnResult.PlayerDefeat
                        : ArtilleryCavalryTurnResult.PlayerVictory;
                }

                if (destroyed)
                    unit.AdvanceSiegeTargetIndex();

                break;
            }

            return ArtilleryCavalryTurnResult.Continue;
        }

        static int[] GetInfiltrationSlotOrder(ArtillerySide defenderSide)
        {
            return defenderSide == ArtillerySide.Right
                ? new[] { 2, 1, 0 }
                : new[] { 0, 1, 2 };
        }

        ArtilleryHitTarget GetSiegeTarget(ArtillerySide defenderSide, int siegeIndex)
        {
            switch (siegeIndex)
            {
                case 0:
                    return defenderSide == ArtillerySide.Left ? leftFortress : rightFortress;
                case 1:
                    return defenderSide == ArtillerySide.Left ? leftPalace : rightPalace;
                case 2:
                    return FindCatapultTarget(defenderSide);
                default:
                    return null;
            }
        }

        ArtilleryHitTarget FindCatapultTarget(ArtillerySide side)
        {
            for (int i = 0; i < hitTargets.Count; i++)
            {
                var target = hitTargets[i];
                if (target.Side == side && target.Kind == ArtilleryTargetKind.Catapult)
                    return target;
            }

            return null;
        }

        static ArtillerySide OppositeSide(ArtillerySide side)
        {
            return side == ArtillerySide.Left ? ArtillerySide.Right : ArtillerySide.Left;
        }

        public ArtilleryProjectileHitResult ResolveProjectileHit(
            ArtillerySide shooterSide,
            float localX,
            float localY,
            float previousLocalX,
            float previousLocalY)
        {
            ArtilleryHitTarget firstTarget = null;
            float firstEntryT = float.MaxValue;

            for (int i = 0; i < hitTargets.Count; i++)
            {
                var target = hitTargets[i];
                if (target.IsDestroyed
                    || target.Side == shooterSide
                    || !target.IsProjectileHittable
                    || !target.TryGetSegmentEntryT(previousLocalX, previousLocalY, localX, localY, out float entryT))
                    continue;

                if (entryT < firstEntryT)
                {
                    firstEntryT = entryT;
                    firstTarget = target;
                }
            }
            if (firstTarget == null)
                return ArtilleryProjectileHitResult.Miss;

            var impact = new ArtilleryProjectileHitResult
            {
                ImpactCenterX = firstTarget.CenterX,
                ImpactBottomY = firstTarget.BottomY,
                TargetWidth = firstTarget.Width,
                Depth = firstTarget.Depth
            };

            bool destroyed = firstTarget.ApplyHit();
            if (destroyed && firstTarget.Kind == ArtilleryTargetKind.Catapult)
                DisableCatapult(firstTarget.Side);

            RefreshCatapultHitboxes();

            impact.Kind = destroyed
                ? ArtilleryProjectileHitKind.UnitDestroyed
                : ArtilleryProjectileHitKind.TargetHit;
            return impact;
        }

        public void RefreshCatapultHitboxes()
        {
            RefreshCatapultHitbox(ArtillerySide.Left);
            RefreshCatapultHitbox(ArtillerySide.Right);
        }

        void RefreshCatapultHitbox(ArtillerySide side)
        {
            var catapultTarget = FindCatapultTarget(side);
            if (catapultTarget == null)
                return;

            catapultTarget.SetProjectileHittable(!IsCatapultProtected(side));
        }

        public void PlayRockImpact(in ArtilleryProjectileHitResult hit)
        {
            if (!hit.StruckTarget)
                return;

            ArtilleryRockImpactEffect.PlayAt(
                transform,
                hit.ImpactCenterX,
                hit.ImpactBottomY,
                hit.TargetWidth,
                hit.Depth);
        }

        public bool IsCatapultProtected(ArtillerySide side)
        {
            for (int i = 0; i < hitTargets.Count; i++)
            {
                var target = hitTargets[i];
                if (target.Side != side || target.IsDestroyed || target.Kind == ArtilleryTargetKind.Catapult)
                    continue;

                return true;
            }

            return false;
        }

        void DisableCatapult(ArtillerySide side)
        {
            var catapult = GetCatapult(side);
            if (catapult == null)
                return;

            if (catapult.Animator != null)
                catapult.Animator.ResetToIdle();

            catapult.gameObject.SetActive(false);
        }

        void RegisterHitTarget(
            GameObject quad,
            ArtillerySide side,
            ArtilleryTargetKind kind,
            float centerX,
            float centerY,
            float width,
            float height,
            float depth,
            int infantrySlot = -1)
        {
            if (quad == null)
                return;

            var target = quad.GetComponent<ArtilleryHitTarget>();
            if (target == null)
                target = quad.AddComponent<ArtilleryHitTarget>();

            target.Configure(side, kind, centerX, centerY, width, height, depth, infantrySlot);
            hitTargets.Add(target);
            TrackRegisteredTarget(target);
        }

        void TrackRegisteredTarget(ArtilleryHitTarget target)
        {
            if (target.Kind == ArtilleryTargetKind.Infantry && target.InfantrySlot >= 0)
            {
                var slots = target.Side == ArtillerySide.Left ? leftInfantryBySlot : rightInfantryBySlot;
                var positions = target.Side == ArtillerySide.Left ? leftInfantrySlotPositions : rightInfantrySlotPositions;
                int slot = target.InfantrySlot;
                if (slot >= 0 && slot < slots.Length)
                {
                    slots[slot] = target;
                    positions[slot] = new Vector2(target.CenterX, target.CenterY);
                }
            }

            switch (target.Kind)
            {
                case ArtilleryTargetKind.Fortress:
                    if (target.Side == ArtillerySide.Left)
                        leftFortress = target;
                    else
                        rightFortress = target;
                    break;
                case ArtilleryTargetKind.Palace:
                    if (target.Side == ArtillerySide.Left)
                        leftPalace = target;
                    else
                        rightPalace = target;
                    break;
            }
        }

        void CreateSky(float screenWidth, float screenHeight)
        {
            var skyMaterial = CreateSkyMaterial();
            CreateScreenQuad(
                "ArtillerySky",
                screenWidth,
                screenHeight,
                screenWidth * 0.5f,
                screenHeight * 0.5f,
                SkyDepth,
                skyMaterial);
        }

        Material CreateSkyMaterial()
        {
            var texture = Resources.Load<Texture2D>(SkyResourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"Monster Miner: sky texture not found at Resources/{SkyResourcePath}.");
                return CreateOpaqueUnlitMaterial(BuildSolidColorTexture(SkyFallbackColor));
            }

            return CreateOpaqueUnlitMaterial(texture);
        }

        void CreatePlainsDistance(float screenWidth)
        {
            float height = ArtilleryFieldProfile.GroundBandHeight;
            CreateScreenQuad(
                "PlainsDistance",
                screenWidth,
                height,
                screenWidth * 0.5f,
                height * 0.5f,
                PlainsDepth,
                CreateOpaqueUnlitMaterial(BuildPlainsDistanceTexture()));
        }

        GameObject CreateScreenQuad(
            string name,
            float width,
            float height,
            float centerX,
            float centerY,
            float depth,
            Material material,
            Transform parent = null)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent != null ? parent : transform, false);
            quad.transform.localPosition = new Vector3(centerX, centerY, depth);
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(width, height, 1f);

            Object.Destroy(quad.GetComponent<MeshCollider>());
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return quad;
        }

        static Texture2D BuildSolidColorTexture(Color color)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static Texture2D BuildPlainsDistanceTexture()
        {
            const int width = 256;
            const int height = 256;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var nearGrass = new Color(0.29f, 0.48f, 0.17f);
            var nearMeadow = new Color(0.44f, 0.60f, 0.23f);
            var nearScrub = new Color(0.42f, 0.35f, 0.20f);
            var haze = new Color(0.74f, 0.82f, 0.90f);
            var ridgeShadow = new Color(0.36f, 0.52f, 0.24f);

            var ridgeLine = new float[width];
            for (int x = 0; x < width; x++)
            {
                float nx = x / (width - 1f);
                ridgeLine[x] = (height - 1f) - SampleDistantHillHeight(nx);
            }

            for (int y = 0; y < height; y++)
            {
                float t = y / (height - 1f);
                float dist = t * t;
                float bandScale = Mathf.Lerp(18f, 90f, dist);

                for (int x = 0; x < width; x++)
                {
                    float nx = x / (width - 1f);
                    float ridge = ridgeLine[x];

                    if (y > ridge)
                    {
                        tex.SetPixel(x, y, haze);
                        continue;
                    }

                    float patch = Mathf.PerlinNoise(nx * bandScale + 4.1f, dist * 14f + 2.7f);
                    float strip = Mathf.PerlinNoise(nx * (bandScale * 0.35f), dist * 40f);
                    Color ground = Color.Lerp(nearGrass, nearMeadow, Mathf.SmoothStep(0.35f, 0.7f, patch));
                    if (strip > 0.72f)
                        ground = Color.Lerp(ground, nearScrub, 0.45f);

                    float belowRidge = ridge - y;
                    if (belowRidge < 3f)
                    {
                        float ridgeT = 1f - Mathf.Clamp01(belowRidge / 3f);
                        ground = Color.Lerp(ground, ridgeShadow, ridgeT * 0.55f);
                        ground = Color.Lerp(ground, haze, ridgeT * 0.35f);
                    }

                    float fade = Mathf.SmoothStep(0.28f, 0.92f, t);
                    var color = Color.Lerp(ground, haze, fade);
                    tex.SetPixel(x, y, color);
                }
            }

            tex.Apply();
            return tex;
        }

        static float SampleDistantHillHeight(float nx)
        {
            float broad = Mathf.PerlinNoise(nx * 2.8f + 0.15f, 0.35f);
            float rolling = Mathf.PerlinNoise(nx * 7.5f + 4.2f, 1.8f);
            float detail = Mathf.PerlinNoise(nx * 18f + 9.1f, 3.4f);
            float profile = broad * 0.55f + rolling * 0.30f + detail * 0.15f;
            return Mathf.Lerp(5f, 10f, profile);
        }

        void CreateBackdrop(float screenWidth, float screenHeight)
        {
            var source = Resources.Load<Texture2D>(BackdropResourcePath);
            if (source == null)
            {
                Debug.LogWarning($"Monster Miner: artillery backdrop not found at Resources/{BackdropResourcePath}.");
                return;
            }

            var pixels = source.GetPixels();
            ClearSkyAndBorder(pixels, source.width, source.height);

            backdropPixels = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            backdropPixels.filterMode = FilterMode.Bilinear;
            backdropPixels.wrapMode = TextureWrapMode.Clamp;
            backdropPixels.SetPixels(pixels);
            backdropPixels.Apply();

            float xScale = screenWidth / ArtilleryFieldProfile.DesignWidth;
            float yScale = screenHeight / ArtilleryFieldProfile.DesignHeight;
            float pixelScaleX = ArtilleryFieldProfile.Pixel * xScale * MountainScale;
            float pixelScaleY = ArtilleryFieldProfile.Pixel * yScale * MountainScale;
            float mountainHeight = source.height * pixelScaleY;

            int leftWidth = ArtilleryFieldProfile.LeftColumnCount;
            int rightX0 = ArtilleryFieldProfile.RightStartColumn;
            int rightWidth = source.width - rightX0;

            var leftTexture = BuildCroppedTexture(backdropPixels, 0, 0, leftWidth, source.height);
            var rightTexture = BuildCroppedTexture(backdropPixels, rightX0, 0, rightWidth, source.height);

            float leftMountainWidth = leftWidth * pixelScaleX;
            float leftShift = SideShiftX(rightSide: false, xScale);
            CreateScreenQuad(
                "ArtilleryBackdropLeft",
                leftMountainWidth,
                mountainHeight,
                leftMountainWidth * 0.5f + leftShift,
                mountainHeight * 0.5f,
                MountainsDepth,
                CreateTransparentUnlitMaterial(leftTexture));

            float rightMountainWidth = rightWidth * pixelScaleX;
            float rightShift = SideShiftX(rightSide: true, xScale);
            CreateScreenQuad(
                "ArtilleryBackdropRight",
                rightMountainWidth,
                mountainHeight,
                screenWidth - rightMountainWidth * 0.5f + rightShift,
                mountainHeight * 0.5f,
                MountainsDepth,
                CreateTransparentUnlitMaterial(rightTexture));
        }

        static Texture2D BuildCroppedTexture(Texture2D source, int x0, int y0, int width, int height)
        {
            var pixels = source.GetPixels(x0, y0, width, height);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        void CreateBuildings(float screenWidth, float screenHeight)
        {
            float xScale = screenWidth / ArtilleryFieldProfile.DesignWidth;
            float yScale = screenHeight / ArtilleryFieldProfile.DesignHeight;

            PlaceBuildingOnPad(
                ArtilleryFieldProfile.LeftPads[2],
                "Textures/Artillery/japanese_fortress_blue",
                ArtillerySide.Left,
                ArtilleryTargetKind.Fortress,
                xScale,
                yScale,
                "LeftFortressBlue",
                horizontalShrinkPixels: 8,
                verticalOffsetPixels: -3);
            PlaceBuildingOnPad(
                ArtilleryFieldProfile.RightPads[0],
                "Textures/Artillery/japanese_fortress_red",
                ArtillerySide.Right,
                ArtilleryTargetKind.Fortress,
                xScale,
                yScale,
                "RightFortressRed",
                horizontalShrinkPixels: 8,
                verticalOffsetPixels: -3);
            PlaceBuildingOnPad(
                ArtilleryFieldProfile.LeftPads[1],
                "Textures/Artillery/japanese_palace_blue",
                ArtillerySide.Left,
                ArtilleryTargetKind.Palace,
                xScale,
                yScale,
                "LeftPalaceBlue",
                verticalOffsetPixels: -3,
                horizontalOffsetPixels: 8,
                sizeScale: 3f);
            PlaceBuildingOnPad(
                ArtilleryFieldProfile.RightPads[1],
                "Textures/Artillery/japanese_palace_red",
                ArtillerySide.Right,
                ArtilleryTargetKind.Palace,
                xScale,
                yScale,
                "RightPalaceRed",
                verticalOffsetPixels: -3,
                horizontalOffsetPixels: -8,
                sizeScale: 3f);
        }

        void CreateSoldierSquads(float screenWidth, float screenHeight)
        {
            float xScale = screenWidth / ArtilleryFieldProfile.DesignWidth;
            float yScale = screenHeight / ArtilleryFieldProfile.DesignHeight;
            float unitWidth = ComputePalaceWidth(xScale) * SoldierSizeScale * 2f;

            CreateSoldierRow(
                ArtilleryFieldProfile.LeftPads[0],
                mountainStartColumn: 0,
                mountainColumnCount: ArtilleryFieldProfile.LeftColumnCount,
                "Textures/Artillery/blue_soldiers",
                ArtillerySide.Left,
                animatedFrameCount: 2,
                xScale,
                yScale,
                unitWidth,
                "LeftSoldiers",
                new SoldierUnitOffset(0, -25, 5),
                new SoldierUnitOffset(2, 25, 5));

            CreateSoldierRow(
                ArtilleryFieldProfile.RightPads[2],
                mountainStartColumn: ArtilleryFieldProfile.RightStartColumn,
                mountainColumnCount: ArtilleryFieldProfile.ImageWidth - ArtilleryFieldProfile.RightStartColumn,
                "Textures/Artillery/red_soldiers",
                ArtillerySide.Right,
                animatedFrameCount: SoldierSheetFrameCount,
                xScale,
                yScale,
                unitWidth,
                "RightSoldiers",
                new SoldierUnitOffset(0, -25, 5),
                new SoldierUnitOffset(2, 25, 5));
        }

        readonly struct SoldierUnitOffset
        {
            public readonly int UnitIndex;
            public readonly int HorizontalPixels;
            public readonly int VerticalPixels;

            public SoldierUnitOffset(int unitIndex, int horizontalPixels, int verticalPixels)
            {
                UnitIndex = unitIndex;
                HorizontalPixels = horizontalPixels;
                VerticalPixels = verticalPixels;
            }
        }

        void CreateCavalry(float screenWidth, float screenHeight)
        {
            float xScale = screenWidth / ArtilleryFieldProfile.DesignWidth;
            float unitWidth = ComputePalaceWidth(xScale) * SoldierSizeScale * CavalrySizeScale;
            float sideMargin = screenWidth * 0.06f;

            float inwardShift = ScaledOffsetX(CavalryInwardShiftPixels, xScale);

            CreateCavalryRow(
                "Textures/Artillery/tai_gallop_cavalry",
                ArtillerySide.Left,
                blueScheme: true,
                flipHorizontal: false,
                rowStartX: sideMargin + SideShiftX(rightSide: false, xScale) + inwardShift,
                unitWidth,
                "LeftCavalry");

            float rightRowStart = screenWidth - sideMargin - unitWidth * (2 * CavalryUnitCount - 1);
            CreateCavalryRow(
                "Textures/Artillery/tai_gallop_cavalry",
                ArtillerySide.Right,
                blueScheme: false,
                flipHorizontal: true,
                rowStartX: rightRowStart + SideShiftX(rightSide: true, xScale) - inwardShift,
                unitWidth,
                "RightCavalry");
        }

        void CreateCavalryRow(
            string resourcePath,
            ArtillerySide side,
            bool blueScheme,
            bool flipHorizontal,
            float rowStartX,
            float unitWidth,
            string rowName)
        {
            var texture = LoadCavalryTexture(resourcePath, blueScheme, flipHorizontal);
            if (texture == null)
            {
                Debug.LogWarning($"Monster Miner: cavalry sprite not found at Resources/{resourcePath}.");
                return;
            }

            float frameWidth = texture.width / (float)CavalrySheetColumns;
            float frameHeight = texture.height / (float)CavalrySheetRows;
            float aspect = frameHeight / frameWidth;
            float unitHeight = unitWidth * aspect;
            float centerY = unitHeight * 0.5f;

            var rowRoot = new GameObject(rowName);
            rowRoot.transform.SetParent(transform, false);

            var material = CreateTransparentUnlitMaterial(texture);
            rowRoot.AddComponent<ArtilleryCavalryAnimator>()
                .Configure(material, CavalrySheetColumns, CavalrySheetRows);

            cavalryUnitWidth = unitWidth;

            for (int i = 0; i < CavalryUnitCount; i++)
            {
                float centerX = rowStartX + unitWidth * (0.5f + i * 2f);
                var quad = CreateScreenQuad(
                    $"{rowName}_{i + 1}",
                    unitWidth,
                    unitHeight,
                    centerX,
                    centerY,
                    CavalryDepth,
                    material,
                    rowRoot.transform);
                RegisterHitTarget(
                    quad,
                    side,
                    ArtilleryTargetKind.Cavalry,
                    centerX,
                    centerY,
                    unitWidth,
                    unitHeight,
                    CavalryDepth);

                var cavalryUnit = quad.AddComponent<ArtilleryCavalryUnit>();
                var hitTarget = quad.GetComponent<ArtilleryHitTarget>();
                cavalryUnit.Bind(hitTarget);
                cavalryUnits.Add(cavalryUnit);
            }
        }

        void CreateCatapults(float screenWidth, float screenHeight)
        {
            float xScale = screenWidth / ArtilleryFieldProfile.DesignWidth;
            float yScale = screenHeight / ArtilleryFieldProfile.DesignHeight;
            float unitWidth = ComputePalaceWidth(xScale) * SoldierSizeScale * CatapultSizeScale;
            float soldierUnitWidth = ComputePalaceWidth(xScale) * SoldierSizeScale * 2f;

            leftCatapult = PlaceCatapultOnPad(
                ArtilleryFieldProfile.LeftPads[1],
                ArtillerySide.Left,
                flipHorizontal: false,
                xScale,
                yScale,
                unitWidth,
                ComputeMiddleInfantryCenterX(
                    xScale,
                    ArtilleryFieldProfile.LeftColumnCount,
                    soldierUnitWidth),
                horizontalOffsetPixels: 10,
                "LeftCatapult");

            rightCatapult = PlaceCatapultOnPad(
                ArtilleryFieldProfile.RightPads[1],
                ArtillerySide.Right,
                flipHorizontal: true,
                xScale,
                yScale,
                unitWidth,
                ComputeMiddleInfantryCenterX(
                    xScale,
                    ArtilleryFieldProfile.ImageWidth - ArtilleryFieldProfile.RightStartColumn,
                    soldierUnitWidth,
                    ArtilleryFieldProfile.RightStartColumn),
                horizontalOffsetPixels: -10,
                "RightCatapult");
        }

        ArtilleryCatapult PlaceCatapultOnPad(
            ArtilleryBuildingPad pad,
            ArtillerySide side,
            bool flipHorizontal,
            float xScale,
            float yScale,
            float unitWidth,
            float centerX,
            int horizontalOffsetPixels,
            string objectName)
        {
            float padTop = MapImageYToWorld(pad.HeightPixels, yScale);
            centerX += ScaledOffsetX(horizontalOffsetPixels, xScale);

            var texture = LoadCatapultTexture("Textures/Artillery/catapult_spritesheet");
            if (texture == null)
            {
                Debug.LogWarning("Monster Miner: catapult sprite not found at Resources/Textures/Artillery/catapult_spritesheet.");
                return null;
            }

            return CreateCatapult(
                side,
                flipHorizontal,
                centerX,
                padTop,
                yScale,
                unitWidth,
                objectName,
                texture);
        }

        ArtilleryCatapult CreateCatapult(
            ArtillerySide side,
            bool flipHorizontal,
            float centerX,
            float padTop,
            float yScale,
            float unitWidth,
            string objectName,
            Texture2D texture = null)
        {
            texture ??= LoadCatapultTexture("Textures/Artillery/catapult_spritesheet");
            if (texture == null)
            {
                Debug.LogWarning("Monster Miner: catapult sprite not found at Resources/Textures/Artillery/catapult_spritesheet.");
                return null;
            }

            float offsetY = ScaledOffsetY(CatapultVerticalOffsetPixels, yScale);
            float placeholderHeight = unitWidth;

            var root = new GameObject(objectName);
            root.transform.SetParent(transform, false);

            var material = CreateTransparentUnlitMaterial(texture);

            var quad = CreateInteractiveScreenQuad(
                objectName + "_Sprite",
                unitWidth,
                placeholderHeight,
                centerX,
                padTop + placeholderHeight * 0.5f + offsetY,
                CatapultDepth,
                material,
                root.transform);

            var renderer = quad.GetComponent<MeshRenderer>();
            var animator = quad.AddComponent<ArtilleryCatapultAnimator>();
            animator.Configure(renderer, texture, CatapultSheetColumns, CatapultSheetRows, flipHorizontal);

            float unitHeight = unitWidth * (animator.NormalizedFrameHeight / (float)Mathf.Max(1, animator.NormalizedFrameWidth));
            quad.transform.localScale = new Vector3(unitWidth, unitHeight, 1f);
            quad.transform.localPosition = new Vector3(centerX, padTop + unitHeight * 0.5f + offsetY, CatapultDepth);

            var catapult = quad.AddComponent<ArtilleryCatapult>();
            catapult.Configure(side, animator, quad.GetComponent<Collider>(), unitWidth, unitHeight);

            float catapultCenterY = padTop + unitHeight * 0.5f + offsetY;
            RegisterHitTarget(
                quad,
                side,
                ArtilleryTargetKind.Catapult,
                centerX,
                catapultCenterY,
                unitWidth,
                unitHeight,
                CatapultDepth);

            return catapult;
        }

        static GameObject CreateInteractiveScreenQuad(
            string name,
            float width,
            float height,
            float centerX,
            float centerY,
            float depth,
            Material material,
            Transform parent)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = new Vector3(centerX, centerY, depth);
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(width, height, 1f);

            Object.Destroy(quad.GetComponent<MeshCollider>());
            var box = quad.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 1f, 0.05f);

            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return quad;
        }

        static Texture2D LoadCatapultTexture(string resourcePath)
        {
            var source = Resources.Load<Texture2D>(resourcePath);
            if (source == null)
                return null;

            int width = source.width;
            int height = source.height;
            var pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var color = source.GetPixel(x, y);
                    if (color.r <= 0.06f && color.g <= 0.06f && color.b <= 0.06f)
                        color = Color.clear;
                    else if (!IsCatapultMarkerPixel(color) && IsCatapultWoodPixel(color))
                        color = NormalizeCatapultWoodPixel(color);
                    pixels[y * width + x] = color;
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        static bool IsCatapultMarkerPixel(Color color)
        {
            if (color.g > 0.55f && color.g > color.r + 0.35f && color.g > color.b + 0.35f)
                return true;
            return color.r > 0.55f && color.r > color.g + 0.35f && color.r > color.b + 0.35f;
        }

        static bool IsCatapultWoodPixel(Color color)
        {
            float r = color.r;
            float g = color.g;
            float b = color.b;
            float lum = (r + g + b) * (1f / 3f);
            float max = Mathf.Max(r, Mathf.Max(g, b));
            float min = Mathf.Min(r, Mathf.Min(g, b));
            float saturation = max - min;

            if (lum < 0.08f)
                return false;
            if (lum < 0.40f && saturation < 0.14f)
                return false;

            return r >= g - 0.08f && g >= b - 0.02f && r > b + 0.04f;
        }

        static Color NormalizeCatapultWoodPixel(Color color)
        {
            float lum = (color.r + color.g + color.b) * (1f / 3f);
            float shade = Mathf.Clamp01(Mathf.InverseLerp(0.12f, 0.62f, lum));
            var dark = new Color(0.66f, 0.56f, 0.43f, color.a);
            return Color.Lerp(dark, CatapultWoodBright, shade);
        }

        static float ComputePalaceWidth(float xScale)
        {
            var pad = ArtilleryFieldProfile.LeftPads[1];
            float padWidth = (pad.EndColumn + 1 - pad.StartColumn) * ArtilleryFieldProfile.Pixel * xScale;
            return padWidth * 3f;
        }

        void CreateSoldierRow(
            ArtilleryBuildingPad basePad,
            int mountainStartColumn,
            int mountainColumnCount,
            string resourcePath,
            ArtillerySide side,
            int animatedFrameCount,
            float xScale,
            float yScale,
            float unitWidth,
            string rowName,
            params SoldierUnitOffset[] unitOffsets)
        {
            var texture = LoadSoldierTexture(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"Monster Miner: soldier sprite not found at Resources/{resourcePath}.");
                return;
            }

            float frameWidth = texture.width / (float)SoldierSheetFrameCount;
            float aspect = texture.height / frameWidth;
            float unitHeight = unitWidth * aspect;
            float padTop = MapImageYToWorld(basePad.HeightPixels, yScale);
            float offsetY = ScaledOffsetY(SoldierVerticalOffsetPixels, yScale);
            float mountainWidth = mountainColumnCount * ArtilleryFieldProfile.Pixel * xScale * MountainScale;
            float totalWidth = unitWidth * SoldierSquadCount;
            float rowStartX = MapImageXToWorld(mountainStartColumn, xScale)
                + (mountainWidth - totalWidth) * 0.5f;

            var rowRoot = new GameObject(rowName);
            rowRoot.transform.SetParent(transform, false);

            var material = CreateTransparentUnlitMaterial(texture);
            rowRoot.AddComponent<ArtillerySoldierSquad>()
                .Configure(material, animatedFrameCount, SoldierSheetFrameCount);

            for (int i = 0; i < SoldierSquadCount; i++)
            {
                float centerX = rowStartX + unitWidth * (i + 0.5f);
                float unitOffsetY = offsetY;
                if (unitOffsets != null)
                {
                    for (int o = 0; o < unitOffsets.Length; o++)
                    {
                        if (unitOffsets[o].UnitIndex != i)
                            continue;

                        centerX += ScaledOffsetX(unitOffsets[o].HorizontalPixels, xScale);
                        unitOffsetY += ScaledOffsetY(unitOffsets[o].VerticalPixels, yScale);
                        break;
                    }
                }

                float centerY = padTop + unitHeight * 0.5f + unitOffsetY;
                var quad = CreateScreenQuad(
                    $"{rowName}_{i + 1}",
                    unitWidth,
                    unitHeight,
                    centerX,
                    centerY,
                    SoldiersDepth,
                    material,
                    rowRoot.transform);
                RegisterHitTarget(
                    quad,
                    side,
                    ArtilleryTargetKind.Infantry,
                    centerX,
                    centerY,
                    unitWidth,
                    unitHeight,
                    SoldiersDepth,
                    i);
            }
        }

        void PlaceBuildingOnPad(
            ArtilleryBuildingPad pad,
            string resourcePath,
            ArtillerySide side,
            ArtilleryTargetKind kind,
            float xScale,
            float yScale,
            string objectName,
            int horizontalShrinkPixels = 0,
            int verticalOffsetPixels = 0,
            int horizontalOffsetPixels = 0,
            float sizeScale = 1f)
        {
            var texture = LoadBuildingTexture(resourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"Monster Miner: artillery building texture not found at Resources/{resourcePath}.");
                return;
            }

            float x0 = MapImageXToWorld(pad.StartColumn, xScale);
            float x1 = MapImageXToWorld(pad.EndColumn + 1, xScale);
            float padTop = MapImageYToWorld(pad.HeightPixels, yScale);
            float padWidth = Mathf.Abs(x1 - x0);
            float shrinkX = ScaledOffsetX(horizontalShrinkPixels, xScale);
            float buildingWidth = Mathf.Max(ArtilleryFieldProfile.Pixel * xScale * MountainScale, padWidth - shrinkX)
                * sizeScale
                * (FieldAssetScale / MountainScale);
            float aspect = texture.height / (float)texture.width;
            float buildingHeight = buildingWidth * aspect;
            float offsetY = ScaledOffsetY(verticalOffsetPixels, yScale);
            float offsetX = ScaledOffsetX(horizontalOffsetPixels, xScale);
            float centerX = (x0 + x1) * 0.5f + offsetX;
            float centerY = padTop + buildingHeight * 0.5f + offsetY;

            var quad = CreateScreenQuad(
                objectName,
                buildingWidth,
                buildingHeight,
                centerX,
                centerY,
                BuildingsDepth,
                CreateTransparentUnlitMaterial(texture));
            RegisterHitTarget(
                quad,
                side,
                kind,
                centerX,
                centerY,
                buildingWidth,
                buildingHeight,
                BuildingsDepth);
        }

        static Texture2D LoadBuildingTexture(string resourcePath)
        {
            var source = Resources.Load<Texture2D>(resourcePath);
            if (source == null)
                return null;

            var pixels = source.GetPixels();
            bool isBlueBuilding = resourcePath.Contains("blue");
            for (int i = 0; i < pixels.Length; i++)
            {
                var color = pixels[i];
                if (color.r <= 0.06f && color.g <= 0.06f && color.b <= 0.06f)
                {
                    pixels[i] = Color.clear;
                    continue;
                }

                color = AdjustBuildingStonePixel(color);

                if (IsBlueRoofPixel(color) && isBlueBuilding)
                    color = NormalizeBlueRoofPixel(color);
                else if (IsRedRoofPixel(color) && !isBlueBuilding)
                    color = NormalizeRedRoofPixel(color);

                pixels[i] = color;
            }

            var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        static bool IsBlueRoofPixel(Color color)
        {
            return color.b > color.r + 0.06f
                && color.b > color.g + 0.02f
                && color.b > 0.22f
                && (color.r + color.g + color.b) * (1f / 3f) < 0.72f;
        }

        static bool IsRedRoofPixel(Color color)
        {
            return color.r > color.g + 0.06f
                && color.r > color.b + 0.06f
                && color.r > 0.22f;
        }

        static Color NormalizeBlueRoofPixel(Color color)
        {
            float lum = (color.r + color.g + color.b) * (1f / 3f);
            float shade = Mathf.Clamp01(Mathf.InverseLerp(0.20f, 0.75f, lum));
            var dark = new Color(0.12f, 0.38f, 0.82f, color.a);
            var bright = new Color(0.30f, 0.60f, 1.0f, color.a);
            return Color.Lerp(Color.Lerp(dark, bright, shade), bright, 0.08f);
        }

        static Color NormalizeBlueInfantryPixel(Color color)
        {
            float lum = (color.r + color.g + color.b) * (1f / 3f);
            float shade = Mathf.Clamp01(Mathf.InverseLerp(0.20f, 0.75f, lum));
            var dark = new Color(0.12f, 0.38f, 0.82f, color.a);
            var bright = new Color(0.30f, 0.60f, 1.0f, color.a);
            return Color.Lerp(Color.Lerp(dark, bright, shade), bright, 0.18f);
        }

        static bool IsCavalryHorsePixel(Color color)
        {
            float r = color.r;
            float g = color.g;
            float b = color.b;
            float lum = (r + g + b) * (1f / 3f);
            float max = Mathf.Max(r, Mathf.Max(g, b));
            float min = Mathf.Min(r, Mathf.Min(g, b));
            float saturation = max - min;

            if (lum < 0.08f || lum > 0.72f)
                return false;
            if (saturation < 0.08f && lum < 0.28f)
                return false;
            if (IsBlueSoldierPrimaryPixel(color))
                return false;
            if (IsRedRoofPixel(color) && r > g + 0.12f)
                return false;

            return r >= g - 0.05f && g >= b - 0.02f && r > b + 0.04f;
        }

        static Color NormalizeCavalryHorsePixel(Color color)
        {
            float lum = (color.r + color.g + color.b) * (1f / 3f);
            float shade = Mathf.Clamp01(Mathf.InverseLerp(0.12f, 0.55f, lum));
            var dark = new Color(0.82f, 0.82f, 0.82f, color.a);
            var bright = new Color(1.0f, 1.0f, 1.0f, color.a);
            return Color.Lerp(dark, bright, shade);
        }

        static bool IsBlueSoldierPrimaryPixel(Color color)
        {
            float r = color.r;
            float g = color.g;
            float b = color.b;
            float lum = (r + g + b) * (1f / 3f);
            float max = Mathf.Max(r, Mathf.Max(g, b));
            float min = Mathf.Min(r, Mathf.Min(g, b));
            float saturation = max - min;

            if (b < 0.12f || b <= r - 0.02f || b < g * 0.82f)
                return false;
            if (lum > 0.75f && saturation < 0.18f)
                return false;
            if (r > 0.28f && g > 0.18f && b < 0.20f && r > b + 0.10f)
                return false;
            if (r > 0.36f && g > 0.24f && b > 0.14f && r >= g - 0.04f && b <= g + 0.10f && saturation < 0.28f)
                return false;

            return b > r + 0.02f;
        }

        static Color NormalizeRedRoofPixel(Color color)
        {
            float lum = (color.r + color.g + color.b) * (1f / 3f);
            float shade = Mathf.Clamp01(Mathf.InverseLerp(0.20f, 0.75f, lum));
            var dark = new Color(0.78f, 0.18f, 0.14f, color.a);
            var bright = new Color(1.0f, 0.34f, 0.28f, color.a);
            return Color.Lerp(Color.Lerp(dark, bright, shade), bright, 0.08f);
        }

        static Color AdjustBuildingStonePixel(Color color)
        {
            float r = color.r;
            float g = color.g;
            float b = color.b;
            float max = Mathf.Max(r, Mathf.Max(g, b));
            float min = Mathf.Min(r, Mathf.Min(g, b));
            float lum = (r + g + b) * (1f / 3f);
            float saturation = max - min;

            if (b > r + 0.06f && b > g + 0.02f && b > 0.22f)
                return color;
            if (r > g + 0.06f && r > b + 0.06f && r > 0.22f)
                return color;
            if (lum > 0.72f && saturation < 0.18f)
                return color;
            if (r > 0.58f && g > 0.48f && b < 0.38f && saturation > 0.12f)
                return color;

            bool isStone = saturation < 0.24f && lum >= 0.16f && lum <= 0.68f;
            if (!isStone)
                return color;

            float shade = Mathf.InverseLerp(0.16f, 0.68f, lum);
            float gray = Mathf.Lerp(0.70f, 0.86f, shade);
            var lightGray = new Color(gray, gray, gray * 1.01f, color.a);
            return Color.Lerp(color, lightGray, 0.88f);
        }

        static Texture2D LoadSoldierTexture(string resourcePath)
        {
            var source = Resources.Load<Texture2D>(resourcePath);
            if (source == null)
                return null;

            var pixels = source.GetPixels();
            bool isBlueSoldiers = resourcePath.Contains("blue");
            for (int i = 0; i < pixels.Length; i++)
            {
                var color = pixels[i];
                if (color.r <= 0.06f && color.g <= 0.06f && color.b <= 0.06f)
                {
                    pixels[i] = Color.clear;
                    continue;
                }

                if (isBlueSoldiers && IsBlueSoldierPrimaryPixel(color))
                    color = NormalizeBlueInfantryPixel(color);
                else if (!isBlueSoldiers && IsRedRoofPixel(color))
                    color = NormalizeRedRoofPixel(color);

                pixels[i] = color;
            }

            var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        static Texture2D LoadCavalryTexture(string resourcePath, bool blueScheme, bool flipHorizontal)
        {
            var source = Resources.Load<Texture2D>(resourcePath);
            if (source == null)
                return null;

            int width = source.width;
            int height = source.height;
            var pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sampleX = flipHorizontal ? width - 1 - x : x;
                    var color = source.GetPixel(sampleX, y);
                    if (color.r <= 0.06f && color.g <= 0.06f && color.b <= 0.06f)
                    {
                        pixels[y * width + x] = Color.clear;
                        continue;
                    }

                    if (IsCavalryHorsePixel(color))
                    {
                        color = NormalizeCavalryHorsePixel(color);
                    }
                    else if (blueScheme)
                    {
                        if (IsBlueSoldierPrimaryPixel(color))
                            color = NormalizeBlueInfantryPixel(color);
                    }
                    else
                    {
                        if (IsBlueSoldierPrimaryPixel(color) || IsRedRoofPixel(color))
                            color = NormalizeRedRoofPixel(color);
                    }

                    pixels[y * width + x] = color;
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        void CreateBuildingPads(ArtilleryBuildingPad[] pads, float xScale, float yScale)
        {
            if (pads == null)
                return;

            foreach (var pad in pads)
            {
                float x0 = MapImageXToWorld(pad.StartColumn, xScale);
                float x1 = MapImageXToWorld(pad.EndColumn + 1, xScale);
                float y = MapImageYToWorld(pad.HeightPixels, yScale);

                var marker = new GameObject(pad.Name).transform;
                marker.SetParent(transform, false);
                marker.localPosition = new Vector3((x0 + x1) * 0.5f, y, MountainsDepth);
                var padComp = marker.gameObject.AddComponent<ArtilleryBuildingPadMarker>();
                padComp.Configure(new Vector3(Mathf.Abs(x1 - x0), 0f, 0f));
            }
        }

        void CreateCamera()
        {
            float halfHeight = ArtilleryFieldProfile.DesignHeight * 0.5f;
            float halfWidth = halfHeight * (Screen.width / (float)Mathf.Max(1, Screen.height));

            var camGo = new GameObject("ArtilleryCamera");
            camGo.transform.SetParent(transform, false);
            camGo.transform.localPosition = new Vector3(halfWidth, halfHeight, -10f);
            camGo.transform.localRotation = Quaternion.identity;

            viewCamera = camGo.AddComponent<Camera>();
            viewCamera.clearFlags = CameraClearFlags.SolidColor;
            viewCamera.backgroundColor = SkyFallbackColor;
            viewCamera.allowHDR = true;
            viewCamera.allowMSAA = true;
            viewCamera.orthographic = true;
            viewCamera.orthographicSize = halfHeight;
            viewCamera.nearClipPlane = 0.05f;
            viewCamera.farClipPlane = 20f;
            viewCamera.tag = "MainCamera";

            listener = camGo.AddComponent<AudioListener>();
        }

        static void ClearSkyAndBorder(Color[] pixels, int width, int height)
        {
            var visited = new bool[pixels.Length];
            var stack = new Stack<int>();

            void TryPush(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height)
                    return;

                int i = y * width + x;
                if (visited[i] || !IsSkyPixel(pixels[i]))
                    return;

                visited[i] = true;
                stack.Push(i);
            }

            for (int x = 0; x < width; x++)
                TryPush(x, height - 1);

            for (int y = 0; y < height; y++)
            {
                TryPush(0, y);
                TryPush(width - 1, y);
            }

            while (stack.Count > 0)
            {
                int i = stack.Pop();
                pixels[i] = Color.clear;
                int x = i % width;
                int y = i / width;
                TryPush(x + 1, y);
                TryPush(x - 1, y);
                TryPush(x, y + 1);
                TryPush(x, y - 1);
            }

            DilateClear(pixels, width, height, 2);
        }

        static bool IsSkyPixel(Color color)
        {
            if (color.a < 0.08f)
                return true;

            float max = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            float min = Mathf.Min(color.r, Mathf.Min(color.g, color.b));
            float lum = (color.r + color.g + color.b) * (1f / 3f);
            return lum > 0.84f && (max - min) < 0.14f;
        }

        static void DilateClear(Color[] pixels, int width, int height, int radius)
        {
            var clear = new bool[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                clear[i] = pixels[i].a < 0.08f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    if (clear[i])
                        continue;

                    bool nearClear = false;
                    for (int dy = -radius; dy <= radius && !nearClear; dy++)
                    {
                        int ny = y + dy;
                        if (ny < 0 || ny >= height)
                            continue;
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int nx = x + dx;
                            if (nx < 0 || nx >= width)
                                continue;
                            if (clear[ny * width + nx])
                            {
                                nearClear = true;
                                break;
                            }
                        }
                    }

                    if (nearClear)
                        pixels[i] = Color.clear;
                }
            }
        }

        static Material CreateOpaqueUnlitMaterial(Texture2D texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Texture");

            var material = new Material(shader);
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_AlphaClip"))
                material.SetFloat("_AlphaClip", 0f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Geometry;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color"))
                material.color = Color.white;
            return material;
        }

        static Material CreateTransparentUnlitMaterial(Texture2D texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            var material = new Material(shader);
            material.renderQueue = (int)RenderQueue.Transparent;
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_AlphaClip"))
                material.SetFloat("_AlphaClip", 0f);
            if (material.HasProperty("_SrcBlend"))
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color"))
                material.color = Color.white;
            return material;
        }
    }
}
