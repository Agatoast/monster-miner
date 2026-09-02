using System.Collections.Generic;
using MonsterMiner.Player;
using MonsterMiner.World;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonsterMiner.Util
{
    public static class KnarrVisualFactory
    {
        const string BoatPrefabEditorPath =
            "Assets/CrowAssets/Assets/Stylized Sailing Boat Set/URP/Prefabs/SailBoat.prefab";
        const string BoatPrefabResourcePath = "Models/Vehicles/sail_boat";
        const float BoatShoreInsetFromWaterlineFeet = -18f;
        const float BoatUniformScale = 2f;
        const float HullRestOnGroundLiftFeet = 0.08f;
        const float BoatAdditionalRaiseFeet = -1f; // prior height + 6 in
        const float DeckCargoEntryAftOffsetFeet = 1.4f;
        const float BowViewInsetFromTipFeet = 1.15f;
        const float BowViewProwScreenHeightFraction = 0.2f;
        const float BowViewWaterScreenHeightFraction = 0.5f;
        const float BowViewVerticalFovDegrees = 60f;
        const float BowViewBowRegionLengthFeet = 2.75f;
        const float BowViewSeatedEyeHeightAboveDeckFeet = 3.25f;
        const float BowViewWaterLookAheadFeet = 24f;
        const float BowViewProwFramingEyeDropFeet = 1.05f;
        const float BowViewProwFramingForwardFeet = 0.35f;
        const float DeckSeatBoardInsetFeet = 0.04f;
        const float DeckOutlineSliceAboveSeatFeet = 0.03f;
        const float DeckPolygonInsetFeet = 0.1f;
        const float DeckHullInteriorInsetRatioX = 0.06f;
        const float DeckHullInteriorInsetRatioZ = 0.06f;
        const float DeckSliceMinAreaRatio = 0.10f;
        const float DeckSliceMaxAreaRatio = 0.72f;
        const float DeckSliceFallbackHullHeightRatio = 0.28f;
        const float DeckVisualThicknessFeet = 0.08f;
        const float DeckSurfaceClearanceFeet = 0.04f;
        const float DeckAdditionalRaiseFeet = 1f;
        const float DeckGridCellFeet = 0.1f;
        const int DeckRenderQueue = 3100;
        const float BeachBoatOcclusionBlockWidthFeet = 10f;
        const float BeachBoatOcclusionBlockLengthFeet = 10f;
        const float BeachBoatOcclusionRampNorthHeightFeet = 2f;
        const float BeachBoatOcclusionBlockNorthOffsetFeet = 1f;
        static readonly Color BoatWalkDeckColor = new Color(1f, 1f, 207f / 255f, 1f); // #FFFFCF
        static readonly Color BeachBoatOcclusionWoodColor = new Color(0.52f, 0.34f, 0.18f);

        public static GameObject CreateAtBeach(Transform contentRoot)
        {
            var prefab = LoadBoatPrefab();
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"Monster Miner: stylized sailboat prefab not found at Resources/{BoatPrefabResourcePath}.");
                return null;
            }

            if (contentRoot == null)
                return null;

            var beachCenter = LakeCatalog.GetBeachCenterContentLocal();
            float waterlineZ = LakeCatalog.GetSandWaterlineContentZ(beachCenter.x);
            float boatContentZ = waterlineZ - WorldScale.Feet(BoatShoreInsetFromWaterlineFeet);
            float plainsBase = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float groundY = PlainsWorldBuilder.SamplePlainsLocalY(beachCenter.x, boatContentZ, plainsBase);
            Vector3 contentLocalPosition = new Vector3(beachCenter.x, groundY, boatContentZ);

            var boat = Object.Instantiate(prefab, contentRoot, false);
            boat.name = "WarrensonsBoat";
            boat.transform.localScale = Vector3.one * BoatUniformScale;
            boat.transform.localPosition = contentLocalPosition;
            StripImportedColliders(boat);
            AlignBoatBowToNorth(boat, contentRoot);
            AlignHullBottomToGround(boat, contentRoot, groundY, boatContentZ, beachCenter.x);
            Color deckColor = SampleBoatDeckColor(boat);
            ConfigureDriveable(boat, contentRoot, deckColor);
            EnsureDeckRendersAboveHull(boat);
            CreateBeachBoatOcclusionBlock(contentRoot, beachCenter.x, waterlineZ);
            return boat;
        }

        static void CreateBeachBoatOcclusionBlock(Transform contentRoot, float alignContentX, float waterlineContentZ)
        {
            if (contentRoot == null)
                return;

            float halfLength = WorldScale.Feet(BeachBoatOcclusionBlockLengthFeet * 0.5f);
            float blockContentZ = waterlineContentZ - halfLength + WorldScale.Feet(BeachBoatOcclusionBlockNorthOffsetFeet);
            float plainsBase = PlainsWorldBuilder.GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float sandLocalY = PlainsWorldBuilder.SamplePlainsLocalY(alignContentX, blockContentZ, plainsBase);
            float halfWidth = WorldScale.Feet(BeachBoatOcclusionBlockWidthFeet * 0.5f);
            float northHeight = WorldScale.Feet(BeachBoatOcclusionRampNorthHeightFeet);

            var rampGo = new GameObject("LakeBeachBoatOcclusionBlock");
            rampGo.transform.SetParent(contentRoot, false);
            rampGo.transform.localPosition = new Vector3(alignContentX, sandLocalY, blockContentZ);
            rampGo.transform.localRotation = Quaternion.identity;
            rampGo.transform.localScale = Vector3.one;

            var meshFilter = rampGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = BuildBeachBoatOcclusionRampMesh(halfWidth, halfLength, northHeight);

            var meshRenderer = rampGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = PrimitiveFactory.CreateColorMaterial(BeachBoatOcclusionWoodColor);
            meshRenderer.shadowCastingMode = ShadowCastingMode.On;
            meshRenderer.receiveShadows = true;

            var meshCollider = rampGo.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
        }

        static Mesh BuildBeachBoatOcclusionRampMesh(float halfWidth, float halfLength, float northHeight)
        {
            var mesh = new Mesh { name = "BeachBoatOcclusionRamp" };
            Vector3[] vertices =
            {
                new Vector3(-halfWidth, 0f, -halfLength),
                new Vector3(halfWidth, 0f, -halfLength),
                new Vector3(halfWidth, 0f, halfLength),
                new Vector3(-halfWidth, 0f, halfLength),
                new Vector3(-halfWidth, northHeight, halfLength),
                new Vector3(halfWidth, northHeight, halfLength),
            };

            int[] triangles =
            {
                0, 2, 1,
                0, 3, 2,
                0, 5, 1,
                0, 4, 5,
                3, 4, 5,
                3, 5, 2,
                0, 3, 4,
                1, 5, 2,
            };

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static bool TryGetSeatBoardTopLocalY(GameObject boat, Transform boatRoot, out float topLocalY)
        {
            topLocalY = float.NegativeInfinity;
            bool found = false;

            foreach (MeshFilter meshFilter in boat.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                if (TryGetSeatBoardSubmeshTopLocalY(meshFilter, boatRoot, out float submeshTop)
                    && submeshTop > topLocalY)
                {
                    topLocalY = submeshTop;
                    found = true;
                }
            }

            foreach (Renderer renderer in boat.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || IsWalkDeckRenderer(renderer))
                    continue;

                if (!IsSeatBoardRenderer(renderer))
                    continue;

                float maxY = boatRoot.InverseTransformPoint(renderer.bounds.max).y;
                if (maxY > topLocalY)
                {
                    topLocalY = maxY;
                    found = true;
                }
            }

            return found;
        }

        static bool TryGetSeatBoardSubmeshTopLocalY(MeshFilter meshFilter, Transform boatRoot, out float topLocalY)
        {
            topLocalY = float.NegativeInfinity;
            var renderer = meshFilter.GetComponent<MeshRenderer>();
            if (renderer == null || meshFilter.sharedMesh == null)
                return false;

            Material[] materials = renderer.sharedMaterials;
            Mesh mesh = EnsureReadableMesh(meshFilter);
            if (mesh == null)
                return false;

            Vector3[] vertices = mesh.vertices;
            Transform meshTransform = meshFilter.transform;
            bool found = false;

            for (int submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++)
            {
                Material material = submeshIndex < materials.Length ? materials[submeshIndex] : null;
                if (!IsSeatBoardMaterial(material))
                    continue;

                int[] triangles = mesh.GetTriangles(submeshIndex);
                for (int i = 0; i < triangles.Length; i++)
                {
                    Vector3 local = boatRoot.InverseTransformPoint(
                        meshTransform.TransformPoint(vertices[triangles[i]]));
                    if (local.y <= topLocalY)
                        continue;

                    topLocalY = local.y;
                    found = true;
                }
            }

            return found;
        }

        static bool IsSeatBoardRenderer(Renderer renderer)
        {
            if (renderer == null)
                return false;

            string objectName = renderer.gameObject.name;
            if (objectName.Equals("Floor", System.StringComparison.OrdinalIgnoreCase)
                || objectName.IndexOf("Plank", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (IsSeatBoardMaterial(materials[i]))
                    return true;
            }

            return false;
        }

        static bool IsSeatBoardMaterial(Material material)
        {
            if (material == null)
                return false;

            string materialName = material.name;
            if (materialName.IndexOf("Floor", System.StringComparison.OrdinalIgnoreCase) >= 0
                || materialName.IndexOf("Plank", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

#if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(material);
            if (!string.IsNullOrEmpty(assetPath)
                && assetPath.IndexOf("Floor.mat", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
#endif
            return false;
        }

        static void HideImportedSeatBoardVisuals(GameObject boat)
        {
            foreach (MeshRenderer renderer in boat.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null || IsWalkDeckRenderer(renderer))
                    continue;

                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    if (IsSeatBoardRenderer(renderer))
                        renderer.enabled = false;
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                if (!RendererUsesSeatBoardMaterial(materials))
                    continue;

                if (RendererUsesOnlySeatBoardMaterials(materials))
                {
                    renderer.enabled = false;
                    continue;
                }

                StripSeatBoardSubmeshes(meshFilter, renderer);
            }
        }

        static Mesh BuildSeatBoardSubmeshMesh(Mesh sourceMesh, Material[] materials)
        {
            if (sourceMesh == null || materials == null)
                return null;

            var triangles = new List<int>();
            for (int submeshIndex = 0; submeshIndex < sourceMesh.subMeshCount; submeshIndex++)
            {
                Material material = submeshIndex < materials.Length ? materials[submeshIndex] : null;
                if (!IsSeatBoardMaterial(material))
                    continue;

                triangles.AddRange(sourceMesh.GetTriangles(submeshIndex));
            }

            if (triangles.Count == 0)
                return null;

            var mesh = new Mesh { name = sourceMesh.name + "_SeatBoardWalk" };
            mesh.vertices = sourceMesh.vertices;
            if (sourceMesh.normals != null && sourceMesh.normals.Length == sourceMesh.vertexCount)
                mesh.normals = sourceMesh.normals;
            if (sourceMesh.uv != null && sourceMesh.uv.Length == sourceMesh.vertexCount)
                mesh.uv = sourceMesh.uv;
            if (sourceMesh.tangents != null && sourceMesh.tangents.Length == sourceMesh.vertexCount)
                mesh.tangents = sourceMesh.tangents;
            mesh.SetTriangles(triangles, 0, false);
            mesh.RecalculateBounds();
            if (mesh.normals == null || mesh.normals.Length == 0)
                mesh.RecalculateNormals();
            return mesh;
        }

        static bool RendererUsesSeatBoardMaterial(Material[] materials)
        {
            if (materials == null)
                return false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (IsSeatBoardMaterial(materials[i]))
                    return true;
            }

            return false;
        }

        static bool RendererUsesOnlySeatBoardMaterials(Material[] materials)
        {
            if (materials == null || materials.Length == 0)
                return false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (!IsSeatBoardMaterial(materials[i]))
                    return false;
            }

            return true;
        }

        static void StripSeatBoardSubmeshes(MeshFilter meshFilter, MeshRenderer renderer)
        {
            Mesh sourceMesh = EnsureReadableMesh(meshFilter);
            if (sourceMesh == null)
                return;

            Material[] materials = renderer.sharedMaterials;
            int subMeshCount = sourceMesh.subMeshCount;
            var keptTriangles = new List<int[]>();
            var keptMaterials = new List<Material>();

            for (int submeshIndex = 0; submeshIndex < subMeshCount; submeshIndex++)
            {
                Material material = submeshIndex < materials.Length ? materials[submeshIndex] : null;
                if (IsSeatBoardMaterial(material))
                    continue;

                keptTriangles.Add(sourceMesh.GetTriangles(submeshIndex));
                keptMaterials.Add(material);
            }

            if (keptTriangles.Count == subMeshCount)
                return;

            if (keptTriangles.Count == 0)
            {
                renderer.enabled = false;
                return;
            }

            var newMesh = new Mesh { name = sourceMesh.name + "_NoSeatBoards" };
            newMesh.subMeshCount = keptTriangles.Count;
            newMesh.vertices = sourceMesh.vertices;
            if (sourceMesh.normals != null && sourceMesh.normals.Length == sourceMesh.vertexCount)
                newMesh.normals = sourceMesh.normals;
            if (sourceMesh.uv != null && sourceMesh.uv.Length == sourceMesh.vertexCount)
                newMesh.uv = sourceMesh.uv;
            if (sourceMesh.tangents != null && sourceMesh.tangents.Length == sourceMesh.vertexCount)
                newMesh.tangents = sourceMesh.tangents;

            for (int i = 0; i < keptTriangles.Count; i++)
                newMesh.SetTriangles(keptTriangles[i], i, false);

            newMesh.RecalculateBounds();
            meshFilter.mesh = newMesh;
            renderer.sharedMaterials = keptMaterials.ToArray();
        }

        static bool IsWalkDeckRenderer(Renderer renderer)
        {
            if (renderer == null)
                return false;

            string name = renderer.gameObject.name;
            return name == "BoatWalkDeck"
                || name == "BoatDeck"
                || name == "BoatFloorWalkCollider"
                || name == "BoatDeckFloor"
                || name == "BoatDeckCap";
        }

        static GameObject LoadBoatPrefab()
        {
#if UNITY_EDITOR
            var editorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoatPrefabEditorPath);
            if (editorPrefab != null)
                return editorPrefab;
#endif
            return Resources.Load<GameObject>(BoatPrefabResourcePath);
        }

        static void AlignBoatBowToNorth(GameObject boat, Transform contentRoot)
        {
            Transform root = boat.transform;
            Vector3 bowLocal = ResolveBowLocalDirection(boat, root);
            bowLocal.y = 0f;
            if (bowLocal.sqrMagnitude < 0.0001f)
                bowLocal = Vector3.forward;
            else
                bowLocal.Normalize();

            Vector3 northLocal = contentRoot != null
                ? contentRoot.InverseTransformDirection(Vector3.forward)
                : Vector3.forward;
            northLocal.y = 0f;
            if (northLocal.sqrMagnitude < 0.0001f)
                northLocal = Vector3.forward;
            else
                northLocal.Normalize();

            root.localRotation = Quaternion.FromToRotation(bowLocal, northLocal);
        }

        static Vector3 ResolveBowLocalDirection(GameObject boat, Transform boatRoot)
        {
            Transform rudder = FindNamedChildTransform(boat.transform, "TillerRudder");
            if (rudder == null)
                rudder = FindNamedChildTransform(boat.transform, "Rudder");
            if (rudder == null)
                rudder = FindChildTransformContainingName(boat.transform, "Rudder");

            if (rudder != null)
            {
                Vector3 sternLocal = boatRoot.InverseTransformPoint(rudder.position);
                sternLocal.y = 0f;
                if (sternLocal.sqrMagnitude > 0.0001f)
                    return (-sternLocal).normalized;
            }

            if (TryResolveBowFromHullAxis(boat, boatRoot, out Vector3 hullBow))
                return hullBow;

            return Vector3.forward;
        }

        static bool TryResolveBowFromHullAxis(GameObject boat, Transform boatRoot, out Vector3 bowDirection)
        {
            bowDirection = Vector3.forward;
            if (!TryFindHullWoodMeshFilter(boat, out MeshFilter hullFilter) || hullFilter.sharedMesh == null)
                return false;

            Bounds meshBounds = hullFilter.sharedMesh.bounds;
            Transform meshTransform = hullFilter.transform;
            Vector3 longAxisMesh = meshBounds.size.x >= meshBounds.size.z
                ? Vector3.right
                : Vector3.forward;
            Vector3 axisLocal = boatRoot.InverseTransformDirection(meshTransform.TransformDirection(longAxisMesh));
            axisLocal.y = 0f;
            if (axisLocal.sqrMagnitude < 0.0001f)
                return false;

            axisLocal.Normalize();
            Transform sternMarker = FindChildTransformContainingName(boat.transform, "Rudder")
                ?? FindNamedChildTransform(boat.transform, "TillerRudder");
            if (sternMarker != null)
            {
                Vector3 sternLocal = boatRoot.InverseTransformPoint(sternMarker.position);
                sternLocal.y = 0f;
                if (sternLocal.sqrMagnitude > 0.0001f)
                {
                    bowDirection = Vector3.Dot(sternLocal, axisLocal) >= 0f ? -axisLocal : axisLocal;
                    return true;
                }
            }

            bowDirection = Vector3.Dot(axisLocal, Vector3.forward) >= 0f ? axisLocal : -axisLocal;
            return true;
        }

        static Transform FindChildTransformContainingName(Transform root, string namePart)
        {
            if (root == null || string.IsNullOrEmpty(namePart))
                return null;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != null
                    && child.gameObject.name.IndexOf(namePart, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return child;
                }
            }

            return null;
        }

        static Transform FindNamedChildTransform(Transform root, string objectName)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.gameObject.name == objectName)
                    return child;
            }

            return null;
        }

        static Vector3 ResolveBowViewSeatLocal(
            GameObject boat,
            Transform boatRoot,
            Bounds worldBounds,
            Vector3 bowLocalDirection,
            float deckTopLocalY)
        {
            if (TryResolveBowViewFromHullMesh(boat, boatRoot, bowLocalDirection, deckTopLocalY, out Vector3 seatLocal))
                return seatLocal;

            Vector3[] worldCorners =
            {
                new Vector3(worldBounds.min.x, worldBounds.min.y, worldBounds.min.z),
                new Vector3(worldBounds.min.x, worldBounds.min.y, worldBounds.max.z),
                new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.min.z),
                new Vector3(worldBounds.min.x, worldBounds.max.y, worldBounds.max.z),
                new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.min.z),
                new Vector3(worldBounds.max.x, worldBounds.min.y, worldBounds.max.z),
                new Vector3(worldBounds.max.x, worldBounds.max.y, worldBounds.min.z),
                new Vector3(worldBounds.max.x, worldBounds.max.y, worldBounds.max.z)
            };

            float maxAlongBow = float.NegativeInfinity;
            Vector3 farthestLocal = Vector3.zero;
            for (int i = 0; i < worldCorners.Length; i++)
            {
                Vector3 local = boatRoot.InverseTransformPoint(worldCorners[i]);
                float alongBow = Vector3.Dot(local, bowLocalDirection);
                if (alongBow <= maxAlongBow)
                    continue;

                maxAlongBow = alongBow;
                farthestLocal = local;
            }

            Vector3 fallbackSeat = farthestLocal - bowLocalDirection * WorldScale.Feet(BowViewInsetFromTipFeet);
            fallbackSeat.y = deckTopLocalY + WorldScale.Feet(BowViewSeatedEyeHeightAboveDeckFeet);
            return fallbackSeat;
        }

        static bool TryResolveBowViewFromHullMesh(
            GameObject boat,
            Transform boatRoot,
            Vector3 bowLocalDirection,
            float deckTopLocalY,
            out Vector3 seatLocal)
        {
            seatLocal = default;
            if (boat == null || boatRoot == null || bowLocalDirection.sqrMagnitude < 0.0001f)
                return false;

            bowLocalDirection.Normalize();
            if (!TryFindHullWoodMeshFilter(boat, out MeshFilter hullFilter))
                return false;

            Mesh hullMesh = EnsureReadableMesh(hullFilter);
            if (hullMesh == null)
                return false;

            float bowTipAlong = float.NegativeInfinity;
            SampleMeshMaxAlongBow(hullFilter, hullMesh, boatRoot, bowLocalDirection, ref bowTipAlong);

            MeshFilter frameFilter = FindNamedChildMeshFilter(boat, "Frame");
            if (frameFilter != null && frameFilter.sharedMesh != null)
            {
                Mesh frameMesh = EnsureReadableMesh(frameFilter);
                if (frameMesh != null)
                    SampleMeshMaxAlongBow(frameFilter, frameMesh, boatRoot, bowLocalDirection, ref bowTipAlong);
            }

            if (float.IsNegativeInfinity(bowTipAlong))
                return false;

            float regionStart = bowTipAlong - WorldScale.Feet(BowViewBowRegionLengthFeet);
            float gunwaleLocalY = float.NegativeInfinity;
            Vector3 positionSum = Vector3.zero;
            int positionCount = 0;

            AccumulateBowRegionSamples(
                hullFilter,
                hullMesh,
                boatRoot,
                bowLocalDirection,
                regionStart,
                ref gunwaleLocalY,
                ref positionSum,
                ref positionCount);

            if (frameFilter != null && frameFilter.sharedMesh != null)
            {
                Mesh frameMesh = frameFilter.mesh != null ? frameFilter.mesh : frameFilter.sharedMesh;
                AccumulateBowRegionSamples(
                    frameFilter,
                    frameMesh,
                    boatRoot,
                    bowLocalDirection,
                    regionStart,
                    ref gunwaleLocalY,
                    ref positionSum,
                    ref positionCount);
            }

            if (positionCount == 0 || float.IsNegativeInfinity(gunwaleLocalY))
                return false;

            Vector3 bowCentroid = positionSum / positionCount;
            seatLocal = bowCentroid - bowLocalDirection * WorldScale.Feet(BowViewInsetFromTipFeet);
            seatLocal.y = deckTopLocalY + WorldScale.Feet(BowViewSeatedEyeHeightAboveDeckFeet);
            return true;
        }

        static float ComputeBowViewPitchDegrees(
            Vector3 eyeLocal,
            Vector3 bowLocalDirection,
            GameObject boat,
            Transform boatRoot,
            float deckTopLocalY)
        {
            float pitchForProw = 8f;
            if (TryResolveProwTipLocal(boat, boatRoot, bowLocalDirection, out Vector3 prowTipLocal))
            {
                pitchForProw = ComputePitchForTargetScreenHeight(
                    eyeLocal,
                    prowTipLocal,
                    bowLocalDirection,
                    BowViewProwScreenHeightFraction);
            }

            Vector3 waterLookPoint = eyeLocal + bowLocalDirection * WorldScale.Feet(BowViewWaterLookAheadFeet);
            waterLookPoint.y = ResolveBowWaterSurfaceLocalY(boat, boatRoot, deckTopLocalY);
            float pitchForWater = ComputePitchForTargetScreenHeight(
                eyeLocal,
                waterLookPoint,
                bowLocalDirection,
                BowViewWaterScreenHeightFraction);

            float pitch = Mathf.Lerp(pitchForProw, pitchForWater, 0.62f);
            return Mathf.Clamp(pitch, -35f, 28f);
        }

        static float ComputePitchForTargetScreenHeight(
            Vector3 eyeLocal,
            Vector3 targetLocal,
            Vector3 bowLocalDirection,
            float screenHeightFraction)
        {
            Vector3 offset = targetLocal - eyeLocal;
            float forward = Vector3.Dot(offset, bowLocalDirection);
            if (forward <= WorldScale.Feet(0.1f))
                return 0f;

            float angleToTarget = Mathf.Atan2(offset.y, forward) * Mathf.Rad2Deg;
            float halfFovRad = BowViewVerticalFovDegrees * 0.5f * Mathf.Deg2Rad;
            float normalizedY = (screenHeightFraction - 0.5f) / 0.5f;
            float targetViewAngle = Mathf.Atan(Mathf.Tan(halfFovRad) * normalizedY) * Mathf.Rad2Deg;

            // Positive helm pitch looks downward, which raises forward content on screen.
            return targetViewAngle - angleToTarget;
        }

        static float ResolveBowWaterSurfaceLocalY(
            GameObject boat,
            Transform boatRoot,
            float deckTopLocalY)
        {
            if (TryFindHullWoodMeshFilter(boat, out MeshFilter hullFilter)
                && TryGetMeshFilterBoatRootBounds(hullFilter, boatRoot, out Vector3 minLocal, out Vector3 maxLocal))
            {
                return minLocal.y + (maxLocal.y - minLocal.y) * 0.18f;
            }

            return deckTopLocalY - WorldScale.Feet(2.5f);
        }

        static bool TryResolveProwTipLocal(
            GameObject boat,
            Transform boatRoot,
            Vector3 bowLocalDirection,
            out Vector3 prowTipLocal)
        {
            prowTipLocal = default;
            if (boat == null || boatRoot == null || bowLocalDirection.sqrMagnitude < 0.0001f)
                return false;

            bowLocalDirection.Normalize();
            float bestAlong = float.NegativeInfinity;
            Vector3 bestLocal = Vector3.zero;
            bool found = false;

            if (TryFindHullWoodMeshFilter(boat, out MeshFilter hullFilter))
            {
                Mesh hullMesh = EnsureReadableMesh(hullFilter);
                if (hullMesh != null)
                    SampleMeshFarthestBowPoint(hullFilter, hullMesh, boatRoot, bowLocalDirection, ref bestAlong, ref bestLocal, ref found);
            }

            MeshFilter frameFilter = FindNamedChildMeshFilter(boat, "Frame");
            if (frameFilter != null && frameFilter.sharedMesh != null)
            {
                Mesh frameMesh = EnsureReadableMesh(frameFilter);
                if (frameMesh != null)
                    SampleMeshFarthestBowPoint(frameFilter, frameMesh, boatRoot, bowLocalDirection, ref bestAlong, ref bestLocal, ref found);
            }

            if (!found)
                return false;

            prowTipLocal = bestLocal;
            return true;
        }

        static void SampleMeshFarthestBowPoint(
            MeshFilter meshFilter,
            Mesh mesh,
            Transform boatRoot,
            Vector3 bowLocalDirection,
            ref float bestAlong,
            ref Vector3 bestLocal,
            ref bool found)
        {
            if (meshFilter == null || mesh == null || boatRoot == null)
                return;

            Transform meshTransform = meshFilter.transform;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 local = boatRoot.InverseTransformPoint(meshTransform.TransformPoint(vertices[i]));
                float alongBow = Vector3.Dot(local, bowLocalDirection);
                if (alongBow > bestAlong + 0.0001f
                    || (Mathf.Abs(alongBow - bestAlong) <= 0.0001f && local.y > bestLocal.y))
                {
                    bestAlong = alongBow;
                    bestLocal = local;
                    found = true;
                }
            }
        }

        static void SampleMeshMaxAlongBow(
            MeshFilter meshFilter,
            Mesh mesh,
            Transform boatRoot,
            Vector3 bowLocalDirection,
            ref float maxAlongBow)
        {
            if (meshFilter == null || mesh == null || boatRoot == null)
                return;

            Transform meshTransform = meshFilter.transform;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 local = boatRoot.InverseTransformPoint(meshTransform.TransformPoint(vertices[i]));
                float alongBow = Vector3.Dot(local, bowLocalDirection);
                if (alongBow > maxAlongBow)
                    maxAlongBow = alongBow;
            }
        }

        static void AccumulateBowRegionSamples(
            MeshFilter meshFilter,
            Mesh mesh,
            Transform boatRoot,
            Vector3 bowLocalDirection,
            float regionStartAlongBow,
            ref float gunwaleLocalY,
            ref Vector3 positionSum,
            ref int positionCount)
        {
            if (meshFilter == null || mesh == null || boatRoot == null)
                return;

            Transform meshTransform = meshFilter.transform;
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 local = boatRoot.InverseTransformPoint(meshTransform.TransformPoint(vertices[i]));
                float alongBow = Vector3.Dot(local, bowLocalDirection);
                if (alongBow < regionStartAlongBow)
                    continue;

                if (local.y > gunwaleLocalY)
                    gunwaleLocalY = local.y;

                positionSum += local;
                positionCount++;
            }
        }

        static MeshFilter FindNamedChildMeshFilter(GameObject root, string objectName)
        {
            if (root == null || string.IsNullOrEmpty(objectName))
                return null;

            foreach (MeshFilter meshFilter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter != null
                    && meshFilter.gameObject.name.Equals(objectName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return meshFilter;
                }
            }

            return null;
        }

        static void AlignHullBottomToGround(
            GameObject boat,
            Transform contentRoot,
            float groundContentLocalY,
            float contentZ,
            float contentX)
        {
            if (!TryGetRendererBounds(boat, out var bounds))
                return;

            float targetGroundWorldY = contentRoot.TransformPoint(new Vector3(
                    contentX,
                    groundContentLocalY,
                    contentZ))
                .y
                + WorldScale.Feet(HullRestOnGroundLiftFeet + BoatAdditionalRaiseFeet);
            float lift = targetGroundWorldY - bounds.min.y;
            if (Mathf.Abs(lift) > 0.0001f)
                boat.transform.position += new Vector3(0f, lift, 0f);
        }

        static void StripImportedColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.Destroy(collider);
        }

        static void ConfigureDriveable(GameObject boat, Transform contentRoot, Color deckColor)
        {
            if (!TryGetHullRendererBounds(boat, out var bounds)
                && !TryGetRendererBounds(boat, out bounds))
            {
                Debug.LogError("Monster Miner: WarrensonsBoat has no renderers; driveable setup aborted.");
                return;
            }

            Transform root = boat.transform;
            Vector3 minLocal = root.InverseTransformPoint(bounds.min);
            Vector3 maxLocal = root.InverseTransformPoint(bounds.max);
            Vector3 localSize = maxLocal - minLocal;
            localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

            Vector3 bowLocalDirection = ResolveBowLocalDirection(boat, root);
            float deckWalkSurfaceLocalY = ResolveDeckFloorLocalY(boat, root);

            Transform deck = CreateHullFitWalkDeck(
                boat,
                root,
                deckWalkSurfaceLocalY,
                deckColor,
                out List<Vector2> deckPolygonBoatRoot);
            if (deck == null)
            {
                Debug.LogWarning("Monster Miner: hull-fit deck failed; using hull interior fallback deck.");
                deck = CreateHullInteriorFallbackDeck(
                    boat,
                    root,
                    deckWalkSurfaceLocalY,
                    deckColor,
                    out deckPolygonBoatRoot);
            }

            if (deck == null)
            {
                Debug.LogWarning("Monster Miner: hull interior deck failed; using renderer bounds fallback deck.");
                deck = CreateRendererBoundsFallbackDeck(
                    bounds,
                    root,
                    deckWalkSurfaceLocalY,
                    deckColor,
                    out deckPolygonBoatRoot);
            }

            if (deck == null)
            {
                Debug.LogError("Monster Miner: WarrensonsBoat deck could not be created.");
                return;
            }

            HideImportedSeatBoardVisuals(boat);

            List<Vector2> deckBoundaryCargoLocal = ConvertDeckPolygonToCargoLocal(
                deckPolygonBoatRoot,
                root,
                deck,
                deckWalkSurfaceLocalY);

            Vector3 deckCenterLocal = root.InverseTransformPoint(deck.position);
            float deckTopLocalY = deckWalkSurfaceLocalY;
            Vector3 deckHalfExtents = GetDeckHalfExtentsLocal(deck);

            Vector3 helmSeatLocal = ResolveBowViewSeatLocal(
                boat,
                root,
                bounds,
                bowLocalDirection,
                deckTopLocalY);

            Vector3 helmInteractCenter = helmSeatLocal;
            Vector3 helmInteractSize = new Vector3(
                WorldScale.Feet(1.8f),
                WorldScale.Feet(1.6f),
                WorldScale.Feet(2.4f));

            var helm = new GameObject("BoatHelmSeat").transform;
            helm.SetParent(root, false);
            float bowViewPitch = ComputeBowViewPitchDegrees(
                helmSeatLocal,
                bowLocalDirection,
                boat,
                root,
                deckTopLocalY);
            helm.localPosition = helmSeatLocal
                - Vector3.up * WorldScale.Feet(BowViewProwFramingEyeDropFeet)
                + bowLocalDirection * WorldScale.Feet(BowViewProwFramingForwardFeet);
            helm.localRotation = Quaternion.LookRotation(bowLocalDirection, Vector3.up)
                * Quaternion.Euler(bowViewPitch, 0f, 0f);

            var rb = boat.GetComponent<Rigidbody>();
            if (rb == null)
                rb = boat.AddComponent<Rigidbody>();
            rb.mass = 80f;
            rb.linearDamping = 1.2f;
            rb.angularDamping = 3f;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var hullBox = boat.GetComponent<BoxCollider>();
            if (hullBox == null)
                hullBox = boat.AddComponent<BoxCollider>();
            hullBox.isTrigger = true;
            hullBox.center = new Vector3(
                (minLocal.x + maxLocal.x) * 0.5f,
                (minLocal.y + maxLocal.y) * 0.5f,
                (minLocal.z + maxLocal.z) * 0.5f);
            hullBox.size = localSize * 0.92f;

            float localInteractHeight = WorldScale.Feet(1.6f);
            Vector3 deckInteractSize = new Vector3(
                deckHalfExtents.x * 2f,
                localInteractHeight,
                deckHalfExtents.z * 2f);
            AddInteractTrigger<BoatHelmInteract>(root, helmInteractCenter, helmInteractSize);
            AddInteractTrigger<BoatDeckInteract>(
                root,
                deckCenterLocal + new Vector3(0f, WorldScale.Feet(0.8f), 0f),
                deckInteractSize);

            Vector3 cargoEntryLocal = ComputeCargoEntryLocalPosition(
                root,
                deck,
                deckCenterLocal,
                bowLocalDirection);

            Vector3 spawnWorldPosition = root.position;
            Quaternion spawnWorldRotation = root.rotation;

            var driveable = boat.GetComponent<DriveableBoat>();
            if (driveable == null)
                driveable = boat.AddComponent<DriveableBoat>();
            driveable.Initialize(
                deck,
                helm,
                deckHalfExtents,
                0f,
                bowLocalDirection,
                deckBoundaryCargoLocal,
                cargoEntryLocal);

            root.SetPositionAndRotation(spawnWorldPosition, spawnWorldRotation);
            rb.position = spawnWorldPosition;
            rb.rotation = spawnWorldRotation;
            Physics.SyncTransforms();
            driveable.PreserveSpawnPose(spawnWorldPosition, spawnWorldRotation);

            foreach (var helmInteract in boat.GetComponentsInChildren<BoatHelmInteract>(true))
                helmInteract.Initialize(driveable);
            foreach (var deckInteract in boat.GetComponentsInChildren<BoatDeckInteract>(true))
                deckInteract.Initialize(driveable);

            LakeCatalog.SetBoatLaunchWaterlineContentZ(ComputeSternContentZ(boat, contentRoot, bowLocalDirection));
        }

        static float ResolveDeckFloorLocalY(GameObject boat, Transform boatRoot)
        {
            float clearance = WorldScale.Feet(DeckSurfaceClearanceFeet);
            float raise = WorldScale.Feet(DeckAdditionalRaiseFeet);
            if (TryGetSeatBoardTopLocalY(boat, boatRoot, out float seatBoardTopY))
                return seatBoardTopY + clearance + raise;

            if (TryFindHullWoodMeshFilter(boat, out MeshFilter hullFilter)
                && TryGetMeshFilterBoatRootBounds(hullFilter, boatRoot, out Vector3 hullMinLocal, out Vector3 hullMaxLocal))
            {
                float fallbackSeatTop = hullMinLocal.y
                    + (hullMaxLocal.y - hullMinLocal.y) * DeckSliceFallbackHullHeightRatio;
                return fallbackSeatTop + clearance + raise;
            }

            if (TryGetRendererBounds(boat, out Bounds bounds)
                && TryGetWorldBoundsBoatRootBounds(bounds, boatRoot, out Vector3 boundsMinLocal, out Vector3 boundsMaxLocal))
            {
                float fallbackSeatTop = boundsMinLocal.y
                    + (boundsMaxLocal.y - boundsMinLocal.y) * DeckSliceFallbackHullHeightRatio;
                return fallbackSeatTop + clearance + raise;
            }

            return boatRoot.localPosition.y + clearance + raise;
        }

        static Vector3 ComputeCargoEntryLocalPosition(
            Transform boatRoot,
            Transform cargoDeck,
            Vector3 deckCenterLocal,
            Vector3 bowLocalDirection)
        {
            Vector3 entryBoatRoot = deckCenterLocal;
            entryBoatRoot -= bowLocalDirection * WorldScale.Feet(DeckCargoEntryAftOffsetFeet);

            Vector3 entryLocal = cargoDeck.InverseTransformPoint(boatRoot.TransformPoint(entryBoatRoot));
            entryLocal.y = WorldScale.CharacterHeightUnits * 0.5f;
            return entryLocal;
        }

        static void AddInteractTrigger<T>(Transform parent, Vector3 localCenter, Vector3 localSize) where T : Component
        {
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            var trigger = go.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = localCenter;
            trigger.size = localSize;
            go.AddComponent<T>();
        }

        static bool TryFindHullWoodMeshFilter(GameObject boat, out MeshFilter meshFilter)
        {
            meshFilter = null;
            MeshFilter best = null;
            int bestVertexCount = 0;

            foreach (MeshFilter candidate in boat.GetComponentsInChildren<MeshFilter>(true))
            {
                if (candidate.sharedMesh == null)
                    continue;

                string name = candidate.gameObject.name;
                if (name.Equals("Boat", System.StringComparison.OrdinalIgnoreCase))
                {
                    meshFilter = candidate;
                    return true;
                }

                if (IsNonHullBoatMeshName(name))
                    continue;

                int vertexCount = candidate.sharedMesh.vertexCount;
                if (vertexCount > bestVertexCount)
                {
                    best = candidate;
                    bestVertexCount = vertexCount;
                }
            }

            meshFilter = best;
            return meshFilter != null;
        }

        static bool IsNonHullBoatMeshName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            if (name.Equals("Boat", System.StringComparison.OrdinalIgnoreCase)
                || name.Equals("SailBoat", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return name.Equals("Sail", System.StringComparison.OrdinalIgnoreCase)
                || name.IndexOf("Oar", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Rope", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Mast", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Frame", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Rudder", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Tiller", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.Equals("Floor", System.StringComparison.OrdinalIgnoreCase)
                || name.IndexOf("Plank", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool TryGetMeshFilterBounds(MeshFilter meshFilter, out Bounds bounds)
        {
            bounds = default;
            if (meshFilter == null)
                return false;

            var renderer = meshFilter.GetComponent<Renderer>();
            if (renderer != null)
            {
                bounds = renderer.bounds;
                return true;
            }

            if (meshFilter.sharedMesh == null)
                return false;

            bounds = TransformBounds(meshFilter.sharedMesh.bounds, meshFilter.transform);
            return true;
        }

        static Bounds TransformBounds(Bounds localBounds, Transform transform)
        {
            Vector3 center = transform.TransformPoint(localBounds.center);
            Vector3 extents = localBounds.extents;
            Vector3 axisX = transform.TransformVector(new Vector3(extents.x, 0f, 0f));
            Vector3 axisY = transform.TransformVector(new Vector3(0f, extents.y, 0f));
            Vector3 axisZ = transform.TransformVector(new Vector3(0f, 0f, extents.z));
            extents = new Vector3(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
            return new Bounds(center, extents * 2f);
        }

        static bool TryGetMeshFilterBoatRootBounds(
            MeshFilter meshFilter,
            Transform boatRoot,
            out Vector3 minLocal,
            out Vector3 maxLocal)
        {
            minLocal = default;
            maxLocal = default;
            if (meshFilter == null || meshFilter.sharedMesh == null || boatRoot == null)
                return false;

            return TryGetLocalBoundsFromMesh(
                meshFilter.sharedMesh.bounds,
                meshFilter.transform,
                boatRoot,
                out minLocal,
                out maxLocal);
        }

        static bool TryGetWorldBoundsBoatRootBounds(
            Bounds worldBounds,
            Transform boatRoot,
            out Vector3 minLocal,
            out Vector3 maxLocal)
        {
            minLocal = default;
            maxLocal = default;
            if (boatRoot == null)
                return false;

            minLocal = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            maxLocal = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            Vector3 center = worldBounds.center;
            Vector3 extents = worldBounds.extents;
            for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
            {
                Vector3 worldCorner = center + Vector3.Scale(extents, new Vector3(xi, yi, zi));
                Vector3 boatLocalCorner = boatRoot.InverseTransformPoint(worldCorner);
                minLocal = Vector3.Min(minLocal, boatLocalCorner);
                maxLocal = Vector3.Max(maxLocal, boatLocalCorner);
            }

            return maxLocal.x > minLocal.x && maxLocal.z > minLocal.z;
        }

        static bool TryGetLocalBoundsFromMesh(
            Bounds meshBounds,
            Transform meshTransform,
            Transform boatRoot,
            out Vector3 minLocal,
            out Vector3 maxLocal)
        {
            minLocal = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            maxLocal = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            Vector3 center = meshBounds.center;
            Vector3 extents = meshBounds.extents;
            for (int xi = -1; xi <= 1; xi += 2)
            for (int yi = -1; yi <= 1; yi += 2)
            for (int zi = -1; zi <= 1; zi += 2)
            {
                Vector3 meshLocalCorner = center + Vector3.Scale(extents, new Vector3(xi, yi, zi));
                Vector3 worldCorner = meshTransform.TransformPoint(meshLocalCorner);
                Vector3 boatLocalCorner = boatRoot.InverseTransformPoint(worldCorner);
                minLocal = Vector3.Min(minLocal, boatLocalCorner);
                maxLocal = Vector3.Max(maxLocal, boatLocalCorner);
            }

            return maxLocal.x > minLocal.x && maxLocal.z > minLocal.z;
        }

        static float ComputeSternContentZ(GameObject boat, Transform contentRoot, Vector3 bowLocalDirection)
        {
            if (!TryGetRendererBounds(boat, out var bounds))
                return LakeCatalog.GetBeachCenterContentLocal().y;

            Vector3 bowContent = contentRoot.InverseTransformDirection(boat.transform.TransformDirection(bowLocalDirection));
            bowContent.y = 0f;
            if (bowContent.sqrMagnitude < 0.0001f)
                return contentRoot.InverseTransformPoint(bounds.center).z;

            bowContent.Normalize();
            Vector3 sternContent = contentRoot.InverseTransformPoint(bounds.center);
            float minAlongBow = float.PositiveInfinity;
            Vector3[] corners =
            {
                new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)
            };

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 content = contentRoot.InverseTransformPoint(corners[i]);
                float alongBow = Vector3.Dot(content, bowContent);
                if (alongBow >= minAlongBow)
                    continue;

                minAlongBow = alongBow;
                sternContent = content;
            }

            return sternContent.z;
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

        static bool TryGetHullRendererBounds(GameObject boat, out Bounds bounds)
        {
            bounds = default;
            bool found = false;

            foreach (Renderer renderer in boat.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled || IsWalkDeckRenderer(renderer))
                    continue;

                if (ShouldExcludeFromHullBounds(renderer.gameObject.name))
                    continue;

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            return found;
        }

        static bool ShouldExcludeFromHullBounds(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return false;

            return objectName.IndexOf("Sail", System.StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Rope", System.StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Mast", System.StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Oar", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static Mesh EnsureReadableMesh(MeshFilter meshFilter)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return null;

            if (meshFilter.mesh != null && !ReferenceEquals(meshFilter.mesh, meshFilter.sharedMesh))
                return meshFilter.mesh;

            Mesh readable = Object.Instantiate(meshFilter.sharedMesh);
            readable.name = meshFilter.sharedMesh.name + "_Readable";
            meshFilter.mesh = readable;
            return readable;
        }

        static List<Vector2> ConvertDeckPolygonToCargoLocal(
            List<Vector2> polygonBoatRoot,
            Transform boatRoot,
            Transform cargoDeck,
            float floorLocalY)
        {
            if (polygonBoatRoot == null || polygonBoatRoot.Count < 3 || boatRoot == null || cargoDeck == null)
                return null;

            var converted = new List<Vector2>(polygonBoatRoot.Count);
            for (int i = 0; i < polygonBoatRoot.Count; i++)
            {
                Vector3 onBoatRoot = new Vector3(polygonBoatRoot[i].x, floorLocalY, polygonBoatRoot[i].y);
                Vector3 cargoLocal = cargoDeck.InverseTransformPoint(boatRoot.TransformPoint(onBoatRoot));
                converted.Add(new Vector2(cargoLocal.x, cargoLocal.z));
            }

            return converted;
        }

        static Transform CreateHullFitWalkDeck(
            GameObject boat,
            Transform boatRoot,
            float floorLocalY,
            Color deckColor,
            out List<Vector2> deckPolygonBoatRoot)
        {
            deckPolygonBoatRoot = null;
            if (!TryFindHullWoodMeshFilter(boat, out MeshFilter meshFilter))
                return null;

            float deckThickness = WorldScale.Feet(DeckVisualThicknessFeet);
            float walkSurfaceLocalY = floorLocalY;

            if (!TryResolveDeckPolygon(boat, boatRoot, meshFilter, walkSurfaceLocalY, out List<Vector2> polygon))
            {
                Debug.LogWarning("Monster Miner: WarrensonsBoat could not trace hull deck outline.");
                return null;
            }

            return BuildWalkDeckFromPolygon(
                boat,
                boatRoot,
                polygon,
                walkSurfaceLocalY,
                deckThickness,
                deckColor,
                out deckPolygonBoatRoot);
        }

        static Transform CreateHullInteriorFallbackDeck(
            GameObject boat,
            Transform boatRoot,
            float floorLocalY,
            Color deckColor,
            out List<Vector2> deckPolygonBoatRoot)
        {
            deckPolygonBoatRoot = null;
            if (!TryFindHullWoodMeshFilter(boat, out MeshFilter meshFilter))
                return null;

            if (!TryBuildShapedHullDeckPolygon(boat, meshFilter, boatRoot, floorLocalY, out List<Vector2> polygon))
            {
                Debug.LogWarning("Monster Miner: WarrensonsBoat hull slice failed; using inset rectangle fallback.");
                if (!TryBuildHullInteriorFallbackPolygon(boat, meshFilter, boatRoot, floorLocalY, out polygon))
                    return null;
            }

            float deckThickness = WorldScale.Feet(DeckVisualThicknessFeet);
            return BuildWalkDeckFromPolygon(
                boat,
                boatRoot,
                polygon,
                floorLocalY,
                deckThickness,
                deckColor,
                out deckPolygonBoatRoot);
        }

        static Transform CreateRendererBoundsFallbackDeck(
            Bounds hullRendererBounds,
            Transform boatRoot,
            float floorLocalY,
            Color deckColor,
            out List<Vector2> deckPolygonBoatRoot)
        {
            deckPolygonBoatRoot = null;
            if (boatRoot == null)
                return null;

            GameObject boat = boatRoot.gameObject;
            if (TryFindHullWoodMeshFilter(boat, out MeshFilter meshFilter)
                && TryBuildShapedHullDeckPolygon(boat, meshFilter, boatRoot, floorLocalY, out List<Vector2> polygon))
            {
                float deckThickness = WorldScale.Feet(DeckVisualThicknessFeet);
                return BuildWalkDeckFromPolygon(
                    boat,
                    boatRoot,
                    polygon,
                    floorLocalY,
                    deckThickness,
                    deckColor,
                    out deckPolygonBoatRoot);
            }

            if (!TryGetWorldBoundsBoatRootBounds(hullRendererBounds, boatRoot, out Vector3 minLocal, out Vector3 maxLocal)
                || !TryBuildInsetRectanglePolygon(
                    minLocal,
                    maxLocal,
                    DeckHullInteriorInsetRatioX,
                    DeckHullInteriorInsetRatioZ,
                    WorldScale.Feet(0.2f),
                    WorldScale.Feet(0.25f),
                    out List<Vector2> boundsPolygon))
            {
                return null;
            }

            float boundsDeckThickness = WorldScale.Feet(DeckVisualThicknessFeet);
            return BuildWalkDeckFromPolygon(
                boat,
                boatRoot,
                boundsPolygon,
                floorLocalY,
                boundsDeckThickness,
                deckColor,
                out deckPolygonBoatRoot);
        }

        static bool TryResolveDeckPolygon(
            GameObject boat,
            Transform boatRoot,
            MeshFilter hullFilter,
            float walkSurfaceLocalY,
            out List<Vector2> polygon)
        {
            polygon = null;
            if (TryBuildShapedHullDeckPolygon(boat, hullFilter, boatRoot, walkSurfaceLocalY, out polygon))
                return true;

            return TryBuildHullInteriorFallbackPolygon(boat, hullFilter, boatRoot, walkSurfaceLocalY, out polygon);
        }

        static float ResolveDeckOutlineSliceLocalY(GameObject boat, Transform boatRoot, float walkSurfaceLocalY)
        {
            if (TryGetSeatBoardTopLocalY(boat, boatRoot, out float seatBoardTopY))
                return seatBoardTopY + WorldScale.Feet(DeckOutlineSliceAboveSeatFeet);

            return walkSurfaceLocalY - WorldScale.Feet(DeckAdditionalRaiseFeet * 0.5f);
        }

        static bool TryBuildShapedHullDeckPolygon(
            GameObject boat,
            MeshFilter hullFilter,
            Transform boatRoot,
            float walkSurfaceLocalY,
            out List<Vector2> polygon)
        {
            polygon = null;
            if (boat == null || hullFilter == null || boatRoot == null)
                return false;

            float outlineSliceY = ResolveDeckOutlineSliceLocalY(boat, boatRoot, walkSurfaceLocalY);
            List<Vector2> candidate = null;

            if (TrySliceSeatBoardDeckPolygon(boat, boatRoot, outlineSliceY, out List<Vector2> seatBoardPolygon))
                candidate = seatBoardPolygon;

            if (TrySliceHullDeckPolygon(hullFilter, boatRoot, outlineSliceY, out List<Vector2> hullSlicePolygon))
            {
                if (candidate == null)
                {
                    candidate = hullSlicePolygon;
                }
                else
                {
                    List<Vector2> clipped = ClipPolygonToConvexPolygon(candidate, BuildConvexHull(hullSlicePolygon));
                    if (clipped != null && clipped.Count >= 3)
                        candidate = clipped;

                    clipped = ClipPolygonToConvexPolygon(hullSlicePolygon, BuildConvexHull(candidate));
                    if (clipped != null && clipped.Count >= 3
                        && Mathf.Abs(ComputeSignedArea2D(clipped)) >= Mathf.Abs(ComputeSignedArea2D(candidate)) * 0.55f)
                    {
                        candidate = clipped;
                    }
                }
            }

            if (candidate == null)
                return false;

            return PrepareDeckPolygon(boat, candidate, hullFilter, boatRoot, walkSurfaceLocalY, out polygon);
        }

        static bool PrepareDeckPolygon(
            GameObject boat,
            List<Vector2> rawPolygon,
            MeshFilter hullFilter,
            Transform boatRoot,
            float walkSurfaceLocalY,
            out List<Vector2> polygon)
        {
            polygon = null;
            if (rawPolygon == null || rawPolygon.Count < 3 || hullFilter == null || boatRoot == null)
                return false;

            if (!IsReasonableDeckPolygon(rawPolygon, hullFilter, boatRoot))
                return false;

            List<Vector2> working = new List<Vector2>(rawPolygon);
            float outlineSliceY = ResolveDeckOutlineSliceLocalY(boat, boatRoot, walkSurfaceLocalY);
            if (TrySliceHullDeckPolygon(hullFilter, boatRoot, outlineSliceY, out List<Vector2> hullSlicePolygon))
            {
                List<Vector2> hullClip = BuildConvexHull(hullSlicePolygon);
                if (hullClip != null && hullClip.Count >= 3)
                {
                    List<Vector2> clipped = ClipPolygonToConvexPolygon(working, hullClip);
                    if (clipped != null && clipped.Count >= 3)
                        working = clipped;
                }
            }

            float inset = WorldScale.Feet(DeckPolygonInsetFeet);
            polygon = InsetPolygonTowardCentroid(working, inset);
            if (polygon == null || polygon.Count < 3)
                return false;

            EnsureCounterClockwiseWinding(polygon);

            if (!TryGetMeshFilterBoatRootBounds(hullFilter, boatRoot, out Vector3 minLocal, out Vector3 maxLocal))
                return true;

            float minArea = Mathf.Max(
                WorldScale.Feet(2f),
                (maxLocal.x - minLocal.x) * (maxLocal.z - minLocal.z) * DeckSliceMinAreaRatio);
            return Mathf.Abs(ComputeSignedArea2D(polygon)) >= minArea;
        }

        static List<Vector2> InsetPolygonTowardCentroid(IReadOnlyList<Vector2> polygon, float inset)
        {
            if (polygon == null || polygon.Count < 3 || inset <= 0f)
                return polygon != null ? new List<Vector2>(polygon) : null;

            Vector2 centroid = ComputePolygonCentroid(polygon);
            var insetPolygon = new List<Vector2>(polygon.Count);
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 point = polygon[i];
                Vector2 toCentroid = centroid - point;
                float distance = toCentroid.magnitude;
                if (distance <= 0.0001f)
                {
                    insetPolygon.Add(point);
                    continue;
                }

                float move = Mathf.Min(inset, distance * 0.45f);
                insetPolygon.Add(point + toCentroid * (move / distance));
            }

            return insetPolygon;
        }

        static Vector2 ComputePolygonCentroid(IReadOnlyList<Vector2> polygon)
        {
            Vector2 centroid = Vector2.zero;
            for (int i = 0; i < polygon.Count; i++)
                centroid += polygon[i];

            return centroid / polygon.Count;
        }

        static bool IsReasonableDeckPolygon(
            List<Vector2> polygon,
            MeshFilter hullFilter,
            Transform boatRoot)
        {
            if (polygon == null || polygon.Count < 3 || hullFilter == null || boatRoot == null)
                return false;

            if (!TryGetMeshFilterBoatRootBounds(hullFilter, boatRoot, out Vector3 minLocal, out Vector3 maxLocal))
                return true;

            float hullArea = Mathf.Max(0.01f, (maxLocal.x - minLocal.x) * (maxLocal.z - minLocal.z));
            float polygonArea = Mathf.Abs(ComputeSignedArea2D(polygon));
            return polygonArea <= hullArea * 1.05f;
        }

        static Transform CreateCargoDeckAnchor(
            Transform boatRoot,
            List<Vector2> polygon,
            float walkSurfaceLocalY,
            float deckThickness)
        {
            if (boatRoot == null || polygon == null || polygon.Count < 3)
                return null;

            GetPolygonBounds(polygon, out float minX, out float maxX, out float minZ, out float maxZ);
            var cargoDeck = new GameObject("BoatDeck").transform;
            cargoDeck.SetParent(boatRoot, false);
            cargoDeck.localPosition = new Vector3(
                (minX + maxX) * 0.5f,
                walkSurfaceLocalY,
                (minZ + maxZ) * 0.5f);
            cargoDeck.localRotation = Quaternion.identity;

            var cargoReference = cargoDeck.gameObject.AddComponent<BoxCollider>();
            cargoReference.isTrigger = false;
            cargoReference.size = new Vector3(
                Mathf.Max(WorldScale.Feet(0.5f), maxX - minX),
                deckThickness,
                Mathf.Max(WorldScale.Feet(0.5f), maxZ - minZ));
            cargoReference.center = new Vector3(0f, -deckThickness * 0.5f, 0f);
            return cargoDeck;
        }

        static Transform BuildWalkDeckFromPolygon(
            GameObject boat,
            Transform boatRoot,
            List<Vector2> polygon,
            float walkSurfaceLocalY,
            float deckThickness,
            Color deckColor,
            out List<Vector2> deckPolygonBoatRoot)
        {
            deckPolygonBoatRoot = polygon;

            Mesh deckMesh = BuildSolidExtrudedDeckMesh(polygon, walkSurfaceLocalY, deckThickness);
            if (deckMesh == null || deckMesh.vertexCount == 0)
                return null;

            var walkDeck = new GameObject("BoatWalkDeck");
            walkDeck.transform.SetParent(boatRoot, false);
            walkDeck.transform.localPosition = Vector3.zero;
            walkDeck.transform.localRotation = Quaternion.identity;
            walkDeck.transform.localScale = Vector3.one;

            var meshFilterComponent = walkDeck.AddComponent<MeshFilter>();
            meshFilterComponent.sharedMesh = deckMesh;

            var meshRenderer = walkDeck.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = CreateBoatDeckMaterial(boat, deckColor);
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.enabled = true;

            float cellSize = WorldScale.Feet(DeckGridCellFeet);
            AddWalkDeckColliders(walkDeck.transform, polygon, walkSurfaceLocalY, cellSize, deckThickness);

            Bounds deckBounds = deckMesh.bounds;
            var cargoDeck = new GameObject("BoatDeck").transform;
            cargoDeck.SetParent(walkDeck.transform, false);
            cargoDeck.localPosition = new Vector3(deckBounds.center.x, deckBounds.max.y, deckBounds.center.z);
            cargoDeck.localRotation = Quaternion.identity;

            var cargoReference = cargoDeck.gameObject.AddComponent<BoxCollider>();
            cargoReference.isTrigger = false;
            cargoReference.size = new Vector3(deckBounds.size.x, deckThickness, deckBounds.size.z);
            cargoReference.center = new Vector3(0f, -deckThickness * 0.5f, 0f);

            return cargoDeck;
        }

        static bool TryBuildHullInteriorFallbackPolygon(
            GameObject boat,
            MeshFilter hullFilter,
            Transform boatRoot,
            float walkSurfaceLocalY,
            out List<Vector2> polygon)
        {
            polygon = null;
            if (hullFilter == null || boatRoot == null)
                return false;

            float outlineSliceY = ResolveDeckOutlineSliceLocalY(boat, boatRoot, walkSurfaceLocalY);
            if (TrySliceHullDeckPolygon(hullFilter, boatRoot, outlineSliceY, out List<Vector2> hullSlicePolygon)
                && PrepareDeckPolygon(boat, hullSlicePolygon, hullFilter, boatRoot, walkSurfaceLocalY, out polygon))
            {
                return true;
            }

            if (!TryGetMeshFilterBoatRootBounds(hullFilter, boatRoot, out Vector3 minLocal, out Vector3 maxLocal))
                return false;

            if (!TryBuildInsetRectanglePolygon(
                    minLocal,
                    maxLocal,
                    DeckHullInteriorInsetRatioX,
                    DeckHullInteriorInsetRatioZ,
                    WorldScale.Feet(0.2f),
                    WorldScale.Feet(0.25f),
                    out List<Vector2> rectangle))
            {
                return false;
            }

            return PrepareDeckPolygon(boat, rectangle, hullFilter, boatRoot, walkSurfaceLocalY, out polygon);
        }

        static bool TryBuildInsetRectanglePolygon(
            Vector3 minLocal,
            Vector3 maxLocal,
            float insetRatioX,
            float insetRatioZ,
            float minInsetX,
            float minInsetZ,
            out List<Vector2> polygon)
        {
            polygon = null;
            float width = maxLocal.x - minLocal.x;
            float length = maxLocal.z - minLocal.z;
            if (width <= WorldScale.Feet(0.5f) || length <= WorldScale.Feet(0.5f))
                return false;

            float insetX = Mathf.Max(minInsetX, width * insetRatioX);
            float insetZ = Mathf.Max(minInsetZ, length * insetRatioZ);
            polygon = new List<Vector2>
            {
                new Vector2(minLocal.x + insetX, minLocal.z + insetZ),
                new Vector2(maxLocal.x - insetX, minLocal.z + insetZ),
                new Vector2(maxLocal.x - insetX, maxLocal.z - insetZ),
                new Vector2(minLocal.x + insetX, maxLocal.z - insetZ),
            };
            EnsureCounterClockwiseWinding(polygon);
            return true;
        }

        static bool TryBuildSeatBoardOutlinePolygon(
            GameObject boat,
            Transform boatRoot,
            out List<Vector2> polygon)
        {
            polygon = null;
            if (!TryGetSeatBoardPlanarBounds(boat, boatRoot, out float minX, out float maxX, out float minZ, out float maxZ))
                return false;

            float inset = WorldScale.Feet(DeckSeatBoardInsetFeet);
            polygon = new List<Vector2>
            {
                new Vector2(minX + inset, minZ + inset),
                new Vector2(maxX - inset, minZ + inset),
                new Vector2(maxX - inset, maxZ - inset),
                new Vector2(minX + inset, maxZ - inset),
            };
            EnsureCounterClockwiseWinding(polygon);
            return true;
        }

        static bool TryGetSeatBoardPlanarBounds(
            GameObject boat,
            Transform boatRoot,
            out float minX,
            out float maxX,
            out float minZ,
            out float maxZ)
        {
            minX = float.PositiveInfinity;
            maxX = float.NegativeInfinity;
            minZ = float.PositiveInfinity;
            maxZ = float.NegativeInfinity;
            bool found = false;

            foreach (MeshFilter meshFilter in boat.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                var renderer = meshFilter.GetComponent<MeshRenderer>();
                if (renderer == null || meshFilter.sharedMesh == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                Mesh mesh = EnsureReadableMesh(meshFilter);
                if (mesh == null)
                    continue;

                Vector3[] vertices = mesh.vertices;
                Transform meshTransform = meshFilter.transform;

                for (int submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++)
                {
                    Material material = submeshIndex < materials.Length ? materials[submeshIndex] : null;
                    if (!IsSeatBoardMaterial(material))
                        continue;

                    int[] triangles = mesh.GetTriangles(submeshIndex);
                    for (int i = 0; i < triangles.Length; i++)
                    {
                        Vector3 local = boatRoot.InverseTransformPoint(
                            meshTransform.TransformPoint(vertices[triangles[i]]));
                        minX = Mathf.Min(minX, local.x);
                        maxX = Mathf.Max(maxX, local.x);
                        minZ = Mathf.Min(minZ, local.z);
                        maxZ = Mathf.Max(maxZ, local.z);
                        found = true;
                    }
                }
            }

            return found && maxX > minX && maxZ > minZ;
        }

        static Vector3 GetDeckHalfExtentsLocal(Transform deck)
        {
            Transform walkDeck = deck.parent != null && deck.parent.name == "BoatWalkDeck"
                ? deck.parent
                : deck.root.Find("BoatWalkDeck");

            if (walkDeck != null && walkDeck.TryGetComponent<MeshFilter>(out MeshFilter meshFilter)
                && meshFilter.sharedMesh != null)
            {
                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                return new Vector3(
                    meshBounds.extents.x,
                    WorldScale.Feet(DeckVisualThicknessFeet * 0.5f),
                    meshBounds.extents.z);
            }

            BoxCollider box = deck.GetComponent<BoxCollider>();
            if (box != null)
                return Vector3.Scale(box.size, deck.lossyScale) * 0.5f;

            return new Vector3(0.8f, WorldScale.Feet(0.05f), 1.2f);
        }

        static bool TrySliceSeatBoardDeckPolygon(
            GameObject boat,
            Transform boatRoot,
            float planeLocalY,
            out List<Vector2> polygon)
        {
            polygon = null;
            if (boat == null || boatRoot == null)
                return false;

            var points = new Dictionary<long, Vector2>();
            var edges = new List<(long a, long b)>();

            foreach (MeshFilter meshFilter in boat.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                var renderer = meshFilter.GetComponent<MeshRenderer>();
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                Mesh mesh = EnsureReadableMesh(meshFilter);
                if (mesh == null)
                    continue;

                Vector3[] vertices = mesh.vertices;
                Transform meshTransform = meshFilter.transform;

                for (int submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++)
                {
                    Material material = submeshIndex < materials.Length ? materials[submeshIndex] : null;
                    if (!IsSeatBoardMaterial(material))
                        continue;

                    int[] triangles = mesh.GetTriangles(submeshIndex);
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        Vector3 v0 = boatRoot.InverseTransformPoint(
                            meshTransform.TransformPoint(vertices[triangles[i]]));
                        Vector3 v1 = boatRoot.InverseTransformPoint(
                            meshTransform.TransformPoint(vertices[triangles[i + 1]]));
                        Vector3 v2 = boatRoot.InverseTransformPoint(
                            meshTransform.TransformPoint(vertices[triangles[i + 2]]));
                        TryAddTrianglePlaneSegment(v0, v1, v2, planeLocalY, points, edges);
                    }
                }
            }

            if (edges.Count < 3)
                return false;

            List<List<Vector2>> loops = ChainAllLoops(points, edges);
            polygon = SelectLargestLoopFromCandidates(loops);
            if (polygon == null || polygon.Count < 3)
                return false;

            EnsureCounterClockwiseWinding(polygon);
            return true;
        }

        static List<Vector2> SelectLargestLoopFromCandidates(List<List<Vector2>> loops)
        {
            List<Vector2> bestLoop = null;
            float bestArea = 0f;
            for (int i = 0; i < loops.Count; i++)
            {
                float area = Mathf.Abs(ComputeSignedArea2D(loops[i]));
                if (area <= bestArea)
                    continue;

                bestArea = area;
                bestLoop = loops[i];
            }

            return bestLoop != null ? new List<Vector2>(bestLoop) : null;
        }

        static List<Vector2> BuildConvexHull(IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count < 3)
                return null;

            var unique = new List<Vector2>();
            var seen = new HashSet<long>();
            for (int i = 0; i < points.Count; i++)
            {
                long key = QuantizePlanarKey(points[i].x, points[i].y);
                if (!seen.Add(key))
                    continue;

                unique.Add(points[i]);
            }

            if (unique.Count < 3)
                return null;

            int lowest = 0;
            for (int i = 1; i < unique.Count; i++)
            {
                Vector2 candidate = unique[i];
                Vector2 best = unique[lowest];
                if (candidate.y < best.y - 0.0001f
                    || (Mathf.Abs(candidate.y - best.y) <= 0.0001f && candidate.x < best.x))
                {
                    lowest = i;
                }
            }

            Vector2 pivot = unique[lowest];
            unique.RemoveAt(lowest);
            unique.Sort((a, b) =>
            {
                float angleA = Mathf.Atan2(a.y - pivot.y, a.x - pivot.x);
                float angleB = Mathf.Atan2(b.y - pivot.y, b.x - pivot.x);
                int compare = angleA.CompareTo(angleB);
                if (compare != 0)
                    return compare;

                float distA = (a - pivot).sqrMagnitude;
                float distB = (b - pivot).sqrMagnitude;
                return distA.CompareTo(distB);
            });

            var hull = new List<Vector2> { pivot };
            for (int i = 0; i < unique.Count; i++)
            {
                Vector2 point = unique[i];
                while (hull.Count >= 2)
                {
                    Vector2 a = hull[hull.Count - 2];
                    Vector2 b = hull[hull.Count - 1];
                    if (Cross(a, b, point) <= 0f)
                        hull.RemoveAt(hull.Count - 1);
                    else
                        break;
                }

                hull.Add(point);
            }

            return hull.Count >= 3 ? hull : null;
        }

        static float Cross(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        }

        static float CrossZ(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        static List<Vector2> ClipPolygonToConvexPolygon(
            IReadOnlyList<Vector2> subject,
            IReadOnlyList<Vector2> convexClip)
        {
            if (subject == null || subject.Count < 3 || convexClip == null || convexClip.Count < 3)
                return null;

            var output = new List<Vector2>(subject);
            for (int i = 0; i < convexClip.Count; i++)
            {
                Vector2 edgeStart = convexClip[i];
                Vector2 edgeEnd = convexClip[(i + 1) % convexClip.Count];
                output = ClipPolygonAgainstEdge(output, edgeStart, edgeEnd);
                if (output.Count == 0)
                    return null;
            }

            return output.Count >= 3 ? output : null;
        }

        static List<Vector2> ClipPolygonAgainstEdge(
            IReadOnlyList<Vector2> input,
            Vector2 edgeStart,
            Vector2 edgeEnd)
        {
            var output = new List<Vector2>(input.Count + 1);
            if (input.Count == 0)
                return output;

            Vector2 edge = edgeEnd - edgeStart;
            Vector2 previous = input[input.Count - 1];
            bool previousInside = IsInsideConvexClipEdge(previous, edgeStart, edge);

            for (int i = 0; i < input.Count; i++)
            {
                Vector2 current = input[i];
                bool currentInside = IsInsideConvexClipEdge(current, edgeStart, edge);
                if (currentInside)
                {
                    if (!previousInside && TryIntersectSegments(previous, current, edgeStart, edgeEnd, out Vector2 hit))
                        output.Add(hit);

                    output.Add(current);
                }
                else if (previousInside && TryIntersectSegments(previous, current, edgeStart, edgeEnd, out Vector2 exitHit))
                {
                    output.Add(exitHit);
                }

                previous = current;
                previousInside = currentInside;
            }

            return output;
        }

        static bool IsInsideConvexClipEdge(Vector2 point, Vector2 edgeStart, Vector2 edge)
        {
            Vector2 relative = point - edgeStart;
            return CrossZ(edge, relative) >= -0.0001f;
        }

        static bool TryIntersectSegments(
            Vector2 a0,
            Vector2 a1,
            Vector2 b0,
            Vector2 b1,
            out Vector2 intersection)
        {
            intersection = default;
            Vector2 r = a1 - a0;
            Vector2 s = b1 - b0;
            float denominator = CrossZ(r, s);
            if (Mathf.Abs(denominator) <= 0.000001f)
                return false;

            Vector2 delta = b0 - a0;
            float t = CrossZ(delta, s) / denominator;
            float u = CrossZ(delta, r) / denominator;
            if (t < -0.001f || t > 1.001f || u < -0.001f || u > 1.001f)
                return false;

            intersection = a0 + r * Mathf.Clamp01(t);
            return true;
        }

        static bool TrySliceHullDeckPolygon(
            MeshFilter meshFilter,
            Transform boatRoot,
            float floorLocalY,
            out List<Vector2> polygon)
        {
            polygon = new List<Vector2>();
            Mesh mesh = EnsureReadableMesh(meshFilter);
            if (mesh == null)
                return false;

            Transform meshTransform = meshFilter.transform;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            var points = new Dictionary<long, Vector2>();
            var edges = new List<(long a, long b)>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = boatRoot.InverseTransformPoint(meshTransform.TransformPoint(vertices[triangles[i]]));
                Vector3 v1 = boatRoot.InverseTransformPoint(meshTransform.TransformPoint(vertices[triangles[i + 1]]));
                Vector3 v2 = boatRoot.InverseTransformPoint(meshTransform.TransformPoint(vertices[triangles[i + 2]]));
                TryAddTrianglePlaneSegment(v0, v1, v2, floorLocalY, points, edges);
            }

            if (edges.Count < 3)
                return false;

            List<List<Vector2>> loops = ChainAllLoops(points, edges);
            polygon = SelectInteriorDeckLoop(loops, meshFilter, boatRoot);
            if (polygon == null || polygon.Count < 3)
                return false;

            EnsureCounterClockwiseWinding(polygon);
            return true;
        }

        static List<List<Vector2>> ChainAllLoops(
            Dictionary<long, Vector2> points,
            List<(long a, long b)> edges)
        {
            var loops = new List<List<Vector2>>();
            if (points == null || edges == null || edges.Count < 3)
                return loops;

            var adjacency = new Dictionary<long, List<long>>();
            foreach ((long a, long b) in edges)
            {
                AddLoopAdjacency(adjacency, a, b);
                AddLoopAdjacency(adjacency, b, a);
            }

            var usedEdges = new HashSet<(long, long)>();
            foreach ((long a, long b) in edges)
            {
                (long, long) edgeKey = UndirectedEdgeKey(a, b);
                if (usedEdges.Contains(edgeKey))
                    continue;

                if (!TryWalkLoop(a, b, adjacency, points, usedEdges, out List<Vector2> loop))
                    continue;

                if (loop != null && loop.Count >= 3)
                    loops.Add(loop);
            }

            return loops;
        }

        static List<Vector2> SelectInteriorDeckLoop(
            List<List<Vector2>> loops,
            MeshFilter hullFilter,
            Transform boatRoot)
        {
            if (loops == null || loops.Count == 0)
                return null;

            if (!TryGetMeshFilterBoatRootBounds(hullFilter, boatRoot, out Vector3 minLocal, out Vector3 maxLocal))
                return ChainLargestLoopFromCandidates(loops);

            float bboxArea = Mathf.Max(
                WorldScale.Feet(1f),
                (maxLocal.x - minLocal.x) * (maxLocal.z - minLocal.z));
            float minArea = bboxArea * DeckSliceMinAreaRatio;
            float maxArea = bboxArea * DeckSliceMaxAreaRatio;

            List<Vector2> bestInterior = null;
            float bestInteriorArea = float.MaxValue;
            List<Vector2> bestUnderMax = null;
            float bestUnderMaxArea = 0f;

            for (int i = 0; i < loops.Count; i++)
            {
                List<Vector2> loop = loops[i];
                float area = Mathf.Abs(ComputeSignedArea2D(loop));
                if (area < minArea)
                    continue;

                if (area <= maxArea && area < bestInteriorArea)
                {
                    bestInteriorArea = area;
                    bestInterior = loop;
                }

                if (area <= maxArea && area > bestUnderMaxArea)
                {
                    bestUnderMaxArea = area;
                    bestUnderMax = loop;
                }
            }

            if (bestInterior != null)
                return new List<Vector2>(bestInterior);

            if (bestUnderMax != null)
                return new List<Vector2>(bestUnderMax);

            List<Vector2> smallest = null;
            float smallestArea = float.MaxValue;
            for (int i = 0; i < loops.Count; i++)
            {
                float area = Mathf.Abs(ComputeSignedArea2D(loops[i]));
                if (area >= minArea && area < smallestArea)
                {
                    smallestArea = area;
                    smallest = loops[i];
                }
            }

            if (smallest != null)
                return InsetPolygonTowardCentroid(
                    smallest,
                    WorldScale.Feet(DeckPolygonInsetFeet * 2f));

            List<Vector2> largest = ChainLargestLoopFromCandidates(loops);
            if (largest == null)
                return null;

            return InsetPolygonTowardCentroid(largest, WorldScale.Feet(DeckPolygonInsetFeet * 3f));
        }

        static List<Vector2> ChainLargestLoopFromCandidates(List<List<Vector2>> loops)
        {
            List<Vector2> bestLoop = null;
            float bestArea = 0f;
            for (int i = 0; i < loops.Count; i++)
            {
                float area = Mathf.Abs(ComputeSignedArea2D(loops[i]));
                if (area <= bestArea)
                    continue;

                bestArea = area;
                bestLoop = loops[i];
            }

            return bestLoop != null ? new List<Vector2>(bestLoop) : null;
        }

        static void TryAddTrianglePlaneSegment(
            Vector3 v0,
            Vector3 v1,
            Vector3 v2,
            float planeY,
            Dictionary<long, Vector2> points,
            List<(long a, long b)> edges)
        {
            var hits = new List<long>(2);
            TryCollectPlaneHit(v0, v1, planeY, points, hits);
            TryCollectPlaneHit(v1, v2, planeY, points, hits);
            TryCollectPlaneHit(v2, v0, planeY, points, hits);

            for (int i = hits.Count - 1; i >= 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    if (hits[i] != hits[j])
                        continue;

                    hits.RemoveAt(i);
                    break;
                }
            }

            if (hits.Count != 2 || hits[0] == hits[1])
                return;

            edges.Add((hits[0], hits[1]));
        }

        static void TryCollectPlaneHit(
            Vector3 a,
            Vector3 b,
            float planeY,
            Dictionary<long, Vector2> points,
            List<long> hits)
        {
            if (Mathf.Abs(a.y - b.y) < 0.00001f)
                return;

            if ((a.y - planeY) * (b.y - planeY) > 0f)
                return;

            float t = (planeY - a.y) / (b.y - a.y);
            if (t < -0.001f || t > 1.001f)
                return;

            Vector3 hit = Vector3.Lerp(a, b, Mathf.Clamp01(t));
            long key = QuantizePlanarKey(hit.x, hit.z);
            points[key] = new Vector2(hit.x, hit.z);
            hits.Add(key);
        }

        static void AddLoopAdjacency(Dictionary<long, List<long>> adjacency, long a, long b)
        {
            if (!adjacency.TryGetValue(a, out List<long> neighbors))
            {
                neighbors = new List<long>(2);
                adjacency[a] = neighbors;
            }

            if (!neighbors.Contains(b))
                neighbors.Add(b);
        }

        static (long, long) UndirectedEdgeKey(long a, long b) => a < b ? (a, b) : (b, a);

        static bool TryWalkLoop(
            long start,
            long firstNext,
            Dictionary<long, List<long>> adjacency,
            Dictionary<long, Vector2> points,
            HashSet<(long, long)> usedEdges,
            out List<Vector2> loop)
        {
            loop = new List<Vector2> { points[start] };
            long previous = start;
            long current = firstNext;
            int guard = adjacency.Count + 4;

            while (guard-- > 0)
            {
                loop.Add(points[current]);
                usedEdges.Add(UndirectedEdgeKey(previous, current));

                if (current == start)
                    return loop.Count >= 3;

                if (!adjacency.TryGetValue(current, out List<long> neighbors))
                    break;

                long next = -1;
                for (int i = 0; i < neighbors.Count; i++)
                {
                    long candidate = neighbors[i];
                    if (candidate == previous)
                        continue;

                    if (usedEdges.Contains(UndirectedEdgeKey(current, candidate)))
                        continue;

                    next = candidate;
                    break;
                }

                if (next == -1)
                    break;

                previous = current;
                current = next;
            }

            loop = null;
            return false;
        }

        static float ComputeSignedArea2D(IReadOnlyList<Vector2> polygon)
        {
            double area = 0d;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                area += (double)a.x * b.y - (double)b.x * a.y;
            }

            return (float)(area * 0.5d);
        }

        static void EnsureCounterClockwiseWinding(List<Vector2> polygon)
        {
            if (ComputeSignedArea2D(polygon) < 0f)
                polygon.Reverse();
        }

        static long QuantizePlanarKey(float x, float z)
        {
            int qx = Mathf.RoundToInt(x * 8000f);
            int qz = Mathf.RoundToInt(z * 8000f);
            return ((long)qx << 32) ^ (uint)qz;
        }

        static Mesh BuildSolidExtrudedDeckMesh(IReadOnlyList<Vector2> polygon, float topLocalY, float thickness)
        {
            if (polygon == null || polygon.Count < 3 || thickness <= 0.0001f)
                return null;

            float bottomLocalY = topLocalY - thickness;
            int count = polygon.Count;
            var vertices = new Vector3[count * 2];
            var triangles = new List<int>(count * 6);

            for (int i = 0; i < count; i++)
            {
                Vector2 p = polygon[i];
                vertices[i] = new Vector3(p.x, topLocalY, p.y);
                vertices[count + i] = new Vector3(p.x, bottomLocalY, p.y);
            }

            for (int i = 1; i < count - 1; i++)
            {
                triangles.Add(0);
                triangles.Add(i);
                triangles.Add(i + 1);
            }

            for (int i = 1; i < count - 1; i++)
            {
                triangles.Add(count);
                triangles.Add(count + i + 1);
                triangles.Add(count + i);
            }

            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                int topA = i;
                int topB = next;
                int bottomA = count + i;
                int bottomB = count + next;
                triangles.Add(topA);
                triangles.Add(topB);
                triangles.Add(bottomB);
                triangles.Add(topA);
                triangles.Add(bottomB);
                triangles.Add(bottomA);
            }

            var mesh = new Mesh { name = "BoatWalkDeckMesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AddWalkDeckColliders(
            Transform walkDeckRoot,
            IReadOnlyList<Vector2> polygon,
            float localY,
            float cellSize,
            float deckThickness)
        {
            var collisionRoot = new GameObject("WalkDeckCollision");
            collisionRoot.transform.SetParent(walkDeckRoot, false);
            collisionRoot.transform.localPosition = Vector3.zero;
            collisionRoot.transform.localRotation = Quaternion.identity;
            collisionRoot.transform.localScale = Vector3.one;

            GetPolygonBounds(polygon, out float minX, out float maxX, out float minZ, out float maxZ);
            for (float x = minX; x < maxX; x += cellSize)
            {
                for (float z = minZ; z < maxZ; z += cellSize)
                {
                    if (!CellIntersectsPolygon(x, z, cellSize, polygon))
                        continue;

                    Vector2 center = new Vector2(x + cellSize * 0.5f, z + cellSize * 0.5f);
                    var cell = new GameObject("WalkDeckCell");
                    cell.transform.SetParent(collisionRoot.transform, false);
                    cell.transform.localPosition = new Vector3(center.x, localY - deckThickness * 0.5f, center.y);
                    cell.transform.localRotation = Quaternion.identity;
                    cell.transform.localScale = Vector3.one;

                    var box = cell.AddComponent<BoxCollider>();
                    box.size = new Vector3(cellSize, deckThickness, cellSize);
                }
            }
        }

        static void GetPolygonBounds(IReadOnlyList<Vector2> polygon, out float minX, out float maxX, out float minZ, out float maxZ)
        {
            minX = float.PositiveInfinity;
            maxX = float.NegativeInfinity;
            minZ = float.PositiveInfinity;
            maxZ = float.NegativeInfinity;
            for (int i = 0; i < polygon.Count; i++)
            {
                minX = Mathf.Min(minX, polygon[i].x);
                maxX = Mathf.Max(maxX, polygon[i].x);
                minZ = Mathf.Min(minZ, polygon[i].y);
                maxZ = Mathf.Max(maxZ, polygon[i].y);
            }
        }

        static bool CellIntersectsPolygon(float x, float z, float cellSize, IReadOnlyList<Vector2> polygon)
        {
            if (IsPointInDeckPolygon(new Vector2(x + cellSize * 0.5f, z + cellSize * 0.5f), polygon))
                return true;

            return IsPointInDeckPolygon(new Vector2(x, z), polygon)
                || IsPointInDeckPolygon(new Vector2(x + cellSize, z), polygon)
                || IsPointInDeckPolygon(new Vector2(x + cellSize, z + cellSize), polygon)
                || IsPointInDeckPolygon(new Vector2(x, z + cellSize), polygon);
        }

        static bool IsPointInDeckPolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
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

        static void ApplyBoatDeckColor(Material mat, Color deckColor)
        {
            Color opaqueColor = new Color(deckColor.r, deckColor.g, deckColor.b, 1f);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", opaqueColor);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", opaqueColor);
            mat.color = opaqueColor;

            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0f);
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", 0f);

            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", null);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", null);
        }

        static Material CreateBoatDeckMaterial(GameObject boat, Color fallbackColor)
        {
            Color deckColor = BoatWalkDeckColor;
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color");
            var mat = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
            mat.name = "BoatDeckFloor";
            ApplyBoatDeckColor(mat, deckColor);

            if (mat.HasProperty("_Cull"))
                mat.SetInt("_Cull", (int)CullMode.Off);
            mat.renderQueue = DeckRenderQueue;
            return mat;
        }

        static bool TryGetSeatBoardSourceMaterial(GameObject boat, out Material source)
        {
            source = null;
            if (boat == null)
                return false;

            foreach (MeshRenderer renderer in boat.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                int submeshCount = Mathf.Min(meshFilter.sharedMesh.subMeshCount, materials.Length);
                for (int i = 0; i < submeshCount; i++)
                {
                    if (!IsSeatBoardMaterial(materials[i]))
                        continue;

                    source = materials[i];
                    return true;
                }
            }

#if UNITY_EDITOR
            const string floorMaterialPath =
                "Assets/CrowAssets/Assets/Stylized Sailing Boat Set/URP/Materials/Floor.mat";
            source = AssetDatabase.LoadAssetAtPath<Material>(floorMaterialPath);
            if (source != null)
                return true;
#endif
            return false;
        }

        static Color SampleBoatDeckColor(GameObject boat)
        {
            return BoatWalkDeckColor;
        }

        static Color? SampleMaterialColor(GameObject boat, System.Func<Material, bool> materialFilter)
        {
            foreach (var renderer in boat.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var mat = materials[i];
                    if (mat == null || !materialFilter(mat))
                        continue;

                    Color color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                    if (color.r + color.g + color.b > 0.25f)
                        return color;
                }
            }

            return null;
        }

        static Color? SampleRendererColor(GameObject boat, string objectName)
        {
            foreach (var renderer in boat.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null
                    || !renderer.gameObject.name.Equals(objectName, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var mat = materials[i];
                    if (mat == null)
                        continue;

                    Color color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                    if (color.r + color.g + color.b > 0.25f)
                        return color;
                }
            }

            return null;
        }

        static void EnsureDeckRendersAboveHull(GameObject boat)
        {
            foreach (var renderer in boat.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.gameObject.name != "BoatWalkDeck")
                    continue;

                Material deckMaterial = renderer.sharedMaterial;
                if (deckMaterial != null)
                    deckMaterial.renderQueue = DeckRenderQueue;
                renderer.enabled = true;
            }
        }
    }
}
