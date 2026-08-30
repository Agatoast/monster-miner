using System;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class LandChunkPlacement
    {
        public const float ChunkSizeFeet = 2000f;
        const float EggSpacingMinFeet = 300f;
        const float EggSpacingMaxFeet = 500f;
        const float TreeSpacingMinFeet = 400f;
        const float TreeSpacingMaxFeet = 650f;
        const float RockSpacingMinFeet = 500f;
        const float RockSpacingMaxFeet = 800f;

        const int EggSeed = 91337;
        const int CreatureSeed = 62418;
        const int TreeSeed = 42857;
        const int RockSeed = 43128;
        const float CreatureGridOffsetFeet = 180f;

        public static float ChunkSize => WorldScale.Feet(ChunkSizeFeet);

        public static bool ChunkIntersectsLand(int chunkX, int chunkZ, CavernBounds bounds)
        {
            if (bounds == null)
                return false;

            float size = ChunkSize;
            float minX = chunkX * size;
            float minZ = chunkZ * size;
            float maxX = minX + size;
            float maxZ = minZ + size;
            float midX = (minX + maxX) * 0.5f;
            float midZ = (minZ + maxZ) * 0.5f;

            if (WorldRegion.IsLandLocal(bounds, midX, midZ))
                return true;

            if (WorldRegion.IsLandLocal(bounds, minX, minZ))
                return true;
            if (WorldRegion.IsLandLocal(bounds, maxX, minZ))
                return true;
            if (WorldRegion.IsLandLocal(bounds, minX, maxZ))
                return true;
            if (WorldRegion.IsLandLocal(bounds, maxX, maxZ))
                return true;

            return false;
        }

        public static void ForEachEggInChunk(int chunkX, int chunkZ, CavernBounds bounds, Action<float, float> visit)
        {
            if (bounds == null || visit == null)
                return;

            float size = ChunkSize;
            float minX = chunkX * size;
            float minZ = chunkZ * size;
            float maxX = minX + size;
            float maxZ = minZ + size;
            float outer = WorldRegion.GetLandOuterRadius(bounds.Radius);

            float x = minX;
            while (x < maxX)
            {
                float z = minZ;
                while (z < maxZ)
                {
                    float spacing = WorldScale.Feet(Mathf.Lerp(
                        EggSpacingMinFeet,
                        EggSpacingMaxFeet,
                        Hash01(Mathf.FloorToInt(x / size), Mathf.FloorToInt(z / size), EggSeed + 11)));

                    float localX = x + spacing * Hash01(
                        Mathf.FloorToInt(x * 10f),
                        Mathf.FloorToInt(z * 10f),
                        EggSeed + 3);
                    float localZ = z + spacing * Hash01(
                        Mathf.FloorToInt(x * 10f),
                        Mathf.FloorToInt(z * 10f),
                        EggSeed + 7);

                    if (localX >= minX && localX < maxX
                        && localZ >= minZ && localZ < maxZ
                        && localX * localX + localZ * localZ <= outer * outer
                        && WorldRegion.IsLandLocal(bounds, localX, localZ))
                    {
                        visit(localX, localZ);
                    }

                    z += spacing;
                }

                x += WorldScale.Feet(Mathf.Lerp(
                    EggSpacingMinFeet,
                    EggSpacingMaxFeet,
                    Hash01(Mathf.FloorToInt(x / size), chunkZ, EggSeed + 19)));
            }
        }

        public static void ForEachCreatureInChunk(int chunkX, int chunkZ, CavernBounds bounds, Action<float, float> visit)
        {
            if (bounds == null || visit == null)
                return;

            float size = ChunkSize;
            float offset = WorldScale.Feet(CreatureGridOffsetFeet);
            float minX = chunkX * size + offset;
            float minZ = chunkZ * size + offset;
            float maxX = chunkX * size + size;
            float maxZ = chunkZ * size + size;
            float outer = WorldRegion.GetLandOuterRadius(bounds.Radius);

            float x = minX;
            while (x < maxX)
            {
                float z = minZ;
                while (z < maxZ)
                {
                    float spacing = WorldScale.Feet(Mathf.Lerp(
                        EggSpacingMinFeet,
                        EggSpacingMaxFeet,
                        Hash01(Mathf.FloorToInt(x / size), Mathf.FloorToInt(z / size), CreatureSeed + 11)));

                    float localX = x + spacing * Hash01(
                        Mathf.FloorToInt(x * 10f),
                        Mathf.FloorToInt(z * 10f),
                        CreatureSeed + 3);
                    float localZ = z + spacing * Hash01(
                        Mathf.FloorToInt(x * 10f),
                        Mathf.FloorToInt(z * 10f),
                        CreatureSeed + 7);

                    if (localX >= minX && localX < maxX
                        && localZ >= minZ && localZ < maxZ
                        && localX * localX + localZ * localZ <= outer * outer
                        && WorldRegion.IsLandLocal(bounds, localX, localZ))
                    {
                        visit(localX, localZ);
                    }

                    z += spacing;
                }

                x += WorldScale.Feet(Mathf.Lerp(
                    EggSpacingMinFeet,
                    EggSpacingMaxFeet,
                    Hash01(Mathf.FloorToInt(x / size), chunkZ, CreatureSeed + 19)));
            }
        }

        public static void ForEachTreeInChunk(int chunkX, int chunkZ, CavernBounds bounds, Action<float, float, int> visit)
        {
            if (bounds == null || visit == null)
                return;

            float size = ChunkSize;
            float minX = chunkX * size;
            float minZ = chunkZ * size;
            float maxX = minX + size;
            float maxZ = minZ + size;
            float outer = WorldRegion.GetLandOuterRadius(bounds.Radius);
            int copseIndex = chunkX * 73856093 ^ chunkZ * 19349663;

            float x = minX;
            while (x < maxX)
            {
                float z = minZ;
                while (z < maxZ)
                {
                    float spacing = WorldScale.Feet(Mathf.Lerp(
                        TreeSpacingMinFeet,
                        TreeSpacingMaxFeet,
                        Hash01(Mathf.FloorToInt(x / size), Mathf.FloorToInt(z / size), TreeSeed + 11)));

                    float localX = x + spacing * Hash01(
                        Mathf.FloorToInt(x * 10f),
                        Mathf.FloorToInt(z * 10f),
                        TreeSeed + 3);
                    float localZ = z + spacing * Hash01(
                        Mathf.FloorToInt(x * 10f),
                        Mathf.FloorToInt(z * 10f),
                        TreeSeed + 7);

                    if (localX >= minX && localX < maxX
                        && localZ >= minZ && localZ < maxZ
                        && localX * localX + localZ * localZ <= outer * outer
                        && WorldRegion.IsLandLocal(bounds, localX, localZ))
                    {
                        copseIndex++;
                        visit(localX, localZ, copseIndex);
                    }

                    z += spacing;
                }

                x += WorldScale.Feet(Mathf.Lerp(
                    TreeSpacingMinFeet,
                    TreeSpacingMaxFeet,
                    Hash01(Mathf.FloorToInt(x / size), chunkZ, TreeSeed + 19)));
            }
        }

        public static void ForEachRockInChunk(int chunkX, int chunkZ, CavernBounds bounds, Action<float, float, int> visit)
        {
            if (bounds == null || visit == null)
                return;

            float size = ChunkSize;
            float minX = chunkX * size;
            float minZ = chunkZ * size;
            float maxX = minX + size;
            float maxZ = minZ + size;
            float outer = WorldRegion.GetLandOuterRadius(bounds.Radius);
            int rockIndex = chunkX * 83492791 ^ chunkZ * 41777;

            float x = minX;
            while (x < maxX)
            {
                float z = minZ;
                while (z < maxZ)
                {
                    float spacing = WorldScale.Feet(Mathf.Lerp(
                        RockSpacingMinFeet,
                        RockSpacingMaxFeet,
                        Hash01(Mathf.FloorToInt(x / size), Mathf.FloorToInt(z / size), RockSeed + 11)));

                    float localX = x + spacing * Hash01(
                        Mathf.FloorToInt(x * 10f),
                        Mathf.FloorToInt(z * 10f),
                        RockSeed + 3);
                    float localZ = z + spacing * Hash01(
                        Mathf.FloorToInt(x * 10f),
                        Mathf.FloorToInt(z * 10f),
                        RockSeed + 7);

                    if (localX >= minX && localX < maxX
                        && localZ >= minZ && localZ < maxZ
                        && localX * localX + localZ * localZ <= outer * outer
                        && WorldRegion.IsLandLocal(bounds, localX, localZ))
                    {
                        rockIndex++;
                        visit(localX, localZ, rockIndex);
                    }

                    z += spacing;
                }

                x += WorldScale.Feet(Mathf.Lerp(
                    RockSpacingMinFeet,
                    RockSpacingMaxFeet,
                    Hash01(Mathf.FloorToInt(x / size), chunkZ, RockSeed + 19)));
            }
        }

        static float Hash01(int a, int b, int salt)
        {
            uint hash = (uint)(a * 73856093 ^ b * 19349663 ^ salt * 83492791);
            return (hash & 0xFFFFFF) / (float)0x1000000;
        }
    }
}
