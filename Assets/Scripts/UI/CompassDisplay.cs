using MonsterMiner.Core;
using MonsterMiner.Inventory;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class CompassDisplay
    {
        static readonly Color RingColor = Color.black;
        static readonly Color NorthArrowColor = new Color(0.95f, 0.12f, 0.1f, 1f);
        static readonly Color MagicArrowColor = new Color(0.95f, 0.78f, 0.22f, 1f);

        static Material glMaterial;

        public static void Draw(GameContext ctx)
        {
            var player = ctx?.Player;
            if (player == null)
                return;

            DrawNorthCompass(player.ViewCamera);
            if (ctx.CaveProgression != null && ctx.CaveProgression.HasMagicCompass)
                DrawMagicCompass(player.ViewCamera, ctx, player.transform.position);
        }

        static void DrawNorthCompass(Camera camera)
        {
            float x = HudIconLayout.CompassX;
            float y = HudIconLayout.CompassY;
            float size = HudIconLayout.IconSize;
            var rect = new Rect(x, y, size, size);
            var center = rect.center;

            float outerRadius = size * 0.46f;
            float innerRadius = size * 0.38f;

            if (Event.current.type == EventType.Repaint)
                DrawRing(center, outerRadius, innerRadius);

            float angle = GetNorthArrowAngle(camera);
            DrawArrow(center, angle, innerRadius, NorthArrowColor);
        }

        static void DrawMagicCompass(Camera camera, GameContext ctx, Vector3 playerWorld)
        {
            float innerRadius = HudIconLayout.IconSize * 0.38f * 2f;
            var center = new Vector2(
                HudIconLayout.MagicCompassCenterX,
                HudIconLayout.MagicCompassCenterY(innerRadius));
            float angle = GetMagicCompassArrowAngle(camera, ctx, playerWorld);
            DrawArrow(center, angle, innerRadius, MagicArrowColor);
        }

        static float GetMagicCompassArrowAngle(Camera camera, GameContext ctx, Vector3 playerWorld)
        {
            if (ctx?.CavernBounds == null)
                return GetNorthArrowAngle(camera);

            Vector3 targetWorld = QuarryCatalog.ResolveQuarryCenterWorld(
                ctx.CavernBounds,
                QuarryCatalog.LandQuarry3Index);
            Vector3 toTarget = targetWorld - playerWorld;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f)
                return GetNorthArrowAngle(camera);

            toTarget.Normalize();
            Vector3 forward = camera != null
                ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up)
                : Vector3.forward;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();

            var targetScreen = new Vector2(toTarget.x, -toTarget.z);
            var forwardScreen = new Vector2(forward.x, -forward.z);
            return Vector2.SignedAngle(forwardScreen, targetScreen);
        }

        static float GetNorthArrowAngle(Camera camera)
        {
            Vector3 forward = camera != null
                ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up)
                : Vector3.forward;
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();

            var forwardScreen = new Vector2(forward.x, -forward.z);
            var northScreen = new Vector2(0f, -1f);
            return Vector2.SignedAngle(forwardScreen, northScreen);
        }

        static void DrawRing(Vector2 center, float outerRadius, float innerRadius)
        {
            const int segments = 72;

            GlMaterial().SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);
            GL.Begin(GL.TRIANGLES);
            GL.Color(RingColor);

            for (int i = 0; i < segments; i++)
            {
                float a0 = i / (float)segments * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
                Vector3 outer0 = RingPoint(a0, outerRadius);
                Vector3 outer1 = RingPoint(a1, outerRadius);
                Vector3 inner0 = RingPoint(a0, innerRadius);
                Vector3 inner1 = RingPoint(a1, innerRadius);

                GL.Vertex(center + (Vector2)outer0);
                GL.Vertex(center + (Vector2)inner0);
                GL.Vertex(center + (Vector2)inner1);
                GL.Vertex(center + (Vector2)outer0);
                GL.Vertex(center + (Vector2)inner1);
                GL.Vertex(center + (Vector2)outer1);
            }

            GL.End();
            GL.PopMatrix();
        }

        static Vector3 RingPoint(float angleRadians, float radius)
        {
            return new Vector3(Mathf.Cos(angleRadians) * radius, -Mathf.Sin(angleRadians) * radius, 0f);
        }

        static void DrawArrow(Vector2 center, float angle, float innerRadius, Color color)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            float halfWidth = innerRadius * 0.24f;

            GlMaterial().SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);
            GL.MultMatrix(Matrix4x4.TRS(
                new Vector3(center.x, center.y, 0f),
                Quaternion.Euler(0f, 0f, angle),
                Vector3.one));
            GL.Begin(GL.TRIANGLES);
            GL.Color(color);
            GL.Vertex(new Vector3(0f, -innerRadius, 0f));
            GL.Vertex(new Vector3(-halfWidth, innerRadius, 0f));
            GL.Vertex(new Vector3(halfWidth, innerRadius, 0f));
            GL.End();
            GL.PopMatrix();
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
    }
}
