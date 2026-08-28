using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class PlainsBiomeVisualFactory
    {
        public const float PlainsSurfaceLocalY = 0.25f;
        const int CopseCount = 8;
        const float MinCopseSpacing = 8f;

        public static void BuildSurroundings(Transform parent, CavernBounds bounds)
        {
            var root = new GameObject("PlainsBiome").transform;
            root.SetParent(parent, false);

            PlainsGroundBuilder.BuildGround(root, bounds, PlainsSurfaceLocalY);
            ScatterTreeCopses(root, bounds);
        }

        static void ScatterTreeCopses(Transform parent, CavernBounds bounds)
        {
            var copseRoot = new GameObject("TreeCopses").transform;
            copseRoot.SetParent(parent, false);

            float SampleGround(float x, float z)
            {
                return PlainsGroundBuilder.SampleGroundLocalY(
                    x,
                    z,
                    bounds.Radius,
                    bounds.FloorTopLocalY,
                    bounds.BowlDepth,
                    PlainsSurfaceLocalY);
            }

            var copseCenters = new System.Collections.Generic.List<Vector2>(CopseCount);
            int attempts = 0;
            while (copseCenters.Count < CopseCount && attempts < CopseCount * 32)
            {
                attempts++;
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float minDistance = WorldScale.Feet(18f);
                float maxDistance = PlateauBoundary.SamplePlateauEdgeDistance(angle, bounds.Radius) - WorldScale.Feet(10f);
                if (maxDistance <= minDistance)
                    continue;

                float distance = Random.Range(minDistance, maxDistance);
                var candidate = new Vector2(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance);

                if (!PlateauBoundary.IsOnPlateau(candidate.x, candidate.y, bounds.Radius))
                    continue;

                if (!bounds.AllowsEggStyleSpawn(candidate.x, candidate.y))
                    continue;

                bool tooClose = false;
                for (int i = 0; i < copseCenters.Count; i++)
                {
                    if (Vector2.Distance(candidate, copseCenters[i]) < MinCopseSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                    continue;

                copseCenters.Add(candidate);
                PlainsTreeVisualFactory.CreateTreeCopse(
                    copseRoot,
                    candidate,
                    copseCenters.Count,
                    SampleGround,
                    bounds);
            }
        }
    }
}
