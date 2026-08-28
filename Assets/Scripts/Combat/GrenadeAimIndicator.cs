using MonsterMiner.Util;
using MonsterMiner.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.Combat
{
    public class GrenadeAimIndicator : MonoBehaviour
    {
        const int CircleSegments = 64;
        const float CrosshairArmLength = 0.85f;
        const float LineWidth = 0.12f;
        static readonly Color IndicatorColor = new Color(0.95f, 0.12f, 0.08f, 1f);

        LineRenderer blastCircle;
        LineRenderer crosshairA;
        LineRenderer crosshairB;
        Material lineMaterial;
        Vector3[] circlePoints;
        bool visible;

        public void SetVisible(bool show)
        {
            visible = show;
            if (blastCircle != null)
                blastCircle.enabled = show;
            if (crosshairA != null)
                crosshairA.enabled = show;
            if (crosshairB != null)
                crosshairB.enabled = show;
        }

        public void UpdateAim(Vector3 groundPoint)
        {
            if (!visible)
                return;

            float radius = WorldScale.GrenadeBlastRadius;
            UpdateCircle(groundPoint, radius);
            UpdateCrosshair(groundPoint);
        }

        void Awake()
        {
            circlePoints = new Vector3[CircleSegments + 1];
            blastCircle = CreateLine("GrenadeBlastRadius", true);
            crosshairA = CreateLine("GrenadeCrosshairA", false);
            crosshairB = CreateLine("GrenadeCrosshairB", false);
            SetVisible(false);
        }

        LineRenderer CreateLine(string name, bool loop)
        {
            var lineGo = new GameObject(name);
            lineGo.transform.SetParent(transform, false);

            var line = lineGo.AddComponent<LineRenderer>();
            line.loop = loop;
            line.useWorldSpace = true;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.widthMultiplier = LineWidth;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.sharedMaterial = GetLineMaterial();
            line.startColor = IndicatorColor;
            line.endColor = IndicatorColor;
            line.positionCount = loop ? CircleSegments + 1 : 2;
            line.enabled = false;
            return line;
        }

        Material GetLineMaterial()
        {
            if (lineMaterial != null)
                return lineMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");
            lineMaterial = new Material(shader);
            if (lineMaterial.HasProperty("_BaseColor"))
                lineMaterial.SetColor("_BaseColor", IndicatorColor);
            else
                lineMaterial.color = IndicatorColor;

            return lineMaterial;
        }

        void UpdateCircle(Vector3 center, float radius)
        {
            for (int i = 0; i <= CircleSegments; i++)
            {
                float angle = i / (float)CircleSegments * Mathf.PI * 2f;
                float x = center.x + Mathf.Cos(angle) * radius;
                float z = center.z + Mathf.Sin(angle) * radius;
                float y = SampleGroundY(x, z, center.y) + 0.04f;
                circlePoints[i] = new Vector3(x, y, z);
            }

            blastCircle.positionCount = circlePoints.Length;
            blastCircle.SetPositions(circlePoints);
        }

        void UpdateCrosshair(Vector3 center)
        {
            float y = center.y + 0.05f;
            crosshairA.SetPosition(0, new Vector3(center.x - CrosshairArmLength, y, center.z));
            crosshairA.SetPosition(1, new Vector3(center.x + CrosshairArmLength, y, center.z));
            crosshairB.SetPosition(0, new Vector3(center.x, y, center.z - CrosshairArmLength));
            crosshairB.SetPosition(1, new Vector3(center.x, y, center.z + CrosshairArmLength));
        }

        static float SampleGroundY(float worldX, float worldZ, float fallbackY)
        {
            var bounds = Core.GameContext.Instance?.CavernBounds;
            if (bounds != null)
            {
                var local = bounds.transform.InverseTransformPoint(new Vector3(worldX, 0f, worldZ));
                if (bounds.TryResolveFloorWorldPoint(local.x, local.z, out var floorPoint))
                    return floorPoint.y;
            }

            var probe = new Vector3(worldX, fallbackY + 16f, worldZ);
            if (FloorAnchor.TryResolveFloorPoint(probe, 0f, 32f, out var hit))
                return hit.y;

            return fallbackY;
        }
    }
}
