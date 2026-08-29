using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.World
{
    public static class PlateauWallBuilder
    {
        const int AngularSegments = 80;
        const int VerticalRings = 48;

        public static void Build(
            Transform parent,
            CavernBounds bounds,
            float plainsBaseLocalY,
            float lowerBaseLocalY)
        {
            var root = new GameObject("CliffWalls");
            root.transform.SetParent(parent, false);
            BuildWallMesh(
                root.transform,
                bounds.Radius,
                plainsBaseLocalY,
                lowerBaseLocalY,
                bounds.FloorTopLocalY,
                bounds.BowlDepth);
        }

        static void BuildWallMesh(
            Transform parent,
            float quarryNominalRadius,
            float plainsBaseLocalY,
            float lowerBaseLocalY,
            float floorTopLocalY,
            float bowlDepth)
        {
            var meshGo = new GameObject("PlateauCliffWalls");
            meshGo.transform.SetParent(parent, false);

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            for (int ring = 0; ring < VerticalRings; ring++)
            {
                float descentT = ring / (float)(VerticalRings - 1);
                for (int segment = 0; segment < AngularSegments; segment++)
                {
                    float angle = segment / (float)AngularSegments * Mathf.PI * 2f;
                    float edgeDistance = PlateauBoundary.SamplePlateauEdgeDistance(angle, quarryNominalRadius);
                    float outward = PlateauWallGeometry.SampleOutwardOffset(descentT);
                    float edgeX = Mathf.Cos(angle) * edgeDistance;
                    float edgeZ = Mathf.Sin(angle) * edgeDistance;
                    float radius = edgeDistance + outward;
                    float x = Mathf.Cos(angle) * radius;
                    float z = Mathf.Sin(angle) * radius;

                    float topY = PlainsGroundBuilder.SampleGroundLocalY(
                        edgeX,
                        edgeZ,
                        quarryNominalRadius,
                        floorTopLocalY,
                        bowlDepth,
                        plainsBaseLocalY);
                    float bottomY = PlainsWorldBuilder.SamplePlainsLocalY(x, z, lowerBaseLocalY);
                    float y = Mathf.Lerp(topY, bottomY, descentT);

                    float rock = Mathf.PerlinNoise(x * 0.07f + 18.4f, z * 0.07f + 6.1f + descentT * 2.4f);
                    float rugged = Mathf.PerlinNoise(x * 0.16f + 91.2f, z * 0.16f + 33.8f);
                    outward += (rock * 0.72f + rugged * 0.28f - 0.5f) * WorldScale.Feet(1.4f);
                    radius = edgeDistance + outward;
                    x = Mathf.Cos(angle) * radius;
                    z = Mathf.Sin(angle) * radius;
                    y += (rock - 0.5f) * WorldScale.Feet(2.5f) * (0.25f + descentT * 0.75f);

                    vertices.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2(segment / (float)AngularSegments, descentT));
                }
            }

            int VertexIndex(int ring, int segment) => ring * AngularSegments + segment;

            for (int ring = 0; ring < VerticalRings - 1; ring++)
            {
                for (int segment = 0; segment < AngularSegments; segment++)
                {
                    int nextSegment = (segment + 1) % AngularSegments;
                    int bottomLeft = VertexIndex(ring, segment);
                    int bottomRight = VertexIndex(ring, nextSegment);
                    int topLeft = VertexIndex(ring + 1, segment);
                    int topRight = VertexIndex(ring + 1, nextSegment);

                    triangles.Add(bottomLeft);
                    triangles.Add(topLeft);
                    triangles.Add(topRight);
                    triangles.Add(bottomLeft);
                    triangles.Add(topRight);
                    triangles.Add(bottomRight);
                }
            }

            var mesh = new Mesh { name = "PlateauCliffWalls" };
            mesh.indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var meshFilter = meshGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            var meshRenderer = meshGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = CreateOpaqueWallMaterial();

            var wallCollider = meshGo.AddComponent<MeshCollider>();
            wallCollider.sharedMesh = mesh;
            wallCollider.convex = false;
        }

        static Material CreateOpaqueWallMaterial()
        {
            var material = new Material(CavernSurfaceMaterialFactory.GetWallMaterial());
            material.SetInt("_Cull", (int)CullMode.Off);
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 0f);
            return material;
        }
    }
}
