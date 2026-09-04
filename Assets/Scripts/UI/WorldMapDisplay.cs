using MonsterMiner.Core;
using MonsterMiner.Player;
using MonsterMiner.Util;
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
        const float EdgeArrowInset = 28f;
        const float EdgeNorthCompassReserve = 36f;
        const float EdgeMarkerStackSpacing = 34f;
        const float EdgeLabelInsideArrowOffset = 28f;
        const float EdgeDirectionBucketDegrees = 14f;
        const float EdgeLetterSize = 48f;
        const float EdgeLabelCharWidth = 18f;
        const float EdgeLabelMaxWidth = 260f;
        const int EdgeLabelFontSize = 22;
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

            bool compactForTruck = IsDrivingTruck(ctx);
            float panelWidth = compactForTruck
                ? Mathf.Min(760f, Screen.width - 24f)
                : Mathf.Min(PanelWidth, Screen.width - 24f);
            float mapSize = compactForTruck
                ? Mathf.Min(520f, panelWidth - PanelPadding * 2f, GetCompactMapMaxHeight())
                : Mathf.Min(MapSize, panelWidth - PanelPadding * 2f, Screen.height - 220f);
            float panelHeight = Mathf.Min(94f + mapSize + 104f, compactForTruck ? mapSize + 170f : Screen.height - 24f);
            float x = Screen.width * 0.5f - panelWidth * 0.5f;
            float y = compactForTruck
                ? 24f
                : Screen.height * 0.5f - panelHeight * 0.5f;
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
                "Yellow arrow is you  ·  Rim arrows point off-screen; site names sit just inside each arrow",
                legendStyle);

            GUI.color = new Color(0.75f, 0.75f, 0.75f);
            GUI.Label(
                new Rect(panelRect.x, panelRect.yMax - 43f, panelRect.width, 32f),
                "M or Esc to close",
                footerStyle);
            GUI.color = Color.white;
        }

        struct EdgeMarkerDraw
        {
            public Vector2 ScreenDir;
            public string Label;
            public Color Color;
            public float Distance;
        }

        static void DrawEdgeMarkers(Rect mapRect, GameContext ctx, Vector2 playerLocal)
        {
            float viewRadius = WorldScale.Feet(LocalViewRadiusFeet);
            var pending = new System.Collections.Generic.List<EdgeMarkerDraw>(12);

            var markers = QuarryCatalog.EdgeMarkers;
            for (int i = 0; i < markers.Length; i++)
            {
                if (!markers[i].IsVisible(ctx))
                    continue;

                TryQueueOffScreenMarker(
                    pending,
                    playerLocal,
                    markers[i].GetLocalXZ(ctx),
                    markers[i].Label,
                    markers[i].Color,
                    viewRadius);
            }

            if (SkyMetalLumpTracker.TryGetWorldLumpLocal(out Vector2 lumpLocal))
            {
                TryQueueOffScreenMarker(
                    pending,
                    playerLocal,
                    lumpLocal,
                    "Sky-Metal",
                    SkyMetalDigSiteCatalog.DetectorBlue,
                    viewRadius);
            }

            DrawStackedEdgeMarkers(mapRect, pending);
        }

        static void TryQueueOffScreenMarker(
            System.Collections.Generic.List<EdgeMarkerDraw> pending,
            Vector2 playerLocal,
            Vector2 targetLocal,
            string label,
            Color color,
            float viewRadius)
        {
            Vector2 delta = targetLocal - playerLocal;
            if (delta.sqrMagnitude < 0.01f || delta.magnitude <= viewRadius)
                return;

            Vector2 dir = delta.normalized;
            pending.Add(new EdgeMarkerDraw
            {
                ScreenDir = new Vector2(dir.x, -dir.y),
                Label = label,
                Color = color,
                Distance = delta.magnitude
            });
        }

        static void DrawStackedEdgeMarkers(Rect mapRect, System.Collections.Generic.List<EdgeMarkerDraw> markers)
        {
            if (markers == null || markers.Count == 0)
                return;

            markers.Sort((a, b) =>
            {
                int bucketA = GetDirectionBucket(a.ScreenDir);
                int bucketB = GetDirectionBucket(b.ScreenDir);
                if (bucketA != bucketB)
                    return bucketA.CompareTo(bucketB);

                return b.Distance.CompareTo(a.Distance);
            });

            float half = mapRect.width * 0.5f;
            int bucket = int.MinValue;
            int stackIndex = 0;
            for (int i = 0; i < markers.Count; i++)
            {
                var marker = markers[i];
                int markerBucket = GetDirectionBucket(marker.ScreenDir);
                if (markerBucket != bucket)
                {
                    bucket = markerBucket;
                    stackIndex = 0;
                }
                else
                {
                    stackIndex++;
                }

                DrawEdgeLetterMarker(mapRect, marker, half, stackIndex);
            }
        }

        static int GetDirectionBucket(Vector2 screenDir)
        {
            if (screenDir.sqrMagnitude < 0.0001f)
                return 0;

            float angle = Mathf.Atan2(screenDir.x, -screenDir.y) * Mathf.Rad2Deg;
            return Mathf.RoundToInt(angle / EdgeDirectionBucketDegrees);
        }

        static void DrawEdgeLetterMarker(Rect mapRect, EdgeMarkerDraw marker, float half, int stackIndex)
        {
            Vector2 screenDir = marker.ScreenDir.normalized;
            float arrowRadius = half - EdgeArrowInset - stackIndex * EdgeMarkerStackSpacing;
            if (screenDir.y < -0.45f)
                arrowRadius -= EdgeNorthCompassReserve;

            arrowRadius = Mathf.Max(half * 0.35f, arrowRadius);
            Vector2 arrowCenter = mapRect.center + screenDir * arrowRadius;
            Vector2 labelCenter = mapRect.center + screenDir * (arrowRadius - EdgeLabelInsideArrowOffset);

            string displayLabel = GetEdgeMarkerLabel(marker.Label, marker.Distance);
            float angle = Vector2.SignedAngle(new Vector2(0f, -1f), screenDir);
            float labelWidth = Mathf.Clamp(displayLabel.Length * EdgeLabelCharWidth, EdgeLetterSize, EdgeLabelMaxWidth);

            var labelRect = new Rect(
                labelCenter.x - labelWidth * 0.5f,
                labelCenter.y - EdgeLetterSize * 0.5f,
                labelWidth,
                EdgeLetterSize);

            DrawColoredArrow(arrowCenter, angle, marker.Color, EdgeArrowSize);

            GUI.color = marker.Color;
            GUI.Label(labelRect, displayLabel, edgeLetterStyle);
            GUI.color = Color.white;
        }

        static string GetEdgeMarkerLabel(string label, float distanceUnits)
        {
            if (string.IsNullOrEmpty(label))
                return "?";

            float miles = distanceUnits / WorldScale.Miles(1f);
            return $"{label} {miles:F1} mi";
        }

        static void DrawPlayerMarker(Rect mapRect, GameContext ctx)
        {
            if (ctx.Player == null)
                return;

            Vector2 screen = mapRect.center;
            float angle = GetPlayerFacingMapAngle(ctx);
            DrawColoredArrow(screen, angle, Color.yellow, PlayerArrowSize);

            GUI.color = Color.white;
            GUI.Label(new Rect(screen.x - 24f, screen.y + PlayerArrowSize * 0.45f, 48f, 16f), "You", labelStyle);
        }

        static float GetPlayerFacingMapAngle(GameContext ctx)
        {
            if (ctx?.Player == null)
                return 0f;

            Vector3 look = ctx.Player.ViewCamera != null
                ? ctx.Player.ViewCamera.transform.forward
                : ctx.Player.transform.forward;
            Vector3 forward = Vector3.ProjectOnPlane(look, Vector3.up);
            if (forward.sqrMagnitude < 0.01f)
                forward = Vector3.forward;
            forward.Normalize();

            var screenDir = new Vector2(forward.x, -forward.z);
            return Vector2.SignedAngle(new Vector2(0f, -1f), screenDir);
        }

        static float GetCompactMapMaxHeight()
        {
            float dashTop = Screen.height - TruckDashboardDisplay.DashboardHeightPixels;
            return Mathf.Max(220f, dashTop - 120f);
        }

        static bool IsDrivingTruck(GameContext ctx)
        {
            var mount = ctx?.Player?.GetComponent<PlayerVehicleMount>();
            return mount != null && mount.IsDriving && mount.CurrentTruck != null;
        }

        static void DrawCompass(Rect mapRect)
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(mapRect.x, mapRect.y + 2f, mapRect.width, 18f), "N", titleStyle);
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

                if (QuarryCatalog.IsLandQuarry2Local(localX, localZ)
                    || LandQuarry2Boundary.IsSnowGroundLocal(localX, localZ))
                {
                    float snow = Mathf.PerlinNoise(localX * 0.018f + 12.4f, localZ * 0.018f + 8.7f);
                    byte tone = (byte)Mathf.Clamp(248f + snow * 7f, 245f, 255f);
                    return new Color32(tone, tone, tone, 255);
                }

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

            if (LakeCatalog.IsBeachLocal(localX, localZ))
                return new Color32(200, 180, 130, 255);

            if (LandQuarry2Boundary.IsLakeApproachLandLocal(localX, localZ)
                || LandQuarry2Boundary.IsSnowGroundLocal(localX, localZ))
            {
                float snow = Mathf.PerlinNoise(localX * 0.018f + 12.4f, localZ * 0.018f + 8.7f);
                byte tone = (byte)Mathf.Clamp(248f + snow * 7f, 245f, 255f);
                return new Color32(tone, tone, tone, 255);
            }

            if (LakeCatalog.IsLakeIslandLocal(localX, localZ))
            {
                float island = Mathf.PerlinNoise(localX * 0.02f + 31.2f, localZ * 0.02f + 17.4f);
                byte tone = (byte)Mathf.Clamp(88 + island * 42f, 72f, 132f);
                return new Color32(58, tone, 44, 255);
            }

            if (LakeCatalog.IsOpenWaterLocal(localX, localZ) || LakeCatalog.IsLakeLocal(localX, localZ))
                return MapWaterColorSampler.Sample(localX, localZ);

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
                fontSize = EdgeLabelFontSize,
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
