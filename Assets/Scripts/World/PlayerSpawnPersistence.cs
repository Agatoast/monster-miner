using MonsterMiner.Core;
using UnityEngine;

namespace MonsterMiner.World
{
    public static class PlayerSpawnPersistence
    {
        const string SavedFlagKey = "MonsterMiner.LandQuarry2SpawnSaved";
        const string SpawnVersionKey = "MonsterMiner.LandQuarry2SpawnVersion";
        const string SpawnXKey = "MonsterMiner.LandQuarry2SpawnX";
        const string SpawnYKey = "MonsterMiner.LandQuarry2SpawnY";
        const string SpawnZKey = "MonsterMiner.LandQuarry2SpawnZ";
        const int CurrentSpawnVersion = 4;

        public static bool HasSavedLandSpawn =>
            PlayerPrefs.GetInt(SavedFlagKey, 0) == 1
            && PlayerPrefs.GetInt(SpawnVersionKey, 0) == CurrentSpawnVersion;

        public static Vector3 LoadSavedLandSpawn()
        {
            return new Vector3(
                PlayerPrefs.GetFloat(SpawnXKey, 0f),
                PlayerPrefs.GetFloat(SpawnYKey, 0f),
                PlayerPrefs.GetFloat(SpawnZKey, 0f));
        }

        public static void SaveLandSpawn(Vector3 worldPoint)
        {
            PlayerPrefs.SetInt(SavedFlagKey, 1);
            PlayerPrefs.SetInt(SpawnVersionKey, CurrentSpawnVersion);
            PlayerPrefs.SetFloat(SpawnXKey, worldPoint.x);
            PlayerPrefs.SetFloat(SpawnYKey, worldPoint.y);
            PlayerPrefs.SetFloat(SpawnZKey, worldPoint.z);
            PlayerPrefs.Save();
        }

        public static void ClearSavedLandSpawn()
        {
            PlayerPrefs.DeleteKey(SavedFlagKey);
            PlayerPrefs.DeleteKey(SpawnVersionKey);
            PlayerPrefs.DeleteKey(SpawnXKey);
            PlayerPrefs.DeleteKey(SpawnYKey);
            PlayerPrefs.DeleteKey(SpawnZKey);
            PlayerPrefs.Save();
        }

        public static void SetSpawnToCurrentPlayerPosition()
        {
            var ctx = GameContext.Instance;
            if (ctx?.Player == null || ctx.CavernBounds == null)
                return;

            Vector3 spawn = PlainsGroundSupport.SnapWorldPointToPlains(
                ctx.CavernBounds,
                ctx.Player.transform.position,
                WorldScale.CharacterHeightUnits * 0.5f);

            ctx.PlayerSpawnPoint = spawn;
            SaveLandSpawn(spawn);
            ctx.Hud?.ShowMessage("Respawn point set here.");
        }
    }
}
