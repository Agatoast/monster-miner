using System.Collections.Generic;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class QuarryFloorBuilder
    {
        public static float SampleLocalY(
            float localX,
            float localZ,
            float nominalRadius,
            float edgeLocalY,
            float bowlDepth)
        {
            return edgeLocalY;
        }

        public static GameObject CreateBowlFloor(
            Transform parent,
            float nominalRadius,
            float edgeLocalY,
            float bowlDepth,
            Material material,
            int radialSegments = 36,
            int ringCount = 18)
        {
            var floorGo = new GameObject("Floor");
            floorGo.transform.SetParent(parent, false);

            var mesh = BuildFlatFloorMesh(nominalRadius, edgeLocalY, radialSegments, ringCount);
            var meshFilter = floorGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = floorGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;

            return floorGo;
        }

        public static void CreateBowlCollision(
            Transform parent,
            float nominalRadius,
            float edgeLocalY,
            float bowlDepth,
            int ringCount = 14,
            int segmentsPerRing = 28)
        {
            const float colliderThickness = 0.36f;

            var root = new GameObject("FloorCollision");
            root.transform.SetParent(parent, false);

            var centerCap = new GameObject("FloorCenterCap");
            centerCap.transform.SetParent(root.transform, false);
            centerCap.transform.localPosition = new Vector3(0f, edgeLocalY, 0f);
            var centerBox = centerCap.AddComponent<BoxCollider>();
            centerBox.size = new Vector3(2.4f, colliderThickness, 2.4f);

            for (int ring = 1; ring <= ringCount; ring++)
            {
                for (int segment = 0; segment < segmentsPerRing; segment++)
                {
                    float angle = segment / (float)segmentsPerRing * Mathf.PI * 2f;
                    float nextAngle = (segment + 1) / (float)segmentsPerRing * Mathf.PI * 2f;
                    float localRadius = PlateauBoundary.SamplePlateauEdgeDistance(angle, nominalRadius);
                    float innerRadius = localRadius * (ring - 1) / ringCount;
                    float outerRadius = localRadius * ring / ringCount;
                    float midRadius = (innerRadius + outerRadius) * 0.5f;
                    float radialDepth = Mathf.Max(0.35f, outerRadius - innerRadius) * 1.08f;
                    float segmentArc = Mathf.Abs(nextAngle - angle);
                    float segmentWidth = Mathf.Max(0.35f, midRadius * segmentArc * 1.06f);
                    float x = Mathf.Cos(angle) * midRadius;
                    float z = Mathf.Sin(angle) * midRadius;

                    var segmentGo = new GameObject($"FloorCollider_{ring}_{segment}");
                    segmentGo.transform.SetParent(root.transform, false);
                    segmentGo.transform.localPosition = new Vector3(x, edgeLocalY, z);
                    segmentGo.transform.localRotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);

                    var box = segmentGo.AddComponent<BoxCollider>();
                    box.size = new Vector3(segmentWidth, colliderThickness, radialDepth);
                }
            }
        }

        static Mesh BuildFlatFloorMesh(
            float nominalRadius,
            float edgeLocalY,
            int radialSegments,
            int ringCount)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            float uvScale = PlateauBoundary.MaxExtent(nominalRadius) * 2f;

            vertices.Add(new Vector3(0f, edgeLocalY, 0f));
            uvs.Add(new Vector2(0.5f, 0.5f));
            const int centerIndex = 0;

            int totalRings = ringCount + 1;
            for (int ring = 0; ring < totalRings; ring++)
            {
                for (int segment = 0; segment < radialSegments; segment++)
                {
                    float angle = segment / (float)radialSegments * Mathf.PI * 2f;
                    float localRadius = PlateauBoundary.SamplePlateauEdgeDistance(angle, nominalRadius);
                    float ringRadius = localRadius * (ring + 1f) / totalRings;
                    float x = Mathf.Cos(angle) * ringRadius;
                    float z = Mathf.Sin(angle) * ringRadius;
                    vertices.Add(new Vector3(x, edgeLocalY, z));
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

            var mesh = new Mesh { name = "QuarryFlatFloor" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
