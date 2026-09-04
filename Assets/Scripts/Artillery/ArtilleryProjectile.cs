using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMiner.Artillery
{
    public static class ArtilleryRockPhysics
    {
        public const float MassPounds = 50f;
        public const float MassKg = MassPounds * 0.45359237f;
        public const float MetersPerWorldUnit = 0.54f;
        public const float Gravity = 9.80665f / MetersPerWorldUnit;
        public const float VisualWorldWidth = 0.15f;
        public const float DragCoefficient = 0.72f;
        public const float AirDensity = 1.225f;
        public const float EffectiveAreaScale = 2.4f;
        public const float MinLaunchSpeed = 6f;
        public const float MaxLaunchSpeed = 28f;
        public const float WindMin = -7f;
        public const float WindMax = 7f;

        static float RadiusMeters => VisualWorldWidth * MetersPerWorldUnit * 0.5f;
        static float CrossSectionArea => Mathf.PI * RadiusMeters * RadiusMeters * EffectiveAreaScale;

        public static float LaunchSpeed(float powerPercent)
        {
            float t = Mathf.Clamp01(powerPercent / 100f);
            return Mathf.Lerp(MinLaunchSpeed, MaxLaunchSpeed, t);
        }

        public static float WindSpeedToMph(float windSpeedWorld)
        {
            return Mathf.Abs(windSpeedWorld) * MetersPerWorldUnit * 2.23694f;
        }

        public static Vector2 ComputeDragAccelerationWorld(Vector2 velocityWorld, float windSpeedWorld)
        {
            float velocityXMps = velocityWorld.x * MetersPerWorldUnit;
            float velocityYMps = velocityWorld.y * MetersPerWorldUnit;
            float windMps = windSpeedWorld * MetersPerWorldUnit;

            float relativeHorizontalMps = windMps - velocityXMps;
            float relativeVerticalMps = -velocityYMps;

            float dragHorizontalMps = 0.5f * AirDensity * DragCoefficient * CrossSectionArea
                * relativeHorizontalMps * Mathf.Abs(relativeHorizontalMps) / MassKg;
            float dragVerticalMps = 0.5f * AirDensity * DragCoefficient * CrossSectionArea
                * relativeVerticalMps * Mathf.Abs(relativeVerticalMps) / MassKg;

            return new Vector2(
                dragHorizontalMps / MetersPerWorldUnit,
                dragVerticalMps / MetersPerWorldUnit);
        }
    }

    public class ArtilleryProjectile : MonoBehaviour
    {
        const string RockResourcePath = "Textures/Artillery/catapult_rock";
        const float ProjectileDepth = -0.12f;
        const float MaxLifetime = 12f;
        const float VisualMotionScale = 0.5f;

        static Material sharedRockMaterial;
        static float rockAspect = 1f;

        Vector2 velocity;
        Vector2 previousLocal;
        float windSpeed;
        float lifetime;
        bool active;
        ArtillerySide shooterSide;
        ArtilleryField field;
        MeshRenderer renderer;
        float collisionRadius;
        Action<ArtilleryProjectileHitKind> onHit;

        public bool IsActive => active;

        public void Launch(
            ArtilleryField artilleryField,
            ArtillerySide firingSide,
            Vector3 worldStart,
            Vector2 initialVelocity,
            float windSpeedWorld,
            Action<ArtilleryProjectileHitKind> hitCallback = null)
        {
            field = artilleryField;
            shooterSide = firingSide;
            velocity = initialVelocity;
            windSpeed = windSpeedWorld;
            onHit = hitCallback;
            lifetime = 0f;
            active = true;
            gameObject.SetActive(true);

            EnsureVisual();
            var local = field.transform.InverseTransformPoint(worldStart);
            local.z = ProjectileDepth;
            transform.position = field.transform.TransformPoint(local);
            previousLocal = new Vector2(local.x, local.y);
        }

        void EnsureVisual()
        {
            if (renderer != null)
                return;

            var material = GetRockMaterial();
            float width = ArtilleryRockPhysics.VisualWorldWidth;
            float height = width * rockAspect;
            collisionRadius = width * 0.45f;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(transform, false);
            quad.transform.localScale = new Vector3(width, height, 1f);
            UnityEngine.Object.Destroy(quad.GetComponent<Collider>());

            renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static Material GetRockMaterial()
        {
            if (sharedRockMaterial != null)
                return sharedRockMaterial;

            var source = Resources.Load<Texture2D>(RockResourcePath);
            if (source == null)
            {
                Debug.LogWarning($"Monster Miner: catapult rock not found at Resources/{RockResourcePath}.");
                sharedRockMaterial = BuildFallbackMaterial();
                return sharedRockMaterial;
            }

            var texture = BuildRockTexture(source);
            rockAspect = texture.height / (float)texture.width;
            sharedRockMaterial = BuildTransparentMaterial(texture);
            return sharedRockMaterial;
        }

        static Texture2D BuildRockTexture(Texture2D source)
        {
            var pixels = source.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                var color = pixels[i];
                if (color.r <= 0.06f && color.g <= 0.06f && color.b <= 0.06f)
                    color = Color.clear;
                pixels[i] = color;
            }

            var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        static Material BuildTransparentMaterial(Texture2D texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Transparent");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            var material = new Material(shader);
            material.renderQueue = (int)RenderQueue.Transparent;
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend"))
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
            return material;
        }

        static Material BuildFallbackMaterial()
        {
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var pixels = new Color[64];
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    float dx = x - 3.5f;
                    float dy = y - 3.5f;
                    pixels[y * 8 + x] = dx * dx + dy * dy <= 10f
                        ? new Color(0.55f, 0.55f, 0.55f, 1f)
                        : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            rockAspect = 1f;
            return BuildTransparentMaterial(texture);
        }

        void Update()
        {
            if (!active)
                return;

            float dt = Time.deltaTime;
            float motionDt = dt * VisualMotionScale;
            lifetime += dt;

            velocity.y -= ArtilleryRockPhysics.Gravity * motionDt;
            velocity += ArtilleryRockPhysics.ComputeDragAccelerationWorld(velocity, windSpeed) * motionDt;

            var local = field.transform.InverseTransformPoint(transform.position);
            float previousX = previousLocal.x;
            float previousY = previousLocal.y;
            local.x += velocity.x * motionDt;
            local.y += velocity.y * motionDt;
            local.z = ProjectileDepth;
            transform.position = field.transform.TransformPoint(local);
            previousLocal = new Vector2(local.x, local.y);

            if (lifetime >= MaxLifetime || HasCollided(local, previousX, previousY))
                Deactivate();
        }

        bool HasCollided(Vector3 localPosition, float previousLocalX, float previousLocalY)
        {
            var hit = field.ResolveProjectileHit(
                shooterSide,
                localPosition.x,
                localPosition.y,
                previousLocalX,
                previousLocalY);
            if (hit.StruckTarget)
            {
                field.PlayRockImpact(hit);
                onHit?.Invoke(hit.Kind);
                onHit = null;
                return true;
            }

            if (localPosition.y <= collisionRadius * 0.5f)
                return true;

            field.GetScreenSize(out float width, out float height);
            if (localPosition.x < -collisionRadius || localPosition.x > width + collisionRadius)
                return true;
            if (localPosition.y > height + collisionRadius)
                return true;

            return false;
        }

        public void Deactivate()
        {
            active = false;
            onHit = null;
            gameObject.SetActive(false);
        }
    }
}
