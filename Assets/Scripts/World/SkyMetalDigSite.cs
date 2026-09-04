using MonsterMiner.Combat;
using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.UI;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public class SkyMetalDigSite : MonoBehaviour
    {
        const string DiscoveryImageResourcePath = "Textures/SkyMetal/ufo_coffin";
        const string DiscoveryBody =
            "Your pickaxe finally rings against something that is not dirt.\n\n"
            + "It looks like you dug up some old farmer's coffin. Strange coffin, but you had better cover it up again before someone sees it.\n\n"
            + "As you begin filling in the hole you think, does this sky-metal detector even work?";

        [SerializeField] int siteIndex;
        [SerializeField] int requiredStrikes = SkyMetalDigSiteCatalog.FirstSiteRequiredStrikes;

        Transform markerRoot;
        Transform pitVisual;
        Transform groundDisc;
        BoxCollider strikeZone;
        MarkerLegState legA;
        MarkerLegState legB;
        int strikeCount;
        bool markerRevealed;
        bool isComplete;
        bool fillRevealed;

        static readonly Color PitDirtColor = new Color(0.12f, 0.08f, 0.05f);
        static readonly Color FilledDirtColor = new Color(0.28f, 0.2f, 0.13f);
        const float UnityCylinderHeight = 2f;
        const float MarkerLift = 0.08f;

        sealed class MarkerLegState
        {
            public Transform Bar;
            public float MinLocalZ;
            public float MaxLocalZ;
            public float Thickness;
            public float Lift;

            public bool HasLength => MaxLocalZ - MinLocalZ > 0.001f;
        }

        public int SiteIndex => siteIndex;
        public bool IsComplete => isComplete;
        public Vector3 WorldCenter => transform.position;

        public static SkyMetalDigSite Create(int siteIndex, Vector3 worldPosition)
        {
            var root = new GameObject($"SkyMetalDigSite_{siteIndex}");
            var site = root.AddComponent<SkyMetalDigSite>();
            site.siteIndex = siteIndex;
            site.requiredStrikes = SkyMetalDigSiteCatalog.GetRequiredStrikes(siteIndex);
            site.transform.position = worldPosition;
            site.BuildVisuals();
            return site;
        }

        void Update()
        {
            if (isComplete || markerRevealed)
                return;

            TryRevealMarkerForNearbyPlayer();
        }

        public bool TryRegisterStrike(Vector3 hitPoint)
        {
            if (!SkyMetalDigSiteCatalog.SiteHasDigMechanics(siteIndex))
                return false;

            if (isComplete)
                return false;

            if (!markerRevealed)
            {
                if (!IsPlayerWithinArrivalRadius())
                    return false;

                RevealMarker();
            }

            if (!IsValidStrikePoint(hitPoint))
                return false;

            RegisterStrike();
            return true;
        }

        public bool IsValidStrikePoint(Vector3 hitPoint)
        {
            if (ContainsStrikePoint(hitPoint))
                return true;

            var player = GameContext.Instance?.Player;
            return player != null && ContainsStrikePoint(player.transform.position);
        }

        bool IsPlayerWithinArrivalRadius()
        {
            var player = GameContext.Instance?.Player;
            if (player == null)
                return false;

            Vector3 delta = player.transform.position - transform.position;
            delta.y = 0f;
            float radius = WorldScale.Feet(SkyMetalDigSiteCatalog.ArrivalRadiusFeet);
            return delta.sqrMagnitude <= radius * radius;
        }

        public void TryRevealMarkerForNearbyPlayer()
        {
            if (markerRevealed || isComplete)
                return;

            if (IsPlayerWithinArrivalRadius())
                RevealMarker();
        }

        void Start()
        {
            TryRevealMarkerForNearbyPlayer();
        }

        public bool ContainsStrikePoint(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            float halfExtent = WorldScale.Feet(SkyMetalDigSiteCatalog.MarkerSizeFeet) * 0.5f;
            return Mathf.Abs(local.x) <= halfExtent && Mathf.Abs(local.z) <= halfExtent;
        }

        void RegisterStrike()
        {
            if (isComplete)
                return;

            strikeCount++;
            UpdatePitVisual();
            BreakNextMarkerFragment();

            if (strikeCount < requiredStrikes)
                return;

            CompleteDig();
        }

        void RevealMarker()
        {
            markerRevealed = true;
            GameContext.Instance?.CaveProgression?.DiscoverSkyMetalSite(siteIndex);
            if (markerRoot != null)
                markerRoot.gameObject.SetActive(true);
            if (groundDisc != null)
                groundDisc.gameObject.SetActive(true);
        }

        void CompleteDig()
        {
            if (isComplete)
                return;

            isComplete = true;
            if (markerRoot != null)
                markerRoot.gameObject.SetActive(false);

            if (siteIndex == SkyMetalDigSiteCatalog.FirstSiteIndex)
            {
                GameContext.Instance?.CaveProgression?.CompleteFirstSkyMetalDig();
                MinerTurnInPopupDisplay.ShowWithImage(
                    DiscoveryBody,
                    DiscoveryImageResourcePath,
                    centerBody: true,
                    okOnly: true,
                    dismissCallback: ShowFilledDisc);
                return;
            }

            if (siteIndex == SkyMetalDigSiteCatalog.ThirdSiteIndex)
            {
                SpawnBurrowCrabMonster();
                GameContext.Instance?.CaveProgression?.CompleteThirdSkyMetalDig();
            }
        }

        void SpawnBurrowCrabMonster()
        {
            MonsterDefinition definition = GameContext.Instance?.Database?.GetMonster(SkyMetalAlienCatalog.GetMonsterId(1));
            if (definition == null)
            {
                Debug.LogWarning("Monster Miner: sky_metal_alien1 definition missing.");
                return;
            }

            Monster.SpawnEmergingFromBurrow(
                definition,
                transform.position,
                SkyMetalDigSiteCatalog.MaxHoleDepthFeet);
        }

        void ShowFilledDisc()
        {
            fillRevealed = true;
            if (pitVisual != null)
                pitVisual.gameObject.SetActive(false);
        }

        void BuildVisuals()
        {
            markerRoot = new GameObject("Marker").transform;
            markerRoot.SetParent(transform, false);
            markerRoot.gameObject.SetActive(false);
            BuildGroundMarker(markerRoot);

            if (siteIndex == SkyMetalDigSiteCatalog.SecondSiteIndex)
                CauldronVisualFactory.CreateCenteredOnDigSite(transform, transform.position);

            if (SkyMetalDigSiteCatalog.SiteHasDigMechanics(siteIndex))
            {
                pitVisual = CreateDirtCylinder("Pit", PitDirtColor, transform);
                pitVisual.gameObject.SetActive(false);
                UpdatePitVisual();

                groundDisc = CreateDirtCylinder("GroundDisc", FilledDirtColor, transform);
                groundDisc.gameObject.SetActive(false);
                ApplyGroundDiscDimensions();

                float markerSize = WorldScale.Feet(SkyMetalDigSiteCatalog.MarkerSizeFeet);
                var zoneGo = new GameObject("StrikeZone");
                zoneGo.transform.SetParent(transform, false);
                zoneGo.transform.localPosition = new Vector3(0f, 0.15f, 0f);
                strikeZone = zoneGo.AddComponent<BoxCollider>();
                strikeZone.size = new Vector3(markerSize, 0.35f, markerSize);
                strikeZone.isTrigger = true;
            }
        }

        void BuildGroundMarker(Transform parent)
        {
            float span = WorldScale.Feet(SkyMetalDigSiteCatalog.MarkerSizeFeet);
            float thickness = WorldScale.Feet(1.8f);
            var material = CreateMarkerMaterial(SkyMetalDigSiteCatalog.MarkerColor);

            legA = CreateMarkerLeg(parent, material, span, thickness, MarkerLift, 45f, "XLegA");
            legB = CreateMarkerLeg(parent, material, span, thickness, MarkerLift, -45f, "XLegB");
        }

        static MarkerLegState CreateMarkerLeg(
            Transform parent,
            Material material,
            float span,
            float thickness,
            float lift,
            float yawDegrees,
            string legName)
        {
            var legRoot = new GameObject(legName).transform;
            legRoot.SetParent(parent, false);
            legRoot.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);

            var bar = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cube,
                legRoot.position,
                new Vector3(thickness, 0.04f, span),
                SkyMetalDigSiteCatalog.MarkerColor,
                "Bar",
                legRoot);
            bar.transform.localPosition = new Vector3(0f, lift, 0f);
            ApplyMarkerPart(bar, material);

            return new MarkerLegState
            {
                Bar = bar.transform,
                MinLocalZ = -span * 0.5f,
                MaxLocalZ = span * 0.5f,
                Thickness = thickness,
                Lift = lift
            };
        }

        void BreakNextMarkerFragment()
        {
            bool useLegA = Random.value < 0.5f;
            if (legA == null || !legA.HasLength)
                useLegA = false;
            if (legB == null || !legB.HasLength)
                useLegA = true;
            if (legA != null && !legA.HasLength && legB != null && !legB.HasLength)
                return;

            var leg = useLegA ? legA : legB;
            float bite = WorldScale.Feet(SkyMetalDigSiteCatalog.MarkerSizeFeet) / (requiredStrikes * 0.5f);

            if (Random.value < 0.5f)
                leg.MaxLocalZ -= bite;
            else
                leg.MinLocalZ += bite;

            UpdateMarkerLegVisual(leg);
        }

        static void UpdateMarkerLegVisual(MarkerLegState leg)
        {
            if (leg?.Bar == null)
                return;

            float length = leg.MaxLocalZ - leg.MinLocalZ;
            if (length <= 0.001f)
            {
                leg.Bar.gameObject.SetActive(false);
                return;
            }

            leg.Bar.gameObject.SetActive(true);
            leg.Bar.localScale = new Vector3(leg.Thickness, 0.04f, length);
            leg.Bar.localPosition = new Vector3(0f, leg.Lift, (leg.MinLocalZ + leg.MaxLocalZ) * 0.5f);
        }

        static void ApplyMarkerPart(GameObject part, Material material)
        {
            StripCollider(part);
            var renderer = part.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
        }

        static Material CreateMarkerMaterial(Color color)
        {
            var material = PrimitiveFactory.CreateColorMaterial(color, 0.2f);
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * 0.35f);
            return material;
        }

        void UpdatePitVisual()
        {
            if (pitVisual == null || fillRevealed)
                return;

            float progress = requiredStrikes > 0 ? strikeCount / (float)requiredStrikes : 0f;
            float radius = GetHoleRadius();
            float depth = WorldScale.Feet(SkyMetalDigSiteCatalog.MaxHoleDepthFeet) * progress;
            if (depth <= 0.001f)
            {
                pitVisual.gameObject.SetActive(false);
                return;
            }

            pitVisual.gameObject.SetActive(true);
            pitVisual.localScale = new Vector3(radius * 2f, depth / UnityCylinderHeight, radius * 2f);
            pitVisual.localPosition = new Vector3(0f, -depth * 0.5f, 0f);
        }

        void ApplyGroundDiscDimensions()
        {
            if (groundDisc == null)
                return;

            float radius = GetHoleRadius();
            const float discThickness = 0.06f;
            groundDisc.localScale = new Vector3(radius * 2f, discThickness / UnityCylinderHeight, radius * 2f);
            groundDisc.localPosition = new Vector3(0f, discThickness * 0.5f, 0f);
        }

        static float GetHoleRadius()
        {
            return WorldScale.Feet(SkyMetalDigSiteCatalog.MarkerSizeFeet) * 0.5f;
        }

        static Transform CreateDirtCylinder(string name, Color color, Transform parent)
        {
            var cylinder = PrimitiveFactory.CreatePrimitive(
                PrimitiveType.Cylinder,
                parent.position,
                Vector3.one,
                color,
                name,
                parent);
            StripCollider(cylinder);
            var renderer = cylinder.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = PrimitiveFactory.CreateColorMaterial(color, 0.15f);
            return cylinder.transform;
        }

        static void StripCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider == null)
                return;

            if (Application.isPlaying)
                Destroy(collider);
            else
                DestroyImmediate(collider);
        }
    }
}
