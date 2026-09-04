using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.Artillery
{
    public class ArtilleryWindFlag : MonoBehaviour
    {
        const int HoistSegments = 5;
        const int SpanSegments = 10;
        const int PoleTopHeightPixels = 108;
        const int PoleBottomTrimPixels = 80;
        const int VerticalOffsetPixels = 35;
        const int YourTurnLabelOffsetPixels = 28;
        const int FlagLengthPixels = 32;
        const int FlagHoistHeightPixels = 18;
        const float BaseFlapAmplitudePixels = 1f;
        const float MaxFlapAmplitudePixels = 4f;
        const float BaseFlapSpeed = 3.5f;
        const float MaxFlapSpeed = 9f;
        const float WindSpeedReference = 1.75f;

        static readonly Color PoleColor = new Color(0.42f, 0.34f, 0.24f);
        static readonly Color FlagColor = Color.white;

        ArtilleryBattleController windSource;
        Mesh flagMesh;
        Vector3[] flagVertices;
        int[] flagTriangles;
        float centerX;
        float poleWidth;
        float poleBottomY;
        float poleTopY;
        float flagLength;
        float flagHoistVertical;
        float pixelScaleX;
        float pixelScaleY;
        float depth;
        float verticalOffsetY;
        float labelCenterY;
        float yourTurnLabelCenterY;
        float lastWindDirection = 1f;

        public float WindLabelCenterX => centerX;
        public float WindLabelCenterY => labelCenterY;
        public float YourTurnLabelCenterY => yourTurnLabelCenterY;
        public float WindLabelDepth => depth;

        public void Build(float screenWidth, float xScale, float yScale, float flagDepth)
        {
            depth = flagDepth;
            centerX = screenWidth * 0.5f;
            pixelScaleX = ArtilleryFieldProfile.Pixel * xScale;
            pixelScaleY = ArtilleryFieldProfile.Pixel * yScale;
            verticalOffsetY = VerticalOffsetPixels * pixelScaleY;
            poleWidth = 5f * pixelScaleX;
            poleBottomY = PoleBottomTrimPixels * pixelScaleY + verticalOffsetY;
            poleTopY = PoleTopHeightPixels * pixelScaleY + verticalOffsetY;
            labelCenterY = PoleBottomTrimPixels * pixelScaleY * 0.5f + verticalOffsetY;
            yourTurnLabelCenterY = poleTopY + YourTurnLabelOffsetPixels * pixelScaleY;
            flagLength = FlagLengthPixels * pixelScaleX;
            flagHoistVertical = FlagHoistHeightPixels * pixelScaleY;

            CreatePole();
            CreateFlagMesh();
        }

        public void Bind(ArtilleryBattleController battle)
        {
            windSource = battle;
        }

        void CreatePole()
        {
            var pole = new GameObject("WindFlagPole");
            pole.transform.SetParent(transform, false);

            var meshFilter = pole.AddComponent<MeshFilter>();
            var meshRenderer = pole.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = BuildSolidMaterial(PoleColor);
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            float halfWidth = poleWidth * 0.5f;
            var vertices = new[]
            {
                new Vector3(centerX - halfWidth, poleBottomY, depth),
                new Vector3(centerX + halfWidth, poleBottomY, depth),
                new Vector3(centerX + halfWidth, poleTopY, depth),
                new Vector3(centerX - halfWidth, poleTopY, depth)
            };
            var triangles = new[] { 0, 2, 1, 0, 3, 2 };
            var mesh = new Mesh { name = "WindFlagPoleMesh" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            meshFilter.sharedMesh = mesh;
        }

        void CreateFlagMesh()
        {
            var flag = new GameObject("WindFlagCloth");
            flag.transform.SetParent(transform, false);

            var meshFilter = flag.AddComponent<MeshFilter>();
            var meshRenderer = flag.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = BuildSolidMaterial(FlagColor);
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            int hoistColumns = HoistSegments + 1;
            int spanColumns = SpanSegments + 1;
            flagVertices = new Vector3[hoistColumns * spanColumns];
            flagTriangles = BuildFlagGridTriangles(HoistSegments, SpanSegments);
            flagMesh = new Mesh { name = "WindFlagClothMesh" };
            flagMesh.vertices = flagVertices;
            flagMesh.triangles = flagTriangles;
            flagMesh.RecalculateNormals();
            meshFilter.sharedMesh = flagMesh;
        }

        static int[] BuildFlagGridTriangles(int hoistSegments, int spanSegments)
        {
            int hoistColumns = hoistSegments + 1;
            int spanColumns = spanSegments + 1;
            var triangles = new int[hoistSegments * spanSegments * 6];
            int triangleIndex = 0;

            for (int i = 0; i < hoistSegments; i++)
            {
                for (int j = 0; j < spanSegments; j++)
                {
                    int a = i * spanColumns + j;
                    int b = a + spanColumns;
                    int c = a + 1;
                    int d = b + 1;

                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = d;
                }
            }

            return triangles;
        }

        static int VertexIndex(int hoistIndex, int spanIndex, int spanColumns)
        {
            return hoistIndex * spanColumns + spanIndex;
        }

        void LateUpdate()
        {
            if (flagMesh == null || flagVertices == null)
                return;

            float wind = windSource != null ? windSource.Wind : 0f;
            float windSpeed = Mathf.Abs(wind);
            if (windSpeed > 0.05f)
                lastWindDirection = Mathf.Sign(wind);

            float windDirection = lastWindDirection;
            float windSpeedSquared = windSpeed * windSpeed;
            float referenceSquared = WindSpeedReference * WindSpeedReference;
            float theta = Mathf.PI * 0.5f * windSpeedSquared / (windSpeedSquared + referenceSquared);

            var vertical = new Vector2(0f, -1f);
            var horizontal = new Vector2(windDirection, 0f);
            var mainAxis = vertical * Mathf.Cos(theta) + horizontal * Mathf.Sin(theta);
            var flapAxis = new Vector2(-mainAxis.y, mainAxis.x);

            float poleSideX = centerX + windDirection * poleWidth * 0.5f;
            var topCorner = new Vector2(poleSideX, poleTopY);
            var bottomHoist = new Vector2(poleSideX, poleTopY - flagHoistVertical);
            var tip = bottomHoist + mainAxis * flagLength;

            float windStrength = Mathf.Clamp01(windSpeed / Mathf.Max(0.01f, ArtilleryRockPhysics.WindMax));
            float flapAmplitude = Mathf.Lerp(
                BaseFlapAmplitudePixels * pixelScaleY,
                MaxFlapAmplitudePixels * pixelScaleY,
                windStrength * windStrength);
            float flapSpeed = Mathf.Lerp(BaseFlapSpeed, MaxFlapSpeed, windStrength);
            float timePhase = Time.time * flapSpeed;

            int spanColumns = SpanSegments + 1;
            for (int i = 0; i <= HoistSegments; i++)
            {
                float hoistT = i / (float)HoistSegments;
                var polePoint = Vector2.Lerp(topCorner, bottomHoist, hoistT);

                for (int j = 0; j <= SpanSegments; j++)
                {
                    float spanT = j / (float)SpanSegments;
                    var position = polePoint * (1f - spanT) + tip * spanT;
                    float flap = Mathf.Sin(timePhase + spanT * Mathf.PI * 1.6f + hoistT * 0.8f)
                        * flapAmplitude
                        * spanT;
                    position += flapAxis * flap;

                    int index = VertexIndex(i, j, spanColumns);
                    flagVertices[index] = new Vector3(position.x, position.y, depth);
                }
            }

            flagMesh.vertices = flagVertices;
            flagMesh.RecalculateNormals();
        }

        static Material BuildSolidMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            var material = new Material(shader);
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)CullMode.Off);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            material.color = color;
            return material;
        }
    }
}
