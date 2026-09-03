using System.Collections.Generic;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class LandQuarry2FloorBuilder
    {
        public static GameObject CreateFloor(
            Transform parent,
            Vector2 jarlCenterContent,
            float plainsBaseLocalY,
            float groundLocalYAtCenter,
            Material material)
        {
            const int radialSegments = 72;
            const int ringCount = 12;

            var floorGo = new GameObject("Quarry2Floor");
            floorGo.transform.SetParent(parent, false);

            var mesh = BuildFloorMesh(
                jarlCenterContent,
                plainsBaseLocalY,
                groundLocalYAtCenter,
                radialSegments,
                ringCount);
            var meshFilter = floorGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = floorGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            return floorGo;
        }

        public static GameObject CreateSnowApron(
            Transform parent,
            Vector2 jarlCenterContent,
            float plainsBaseLocalY,
            float groundLocalYAtCenter,
            Material material)
        {
            const float cellSizeFeet = 48f;
            float cellSize = WorldScale.Feet(cellSizeFeet);
            var lakeCenter = LakeCatalog.GetCenterLocal();
            float lakeRadius = LakeCatalog.GetNominalRadiusUnits();
            float minX = lakeCenter.x - lakeRadius;
            float maxX = lakeCenter.x + lakeRadius;
            float minZ = jarlCenterContent.y - LandQuarry2Boundary.MaxEdgeDistance;
            float maxZ = Mathf.Max(
                lakeCenter.y,
                LakeCatalog.GetBeachNorthEdgeZ() + WorldScale.Feet(30f));

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            float uvScale = lakeRadius * 2.2f;

            for (float contentX = minX; contentX <= maxX; contentX += cellSize)
            {
                for (float contentZ = minZ; contentZ <= maxZ; contentZ += cellSize)
                {
                    float centerX = contentX + cellSize * 0.5f;
                    float centerZ = contentZ + cellSize * 0.5f;
                    if (!LandQuarry2Boundary.IsSnowGroundLocal(centerX, centerZ))
                        continue;

                    if (LandQuarry2Boundary.ContainsLocal(centerX, centerZ))
                        continue;

                    float localX = centerX - jarlCenterContent.x;
                    float localZ = centerZ - jarlCenterContent.y;
                    float half = cellSize * 0.53f;
                    int baseIndex = vertices.Count;

                    float y00 = SampleFloorLocalY(contentX - half, contentZ - half, jarlCenterContent, plainsBaseLocalY, groundLocalYAtCenter);
                    float y10 = SampleFloorLocalY(contentX + half, contentZ - half, jarlCenterContent, plainsBaseLocalY, groundLocalYAtCenter);
                    float y11 = SampleFloorLocalY(contentX + half, contentZ + half, jarlCenterContent, plainsBaseLocalY, groundLocalYAtCenter);
                    float y01 = SampleFloorLocalY(contentX - half, contentZ + half, jarlCenterContent, plainsBaseLocalY, groundLocalYAtCenter);

                    vertices.Add(new Vector3(localX - half, y00, localZ - half));
                    vertices.Add(new Vector3(localX + half, y10, localZ - half));
                    vertices.Add(new Vector3(localX + half, y11, localZ + half));
                    vertices.Add(new Vector3(localX - half, y01, localZ + half));

                    uvs.Add(new Vector2((centerX - half - lakeCenter.x) / uvScale + 0.5f, (centerZ - half - lakeCenter.y) / uvScale + 0.5f));
                    uvs.Add(new Vector2((centerX + half - lakeCenter.x) / uvScale + 0.5f, (centerZ - half - lakeCenter.y) / uvScale + 0.5f));
                    uvs.Add(new Vector2((centerX + half - lakeCenter.x) / uvScale + 0.5f, (centerZ + half - lakeCenter.y) / uvScale + 0.5f));
                    uvs.Add(new Vector2((centerX - half - lakeCenter.x) / uvScale + 0.5f, (centerZ + half - lakeCenter.y) / uvScale + 0.5f));

                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 3);
                    triangles.Add(baseIndex + 2);
                }
            }

            if (vertices.Count == 0)
                return null;

            var apronGo = new GameObject("Quarry2SnowApron");
            apronGo.transform.SetParent(parent, false);

            var mesh = new Mesh { name = "LandQuarry2SnowApron" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var meshFilter = apronGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = apronGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            var collisionGo = new GameObject("Quarry2SnowApronCollision");
            collisionGo.transform.SetParent(apronGo.transform, false);
            var collisionFilter = collisionGo.AddComponent<MeshFilter>();
            collisionFilter.sharedMesh = mesh;
            var meshCollider = collisionGo.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;

            return apronGo;
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

        static float SampleFloorLocalY(
            float contentX,
            float contentZ,
            Vector2 jarlCenterContent,
            float plainsBaseLocalY,
            float groundLocalYAtCenter)
        {
            float contentGroundY = LandQuarry2Boundary.SampleSnowFloorLocalY(contentX, contentZ, plainsBaseLocalY);
            return contentGroundY - groundLocalYAtCenter;
        }

        static Mesh BuildFloorMesh(
            Vector2 jarlCenterContent,
            float plainsBaseLocalY,
            float groundLocalYAtCenter,
            int radialSegments,
            int ringCount)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            float uvScale = LandQuarry2Boundary.MaxEdgeDistance * 2.2f;

            float centerY = SampleFloorLocalY(jarlCenterContent.x, jarlCenterContent.y, jarlCenterContent, plainsBaseLocalY, groundLocalYAtCenter);
            vertices.Add(new Vector3(0f, centerY, 0f));
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
                    float contentX = jarlCenterContent.x + x;
                    float contentZ = jarlCenterContent.y + z;
                    float y = SampleFloorLocalY(contentX, contentZ, jarlCenterContent, plainsBaseLocalY, groundLocalYAtCenter);
                    vertices.Add(new Vector3(x, y, z));
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
