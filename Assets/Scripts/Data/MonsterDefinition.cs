using UnityEngine;

namespace MonsterMiner.Data
{
    [CreateAssetMenu(fileName = "Monster", menuName = "Monster Miner/Monster Definition")]
    public class MonsterDefinition : ScriptableObject
    {
        public string monsterId;
        public string displayName;
        public float maxHealth = 20f;
        public float moveSpeedMph = 11f;
        public float attackDamage = 8f;
        public float attackRange = 1.6f;
        public float attackCooldown = 1.2f;
        public float knockbackForce = 4f;
        public float scale = 1f;
        public Color bodyColor = Color.red;
        public ItemDefinition dropItem;
        public bool explodesOnDeath;
        public float explosionRadius = 3f;
        public float explosionForce = 12f;
        [Range(0f, 1f)]         public float spawnWeight = 1f;
        public bool isQuestBoss;
        public bool fleesFromPlayer;
        public bool fleesOverPlateauEdge;
        public bool alwaysChasePlayer;
        public bool chaseWhenPlayerOnIsland;
        public string visualPrefabResourcePath;
    }
}
