using MonsterMiner.Core;
using MonsterMiner.Player;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class TruckDashboardDisplay
    {
        const float GaugeMaxMph = 70f;
        const float DashHeightPixels = 300f;
        public const float DashboardHeightPixels = DashHeightPixels;
        const float SpeedometerRightOffsetPixels = 200f;
        const float SteerMaxDegrees = 55f;
        const float SteerTurnSpeed = 260f;
        const int GaugeTickCount = 8;

        static readonly Color WheelBrown = new Color(0.42f, 0.26f, 0.12f);

        static Texture2D pixel;
        static Texture2D circle;
        static Material glMaterial;
        static float wheelAngle;

        public static void Draw(GameContext ctx)
        {
            var mount = ctx?.Player?.GetComponent<PlayerVehicleMount>();
            if (mount == null || !mount.IsDriving || mount.CurrentTruck == null)
                return;

            float mph = mount.CurrentTruck.DisplaySpeedMph;
            var dash = GetDashboardRect();

            DrawDashboardBackground(dash);
            DrawSteeringWheel(dash);

            float gaugeSize = Mathf.Min(dash.height * 0.9f, Screen.width * 0.2f);
            float gaugeX = Screen.width * 0.70f + SpeedometerRightOffsetPixels;
            float gaugeY = dash.y + (dash.height - gaugeSize) * 0.42f;
            DrawSpeedometer(new Rect(gaugeX, gaugeY, gaugeSize, gaugeSize), mph);
        }

        static Rect GetDashboardRect()
        {
            return new Rect(0f, Screen.height - DashHeightPixels, Screen.width, DashHeightPixels);
        }

        static void DrawDashboardBackground(Rect dash)
        {
            GUI.color = Color.black;
            GUI.DrawTexture(dash, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        static void DrawSteeringWheel(Rect dash)
        {
            float steer = Input.GetAxisRaw("Horizontal");
            float targetAngle = -steer * SteerMaxDegrees;
            wheelAngle = Mathf.MoveTowards(wheelAngle, targetAngle, SteerTurnSpeed * Time.deltaTime);

            float radius = Mathf.Min(dash.width * 0.88f, dash.height * 1.24f);
            var center = new Vector2(dash.x + dash.width * 0.5f, dash.y + dash.height * 0.58f);

            float innerRadius = radius * 0.89f;

            if (Event.current.type == EventType.Repaint)
                DrawSolidWheel(center, radius, innerRadius, wheelAngle);
        }

        static void DrawSolidWheel(Vector2 center, float outerRadius, float innerRadius, float rotationDegrees)
        {
            const int ringSegments = 96;
            float thickness = outerRadius - innerRadius;
            float halfWidth = thickness * 0.5f;

            GlMaterial().SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);
            GL.MultMatrix(Matrix4x4.TRS(
                new Vector3(center.x, center.y, 0f),
                Quaternion.Euler(0f, 0f, rotationDegrees),
                Vector3.one));

            GL.Begin(GL.TRIANGLES);
            GL.Color(WheelBrown);

            for (int i = 0; i < ringSegments; i++)
            {
                float a0 = i / (float)ringSegments * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)ringSegments * Mathf.PI * 2f;
                Vector3 outer0 = LocalWheelPoint(a0, outerRadius);
                Vector3 outer1 = LocalWheelPoint(a1, outerRadius);
                Vector3 inner0 = LocalWheelPoint(a0, innerRadius);
                Vector3 inner1 = LocalWheelPoint(a1, innerRadius);

                GL.Vertex(outer0);
                GL.Vertex(inner0);
                GL.Vertex(inner1);
                GL.Vertex(outer0);
                GL.Vertex(inner1);
                GL.Vertex(outer1);
            }

            for (int spoke = 0; spoke < 3; spoke++)
            {
                float angle = spoke * 120f * Mathf.Deg2Rad;
                Vector3 dir = LocalWheelPoint(angle, 1f);
                Vector3 perp = new Vector3(-dir.y, dir.x, 0f);
                Vector3 tip = dir * outerRadius;

                GL.Vertex(Vector3.zero + perp * halfWidth);
                GL.Vertex(Vector3.zero - perp * halfWidth);
                GL.Vertex(tip - perp * halfWidth);
                GL.Vertex(Vector3.zero + perp * halfWidth);
                GL.Vertex(tip - perp * halfWidth);
                GL.Vertex(tip + perp * halfWidth);
            }

            GL.End();
            GL.PopMatrix();
        }

        static Vector3 LocalWheelPoint(float angleRadians, float radius)
        {
            return new Vector3(Mathf.Cos(angleRadians) * radius, -Mathf.Sin(angleRadians) * radius, 0f);
        }

        static Material GlMaterial()
        {
            if (glMaterial != null)
                return glMaterial;

            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            glMaterial = new Material(shader);
            glMaterial.hideFlags = HideFlags.HideAndDontSave;
            return glMaterial;
        }

        static void DrawSpeedometer(Rect rect, float mph)
        {
            var face = Circle();
            GUI.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            GUI.DrawTexture(rect, face, ScaleMode.ScaleToFit);
            GUI.color = new Color(0.22f, 0.22f, 0.22f, 1f);
            float inset = rect.width * 0.07f;
            GUI.DrawTexture(new Rect(rect.x + inset, rect.y + inset, rect.width - inset * 2f, rect.height - inset * 2f), face, ScaleMode.ScaleToFit);
            GUI.color = Color.white;

            Vector2 center = rect.center;
            float radius = rect.width * 0.38f;
            var tickStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(10, Mathf.RoundToInt(rect.width * 0.08f)),
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };

            for (int i = 0; i < GaugeTickCount; i++)
            {
                float tickMph = i * 10f;
                float t = tickMph / GaugeMaxMph;
                float angle = TickAngle(t);
                Vector2 dir = AngleDirection(angle);
                Vector2 inner = center + dir * (radius * 0.72f);
                Vector2 outer = center + dir * radius;
                DrawLine(inner, outer, 3f, new Color(0.9f, 0.9f, 0.9f));

                Vector2 labelPos = center + dir * (radius * 0.54f);
                GUI.Label(new Rect(labelPos.x - 14f, labelPos.y - 10f, 28f, 20f), tickMph.ToString("0"), tickStyle);
            }

            float needleT = Mathf.Clamp01(mph / GaugeMaxMph);
            Vector2 needleEnd = center + AngleDirection(TickAngle(needleT)) * (radius * 0.78f);
            DrawLine(center, needleEnd, 4f, new Color(0.95f, 0.15f, 0.12f));

            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            float hub = rect.width * 0.06f;
            GUI.DrawTexture(new Rect(center.x - hub * 0.5f, center.y - hub * 0.5f, hub, hub), face, ScaleMode.ScaleToFit);
            GUI.color = Color.white;

            var mphStyle = new GUIStyle(tickStyle)
            {
                fontSize = Mathf.Max(11, Mathf.RoundToInt(rect.width * 0.09f)),
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            GUI.Label(new Rect(rect.x, rect.yMax - rect.height * 0.28f, rect.width, 22f), "MPH", mphStyle);
        }

        static float TickAngle(float t) => Mathf.Lerp(210f, -30f, t);

        static Vector2 AngleDirection(float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));
        }

        static void DrawLine(Vector2 from, Vector2 to, float width, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f)
                return;

            float angle = Vector2.SignedAngle(Vector2.right, delta);
            var matrix = GUI.matrix;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, from);
            GUI.DrawTexture(new Rect(from.x, from.y - width * 0.5f, length, width), Pixel(), ScaleMode.StretchToFill);
            GUI.matrix = matrix;
            GUI.color = Color.white;
        }

        static Texture2D Pixel()
        {
            if (pixel != null)
                return pixel;

            pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
            return pixel;
        }

        static Texture2D Circle()
        {
            if (circle != null)
                return circle;

            const int size = 128;
            circle = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r;
                    float dy = y - r;
                    circle.SetPixel(x, y, dx * dx + dy * dy <= r * r ? Color.white : Color.clear);
                }
            }

            circle.Apply();
            return circle;
        }
    }
}
