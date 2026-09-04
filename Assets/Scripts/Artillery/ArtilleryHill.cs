using System.Collections.Generic;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.Artillery
{
    public class ArtilleryHill : MonoBehaviour
    {
        const int MaxHeightPixels = 168;

        int originColumn;
        int columns;
        int rows;
        bool[] filled;
        float cell;
        float depth;
        Mesh mesh;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        public static ArtilleryHill Create(
            Transform parent,
            Vector3 fieldOrigin,
            string name,
            byte[] heights,
            int originColumn)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = fieldOrigin;

            var hill = go.AddComponent<ArtilleryHill>();
            hill.originColumn = originColumn;
            hill.columns = heights.Length;
            hill.rows = MaxHeightPixels;
            hill.cell = ArtilleryFieldProfile.Pixel;
            hill.depth = 1.72f;
            hill.filled = new bool[hill.columns * hill.rows];

            for (int x = 0; x < hill.columns; x++)
            {
                int h = Mathf.Clamp(heights[x], 0, hill.rows);
                for (int y = 0; y < h; y++)
                    hill.filled[x * hill.rows + y] = true;
            }

            hill.meshFilter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[]
            {
                PrimitiveFactory.CreateColorMaterial(new Color(0.27f, 0.24f, 0.20f), 0.12f),
                PrimitiveFactory.CreateColorMaterial(new Color(0.32f, 0.42f, 0.18f), 0.08f),
                PrimitiveFactory.CreateColorMaterial(new Color(0.16f, 0.15f, 0.13f), 0.18f)
            };

            hill.mesh = new Mesh { name = name + "Mesh" };
            hill.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            hill.meshFilter.sharedMesh = hill.mesh;
            hill.meshCollider = go.AddComponent<MeshCollider>();
            hill.RebuildMesh();
            return hill;
        }

        public void Carve(Vector3 worldPoint, float radius)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            int cx = Mathf.FloorToInt(local.x / cell) - originColumn;
            int cy = Mathf.FloorToInt(local.y / cell);
            int r = Mathf.CeilToInt(radius / cell);
            bool changed = false;

            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (dx * dx + dy * dy > r * r)
                        continue;

                    int x = cx + dx;
                    int y = cy + dy;
                    if (x < 0 || x >= columns || y < 0 || y >= rows)
                        continue;
                    int i = x * rows + y;
                    if (!filled[i])
                        continue;
                    filled[i] = false;
                    changed = true;
                }
            }

            if (changed)
                RebuildMesh();
        }

        public Vector3 SurfacePoint(int imageColumn)
        {
            int x = imageColumn - originColumn;
            if (x < 0 || x >= columns)
                return transform.position;

            int top = ColumnTop(x);
            float wx = (originColumn + x + 0.5f) * cell;
            float wy = top * cell;
            return transform.position + new Vector3(wx, wy, 0f);
        }

        public void CreateBuildingPads(ArtilleryBuildingPad[] pads)
        {
            if (pads == null)
                return;

            foreach (var pad in pads)
            {
                int localStart = pad.StartColumn - originColumn;
                int localEnd = pad.EndColumn - originColumn;
                if (localEnd < 0 || localStart >= columns)
                    continue;

                localStart = Mathf.Clamp(localStart, 0, columns - 1);
                localEnd = Mathf.Clamp(localEnd, 0, columns - 1);
                float x0 = (originColumn + localStart) * cell;
                float x1 = (originColumn + localEnd + 1) * cell;
                float y = pad.HeightPixels * cell;
                float z = depth * 0.5f;

                var marker = new GameObject(pad.Name).transform;
                marker.SetParent(transform, false);
                marker.localPosition = new Vector3((x0 + x1) * 0.5f, y, 0f);
                var padComp = marker.gameObject.AddComponent<ArtilleryBuildingPadMarker>();
                padComp.Configure(new Vector3(x1 - x0, 0f, z));
            }
        }

        int ColumnTop(int x)
        {
            int top = 0;
            int baseIndex = x * rows;
            for (int y = 0; y < rows; y++)
            {
                if (filled[baseIndex + y])
                    top = y + 1;
            }

            return top;
        }

        void RebuildMesh()
        {
            var rock = new MeshLists();
            var moss = new MeshLists();
            var dark = new MeshLists();

            for (int x = 0; x < columns; x++)
            {
                int y = 0;
                while (y < rows)
                {
                    if (!IsFilled(x, y))
                    {
                        y++;
                        continue;
                    }

                    int y1 = y + 1;
                    while (y1 < rows && IsFilled(x, y1))
                        y1++;

                    EmitColumnRun(x, y, y1, rock, moss, dark);
                    y = y1;
                }
            }

            mesh.Clear();
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var rockTris = new List<int>();
            var mossTris = new List<int>();
            var darkTris = new List<int>();

            rock.AppendTo(verts, norms, uvs, rockTris);
            moss.AppendTo(verts, norms, uvs, mossTris);
            dark.AppendTo(verts, norms, uvs, darkTris);

            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 3;
            mesh.SetTriangles(rockTris, 0);
            mesh.SetTriangles(mossTris, 1);
            mesh.SetTriangles(darkTris, 2);
            mesh.RecalculateBounds();
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }

        void EmitColumnRun(int x, int y0, int y1, MeshLists rock, MeshLists moss, MeshLists dark)
        {
            float x0 = (originColumn + x) * cell;
            float x1 = x0 + cell;
            float yy0 = y0 * cell;
            float yy1 = y1 * cell;
            float front = FrontZ(x, y0, y1);
            float back = front + DepthFor(y0, y1);
            bool leftOpen = x == 0 || !RangeFilled(x - 1, y0, y1);
            bool rightOpen = x == columns - 1 || !RangeFilled(x + 1, y0, y1);
            bool topOpen = y1 >= rows || !IsFilled(x, y1);
            bool bottomOpen = y0 == 0 || !IsFilled(x, y0 - 1);
            bool steep = IsSteep(x);

            var frontLists = steep ? dark : rock;
            frontLists.AddQuad(
                new Vector3(x0, yy0, front),
                new Vector3(x1, yy0, front),
                new Vector3(x1, yy1, front),
                new Vector3(x0, yy1, front),
                Vector3.back);

            rock.AddQuad(
                new Vector3(x1, yy0, back),
                new Vector3(x0, yy0, back),
                new Vector3(x0, yy1, back),
                new Vector3(x1, yy1, back),
                Vector3.forward);

            if (leftOpen)
            {
                dark.AddQuad(
                    new Vector3(x0, yy0, back),
                    new Vector3(x0, yy0, front),
                    new Vector3(x0, yy1, front),
                    new Vector3(x0, yy1, back),
                    Vector3.left);
            }

            if (rightOpen)
            {
                dark.AddQuad(
                    new Vector3(x1, yy0, front),
                    new Vector3(x1, yy0, back),
                    new Vector3(x1, yy1, back),
                    new Vector3(x1, yy1, front),
                    Vector3.right);
            }

            if (topOpen)
            {
                moss.AddQuad(
                    new Vector3(x0, yy1, front),
                    new Vector3(x1, yy1, front),
                    new Vector3(x1, yy1, back),
                    new Vector3(x0, yy1, back),
                    Vector3.up);
            }

            if (bottomOpen && y0 > 0)
            {
                rock.AddQuad(
                    new Vector3(x0, yy0, back),
                    new Vector3(x1, yy0, back),
                    new Vector3(x1, yy0, front),
                    new Vector3(x0, yy0, front),
                    Vector3.down);
            }
        }

        float FrontZ(int x, int y0, int y1)
        {
            float n = Hash(originColumn + x, y0) * 0.10f - 0.05f;
            if (IsFlatRun(x))
                n *= 0.15f;
            return n;
        }

        float DepthFor(int y0, int y1)
        {
            float t = (y0 + y1) * 0.5f / MaxHeightPixels;
            return Mathf.Lerp(2.05f, 1.42f, t);
        }

        bool IsSteep(int x)
        {
            int left = x > 0 ? ColumnTop(x - 1) : ColumnTop(x);
            int right = x < columns - 1 ? ColumnTop(x + 1) : ColumnTop(x);
            int self = ColumnTop(x);
            return Mathf.Abs(self - left) > 8 || Mathf.Abs(self - right) > 8;
        }

        bool IsFlatRun(int x)
        {
            int self = ColumnTop(x);
            int left = x > 0 ? ColumnTop(x - 1) : self;
            int right = x < columns - 1 ? ColumnTop(x + 1) : self;
            return Mathf.Abs(self - left) <= 2 && Mathf.Abs(self - right) <= 2 && self > 40;
        }

        bool IsFilled(int x, int y) => filled[x * rows + y];

        bool RangeFilled(int x, int y0, int y1)
        {
            for (int y = y0; y < y1; y++)
            {
                if (!IsFilled(x, y))
                    return false;
            }

            return true;
        }

        static float Hash(int x, int y)
        {
            int n = x * 374761393 + y * 668265263;
            n = (n ^ (n >> 13)) * 1274126177;
            return ((n ^ (n >> 16)) & 0x7FFFFFFF) / (float)int.MaxValue;
        }

        class MeshLists
        {
            public readonly List<Vector3> Verts = new List<Vector3>();
            public readonly List<Vector3> Norms = new List<Vector3>();
            public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<int> Tris = new List<int>();

            public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                int i = Verts.Count;
                Verts.Add(a);
                Verts.Add(b);
                Verts.Add(c);
                Verts.Add(d);
                Norms.Add(normal);
                Norms.Add(normal);
                Norms.Add(normal);
                Norms.Add(normal);
                Uvs.Add(new Vector2(0f, 0f));
                Uvs.Add(new Vector2(1f, 0f));
                Uvs.Add(new Vector2(1f, 1f));
                Uvs.Add(new Vector2(0f, 1f));
                Tris.Add(i);
                Tris.Add(i + 2);
                Tris.Add(i + 1);
                Tris.Add(i);
                Tris.Add(i + 3);
                Tris.Add(i + 2);
            }

            public void AppendTo(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris)
            {
                int offset = verts.Count;
                verts.AddRange(Verts);
                norms.AddRange(Norms);
                uvs.AddRange(Uvs);
                for (int i = 0; i < Tris.Count; i++)
                    tris.Add(Tris[i] + offset);
            }
        }
    }
}
