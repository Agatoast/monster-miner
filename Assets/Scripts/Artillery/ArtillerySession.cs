using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Player;
using MonsterMiner.UI;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Artillery
{
    public class ArtillerySession : MonoBehaviour
    {
        static readonly Vector3 NormalPlayerScale = new Vector3(0.8f, 1f, 0.8f);

        public static ArtillerySession Instance { get; private set; }
        public static bool IsActive => Instance != null && Instance.active;
        public static bool HasBeenEntered { get; private set; }

        static bool? lastBattleVictory;

        public static void SetLastBattleResult(bool playerVictory)
        {
            lastBattleVictory = playerVictory;
        }

        bool active;
        bool isPracticeRun;
        bool savedFog;
        Camera playerCamera;
        Transform returnNpc;
        Vector3 returnPosition;
        Quaternion returnRotation;
        ArtilleryField field;
        ArtilleryBattleController battle;

        public static void Enter(Quarry3QuestNpc questGiver)
        {
            EnterInternal(questGiver, practice: false);
        }

        public static void EnterForPractice(Quarry3QuestNpc questGiver)
        {
            EnterInternal(questGiver, practice: true);
        }

        static void EnterInternal(Quarry3QuestNpc questGiver, bool practice)
        {
            if (IsActive)
                return;

            var go = new GameObject("ArtillerySession");
            var session = go.AddComponent<ArtillerySession>();
            session.Begin(questGiver, practice);
        }

        public static void Finish()
        {
            Instance?.End(returnToQuestGiver: true);
        }

        public static void LeaveToMainMenu()
        {
            Instance?.End(returnToQuestGiver: false);
            MainMenuDisplay.Show();
        }

        void Begin(Quarry3QuestNpc questGiver, bool practice = false)
        {
            Instance = this;
            active = true;
            isPracticeRun = practice;
            HasBeenEntered = true;
            ArtilleryPauseDisplay.Close();
            returnNpc = questGiver != null ? questGiver.transform : null;
            RememberReturnPose();

            var ctx = GameContext.Instance;
            playerCamera = ctx?.Player?.ViewCamera;
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                playerCamera.tag = "Untagged";
                var playerListener = playerCamera.GetComponent<AudioListener>();
                if (playerListener != null)
                    playerListener.enabled = false;
            }

            savedFog = RenderSettings.fog;
            RenderSettings.fog = false;

            field = ArtilleryField.Build();
            battle = gameObject.AddComponent<ArtilleryBattleController>();
            battle.Initialize(field);
        }

        void Update()
        {
            if (!active)
                return;

            ArtilleryPauseDisplay.HandleInput();
            MinerTurnInPopupDisplay.HandleInput();
            battle?.HandleInput();
            if (ArtilleryShotPanel.IsOpen)
                ArtilleryShotPanel.HandleKeyboardSubmit();
        }

        void OnGUI()
        {
            if (!active)
                return;

            battle?.DrawHud();
            battle?.DrawWindMph();
            battle?.DrawTargetLabels();
            battle?.DrawCenterMessage();
            battle?.DrawShotPanel();
            ArtilleryPauseDisplay.Draw();
            MinerTurnInPopupDisplay.Draw();
        }

        void End(bool returnToQuestGiver)
        {
            if (!active)
                return;

            active = false;
            ArtilleryPauseDisplay.Close();
            field?.TearDown();
            field = null;
            battle = null;
            RenderSettings.fog = savedFog;

            if (playerCamera != null)
            {
                playerCamera.enabled = true;
                playerCamera.tag = "MainCamera";
                var playerListener = playerCamera.GetComponent<AudioListener>();
                if (playerListener != null)
                    playerListener.enabled = true;
            }

            if (returnToQuestGiver)
                PlacePlayerAtQuestGiver();

            Instance = null;
            Destroy(gameObject);
        }

        void RememberReturnPose()
        {
            var player = GameContext.Instance?.Player;
            if (player == null)
                return;

            if (returnNpc != null)
            {
                returnPosition = returnNpc.position + returnNpc.forward * 3f;
                returnRotation = Quaternion.LookRotation(
                    (returnNpc.position - returnPosition).normalized,
                    Vector3.up);
                return;
            }

            returnPosition = player.transform.position;
            returnRotation = player.transform.rotation;
        }

        void PlacePlayerAtQuestGiver()
        {
            var player = GameContext.Instance?.Player;
            if (player == null)
                return;

            player.GetComponent<PlayerVehicleMount>()?.ForceDismount();
            player.GetComponent<PlayerWingsFlight>()?.CancelFlightAndRestoreWings();

            var guide = ResolveQuestGiver();
            var bounds = GameContext.Instance?.CavernBounds;
            Vector3 spawnPoint = bounds != null && guide != null
                ? QuarryCatalog.ResolveQuarry3PlayerSpawnWorld(bounds, guide.transform)
                : returnPosition;

            player.transform.localScale = NormalPlayerScale;
            player.Respawn(spawnPoint);

            if (guide != null)
            {
                Vector3 toGuide = guide.transform.position - player.transform.position;
                toGuide.y = 0f;
                if (toGuide.sqrMagnitude > 0.01f)
                    player.transform.rotation = Quaternion.LookRotation(toGuide.normalized, Vector3.up);
            }
            else
            {
                player.transform.rotation = returnRotation;
            }

            player.ResetPlainsMovementState();
            player.ResetViewPitch(0f);
            ShowOfframpDialogueIfNeeded();
        }

        void ShowOfframpDialogueIfNeeded()
        {
            if (!lastBattleVictory.HasValue)
                return;

            bool victory = lastBattleVictory.Value;
            lastBattleVictory = null;

            if (isPracticeRun)
                return;

            var progression = GameContext.Instance?.CaveProgression;

            if (!victory)
            {
                progression?.MarkArtilleryTrialLost();
                return;
            }

            progression?.CompleteArtilleryTrial();
            Quarry3QuestNpc.ShowVictoryOfframpDialogue();
        }

        Quarry3QuestNpc ResolveQuestGiver()
        {
            if (returnNpc != null)
            {
                var guide = returnNpc.GetComponent<Quarry3QuestNpc>();
                if (guide != null)
                    return guide;
            }

            return Object.FindFirstObjectByType<Quarry3QuestNpc>();
        }
    }
}
