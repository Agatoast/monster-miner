namespace MonsterMiner.Economy
{
    public readonly struct ShopMenuEntry
    {
        public readonly string id;
        public readonly string label;
        public readonly int cost;
        public readonly bool canPurchase;
        public readonly string statusText;

        public ShopMenuEntry(string id, string label, int cost, bool canPurchase, string statusText = null)
        {
            this.id = id;
            this.label = label;
            this.cost = cost;
            this.canPurchase = canPurchase;
            this.statusText = statusText;
        }

        public string DisplayLine
        {
            get
            {
                if (!string.IsNullOrEmpty(statusText))
                    return $"{label} ({statusText})";
                return $"{label} (${cost})";
            }
        }
    }
}
