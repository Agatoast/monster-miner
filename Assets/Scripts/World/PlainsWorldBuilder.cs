using System.Collections.Generic;
using MonsterMiner.Util;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.World
{
    public static class PlainsWorldBuilder
    {
        const int CloudCount = 22;
        const int PlacementSeed = 42857;
        const int PatchTypeGrass = 0;
        const int PatchTypeMeadow = 1;
        const int PatchTypeScrub = 2;
        const float CloudMinHeightFeet = 100f;
        const float CloudMaxHeightFeet = 150f;

        static Material cloudMaterial;

        public static float GetPlainsWorldRadius(float quarryNominalRadius)
        {
            return WorldRegion.GetLandOuterRadius(quarryNominalRadius);
        }

        public static float GetPlainsGroundBaseY(float plateauSurfaceLocalY)
        {
            return plateauSurfaceLocalY - WorldScale.Feet(WorldScale.PlateauCliffHeightFeet);
        }

        public static void Build(Transform parent, float plateauSurfaceLocalY, CavernBounds bounds)
        {
            float plainsBaseY = GetPlainsGroundBaseY(plateauSurfaceLocalY);
            var root = new GameObject("PlainsWorld").transform;
            root.SetParent(parent, false);

            BuildPlainsGround(root, plainsBaseY, bounds.Radius);
            CreateClouds(root, plainsBaseY, bounds.Radius);
        }

        public static void RebuildGroundExcludingLandFeatures(Transform contentRoot, CavernBounds bounds)
        {
            if (contentRoot == null || bounds == null)
                return;

            var plainsWorld = contentRoot.Find("PlateauBluff/PlainsWorld");
            if (plainsWorld == null)
                return;

            var groundGo = plainsWorld.Find("PlainsGround");
            if (groundGo == null)
                return;

            float plainsBaseY = GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float outerRadius = GetPlainsWorldRadius(bounds.Radius);
            float innerRadius = WorldScale.Feet(2f);
            const int visualRings = 48;
            const int collisionRings = 36;
            const int angularSegments = 72;

            var renderMesh = BuildPlainsMesh(
                plainsBaseY,
                outerRadius,
                innerRadius,
                visualRings,
                angularSegments,
                ShouldExcludePlainsTriangle);
            var collisionMesh = BuildPlainsTopCollisionMesh(
                plainsBaseY,
                outerRadius,
                innerRadius,
                collisionRings,
                angularSegments,
                ShouldExcludePlainsTriangle);

            var meshFilter = groundGo.GetComponent<MeshFilter>();
            if (meshFilter != null)
                meshFilter.sharedMesh = renderMesh;

            var collisionGo = groundGo.Find("PlainsGroundCollision");
            if (collisionGo != null)
            {
                var collisionFilter = collisionGo.GetComponent<MeshFilter>();
                if (collisionFilter != null)
                    collisionFilter.sharedMesh = collisionMesh;

                var meshCollider = collisionGo.GetComponent<MeshCollider>();
                if (meshCollider != null)
                    meshCollider.sharedMesh = collisionMesh;
            }

            Physics.SyncTransforms();
        }

        static bool ShouldExcludePlainsTriangle(float localX, float localZ)
        {
            if (LandQuarry2Boundary.IsSnowGroundLocal(localX, localZ))
                return true;

            if (LakeCatalog.IsBeachLocal(localX, localZ))
                return true;

            if (LakeCatalog.IsLakeIslandLocal(localX, localZ))
                return true;

            if (LakeCatalog.IsOpenWaterLocal(localX, localZ))
                return true;

            if (LakeCatalog.IsLakeLocal(localX, localZ))
                return true;

            if (LandQuarry3Boundary.ContainsLocal(localX, localZ))
                return true;

            if (LandQuarry4Boundary.ContainsLocal(localX, localZ))
                return true;

            return false;
        }

        static bool ShouldExcludePlainsTriangleVertices(Vector3 a, Vector3 b, Vector3 c)
        {
            if (ShouldExcludePlainsTriangle(a.x, a.z))
                return true;
            if (ShouldExcludePlainsTriangle(b.x, b.z))
                return true;
            if (ShouldExcludePlainsTriangle(c.x, c.z))
                return true;

            return false;
        }

        static float GetVisibleInnerRadius(float quarryNominalRadius)
        {
            float angle = 0f;
            return PlateauWallGeometry.GetWallBaseOutwardRadius(angle, quarryNominalRadius) + WorldScale.Feet(12f);
        }

        public static float SamplePlainsLocalY(float localX, float localZ, float baseLocalY)
        {
            return PlainsGroundBuilder.SamplePlainsLocalY(localX, localZ, baseLocalY);
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

        static float GetRingRadius(int ring, int radialRings, float innerRadius, float outerRadius)
        {
            if (radialRings <= 0)
                return innerRadius;

            float t = ring / (float)radialRings;
            float nearOuter = Mathf.Min(outerRadius, innerRadius + WorldScale.Feet(900f));
            const float nearRingFraction = 0.62f;
            if (t <= nearRingFraction)
                return Mathf.Lerp(innerRadius, nearOuter, t / nearRingFraction);
            return Mathf.Lerp(nearOuter, outerRadius, (t - nearRingFraction) / (1f - nearRingFraction));
        }

        public static float SamplePlainsWorldY(Transform boundsTransform, float localX, float localZ)
        {
            float plainsBase = GetPlainsGroundBaseY(PlainsBiomeVisualFactory.PlainsSurfaceLocalY);
            float localY = SamplePlainsLocalY(localX, localZ, plainsBase);
            return boundsTransform.TransformPoint(new Vector3(localX, localY, localZ)).y;
        }

        public static float SamplePlainsWorldY(Transform boundsTransform, Vector3 worldPoint)
        {
            var local = boundsTransform.InverseTransformPoint(worldPoint);
            return SamplePlainsWorldY(boundsTransform, local.x, local.z);
        }

        static void BuildPlainsGround(Transform parent, float plainsBaseY, float quarryNominalRadius)
        {
            const int visualRings = 48;
            const int collisionRings = 36;
            const int angularSegments = 72;
            float innerRadius = WorldScale.Feet(2f);
            float outerRadius = GetPlainsWorldRadius(quarryNominalRadius);
            float collisionRadius = outerRadius;

            var groundGo = new GameObject("PlainsGround");
            groundGo.transform.SetParent(parent, false);

            var mesh = BuildPlainsMesh(plainsBaseY, outerRadius, innerRadius, visualRings, angularSegments);
            var meshFilter = groundGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = groundGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = new[]
            {
                CavernSurfaceMaterialFactory.GetPlainsGrassMaterial(),
                CavernSurfaceMaterialFactory.GetPlainsMeadowMaterial(),
                CavernSurfaceMaterialFactory.GetPlainsScrubMaterial()
            };

            var collisionMesh = BuildPlainsTopCollisionMesh(
                plainsBaseY,
                collisionRadius,
                innerRadius,
                collisionRings,
                angularSegments);
            var collisionGo = new GameObject("PlainsGroundCollision");
            collisionGo.transform.SetParent(groundGo.transform, false);
            var collisionFilter = collisionGo.AddComponent<MeshFilter>();
            collisionFilter.sharedMesh = collisionMesh;
            var meshCollider = collisionGo.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = collisionMesh;
            meshCollider.convex = false;
            Physics.SyncTransforms();
        }

        static Mesh BuildPlainsTopCollisionMesh(
            float plainsBaseY,
            float radius,
            float innerRadius,
            int radialRings,
            int angularSegments,
            System.Func<float, float, bool> excludeTriangleAt = null)
        {
            var vertices = new List<Vector3>();
            for (int segment = 0; segment < angularSegments; segment++)
            {
                for (int ring = 0; ring <= radialRings; ring++)
                {
                    float ringRadius = GetRingRadius(ring, radialRings, innerRadius, radius);
                    float angle = segment / (float)angularSegments * Mathf.PI * 2f;
                    float x = Mathf.Cos(angle) * ringRadius;
                    float z = Mathf.Sin(angle) * ringRadius;
                    float y = SamplePlainsLocalY(x, z, plainsBaseY);
                    vertices.Add(new Vector3(x, y, z));
                }
            }

            int ColumnVertex(int segment, int ring) => segment * (radialRings + 1) + ring;
            var triangles = new List<int>();
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
                    if (excludeTriangleAt == null
                        || (!ShouldExcludePlainsTriangleVertices(p0, p1, p2)
                            && !excludeTriangleAt(triCenterA.x, triCenterA.z)))
                    {
                        triangles.Add(bottomLeft);
                        triangles.Add(topLeft);
                        triangles.Add(topRight);
                    }

                    Vector3 p3 = vertices[bottomRight];
                    Vector3 triCenterB = (p0 + p2 + p3) / 3f;
                    if (excludeTriangleAt == null
                        || (!ShouldExcludePlainsTriangleVertices(p0, p2, p3)
                            && !excludeTriangleAt(triCenterB.x, triCenterB.z)))
                    {
                        triangles.Add(bottomLeft);
                        triangles.Add(topRight);
                        triangles.Add(bottomRight);
                    }
                }
            }

            return BuildSolidCollisionMesh(vertices, triangles, WorldScale.PlateauGroundThickness * 2f);
        }

        static Mesh BuildSolidCollisionMesh(List<Vector3> topVertices, List<int> topTriangles, float thickness)
        {
            int topCount = topVertices.Count;
            var vertices = new Vector3[topCount * 2];
            for (int i = 0; i < topCount; i++)
            {
                vertices[i] = topVertices[i];
                var top = topVertices[i];
                vertices[i + topCount] = new Vector3(top.x, top.y - thickness, top.z);
            }

            var triangles = new List<int>(topTriangles.Count * 2);
            triangles.AddRange(topTriangles);
            for (int i = 0; i < topTriangles.Count; i += 3)
            {
                triangles.Add(topTriangles[i] + topCount);
                triangles.Add(topTriangles[i + 2] + topCount);
                triangles.Add(topTriangles[i + 1] + topCount);
            }

            var mesh = new Mesh { name = "PlainsGroundCollision" };
            mesh.indexFormat = vertices.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh BuildPlainsMesh(
            float plainsBaseY,
            float radius,
            float innerRadius,
            int radialRings,
            int angularSegments,
            System.Func<float, float, bool> excludeTriangleAt = null)
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
                    float ringRadius = GetRingRadius(ring, radialRings, innerRadius, radius);
                    float angle = segment / (float)angularSegments * Mathf.PI * 2f;
                    float x = Mathf.Cos(angle) * ringRadius;
                    float z = Mathf.Sin(angle) * ringRadius;
                    float y = SamplePlainsLocalY(x, z, plainsBaseY);
                    vertices.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(x * 0.014f, z * 0.014f));
                }
            }

            int ColumnVertex(int segment, int ring) => segment * (radialRings + 1) + ring;

            void AddTriangle(int a, int b, int c, float centerX, float centerZ)
            {
                if (excludeTriangleAt != null)
                {
                    Vector3 p0 = vertices[a];
                    Vector3 p1 = vertices[b];
                    Vector3 p2 = vertices[c];
                    if (ShouldExcludePlainsTriangleVertices(p0, p1, p2)
                        || excludeTriangleAt(centerX, centerZ))
                        return;
                }

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

            var mesh = new Mesh { name = "PlainsGround" };
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

        static void CreateClouds(Transform parent, float plainsBaseY, float quarryNominalRadius)
        {
            var cloudRoot = new GameObject("PlainsClouds").transform;
            cloudRoot.SetParent(parent, false);
            float innerRadius = GetVisibleInnerRadius(quarryNominalRadius);

            var randomState = Random.state;
            Random.InitState(PlacementSeed + 913);

            for (int i = 0; i < CloudCount; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float distance = Random.Range(innerRadius, (innerRadius + WorldScale.Feet(420f)) * 0.96f);
                float x = Mathf.Cos(angle) * distance;
                float z = Mathf.Sin(angle) * distance;
                float groundY = SamplePlainsLocalY(x, z, plainsBaseY);
                float cloudBaseFeet = Random.Range(CloudMinHeightFeet, CloudMaxHeightFeet);
                float cloudCenterY = groundY + WorldScale.Feet(cloudBaseFeet);
                float cloudHeight = Random.Range(WorldScale.Feet(10f), WorldScale.Feet(24f));
                float cloudWidth = Random.Range(WorldScale.Feet(22f), WorldScale.Feet(48f));
                float cloudDepth = Random.Range(WorldScale.Feet(14f), WorldScale.Feet(30f));

                var cloud = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cloud.name = $"PlainsCloud_{i}";
                cloud.transform.SetParent(cloudRoot, false);
                cloud.transform.localPosition = new Vector3(x, cloudCenterY, z);
                cloud.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                cloud.transform.localScale = new Vector3(cloudWidth, cloudHeight, cloudDepth);
                cloud.GetComponent<Renderer>().sharedMaterial = GetCloudMaterial();
                Object.Destroy(cloud.GetComponent<Collider>());
            }

            Random.state = randomState;
        }

        static Material GetCloudMaterial()
        {
            if (cloudMaterial != null)
                return cloudMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            cloudMaterial = shader != null ? new Material(shader) : PrimitiveFactory.CreateColorMaterial(Color.white);
            cloudMaterial.name = "PlainsCloud";
            cloudMaterial.SetFloat("_Surface", 1f);
            cloudMaterial.SetFloat("_Blend", 0f);
            cloudMaterial.SetFloat("_Smoothness", 0.02f);
            cloudMaterial.SetColor("_BaseColor", new Color(0.88f, 0.92f, 0.96f, 0.16f));
            cloudMaterial.renderQueue = (int)RenderQueue.Transparent;
            cloudMaterial.SetOverrideTag("RenderType", "Transparent");
            cloudMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            cloudMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            cloudMaterial.SetInt("_ZWrite", 0);
            cloudMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            cloudMaterial.EnableKeyword("_ALPHABLEND_ON");
            cloudMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            return cloudMaterial;
        }
    }
}
