using System.Collections.Generic;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class LandQuarry2FloorBuilder
    {
        public static GameObject CreateFloor(Transform parent, float floorLocalY, Material material)
        {
            const int radialSegments = 72;
            const int ringCount = 12;

            var floorGo = new GameObject("Quarry2Floor");
            floorGo.transform.SetParent(parent, false);

            var mesh = BuildFloorMesh(floorLocalY, radialSegments, ringCount);
            var meshFilter = floorGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = floorGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            return floorGo;
        }

        public static void CreateFloorCollision(Transform parent, float floorLocalY)
        {
            const float colliderThickness = 0.36f;
            const int ringCount = 10;
            const int segmentsPerRing = 36;

            var root = new GameObject("Quarry2FloorCollision");
            root.transform.SetParent(parent, false);

            var centerCap = new GameObject("Quarry2FloorCenterCap");
            centerCap.transform.SetParent(root.transform, false);
            centerCap.transform.localPosition = new Vector3(0f, floorLocalY, 0f);
            var centerBox = centerCap.AddComponent<BoxCollider>();
            centerBox.size = new Vector3(2.4f, colliderThickness, 2.4f);

            for (int ring = 1; ring <= ringCount; ring++)
            {
                for (int segment = 0; segment < segmentsPerRing; segment++)
                {
                    float angle = segment / (float)segmentsPerRing * Mathf.PI * 2f;
                    float nextAngle = (segment + 1) / (float)segmentsPerRing * Mathf.PI * 2f;
                    float outerRadius = LandQuarry2Boundary.SampleEdgeDistance(angle);
                    float innerRadius = outerRadius * (ring - 1) / ringCount;
                    float midRadius = (innerRadius + outerRadius * (ring / (float)ringCount)) * 0.5f;
                    float radialDepth = Mathf.Max(0.35f, (outerRadius * ring / ringCount) - innerRadius) * 1.08f;
                    float segmentArc = Mathf.Abs(nextAngle - angle);
                    float segmentWidth = Mathf.Max(0.35f, midRadius * segmentArc * 1.06f);
                    float x = Mathf.Cos(angle) * midRadius;
                    float z = Mathf.Sin(angle) * midRadius;

                    var segmentGo = new GameObject($"Quarry2FloorCollider_{ring}_{segment}");
                    segmentGo.transform.SetParent(root.transform, false);
                    segmentGo.transform.localPosition = new Vector3(x, floorLocalY, z);
                    segmentGo.transform.localRotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);

                    var box = segmentGo.AddComponent<BoxCollider>();
                    box.size = new Vector3(segmentWidth, colliderThickness, radialDepth);
                }
            }
        }

        static Mesh BuildFloorMesh(float floorLocalY, int radialSegments, int ringCount)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            float uvScale = LandQuarry2Boundary.MaxEdgeDistance * 2.2f;

            vertices.Add(new Vector3(0f, floorLocalY, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));
            const int centerIndex = 0;

            int totalRings = ringCount + 1;
            for (int ring = 0; ring < totalRings; ring++)
            {
                for (int segment = 0; segment < radialSegments; segment++)
                {
                    float angle = segment / (float)radialSegments * Mathf.PI * 2f;
                    float edgeRadius = LandQuarry2Boundary.SampleEdgeDistance(angle);
                    float ringRadius = edgeRadius * (ring + 1f) / totalRings;
                    float x = Mathf.Cos(angle) * ringRadius;
                    float z = Mathf.Sin(angle) * ringRadius;
                    vertices.Add(new Vector3(x, floorLocalY, z));
                    uvs.Add(new Vector2(x / uvScale + 0.5f, z / uvScale + 0.5f));
                }
            }

            int RingVertex(int ring, int segment) => 1 + ring * radialSegments + segment;

            for (int segment = 0; segment < radialSegments; segment++)
            {
                int nextSegment = (segment + 1) % radialSegments;
                triangles.Add(centerIndex);
                triangles.Add(RingVertex(0, segment));
                triangles.Add(RingVertex(0, nextSegment));
            }

            for (int ring = 0; ring < totalRings - 1; ring++)
            {
                for (int segment = 0; segment < radialSegments; segment++)
                {
                    int nextSegment = (segment + 1) % radialSegments;
                    int bottomLeft = RingVertex(ring, segment);
                    int bottomRight = RingVertex(ring, nextSegment);
                    int topLeft = RingVertex(ring + 1, segment);
                    int topRight = RingVertex(ring + 1, nextSegment);

                    triangles.Add(bottomLeft);
                    triangles.Add(topLeft);
                    triangles.Add(topRight);
                    triangles.Add(bottomLeft);
                    triangles.Add(topRight);
                    triangles.Add(bottomRight);
                }
            }

            var mesh = new Mesh { name = "LandQuarry2Floor" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
