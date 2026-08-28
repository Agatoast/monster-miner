using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.World
{
    public static class PlainsGroundBuilder
    {
        public const float HillAmplitude = 0.16f;

        const float NoiseOffsetX = 17.3f;
        const float NoiseOffsetZ = 41.7f;

        public static float SamplePlainsLocalY(float localX, float localZ, float baseLocalY)
        {
            float large = Mathf.PerlinNoise(localX * 0.055f + NoiseOffsetX, localZ * 0.055f + NoiseOffsetZ);
            float medium = Mathf.PerlinNoise(localX * 0.13f + 3.1f, localZ * 0.13f + 9.4f);
            float blend = large * 0.68f + medium * 0.32f;
            float roll = (blend * 2f - 1f) * HillAmplitude;
            return baseLocalY + roll;
        }

        public static float SampleGroundLocalY(
            float localX,
            float localZ,
            float plateauNominalRadius,
            float floorTopLocalY,
            float bowlDepth,
            float plainsBaseLocalY)
        {
            return SamplePlainsLocalY(localX, localZ, plainsBaseLocalY);
        }

        public static void BuildGround(
            Transform parent,
            CavernBounds bounds,
            float plainsBaseLocalY)
        {
            var groundGo = new GameObject("PlainsGround");
            groundGo.transform.SetParent(parent, false);

            BuildGroundMeshes(
                bounds.Radius,
                plainsBaseLocalY,
                out var renderMesh,
                out var topCollisionMesh);

            var meshFilter = groundGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = renderMesh;

            var meshRenderer = groundGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = CavernSurfaceMaterialFactory.GetGrassMaterial();

            var topCollisionGo = new GameObject("PlainsGroundTopCollision");
            topCollisionGo.transform.SetParent(groundGo.transform, false);

            var topCollisionFilter = topCollisionGo.AddComponent<MeshFilter>();
            topCollisionFilter.sharedMesh = topCollisionMesh;

            var topMeshCollider = topCollisionGo.AddComponent<MeshCollider>();
            topMeshCollider.sharedMesh = topCollisionMesh;
            topMeshCollider.convex = false;

            Physics.SyncTransforms();
        }

        static void BuildGroundMeshes(
            float plateauNominalRadius,
            float plainsBaseLocalY,
            out Mesh renderMesh,
            out Mesh topCollisionMesh)
        {
            const int radialRings = 28;
            const int angularSegments = 56;
            float maxExtent = PlateauBoundary.MaxExtent(plateauNominalRadius);
            float innerRadius = WorldScale.Feet(2f);

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var grassTriangles = new List<int>();
            var surfaceTriangles = new List<int>();

            float centerY = SamplePlainsLocalY(0f, 0f, plainsBaseLocalY);
            vertices.Add(new Vector3(0f, centerY, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));
            const int centerIndex = 0;

            for (int ring = 0; ring <= radialRings; ring++)
            {
                for (int segment = 0; segment < angularSegments; segment++)
                {
                    float angle = segment / (float)angularSegments * Mathf.PI * 2f;
                    float plateauEdge = PlateauBoundary.SamplePlateauEdgeDistance(angle, plateauNominalRadius);
                    float t = ring / (float)radialRings;
                    float radius = Mathf.Lerp(innerRadius, plateauEdge, t);
                    float x = Mathf.Cos(angle) * radius;
                    float z = Mathf.Sin(angle) * radius;
                    float y = SamplePlainsLocalY(x, z, plainsBaseLocalY);
                    vertices.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(x / (maxExtent * 2f) + 0.5f, z / (maxExtent * 2f) + 0.5f));
                }
            }

            int RingVertex(int ring, int segment) => 1 + ring * angularSegments + segment;

            void AddTriangle(int a, int b, int c)
            {
                grassTriangles.Add(a);
                grassTriangles.Add(b);
                grassTriangles.Add(c);

                surfaceTriangles.Add(a);
                surfaceTriangles.Add(b);
                surfaceTriangles.Add(c);
            }

            for (int segment = 0; segment < angularSegments; segment++)
            {
                int nextSegment = (segment + 1) % angularSegments;
                AddTriangle(centerIndex, RingVertex(0, segment), RingVertex(0, nextSegment));
            }

            for (int ring = 0; ring < radialRings; ring++)
            {
                for (int segment = 0; segment < angularSegments; segment++)
                {
                    int nextSegment = (segment + 1) % angularSegments;
                    int bottomLeft = RingVertex(ring, segment);
                    int bottomRight = RingVertex(ring, nextSegment);
                    int topLeft = RingVertex(ring + 1, segment);
                    int topRight = RingVertex(ring + 1, nextSegment);

                    AddTriangle(bottomLeft, topLeft, topRight);
                    AddTriangle(bottomLeft, topRight, bottomRight);
                }
            }

            renderMesh = new Mesh { name = "PlateauGrassGround" };
            renderMesh.indexFormat = vertices.Count > 65000
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;
            renderMesh.SetVertices(vertices);
            renderMesh.SetUVs(0, uvs);
            renderMesh.SetTriangles(grassTriangles, 0);
            renderMesh.RecalculateNormals();
            renderMesh.RecalculateBounds();

            topCollisionMesh = BuildSolidCollisionMesh(vertices, surfaceTriangles, WorldScale.PlateauGroundThickness);
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

            var mesh = new Mesh { name = "PlateauGroundSolidCollision" };
            mesh.indexFormat = vertices.Length > 65000
                ? IndexFormat.UInt32
                : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
