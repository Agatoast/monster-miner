using MonsterMiner.Core;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.UI
{
    public static class WorldMapDisplay
    {
        const float PanelWidth = 1116f;
        const float MapSize = 864f;
        const float PanelPadding = 32f;
        const int MapTextureSize = 256;
        const float PlayerArrowSize = 32f;
        const float EdgeArrowSize = 14f;
        const float EdgeLetterInset = 16f;
        const float EdgeLetterSize = 24f;
        const float LocalViewRadiusFeet = 500f;
        const float MapRebakeMoveFeet = 40f;

        static GUIStyle titleStyle;
        static GUIStyle labelStyle;
        static GUIStyle edgeLetterStyle;
        static GUIStyle legendStyle;
        static GUIStyle footerStyle;
        static Texture2D mapTexture;
        static Texture2D arrowTexture;
        static float bakedViewRadius = -1f;
        static Vector2 bakedCenterLocal = new Vector2(float.MaxValue, float.MaxValue);

        static bool isActive;
        static int shownFrame = -1;

        public static bool IsActive => isActive;

        public static void Show()
        {
            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null || !progression.HasWorldMap)
                return;

            isActive = true;
            shownFrame = Time.frameCount;
        }

        public static void Hide()
        {
            isActive = false;
            shownFrame = -1;
        }

        public static void Toggle()
        {
            if (isActive)
                Hide();
            else
                Show();
        }

        public static void HandleInput()
        {
            var ctx = GameContext.Instance;
            if (ctx?.CaveProgression == null || !ctx.CaveProgression.HasWorldMap)
                return;

            if (ctx.IsPlayerDead || DeathScreenDisplay.IsActive)
            {
                Hide();
                return;
            }

            if (MinerTurnInPopupDisplay.IsActive
                || SellConfirmationDisplay.IsActive
                || (ctx.Shop != null && ctx.Shop.IsMenuOpen))
                return;

            if (Time.frameCount <= shownFrame)
                return;

            if (Input.GetKeyDown(KeyCode.M))
            {
                Toggle();
                return;
            }

            if (isActive && Input.GetKeyDown(KeyCode.Escape))
                Hide();
        }

        public static void Draw()
        {
            if (!isActive)
                return;

            var ctx = GameContext.Instance;
            if (ctx?.CaveProgression == null || !ctx.CaveProgression.HasWorldMap)
                return;

            EnsureStyles();
            Vector2 playerLocal = GetPlayerMapLocal(ctx);
            EnsureMapTexture(ctx.CavernBounds, playerLocal);

            float panelWidth = Mathf.Min(PanelWidth, Screen.width - 24f);
            float mapSize = Mathf.Min(MapSize, panelWidth - PanelPadding * 2f, Screen.height - 220f);
            float panelHeight = Mathf.Min(94f + mapSize + 104f, Screen.height - 24f);
            float x = Screen.width * 0.5f - panelWidth * 0.5f;
            float y = Screen.height * 0.5f - panelHeight * 0.5f;
            var panelRect = new Rect(x, y, panelWidth, panelHeight);

            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);

            GUI.color = new Color(0.08f, 0.08f, 0.1f, 0.96f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            DrawBorder(panelRect, new Color(0.55f, 0.45f, 0.25f));

            GUI.color = Color.white;
            GUI.Label(new Rect(panelRect.x, panelRect.y + 14f, panelRect.width, 58f), "Map", titleStyle);

            var mapRect = new Rect(
                panelRect.x + (panelWidth - mapSize) * 0.5f,
                panelRect.y + 79f,
                mapSize,
                mapSize);

            GUI.color = Color.white;
            if (mapTexture != null)
                GUI.DrawTexture(mapRect, mapTexture);

            DrawBorder(mapRect, new Color(0.2f, 0.18f, 0.12f));
            DrawPlayerMarker(mapRect, ctx);
            DrawEdgeMarkers(mapRect, ctx, playerLocal);
            DrawCompass(mapRect);

            GUI.color = new Color(0.85f, 0.85f, 0.8f);
            GUI.Label(
                new Rect(panelRect.x + PanelPadding, mapRect.yMax + 11f, panelRect.width - PanelPadding * 2f, 40f),
                "Yellow arrow is you  ·  Rim letters and arrows point to off-screen sites",
                legendStyle);

            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            GUI.Label(
                new Rect(panelRect.x, panelRect.yMax - 43f, panelRect.width, 32f),
                "M or Esc to close",
                footerStyle);
            GUI.color = Color.white;
        }

        static void DrawEdgeMarkers(Rect mapRect, GameContext ctx, Vector2 playerLocal)
        {
            float viewRadius = WorldScale.Feet(LocalViewRadiusFeet);
            var markers = QuarryCatalog.EdgeMarkers;
            for (int i = 0; i < markers.Length; i++)
            {
                if (!markers[i].IsVisible(ctx))
                    continue;

                Vector2 targetLocal = markers[i].GetLocalXZ(ctx);
                Vector2 delta = targetLocal - playerLocal;
                if (delta.sqrMagnitude < 0.01f)
                    continue;

                if (delta.magnitude <= viewRadius)
                    continue;

                DrawEdgeLetterMarker(mapRect, delta, markers[i].Label, markers[i].Color);
            }
        }

        static void DrawEdgeLetterMarker(Rect mapRect, Vector2 delta, string label, Color color)
        {
            Vector2 dir = delta.normalized;
            var screenDir = new Vector2(dir.x, -dir.y);
            float half = mapRect.width * 0.5f;
            float letterRadius = half - EdgeLetterInset;
            Vector2 letterCenter = mapRect.center + screenDir * letterRadius;

            string letter = GetEdgeLetter(label);
            float angle = Vector2.SignedAngle(new Vector2(0f, -1f), screenDir);

            var letterRect = new Rect(
                letterCenter.x - EdgeLetterSize * 0.5f,
                letterCenter.y - EdgeLetterSize * 0.5f,
                EdgeLetterSize,
                EdgeLetterSize);

            GUI.color = color;
            GUI.Label(letterRect, letter, edgeLetterStyle);
            DrawColoredArrow(letterCenter, angle, color, EdgeArrowSize);
            GUI.color = Color.white;
        }

        static string GetEdgeLetter(string label)
        {
            if (string.IsNullOrEmpty(label))
                return "?";

            if (label.StartsWith("Quarry "))
            {
                string number = label.Substring("Quarry ".Length).Trim();
                return string.IsNullOrEmpty(number) ? "Q" : number;
            }

            return label.Substring(0, 1).ToUpperInvariant();
        }

        static void DrawPlayerMarker(Rect mapRect, GameContext ctx)
        {
            if (ctx.Player == null)
                return;

            Vector2 screen = mapRect.center;

            Vector3 look = ctx.Player.ViewCamera != null
                ? ctx.Player.ViewCamera.transform.forward
                : ctx.Player.transform.forward;
            Vector3 forward = Vector3.ProjectOnPlane(look, Vector3.up);
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();

            var screenDir = new Vector2(forward.x, -forward.z);
            float angle = Vector2.SignedAngle(new Vector2(0f, -1f), screenDir);
            DrawColoredArrow(screen, angle, Color.yellow, PlayerArrowSize);

            GUI.color = Color.white;
            GUI.Label(new Rect(screen.x - 24f, screen.y + PlayerArrowSize * 0.45f, 48f, 16f), "You", labelStyle);
        }

        static void DrawCompass(Rect mapRect)
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(mapRect.x, mapRect.y + 4f, mapRect.width, 18f), "N", titleStyle);
        }

        static Vector2 GetPlayerMapLocal(GameContext ctx)
        {
            Vector3 world = ctx.Player.transform.position;
            var bounds = ctx.CavernBounds;
            if (bounds != null)
            {
                Vector3 local = bounds.transform.InverseTransformPoint(world);
                return new Vector2(local.x, local.z);
            }

            return new Vector2(world.x, world.z);
        }

        static void DrawColoredArrow(Vector2 center, float angle, Color color, float size)
        {
            EnsureArrowTexture();
            var rect = new Rect(
                center.x - size * 0.5f,
                center.y - size * 0.5f,
                size,
                size);
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, center);
            GUI.color = color;
            GUI.DrawTexture(rect, arrowTexture);
            GUI.matrix = matrix;
            GUI.color = Color.white;
        }

        static void EnsureMapTexture(CavernBounds bounds, Vector2 playerLocal)
        {
            float viewRadius = WorldScale.Feet(LocalViewRadiusFeet);
            bool needsRebake = mapTexture == null
                || Mathf.Abs(bakedViewRadius - viewRadius) > 0.01f
                || Vector2.Distance(bakedCenterLocal, playerLocal) > WorldScale.Feet(MapRebakeMoveFeet);

            if (!needsRebake)
                return;

            bakedViewRadius = viewRadius;
            bakedCenterLocal = playerLocal;

            if (mapTexture == null)
            {
                mapTexture = new Texture2D(MapTextureSize, MapTextureSize, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            var pixels = new Color32[MapTextureSize * MapTextureSize];
            for (int y = 0; y < MapTextureSize; y++)
            {
                float v = ((y + 0.5f) / MapTextureSize) * 2f - 1f;
                for (int x = 0; x < MapTextureSize; x++)
                {
                    float u = ((x + 0.5f) / MapTextureSize) * 2f - 1f;
                    float localX = playerLocal.x + u * viewRadius;
                    float localZ = playerLocal.y + v * viewRadius;
                    pixels[y * MapTextureSize + x] = SampleMapColor(localX, localZ, bounds);
                }
            }

            mapTexture.SetPixels32(pixels);
            mapTexture.Apply(false, false);
        }

        static Color32 SampleMapColor(float localX, float localZ, CavernBounds bounds)
        {
            float plateauRadius = bounds != null ? bounds.Radius : WorldScale.Feet(WorldScale.PlateauNominalRadiusFeet);

            if (bounds != null && WorldRegion.IsQuarryLocal(bounds, localX, localZ))
            {
                if (bounds.IsOnPlateauLocal(localX, localZ))
                    return new Color32(168, 142, 98, 255);

                if (QuarryCatalog.IsLandQuarry2Local(localX, localZ))
                    return new Color32(156, 136, 92, 255);

                return new Color32(148, 122, 86, 255);
            }

            float distance = new Vector2(localX, localZ).magnitude;
            float angle = Mathf.Atan2(localZ, localX);
            float plateauEdge = PlateauBoundary.SamplePlateauEdgeDistance(angle, plateauRadius);
            float wallBase = PlateauWallGeometry.GetWallBaseOutwardRadius(angle, plateauRadius);

            if (distance <= plateauEdge)
                return new Color32(168, 142, 98, 255);

            if (distance <= wallBase)
                return new Color32(92, 74, 58, 255);

            if (bounds != null && !WorldRegion.IsLandLocalRegion(bounds, localX, localZ))
                return new Color32(40, 36, 30, 255);

            float grass = Mathf.PerlinNoise(localX * 0.012f + 4.1f, localZ * 0.012f + 9.7f);
            byte g = (byte)Mathf.Clamp(70 + grass * 50f, 60f, 130f);
            return new Color32(62, g, 48, 255);
        }

        static void EnsureArrowTexture()
        {
            if (arrowTexture != null)
                return;

            const int size = 48;
            arrowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            var tip = new Vector2((size - 1) * 0.5f, size - 2f);
            var left = new Vector2(14f, 6f);
            var right = new Vector2(size - 15f, 6f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (!PointInTriangle(new Vector2(x, y), tip, left, right))
                        continue;

                    pixels[y * size + x] = new Color32(255, 255, 255, 255);
                }
            }

            arrowTexture.SetPixels32(pixels);
            arrowTexture.Apply(false, true);
        }

        static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(point, a, b);
            float d2 = Sign(point, b, c);
            float d3 = Sign(point, c, a);
            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }

        static void DrawBorder(Rect rect, Color color)
        {
            const float thickness = 2f;
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        static void EnsureStyles()
        {
            if (titleStyle != null)
                return;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            edgeLetterStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            legendStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                normal = { textColor = new Color(0.85f, 0.85f, 0.8f) }
            };

            footerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                normal = { textColor = new Color(0.75f, 0.75f, 0.75f) }
            };
        }
    }
}
