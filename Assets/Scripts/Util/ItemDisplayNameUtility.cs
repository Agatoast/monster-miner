using System;

namespace MonsterMiner.Util
{
    public static class ItemDisplayNameUtility
    {
        public static string FormatFinderName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
                return displayName;

            if (displayName.Equals("Lesser Sea Monster", StringComparison.OrdinalIgnoreCase))
                return "Lesser\nSea Monster";

            return displayName.Replace(' ', '\n');
        }
    }
}
