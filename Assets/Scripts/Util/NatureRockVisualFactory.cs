using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Util
{
    public static class NatureRockVisualFactory
    {
        const string Rock5ResourcePath = "Models/Props/nature_rock5";
        public const string Rock5ObjectName = "NatureRock5";
        const float InterlockOverlapFactor = 0.85f;
        const float PairYawDegrees = 0f;
        const float PairBuryFeet = 5f;

        // Rock5 prefab-local lobe directions (degrees from +X toward +Z).
        const float Rock5LargeLobeYawDeg = 310f;
        const float Rock5SmallLobeYawDeg = 70f;
        const float NorthYawDeg = 0f;
        const float SouthYawDeg = 180f;
        const float PeakPinRotateDegrees = 30f;

        public static GameObject CreateInterlockedRock5Pair(
            Transform parent,
            Vector3 anchorLocal,
            float floorWorldY,
            float scale = 2f)
        {
            var groupGo = new GameObject("NatureRock5Pair");
            var group = groupGo.transform;
            group.SetParent(parent, false);
            group.localPosition = anchorLocal;

            var rockNorth = CreateRock5InGroup(
                group,
                Vector3.zero,
                Quaternion.Euler(0f, NorthYawDeg, 0f),
                scale);
            if (rockNorth != null)
                rockNorth.name = "NatureRock5_N";

            var rockSouth = CreateRock5InGroup(
                group,
                Vector3.zero,
                Quaternion.Euler(0f, SouthYawDeg, 0f),
                scale);
            if (rockSouth != null)
            {
                rockSouth.name = "NatureRock5_S";
                rockSouth.transform.localPosition = ComputeSouthernInterlockOffset(group, rockNorth, rockSouth);
            }

            RotateAroundPeakPin(group, rockNorth, PeakPinRotateDegrees);
            RotateAroundPeakPin(group, rockSouth, PeakPinRotateDegrees);

            group.localRotation = Quaternion.Euler(0f, PairYawDegrees, 0f);
            BuryPairBottom(groupGo);

            NatureRockPairCollisionBuilder.BuildPairCollision(groupGo);
            if (groupGo.GetComponent<Quarry3RockPairSolid>() == null)
                groupGo.AddComponent<Quarry3RockPairSolid>();
            Physics.SyncTransforms();
            return groupGo;
        }

        static GameObject CreateRock5InGroup(
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            float scale)
        {
            var prefab = Resources.Load<GameObject>(Rock5ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"Monster Miner: rock prefab not found at Resources/{Rock5ResourcePath}. "
                    + "Copy NaturePack/Prefabs/Rock/Rock5 into Assets/Resources/Models/Props/nature_rock5.prefab.");
                return null;
            }

            var rock = Object.Instantiate(prefab, parent, false);
            rock.name = Rock5ObjectName;
            rock.transform.localRotation = localRotation;
            rock.transform.localScale = Vector3.one * scale;
            rock.transform.localPosition = localPosition;
            KnifeVisualFactory.ApplyUrpMaterials(rock);
            return rock;
        }

        static void BuryPairBottom(GameObject pairRoot)
        {
            if (pairRoot == null)
                return;

            Physics.SyncTransforms();
            float groundY = FloorAnchor.ResolveFloorSurfaceY(pairRoot.transform.position);
            float bottomY = FloorAnchor.GetVisualBottomY(pairRoot);
            float targetBottomY = groundY - WorldScale.Feet(PairBuryFeet);
            pairRoot.transform.position += Vector3.up * (targetBottomY - bottomY);
            Physics.SyncTransforms();
        }

        static Vector3 ComputeSouthernInterlockOffset(
            Transform group,
            GameObject rockNorth,
            GameObject rockSouth)
        {
            if (rockNorth == null || rockSouth == null)
                return new Vector3(0f, 0f, -WorldScale.Feet(3f));

            Physics.SyncTransforms();
            if (!TryGetRendererBounds(rockNorth, out var northBounds)
                || !TryGetRendererBounds(rockSouth, out var southBounds))
            {
                return new Vector3(0f, 0f, -WorldScale.Feet(3f));
            }

            Vector3 northCenter = group.InverseTransformPoint(northBounds.center);
            Vector3 southCenter = group.InverseTransformPoint(southBounds.center);

            Vector3 northLargeDir = YawToXZ(Rock5LargeLobeYawDeg + NorthYawDeg);
            Vector3 northSmallDir = YawToXZ(Rock5SmallLobeYawDeg + NorthYawDeg);
            Vector3 southLargeDir = YawToXZ(Rock5LargeLobeYawDeg + SouthYawDeg);
            Vector3 southSmallDir = YawToXZ(Rock5SmallLobeYawDeg + SouthYawDeg);

            float northLargeReach = GetReachAlongDirection(group, northBounds, northLargeDir);
            float northSmallReach = GetReachAlongDirection(group, northBounds, northSmallDir);
            float southLargeReach = GetReachAlongDirection(group, southBounds, southLargeDir);
            float southSmallReach = GetReachAlongDirection(group, southBounds, southSmallDir);

            // Panel 2: north large covers south small; south large covers north small.
            Vector3 northLargeCoversSouthSmall =
                northLargeDir * northLargeReach - southSmallDir * southSmallReach;
            Vector3 southLargeCoversNorthSmall =
                northSmallDir * northSmallReach - southLargeDir * southLargeReach;
            Vector3 coverOffset =
                (northLargeCoversSouthSmall + southLargeCoversNorthSmall) * (0.5f * InterlockOverlapFactor);
            coverOffset.y = 0f;

            return northCenter - southCenter + coverOffset;
        }

        static void RotateAroundPeakPin(Transform group, GameObject rock, float degrees)
        {
            if (rock == null)
                return;

            Physics.SyncTransforms();
            Vector3 pin = ComputePeakPin(group, rock);
            RotateAroundGroupLocalY(rock.transform, pin, degrees);
        }

        static Vector3 ComputePeakPin(Transform group, GameObject rock)
        {
            if (!TryGetRendererBounds(rock, out var bounds))
                return rock.transform.localPosition;

            float yaw = rock.transform.localEulerAngles.y;
            Vector3 largeDir = YawToXZ(Rock5LargeLobeYawDeg + yaw);
            float largeReach = GetReachAlongDirection(group, bounds, largeDir);

            Vector3 baseCenter = group.InverseTransformPoint(
                new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            Vector3 top = group.InverseTransformPoint(bounds.max);

            return new Vector3(
                baseCenter.x + largeDir.x * largeReach * 0.82f,
                top.y,
                baseCenter.z + largeDir.z * largeReach * 0.82f);
        }

        static void RotateAroundGroupLocalY(Transform target, Vector3 pivotGroupLocal, float degrees)
        {
            Quaternion delta = Quaternion.Euler(0f, degrees, 0f);
            Vector3 offset = target.localPosition - pivotGroupLocal;
            target.localRotation = delta * target.localRotation;
            target.localPosition = pivotGroupLocal + delta * offset;
        }

        static Vector3 YawToXZ(float yawDeg)
        {
            float rad = yawDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
        }

        static float GetReachAlongDirection(Transform group, Bounds worldBounds, Vector3 localDirection)
        {
            localDirection.Normalize();
            Vector3 center = group.InverseTransformPoint(worldBounds.center);
            Vector3 extents = worldBounds.extents;
            Vector3 worldCenter = worldBounds.center;

            GetWorldAxisCorners(worldCenter, extents, out Vector3 c0, out Vector3 c1, out Vector3 c2, out Vector3 c3,
                out Vector3 c4, out Vector3 c5, out Vector3 c6, out Vector3 c7);

            float reach = float.MinValue;
            AccumulateReach(group.InverseTransformPoint(c0), center, localDirection, ref reach);
            AccumulateReach(group.InverseTransformPoint(c1), center, localDirection, ref reach);
            AccumulateReach(group.InverseTransformPoint(c2), center, localDirection, ref reach);
            AccumulateReach(group.InverseTransformPoint(c3), center, localDirection, ref reach);
            AccumulateReach(group.InverseTransformPoint(c4), center, localDirection, ref reach);
            AccumulateReach(group.InverseTransformPoint(c5), center, localDirection, ref reach);
            AccumulateReach(group.InverseTransformPoint(c6), center, localDirection, ref reach);
            AccumulateReach(group.InverseTransformPoint(c7), center, localDirection, ref reach);
            return Mathf.Max(0f, reach);
        }

        static void AccumulateReach(Vector3 corner, Vector3 center, Vector3 direction, ref float reach)
        {
            float projection = Vector3.Dot(corner - center, direction);
            if (projection > reach)
                reach = projection;
        }

        static void GetWorldAxisCorners(
            Vector3 center,
            Vector3 extents,
            out Vector3 c0,
            out Vector3 c1,
            out Vector3 c2,
            out Vector3 c3,
            out Vector3 c4,
            out Vector3 c5,
            out Vector3 c6,
            out Vector3 c7)
        {
            Vector3 e = extents;
            c0 = center + new Vector3(-e.x, -e.y, -e.z);
            c1 = center + new Vector3(e.x, -e.y, -e.z);
            c2 = center + new Vector3(-e.x, -e.y, e.z);
            c3 = center + new Vector3(e.x, -e.y, e.z);
            c4 = center + new Vector3(-e.x, e.y, -e.z);
            c5 = center + new Vector3(e.x, e.y, -e.z);
            c6 = center + new Vector3(-e.x, e.y, e.z);
            c7 = center + new Vector3(e.x, e.y, e.z);
        }

        static void ConfigureRockCollision(GameObject rock)
        {
            if (rock == null)
                return;

            NatureRockCollisionBuilder.BuildSolidCollision(rock);
            if (rock.GetComponent<Quarry3RockSolid>() == null)
                rock.AddComponent<Quarry3RockSolid>();
            Physics.SyncTransforms();
        }

        // Pair collision is built on NatureRock5Pair via NatureRockPairCollisionBuilder.

        public static GameObject CreateRock5AtLocalPoint(
            Transform parent,
            Vector3 localPosition,
            float floorWorldY,
            Quaternion localRotation,
            float scale = 1f)
        {
            var prefab = Resources.Load<GameObject>(Rock5ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"Monster Miner: rock prefab not found at Resources/{Rock5ResourcePath}. "
                    + "Copy NaturePack/Prefabs/Rock/Rock5 into Assets/Resources/Models/Props/nature_rock5.prefab.");
                return null;
            }

            var rock = Object.Instantiate(prefab, parent, false);
            rock.name = Rock5ObjectName;
            rock.transform.localRotation = localRotation;
            rock.transform.localScale = Vector3.one * scale;
            rock.transform.localPosition = localPosition;
            KnifeVisualFactory.ApplyUrpMaterials(rock);
            AlignBaseCenterToLocalPoint(rock, parent, localPosition);
            FloorAnchor.SnapBottomToFloor(rock, floorWorldY);
            return rock;
        }

        static void AlignBaseCenterToLocalPoint(GameObject rock, Transform parent, Vector3 targetLocal)
        {
            Physics.SyncTransforms();
            if (!TryGetRendererBounds(rock, out var bounds))
            {
                rock.transform.localPosition = targetLocal;
                return;
            }

            Vector3 baseCenterLocal = parent.InverseTransformPoint(
                new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            rock.transform.localPosition += targetLocal - baseCenterLocal;
        }

        static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return false;

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled)
                    continue;

                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }
    }
}
