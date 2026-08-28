using System.Collections.Generic;
using MonsterMiner.Util;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.World
{
    public static class LowerWorldBuilder
    {
        const int CopseCount = 44;
        const int FogWispCount = 22;
        const int PlacementSeed = 42857;
        const int PatchTypeGrass = 0;
        const int PatchTypeMeadow = 1;
        const int PatchTypeScrub = 2;

        static readonly float LowerWorldRadius = WorldScale.Feet(520f);
        static readonly float LowerHillAmplitude = WorldScale.Feet(14f);
        static Material fogWispMaterial;

        public static float GetLowerGroundBaseY(float plateauSurfaceLocalY)
        {
            return plateauSurfaceLocalY - WorldScale.Feet(WorldScale.PlateauCliffHeightFeet);
        }

        public static void Build(Transform parent, float plateauSurfaceLocalY, float quarryNominalRadius)
        {
            float lowerBaseY = GetLowerGroundBaseY(plateauSurfaceLocalY);
            var root = new GameObject("LowerWorld").transform;
            root.SetParent(parent, false);

            BuildLowerPlainsGround(root, lowerBaseY, quarryNominalRadius);
            ScatterTreeCopses(root, lowerBaseY, quarryNominalRadius);
            CreateFogWisps(root, lowerBaseY, quarryNominalRadius);
        }

        static float GetVisibleInnerRadius(float quarryNominalRadius)
        {
            float angle = 0f;
            return PlateauWallGeometry.GetWallBaseOutwardRadius(angle, quarryNominalRadius) + WorldScale.Feet(12f);
        }

        public static float SampleLowerPlainsLocalY(float localX, float localZ, float baseLocalY)
        {
            const float noiseOffsetX = 117.9f;
            const float noiseOffsetZ = 53.2f;
            float large = Mathf.PerlinNoise(localX * 0.048f + noiseOffsetX, localZ * 0.048f + noiseOffsetZ);
            float medium = Mathf.PerlinNoise(localX * 0.115f + 8.4f, localZ * 0.115f + 21.6f);
            float fine = Mathf.PerlinNoise(localX * 0.24f + 31.7f, localZ * 0.24f + 12.8f);
            float blend = large * 0.58f + medium * 0.3f + fine * 0.12f;
            float roll = (blend * 2f - 1f) * LowerHillAmplitude;
            return baseLocalY + roll;
        }

        static int ClassifyGroundPatch(float localX, float localZ)
        {
            float large = Mathf.PerlinNoise(localX * 0.011f + 90.4f, localZ * 0.011f + 41.8f);
            float medium = Mathf.PerlinNoise(localX * 0.028f + 12.7f, localZ * 0.028f + 77.1f);
            float blend = large * 0.68f + medium * 0.32f;
            if (blend < 0.36f)
                return PatchTypeScrub;
            if (blend < 0.62f)
                return PatchTypeMeadow;
            return PatchTypeGrass;
        }

        static void BuildLowerPlainsGround(Transform parent, float lowerBaseY, float quarryNominalRadius)
        {
            const int radialRings = 28;
            const int angularSegments = 64;
            float innerRadius = GetVisibleInnerRadius(quarryNominalRadius);

            var groundGo = new GameObject("LowerPlainsGround");
            groundGo.transform.SetParent(parent, false);

            var mesh = BuildLowerPlainsMesh(lowerBaseY, LowerWorldRadius, innerRadius, radialRings, angularSegments);
            var meshFilter = groundGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = groundGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = new[]
            {
                CavernSurfaceMaterialFactory.GetLowerPlainsGrassMaterial(),
                CavernSurfaceMaterialFactory.GetLowerPlainsMeadowMaterial(),
                CavernSurfaceMaterialFactory.GetLowerPlainsScrubMaterial()
            };

            var meshCollider = groundGo.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = false;
        }

        static Mesh BuildLowerPlainsMesh(
            float lowerBaseY,
            float radius,
            float innerRadius,
            int radialRings,
            int angularSegments)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var grassTriangles = new List<int>();
            var meadowTriangles = new List<int>();
            var scrubTriangles = new List<int>();

            for (int segment = 0; segment < angularSegments; segment++)
            {
                for (int ring = 0; ring <= radialRings; ring++)
                {
                    float t = ring / (float)radialRings;
                    float ringRadius = Mathf.Lerp(innerRadius, radius, t);
                    float angle = segment / (float)angularSegments * Mathf.PI * 2f;
                    float x = Mathf.Cos(angle) * ringRadius;
                    float z = Mathf.Sin(angle) * ringRadius;
                    float y = SampleLowerPlainsLocalY(x, z, lowerBaseY);
                    vertices.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(x * 0.014f, z * 0.014f));
                }
            }

            int ColumnVertex(int segment, int ring) => segment * (radialRings + 1) + ring;

            void AddTriangle(int a, int b, int c, float centerX, float centerZ)
            {
                var target = ClassifyGroundPatch(centerX, centerZ) switch
                {
                    PatchTypeScrub => scrubTriangles,
                    PatchTypeMeadow => meadowTriangles,
                    _ => grassTriangles
                };
                target.Add(a);
                target.Add(b);
                target.Add(c);
            }

            for (int segment = 0; segment < angularSegments; segment++)
            {
                int nextSegment = (segment + 1) % angularSegments;
                for (int ring = 0; ring < radialRings; ring++)
                {
                    int bottomLeft = ColumnVertex(segment, ring);
                    int bottomRight = ColumnVertex(nextSegment, ring);
                    int topLeft = ColumnVertex(segment, ring + 1);
                    int topRight = ColumnVertex(nextSegment, ring + 1);

                    Vector3 p0 = vertices[bottomLeft];
                    Vector3 p1 = vertices[topLeft];
                    Vector3 p2 = vertices[topRight];
                    Vector3 triCenterA = (p0 + p1 + p2) / 3f;
                    AddTriangle(bottomLeft, topLeft, topRight, triCenterA.x, triCenterA.z);

                    Vector3 p3 = vertices[bottomRight];
                    Vector3 triCenterB = (p0 + p2 + p3) / 3f;
                    AddTriangle(bottomLeft, topRight, bottomRight, triCenterB.x, triCenterB.z);
                }
            }

            var mesh = new Mesh { name = "LowerPlainsGround" };
            mesh.indexFormat = vertices.Count > 65000
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;
            mesh.subMeshCount = 3;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(grassTriangles, PatchTypeGrass);
            mesh.SetTriangles(meadowTriangles, PatchTypeMeadow);
            mesh.SetTriangles(scrubTriangles, PatchTypeScrub);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void ScatterTreeCopses(Transform parent, float lowerBaseY, float quarryNominalRadius)
        {
            var copseRoot = new GameObject("LowerTreeCopses").transform;
            copseRoot.SetParent(parent, false);

            float SampleGround(float x, float z) => SampleLowerPlainsLocalY(x, z, lowerBaseY);
            var copseCenters = new List<Vector2>(CopseCount);
            var randomState = Random.state;
            Random.InitState(PlacementSeed);

            float nearBandMin = GetVisibleInnerRadius(quarryNominalRadius) + WorldScale.Feet(15f);
            float nearBandMax = GetVisibleInnerRadius(quarryNominalRadius) + WorldScale.Feet(420f);

            int attempts = 0;
            while (copseCenters.Count < CopseCount && attempts < CopseCount * 40)
            {
                attempts++;
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float distance = Random.value < 0.72f
                    ? Random.Range(nearBandMin, nearBandMax)
                    : Random.Range(LowerWorldRadius * 0.08f, LowerWorldRadius * 0.94f);
                var candidate = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);

                bool tooClose = false;
                for (int i = 0; i < copseCenters.Count; i++)
                {
                    if (Vector2.Distance(candidate, copseCenters[i]) < WorldScale.Feet(70f))
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                    continue;

                copseCenters.Add(candidate);
                PlainsTreeVisualFactory.CreateVistaTreeCopse(
                    copseRoot,
                    candidate,
                    copseCenters.Count,
                    SampleGround);
            }

            Random.state = randomState;
        }

        static void CreateFogWisps(Transform parent, float lowerBaseY, float quarryNominalRadius)
        {
            var wispRoot = new GameObject("LowerFogWisps").transform;
            wispRoot.SetParent(parent, false);
            float innerRadius = GetVisibleInnerRadius(quarryNominalRadius);

            var randomState = Random.state;
            Random.InitState(PlacementSeed + 913);

            for (int i = 0; i < FogWispCount; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float distance = Random.Range(innerRadius, LowerWorldRadius * 0.96f);
                float x = Mathf.Cos(angle) * distance;
                float z = Mathf.Sin(angle) * distance;
                float groundY = SampleLowerPlainsLocalY(x, z, lowerBaseY);
                float wispHeight = Random.Range(WorldScale.Feet(2f), WorldScale.Feet(7f));
                float wispWidth = Random.Range(WorldScale.Feet(18f), WorldScale.Feet(42f));
                float wispDepth = Random.Range(WorldScale.Feet(10f), WorldScale.Feet(24f));

                var wisp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wisp.name = $"LowerFogWisp_{i}";
                wisp.transform.SetParent(wispRoot, false);
                wisp.transform.localPosition = new Vector3(
                    x,
                    groundY + wispHeight * 0.35f,
                    z);
                wisp.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                wisp.transform.localScale = new Vector3(wispWidth, wispHeight, wispDepth);
                wisp.GetComponent<Renderer>().sharedMaterial = GetFogWispMaterial();
                Object.Destroy(wisp.GetComponent<Collider>());
            }

            Random.state = randomState;
        }

        static Material GetFogWispMaterial()
        {
            if (fogWispMaterial != null)
                return fogWispMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            fogWispMaterial = shader != null ? new Material(shader) : PrimitiveFactory.CreateColorMaterial(Color.white);
            fogWispMaterial.name = "LowerFogWisp";
            fogWispMaterial.SetFloat("_Surface", 1f);
            fogWispMaterial.SetFloat("_Blend", 0f);
            fogWispMaterial.SetFloat("_Smoothness", 0.02f);
            fogWispMaterial.SetColor("_BaseColor", new Color(0.88f, 0.92f, 0.96f, 0.16f));
            fogWispMaterial.renderQueue = (int)RenderQueue.Transparent;
            fogWispMaterial.SetOverrideTag("RenderType", "Transparent");
            fogWispMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            fogWispMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            fogWispMaterial.SetInt("_ZWrite", 0);
            fogWispMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            fogWispMaterial.EnableKeyword("_ALPHABLEND_ON");
            fogWispMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            return fogWispMaterial;
        }
    }
}
