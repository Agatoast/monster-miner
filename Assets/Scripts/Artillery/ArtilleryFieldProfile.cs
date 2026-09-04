namespace MonsterMiner.Artillery
{
    static class ArtilleryFieldProfile
    {
        public const float DesignWidth = 19.2f;
        public const float DesignHeight = 10.8f;
        public const int ImageWidth = 1024;
        public const int ImageHeight = 576;
        public const int MaxMountainHeightPixels = 167;
        public static float MountainHeight => MaxMountainHeightPixels * Pixel;
        public const float GroundScreenFraction = 0.2f;
        public static float GroundBandHeight => DesignHeight * GroundScreenFraction;
        public const int LeftColumnCount = 276;
        public const int RightStartColumn = 748;
        public const float Pixel = DesignWidth / ImageWidth;
        public const bool SpawnBlueForcesWithOnlyCatapultForTesting = false;

        public static readonly byte[] LeftHeights =
        {
            2, 5, 6, 6, 6, 6, 8, 10, 10, 11, 10, 12, 13, 16, 17, 20, 22, 24, 25, 26,
            27, 27, 31, 33, 34, 35, 37, 39, 41, 44, 47, 48, 50, 50, 50, 50, 51, 51, 52, 53,
            54, 55, 83, 61, 62, 86, 85, 86, 84, 84, 84, 84, 80, 81, 83, 86, 89, 94, 96, 99,
            101, 103, 104, 107, 109, 116, 137, 138, 137, 137, 138, 138, 138, 138, 138, 138, 138, 138, 137, 138,
            138, 139, 139, 140, 143, 143, 143, 144, 147, 149, 152, 154, 155, 156, 158, 161, 165, 165, 165, 166,
            166, 166, 165, 166, 166, 166, 166, 166, 167, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166,
            167, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 165, 164, 162, 159,
            158, 157, 155, 154, 149, 145, 144, 143, 143, 142, 139, 138, 137, 133, 131, 130, 131, 130, 130, 129,
            129, 128, 126, 125, 123, 121, 119, 116, 116, 122, 122, 121, 122, 106, 102, 102, 99, 98, 99, 114,
            92, 91, 91, 91, 90, 90, 89, 89, 89, 89, 89, 89, 89, 88, 88, 87, 85, 85, 83, 82,
            81, 79, 78, 76, 74, 72, 71, 72, 72, 69, 71, 72, 67, 73, 76, 76, 78, 77, 55, 55,
            52, 53, 49, 48, 48, 47, 47, 47, 46, 46, 47, 47, 46, 45, 46, 45, 44, 43, 42, 40,
            39, 39, 37, 36, 34, 30, 26, 24, 22, 21, 19, 19, 17, 17, 17, 17, 17, 15, 14, 12,
            11, 9, 8, 8, 8, 7, 7, 7, 6, 6, 5, 5, 4, 3, 2, 1
        };

        public static readonly byte[] RightHeights =
        {
            1, 2, 3, 4, 5, 5, 6, 6, 7, 7, 7, 8, 8, 8, 9, 11, 12, 14, 15, 17,
            17, 17, 17, 17, 19, 19, 21, 22, 24, 26, 30, 34, 35, 37, 39, 39, 40, 42, 43, 44,
            45, 46, 45, 46, 47, 47, 46, 46, 47, 47, 47, 48, 48, 49, 49, 52, 55, 55, 77, 77,
            76, 76, 73, 67, 72, 71, 69, 72, 72, 71, 72, 74, 76, 78, 79, 81, 82, 83, 84, 85,
            87, 88, 88, 89, 89, 89, 89, 89, 89, 89, 90, 90, 91, 91, 92, 92, 114, 99, 98, 99,
            101, 102, 106, 122, 121, 122, 116, 117, 116, 119, 121, 123, 125, 126, 128, 129, 129, 130, 130, 131,
            130, 131, 133, 137, 138, 139, 142, 143, 143, 144, 145, 149, 154, 155, 157, 158, 159, 162, 164, 165,
            166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166, 166,
            166, 166, 166, 166, 166, 166, 166, 167, 166, 166, 166, 166, 166, 165, 166, 166, 166, 165, 165, 165,
            161, 158, 156, 155, 153, 151, 149, 147, 144, 144, 143, 143, 141, 139, 139, 138, 138, 137, 137, 138,
            138, 138, 138, 138, 138, 138, 138, 138, 138, 137, 116, 110, 108, 104, 103, 101, 99, 96, 94, 89,
            86, 83, 81, 80, 84, 84, 84, 85, 86, 85, 86, 83, 60, 83, 55, 53, 53, 52, 51, 51,
            50, 50, 50, 50, 48, 47, 44, 41, 38, 37, 36, 34, 32, 31, 27, 26, 26, 25, 24, 22,
            20, 17, 16, 13, 12, 11, 11, 12, 12, 8, 7, 6, 6, 5, 5, 3
        };

        public static readonly ArtilleryBuildingPad[] LeftPads =
        {
            new ArtilleryBuildingPad("LeftBase", 32, 41, 50),
            new ArtilleryBuildingPad("LeftMiddle", 66, 83, 138),
            new ArtilleryBuildingPad("LeftUpper", 96, 138, 166)
        };

        public static readonly ArtilleryBuildingPad[] RightPads =
        {
            new ArtilleryBuildingPad("RightUpper", 888, 927, 166),
            new ArtilleryBuildingPad("RightMiddle", 941, 957, 138),
            new ArtilleryBuildingPad("RightBase", 982, 991, 50)
        };

        public static readonly int[] LeftTreeColumns =
        {
            40, 52, 62, 88, 118, 150, 176, 214
        };

        public static readonly int[] RightTreeColumns =
        {
            799, 848, 874, 905, 933, 961, 971, 983
        };
    }

    public readonly struct ArtilleryBuildingPad
    {
        public readonly string Name;
        public readonly int StartColumn;
        public readonly int EndColumn;
        public readonly int HeightPixels;

        public ArtilleryBuildingPad(string name, int startColumn, int endColumn, int heightPixels)
        {
            Name = name;
            StartColumn = startColumn;
            EndColumn = endColumn;
            HeightPixels = heightPixels;
        }
    }
}
