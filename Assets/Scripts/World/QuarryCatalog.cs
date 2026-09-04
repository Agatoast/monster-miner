using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class QuarryCatalog
    {
        public const float Quarry2NominalRadiusFeet = 350f;

        public const float VikingCharacterRightOfHallCenterFeet = 7f;
        public const float VikingCharacterSouthOfHallFaceFeet = 9f;
        public const float VikingCharacterShiftLeftFeet = 15f;
        public const float VikingCharacterShiftRightFeet = 50f;
        public const float VikingCharacterShiftNorthFeet = 85f;
        public const float LandQuarry2PlayerSpawnShiftNorthFeet = 90f;
        public const float LandQuarry2ShopWestOfSpawnFeet = 20f;
        public const float LandQuarry2BeachApproachSpawnSouthOfSandFeet = 32f;
        public const float LandQuarry2BeachApproachSpawnWestOfBeachCenterFeet = 12f;
        public const bool SpawnPlayerOnIslandForTesting = false;
        public const bool SpawnPlayerAtJarlLandShopForTesting = false;
        public const bool SpawnPlayerAtQuarry3ForTesting = false;
        public const bool SpawnPlayerAtQuarry4ForTesting = false;
        public const bool SpawnPlayerAtFirstSkyMetalSiteForTesting = false;
        public const bool SpawnPlayerAtSecondSkyMetalSiteForTesting = true;
        public const bool SpawnPlayerAtThirdSkyMetalSiteForTesting = false;
        public const float Quarry3PlayerSpawnInFrontOfGuideFeet = 8f;
        public const float Quarry4PlayerSpawnOffsetFromCenterFeet = 8f;
        public const float FirstSkyMetalSiteSpawnOffsetFeet = 8f;
        public const float SecondSkyMetalSiteSpawnOffsetFeet = SkyMetalDigSiteCatalog.SecondSitePlayerSpawnNorthOfDigSiteFeet;
        public const float ThirdSkyMetalSiteSpawnOffsetFeet = 8f;
        public const float Quarry3ShopNorthOfGuideFeet = 30f;
        public const float Quarry3ShopWestOfGuideFeet = 50f;
        public const float IslandPlayerSpawnOffsetFromCenterFeet = 30f;
        public const float JarlLandShopFrontSpawnFeet = 10f;
        public const string JarlLandDisplayName = "Jarl Land";

        public static readonly Color JarlLandMapColor = new Color(0.2f, 0.85f, 0.3f);
        public static readonly Color ShogunMapColor = Color.blue;
        public static readonly Color OrinMapColor = Color.yellow;
        public static readonly Color DragonMapColor = Color.white;

        public const int PlateauQuarryIndex = 1;
        public const int LandQuarry2Index = 2;
        public const int LandQuarry3Index = 3;
        public const int LandQuarry4Index = 4;
        public const int LandQuarry5Index = 5;

        /// <summary>Override when Quarry 4 / Orin site is placed; zero uses east-of-Q3 placeholder.</summary>
        public static Vector2 MapOrinLocalXZ;

        /// <summary>Override when Dragon quest site is placed; zero uses 1 mi SE of Orin placeholder.</summary>
        public static Vector2 MapDragonLocalXZ;

        public readonly struct MapEdgeMarker
        {
            public readonly string Label;
            public readonly Color Color;
            readonly System.Func<GameContext, bool> isVisible;
            readonly System.Func<GameContext, Vector2> getLocalXZ;

            public MapEdgeMarker(
                string label,
                Color color,
                System.Func<GameContext, bool> isVisible,
                System.Func<GameContext, Vector2> getLocalXZ)
            {
                Label = label;
                Color = color;
                this.isVisible = isVisible;
                this.getLocalXZ = getLocalXZ;
            }

            public bool IsVisible(GameContext ctx) => isVisible != null && isVisible(ctx);

            public Vector2 GetLocalXZ(GameContext ctx) => getLocalXZ != null ? getLocalXZ(ctx) : Vector2.zero;
        }

        public static readonly MapEdgeMarker[] EdgeMarkers =
        {
            new MapEdgeMarker(
                "Plateau",
                Color.red,
                ctx => ctx?.CaveProgression != null && ctx.CaveProgression.HasWorldMap,
                _ => Vector2.zero),
            new MapEdgeMarker(
                "Truck",
                new Color(0.45f, 0.65f, 1f),
                ctx => ctx?.PlayerTruck != null,
                GetTruckLocal),
            new MapEdgeMarker(
                JarlLandDisplayName,
                JarlLandMapColor,
                ctx => ctx?.CaveProgression != null && ctx.CaveProgression.HasWorldMap,
                _ => GetLandQuarry2Center()),
            new MapEdgeMarker(
                "Shogun",
                ShogunMapColor,
                ctx => ctx?.CaveProgression != null && ctx.CaveProgression.ArtilleryTrialWon,
                _ => GetLandQuarry3Center()),
            new MapEdgeMarker(
                "Orin",
                OrinMapColor,
                ctx => ctx?.CaveProgression != null && ctx.CaveProgression.ArtilleryTrialWon,
                _ => GetOrinMapTargetLocal()),
            new MapEdgeMarker(
                "Dragon",
                DragonMapColor,
                ctx => ctx?.CaveProgression != null
                    && ctx.CaveProgression.HasLegendarySkyMetalMachineGun
                    && !ctx.CaveProgression.Quest5Complete,
                _ => GetDragonMapTargetLocal()),
            new MapEdgeMarker(
                SkyMetalDigSiteCatalog.GetMapLabel(SkyMetalDigSiteCatalog.FirstSiteIndex),
                SkyMetalDigSiteCatalog.DetectorBlue,
                ctx => IsDiscoveredSkyMetalSite(ctx, SkyMetalDigSiteCatalog.FirstSiteIndex),
                _ => SkyMetalDigSiteCatalog.GetFirstSiteContentLocalXZ()),
            new MapEdgeMarker(
                SkyMetalDigSiteCatalog.GetMapLabel(SkyMetalDigSiteCatalog.SecondSiteIndex),
                SkyMetalDigSiteCatalog.DetectorBlue,
                ctx => IsDiscoveredSkyMetalSite(ctx, SkyMetalDigSiteCatalog.SecondSiteIndex),
                _ => SkyMetalDigSiteCatalog.GetSecondSiteContentLocalXZ()),
            new MapEdgeMarker(
                SkyMetalDigSiteCatalog.GetMapLabel(SkyMetalDigSiteCatalog.ThirdSiteIndex),
                SkyMetalDigSiteCatalog.DetectorBlue,
                ctx => IsDiscoveredSkyMetalSite(ctx, SkyMetalDigSiteCatalog.ThirdSiteIndex),
                _ => SkyMetalDigSiteCatalog.GetThirdSiteContentLocalXZ()),
        };

        public static Vector2 GetLandQuarry2Center() => new Vector2(0f, WorldScale.Miles(1f));

        public static Vector2 GetLandQuarry3Center()
        {
            var quarry2 = GetLandQuarry2Center();
            return new Vector2(quarry2.x + WorldScale.Miles(1f), quarry2.y);
        }

        public static Vector2 GetLandQuarry4Center()
        {
            // One mile east of plateau, one mile south of Quarry 3.
            return new Vector2(WorldScale.Miles(1f), 0f);
        }

        public static Vector2 GetOrinMapTargetLocal()
        {
            if (MapOrinLocalXZ.sqrMagnitude > 0.01f)
                return MapOrinLocalXZ;

            return GetLandQuarry4Center();
        }

        public static Vector2 GetDragonMapTargetLocal()
        {
            if (MapDragonLocalXZ.sqrMagnitude > 0.01f)
                return MapDragonLocalXZ;

            var orin = GetOrinMapTargetLocal();
            return new Vector2(orin.x + WorldScale.Miles(1f), orin.y - WorldScale.Miles(1f));
        }

        public static bool TryGetQuarryGuideCompassTarget(
            CavernBounds bounds,
            CaveProgression progression,
            out Vector3 worldTarget,
            out Color color)
        {
            worldTarget = Vector3.zero;
            color = default;

            if (bounds == null || progression == null || !progression.HasWorldMap)
                return false;

            if (progression.HasSkyMetalDetector
                && SkyMetalDigSiteManager.GetActiveCompassTargetWorld().HasValue)
                return false;

            if (!progression.HasHeardJarlIntro)
            {
                color = JarlLandMapColor;
                worldTarget = progression.HasLandedOnLand
                    ? ResolveVikingCharacterSpawnWorld(bounds)
                    : ResolveQuarryCenterWorld(bounds, LandQuarry2Index);
                if (worldTarget == Vector3.zero)
                    worldTarget = ResolveQuarryCenterWorld(bounds, LandQuarry2Index);
                return worldTarget != Vector3.zero;
            }

            if (progression.ArtilleryTrialWon && !progression.HasSkyMetalDetector)
            {
                color = OrinMapColor;
                worldTarget = ResolveOrinMapTargetWorld(bounds);
                return worldTarget != Vector3.zero;
            }

            if (progression.HasMagicCompass)
                return false;

            if (progression.HasLegendarySkyMetalMachineGun && !progression.Quest5Complete)
            {
                color = DragonMapColor;
                worldTarget = ResolveDragonMapTargetWorld(bounds);
                return worldTarget != Vector3.zero;
            }

            return false;
        }

        public static Vector3 ResolveDragonMapTargetWorld(CavernBounds bounds)
        {
            if (bounds == null)
                return Vector3.zero;

            Vector2 local = GetDragonMapTargetLocal();
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(local.x, local.y, plainsBaseY);
            return bounds.transform.TransformPoint(new Vector3(local.x, groundY, local.y));
        }

        public static Vector3 ResolveOrinMapTargetWorld(CavernBounds bounds)
        {
            if (bounds == null)
                return Vector3.zero;

            Vector2 local = GetOrinMapTargetLocal();
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(local.x, local.y, plainsBaseY);
            return bounds.transform.TransformPoint(new Vector3(local.x, groundY, local.y));
        }

        static bool IsDiscoveredSkyMetalSite(GameContext ctx, int siteIndex) =>
            ctx?.CaveProgression != null && ctx.CaveProgression.HasDiscoveredSkyMetalSite(siteIndex);

        public static Vector2 GetLandQuarryCenter(int quarryIndex)
        {
            if (quarryIndex == LandQuarry2Index)
                return GetLandQuarry2Center();
            if (quarryIndex == LandQuarry3Index)
                return GetLandQuarry3Center();
            if (quarryIndex == LandQuarry4Index)
                return GetLandQuarry4Center();
            if (quarryIndex == LandQuarry5Index)
                return GetDragonMapTargetLocal();

            return Vector2.zero;
        }

        public static bool IsLandQuarry2Local(float localX, float localZ)
        {
            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null || !progression.HasLandQuarry2)
                return false;

            return LandQuarry2Boundary.ContainsLocal(localX, localZ);
        }

        public static bool IsLandQuarry3Local(float localX, float localZ)
        {
            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null || !progression.HasLandQuarry3)
                return false;

            return LandQuarry3Boundary.ContainsLocal(localX, localZ);
        }

        public static bool IsLandQuarry4Local(float localX, float localZ)
        {
            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null || !progression.HasLandQuarry4)
                return false;

            return LandQuarry4Boundary.ContainsLocal(localX, localZ);
        }

        public static Vector3 ResolveQuarry3ShopAnchorLocal()
        {
            return new Vector3(
                -WorldScale.Feet(Quarry3ShopWestOfGuideFeet),
                0f,
                WorldScale.Feet(Quarry3ShopNorthOfGuideFeet));
        }

        public static Vector3 ResolveQuarry3PlayerSpawnLocal()
        {
            return new Vector3(0f, 0f, -WorldScale.Feet(Quarry3PlayerSpawnInFrontOfGuideFeet));
        }

        public static Vector3 ResolveQuarry4PlayerSpawnWorld(CavernBounds bounds)
        {
            if (bounds == null)
                return Vector3.zero;

            var center = GetLandQuarry4Center();
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(center.x, center.y, plainsBaseY);
            var local = new Vector3(
                center.x,
                groundY,
                center.y + WorldScale.Feet(Quarry4PlayerSpawnOffsetFromCenterFeet));
            return PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                bounds.transform.TransformPoint(local),
                WorldScale.CharacterHeightUnits * 0.5f);
        }

        public static Vector3 ResolveHallFrontSpawnWorld(CavernBounds bounds)
        {
            if (!SpawnPlayerOnIslandForTesting
                && !SpawnPlayerAtQuarry3ForTesting
                && !SpawnPlayerAtQuarry4ForTesting
                && !SpawnPlayerAtFirstSkyMetalSiteForTesting
                && !SpawnPlayerAtSecondSkyMetalSiteForTesting
                && !SpawnPlayerAtThirdSkyMetalSiteForTesting
                && PlayerSpawnPersistence.HasSavedLandSpawn)
                return PlayerSpawnPersistence.LoadSavedLandSpawn();

            return ResolvePlayerSpawnWorld(bounds);
        }

        public static Vector3 ResolveFirstSkyMetalSitePlayerSpawnWorld(CavernBounds bounds)
        {
            if (bounds == null)
                return Vector3.zero;

            Vector2 siteLocal = SkyMetalDigSiteCatalog.GetFirstSiteContentLocalXZ();
            float offset = WorldScale.Feet(FirstSkyMetalSiteSpawnOffsetFeet);
            var contentLocal = new Vector3(siteLocal.x, 0f, siteLocal.y - offset);
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            contentLocal.y = PlainsWorldBuilder.SamplePlainsLocalY(contentLocal.x, contentLocal.z, plainsBaseY);

            return PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                bounds.transform.TransformPoint(contentLocal),
                WorldScale.CharacterHeightUnits * 0.5f);
        }

        public static Vector3 ResolveSecondSkyMetalSitePlayerSpawnWorld(CavernBounds bounds)
        {
            if (bounds == null)
                return Vector3.zero;

            Vector3 contentLocal = SkyMetalDigSiteCatalog.ResolveSecondSitePlayerSpawnContentLocal();

            return PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                bounds.transform.TransformPoint(contentLocal),
                WorldScale.CharacterHeightUnits * 0.5f);
        }

        public static Vector3 ResolveThirdSkyMetalSitePlayerSpawnWorld(CavernBounds bounds)
        {
            if (bounds == null)
                return Vector3.zero;

            Vector2 siteLocal = SkyMetalDigSiteCatalog.GetThirdSiteContentLocalXZ();
            float offset = WorldScale.Feet(ThirdSkyMetalSiteSpawnOffsetFeet);
            var contentLocal = new Vector3(siteLocal.x, 0f, siteLocal.y - offset);
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            contentLocal.y = PlainsWorldBuilder.SamplePlainsLocalY(contentLocal.x, contentLocal.z, plainsBaseY);

            return PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                bounds.transform.TransformPoint(contentLocal),
                WorldScale.CharacterHeightUnits * 0.5f);
        }

        public static Vector3 ResolveQuarry3PlayerSpawnWorld(CavernBounds bounds, Transform guide = null)
        {
            if (bounds == null)
                return Vector3.zero;

            if (guide != null)
            {
                Vector3 spawn = guide.position
                    + guide.forward * WorldScale.Feet(Quarry3PlayerSpawnInFrontOfGuideFeet);
                return PlainsGroundSupport.SnapWorldPointToPlains(
                    bounds,
                    spawn,
                    WorldScale.CharacterHeightUnits * 0.5f);
            }

            var center = GetLandQuarry3Center();
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(center.x, center.y, plainsBaseY);
            var local = new Vector3(
                center.x,
                groundY,
                center.y - WorldScale.Feet(Quarry3PlayerSpawnInFrontOfGuideFeet));
            return PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                bounds.transform.TransformPoint(local),
                WorldScale.CharacterHeightUnits * 0.5f);
        }

        public static Vector3 ResolvePlayerSpawnWorld(CavernBounds bounds)
        {
            if (bounds == null)
                return Vector3.zero;

            if (!SpawnPlayerOnIslandForTesting
                && !SpawnPlayerAtQuarry3ForTesting
                && !SpawnPlayerAtQuarry4ForTesting
                && !SpawnPlayerAtFirstSkyMetalSiteForTesting
                && !SpawnPlayerAtSecondSkyMetalSiteForTesting
                && !SpawnPlayerAtThirdSkyMetalSiteForTesting
                && PlayerSpawnPersistence.HasSavedLandSpawn)
                return PlayerSpawnPersistence.LoadSavedLandSpawn();

            if (SpawnPlayerAtThirdSkyMetalSiteForTesting)
                return ResolveThirdSkyMetalSitePlayerSpawnWorld(bounds);

            if (SpawnPlayerAtSecondSkyMetalSiteForTesting)
                return ResolveSecondSkyMetalSitePlayerSpawnWorld(bounds);

            if (SpawnPlayerAtFirstSkyMetalSiteForTesting)
                return ResolveFirstSkyMetalSitePlayerSpawnWorld(bounds);

            if (SpawnPlayerAtQuarry4ForTesting)
                return ResolveQuarry4PlayerSpawnWorld(bounds);

            if (SpawnPlayerAtQuarry3ForTesting)
                return ResolveQuarry3PlayerSpawnWorld(bounds);

            Vector3 worldPoint;
            if (SpawnPlayerAtJarlLandShopForTesting)
            {
                var quarry = FindQuarry2Root(bounds);
                worldPoint = quarry != null
                    ? quarry.TransformPoint(ResolveJarlLandShopFrontSpawnLocal(quarry))
                    : ResolvePlayerSpawnWorldFromContentLocal(bounds, ResolveBeachApproachSpawnContentLocal());
            }
            else
            {
                Vector3 contentLocal = SpawnPlayerOnIslandForTesting
                    ? ResolveIslandPlayerSpawnContentLocal()
                    : ResolveBeachApproachSpawnContentLocal();
                worldPoint = ResolvePlayerSpawnWorldFromContentLocal(bounds, contentLocal);
            }

            return PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                worldPoint,
                WorldScale.CharacterHeightUnits * 0.5f);
        }

        static Vector3 ResolvePlayerSpawnWorldFromContentLocal(CavernBounds bounds, Vector3 contentLocal)
        {
            Vector3 worldPoint = bounds.transform.TransformPoint(contentLocal);
            if (SpawnPlayerOnIslandForTesting
                && LakeIslandVisualFactory.TrySampleIslandMeshWorldY(
                    contentLocal.x,
                    contentLocal.z,
                    bounds.transform,
                    out float islandSurfaceY))
            {
                worldPoint.y = islandSurfaceY;
            }

            return worldPoint;
        }

        public static Vector3 ResolveJarlLandShopFrontSpawnLocal(Transform quarryRoot)
        {
            if (quarryRoot == null)
                return Vector3.zero;

            var hall = quarryRoot.Find(VikingBuildingVisualFactory.HallObjectName);
            GameObject hallObject = hall != null ? hall.gameObject : null;
            Vector3 shopAnchorLocal = ResolveQuarryShopAnchorLocal(hallObject, quarryRoot);
            Vector3 defaultSpawnLocal = ResolvePlayerSpawnLocal(hallObject, quarryRoot);
            Vector3 toCustomer = defaultSpawnLocal - shopAnchorLocal;
            toCustomer.y = 0f;
            if (toCustomer.sqrMagnitude < 0.001f)
                toCustomer = Vector3.right;
            else
                toCustomer.Normalize();

            return shopAnchorLocal + toCustomer * WorldScale.Feet(JarlLandShopFrontSpawnFeet);
        }

        public static Vector3 ResolveIslandPlayerSpawnContentLocal()
        {
            Vector2 center = LakeCatalog.GetLakeIslandCenterLocal();
            float offset = WorldScale.Feet(IslandPlayerSpawnOffsetFromCenterFeet);
            return new Vector3(center.x, 0f, center.y - offset);
        }

        public static Vector3 ResolveBeachApproachSpawnContentLocal()
        {
            var beachCenter = LakeCatalog.GetBeachCenterContentLocal();
            return new Vector3(
                beachCenter.x - WorldScale.Feet(LandQuarry2BeachApproachSpawnWestOfBeachCenterFeet),
                0f,
                LakeCatalog.GetBeachSouthEdgeZ() - WorldScale.Feet(LandQuarry2BeachApproachSpawnSouthOfSandFeet));
        }

        public static Vector3 ResolveVikingCharacterSpawnWorld(CavernBounds bounds)
        {
            if (bounds == null)
                return Vector3.zero;

            var quarry = FindQuarry2Root(bounds);
            if (quarry == null)
                return Vector3.zero;

            var hall = FindQuarry2Hall(bounds);
            Vector3 characterLocal = ResolveBesideHallLocal(
                hall != null ? hall.gameObject : null,
                quarry,
                VikingCharacterShiftNorthFeet);
            Vector3 worldPoint = quarry.TransformPoint(characterLocal);
            return PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                worldPoint,
                WorldScale.CharacterHeightUnits * 0.5f);
        }

        public static Vector3 ResolveVikingCharacterLocal(GameObject hall, Transform quarryRoot)
        {
            return ResolveBesideHallLocal(hall, quarryRoot, VikingCharacterShiftNorthFeet)
                + new Vector3(WorldScale.Feet(VikingCharacterShiftRightFeet), 0f, 0f);
        }

        public static Vector3 ResolvePlayerSpawnLocal(GameObject hall, Transform quarryRoot)
        {
            return ResolveBesideHallLocal(hall, quarryRoot, LandQuarry2PlayerSpawnShiftNorthFeet);
        }

        public static Vector3 ResolveQuarryShopAnchorLocal(GameObject hall, Transform quarryRoot)
        {
            Vector3 spawnLocal = ResolvePlayerSpawnLocal(hall, quarryRoot);
            return spawnLocal + new Vector3(-WorldScale.Feet(LandQuarry2ShopWestOfSpawnFeet), 0f, 0f);
        }

        static Vector3 ResolveBesideHallLocal(GameObject hall, Transform quarryRoot, float shiftNorthFeet)
        {
            float rightOffset = WorldScale.Feet(VikingCharacterRightOfHallCenterFeet);
            float southOffset = -WorldScale.Feet(VikingCharacterSouthOfHallFaceFeet);

            Vector3 baseLocal;
            if (hall != null && VikingPropVisualFactory.TryGetLocalBounds(hall, quarryRoot, out var hallBounds))
            {
                float centerX = (hallBounds.min.x + hallBounds.max.x) * 0.5f;
                baseLocal = new Vector3(centerX + rightOffset, 0f, hallBounds.min.z + southOffset);
            }
            else
            {
                baseLocal = new Vector3(rightOffset, 0f, southOffset);
            }

            return baseLocal + new Vector3(
                -WorldScale.Feet(VikingCharacterShiftLeftFeet),
                0f,
                WorldScale.Feet(shiftNorthFeet));
        }

        static Transform FindQuarry2Root(CavernBounds bounds)
        {
            if (bounds == null)
                return null;

            var quarry = bounds.transform.Find("CavernContent/JarlLand");
            if (quarry == null)
                quarry = bounds.transform.Find("JarlLand");
            if (quarry == null)
                quarry = bounds.transform.Find("CavernContent/LandQuarry2");
            if (quarry == null)
                quarry = bounds.transform.Find("LandQuarry2");

            return quarry;
        }

        static Transform FindQuarry2Hall(CavernBounds bounds)
        {
            var quarry = FindQuarry2Root(bounds);
            return quarry != null ? quarry.Find(VikingBuildingVisualFactory.HallObjectName) : null;
        }

        public static Vector3 ResolveEdgeSpawnWorld(CavernBounds bounds, float edgeAngleRadians = -Mathf.PI * 0.5f)
        {
            if (bounds == null)
                return Vector3.zero;

            var edgeLocal = LandQuarry2Boundary.GetEdgeLocalPoint(edgeAngleRadians);
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(edgeLocal.x, edgeLocal.y, plainsBaseY);
            var worldPoint = bounds.transform.TransformPoint(new Vector3(edgeLocal.x, groundY, edgeLocal.y));
            return PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                worldPoint,
                WorldScale.CharacterHeightUnits * 0.5f);
        }

        public static Vector3 ResolveCenterWorld(CavernBounds bounds)
        {
            return ResolveQuarryCenterWorld(bounds, LandQuarry2Index);
        }

        public static Vector3 ResolveQuarryCenterWorld(CavernBounds bounds, int quarryIndex)
        {
            if (bounds == null)
                return Vector3.zero;

            var center = GetLandQuarryCenter(quarryIndex);
            float plainsBaseY = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(center.x, center.y, plainsBaseY);
            return bounds.transform.TransformPoint(new Vector3(center.x, groundY, center.y));
        }

        public static Vector3 ResolveQuarryShopRespawnWorld(CavernBounds bounds, int quarryIndex)
        {
            if (bounds == null)
                return Vector3.zero;

            switch (quarryIndex)
            {
                case LandQuarry2Index:
                    return ResolveLandQuarry2ShopRespawnWorld(bounds);
                default:
                    return ResolvePlateauShopRespawnWorld(bounds);
            }
        }

        public static Vector3 ResolveNearDroppedEquipmentSpawnWorld(CavernBounds bounds, Vector3 equipmentCenter)
        {
            if (bounds == null)
                return equipmentCenter;

            Vector2 offset = Random.insideUnitCircle;
            if (offset.sqrMagnitude < 0.01f)
                offset = Vector2.up;
            offset.Normalize();

            Vector3 spawn = equipmentCenter + new Vector3(offset.x, 0f, offset.y) * WorldScale.Feet(10f);
            return PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                spawn,
                WorldScale.CharacterHeightUnits * 0.5f);
        }

        static Vector3 ResolvePlateauShopRespawnWorld(CavernBounds bounds)
        {
            float shopAnchorZ = WorldScale.Feet(WorldScale.ShopDistanceFromSpawnFeet);
            const float counterLocalZ = -1.1f;
            float spawnZ = shopAnchorZ + counterLocalZ - WorldScale.Feet(5f);

            Vector3 worldPoint;
            if (bounds.TryResolveFloorWorldPoint(0f, spawnZ, out Vector3 floorPoint))
                worldPoint = floorPoint;
            else
                worldPoint = bounds.transform.TransformPoint(new Vector3(0f, bounds.SampleFloorWorldY(0f, spawnZ), spawnZ));

            return PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                worldPoint,
                WorldScale.CharacterHeightUnits * 0.5f);
        }

        static Vector3 ResolveLandQuarry2ShopRespawnWorld(CavernBounds bounds)
        {
            var quarry = FindQuarry2Root(bounds);
            if (quarry == null)
                return ResolvePlateauShopRespawnWorld(bounds);

            var hall = FindQuarry2Hall(bounds);
            Vector3 spawnLocal = ResolvePlayerSpawnLocal(hall != null ? hall.gameObject : null, quarry);
            Vector3 worldPoint = quarry.TransformPoint(spawnLocal);
            return PlainsGroundSupport.SnapWorldPointToPlains(
                bounds,
                worldPoint,
                WorldScale.CharacterHeightUnits * 0.5f);
        }

        static Vector2 GetTruckLocal(GameContext ctx)
        {
            if (ctx?.PlayerTruck == null || ctx.CavernBounds == null)
                return Vector2.zero;

            Vector3 local = ctx.CavernBounds.transform.InverseTransformPoint(ctx.PlayerTruck.transform.position);
            return new Vector2(local.x, local.z);
        }
    }
}
