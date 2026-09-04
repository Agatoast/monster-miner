using MonsterMiner.Artillery;
using MonsterMiner.Core;
using MonsterMiner.Interaction;
using MonsterMiner.UI;
using UnityEngine;

namespace MonsterMiner.Economy
{
    public class Quarry3QuestNpc : MonoBehaviour, IInteractable, IInteractPromptBounds
    {
        public const string CharacterPersonalName = "Katsura Morinobu";
        public const string DaimyoTitle = "Daimyo";
        public const string ShogunTitle = "Shogun";
        public const string DaimyoCharacterName = DaimyoTitle + "\n" + CharacterPersonalName;
        public const string ShogunCharacterName = ShogunTitle + "\n" + CharacterPersonalName;

        const string IntroDialogue =
            "\"My compass! Give that to me, imbecile.\"\n\n"
            + "\"You should have been here two days ago and the situation has grown more dire. When I sent for the best artillerist in the land, I expected you to be punctual.\"\n\n"
            + "You feel confusion and before you can ask him what he is on about, he says, \"Well, there is nothing for it now. My great enemy, Daimyo Matsunaga Tomohide, challenges my right to become Shogun. Even now he bombards my forces with his catapult. Get to your post and destroy this upstart of low birth who lacks the Mandate of Heaven.\"\n\n"
            + "You realize this is a case of mistaken identity, but you decide your chances to survive the encounter with the Daimyo are better if you play along.";

        const string TutorialDialogue =
            "Using the Catapult\n\n"
            + "You command the blue forces on the left. Destroy Daimyo Matsunaga's red forces before he destroys yours.\n\n"
            + "On your turn, press F to fire. Enter launch angle (20-89 degrees), then power (1-100%).\n\n"
            + "Watch the wind flag - wind speed in MPH creates drag on each shot. Adjust angle and power accordingly to compensate.\n\n"
            + "Most enemies require more than one hit to destroy.\n\n"
            + "Cavalry advances after the shooter's turn. If cavalry makes it to the other side it will start killing enemy units. Don't let them get too close!\n\n"
            + "You must defeat the enemy forces to progress in your quest to find the Dragon Stone.";

        const string VictoryDialogue =
            "\"You have done it! I never doubted you.\"\n\n"
            + "\"My scurrilous enemy is defeated and his name shall never again be spoken.\"\n\n"
            + "\"The Emperor is pleased and has appointed me Shogun of his Empire.\"";

        const string VictoryLetterDialogue =
            "\"Here is the letter of reference that I promised for the legendary weapon maker, Orin Ironbreaker.\"\n\n"
            + "Even now you think better of correcting the Daimyo's...er...Shogun's misunderstanding of who you are. You got the job done so, isn't that all the matters?\n\n"
            + "Besides, who couldn't use a Legendary Weapon crafted by Orin Ironbreaker?";

        const string TrialCompleteDialogue =
            "Thank you, warrior, for your small part in my rise to glory.\n\n"
            + "Make yourself at home in my lands.";

        const string RebuildCatapultDialogue =
            "I am holding you responsible for this setback.\n\n"
            + "You must find more resources to rebuild the catapult you let be destroyed.\n\n"
            + "Bring me $500 to secure more wood to build it.";

        const string RebuildConfirmDialogue = "Give Daimyo $500?";

        const int RebuildCost = 500;

        static Quarry3QuestNpc pendingQuestGiver;

        public static string GetCharacterTitle()
        {
            var progression = GameContext.Instance?.CaveProgression;
            return progression != null && progression.ArtilleryTrialWon
                ? ShogunTitle
                : DaimyoTitle;
        }

        public static string GetCharacterName()
        {
            return $"{GetCharacterTitle()}\n{CharacterPersonalName}";
        }

        public string GetPrompt() => InteractPromptDisplay.FormatPrompt($"{GetCharacterTitle()}\n{CharacterPersonalName} [E]");

        public bool CanInteract(GameObject interactor)
        {
            return !ArtillerySession.IsActive && !MinerTurnInPopupDisplay.IsActive;
        }

        public void Interact(GameObject interactor)
        {
            var ctx = GameContext.Instance;
            var progression = ctx?.CaveProgression;
            if (progression == null)
                return;

            if (progression.ArtilleryTrialWon)
            {
                pendingQuestGiver = this;
                MinerTurnInPopupDisplay.Show(
                    TrialCompleteDialogue,
                    centerBody: true,
                    okOnly: true,
                    secondaryButtonLabel: "Play",
                    okButtonHint: "Click OK to continue",
                    secondaryButtonHint: "Play Minigame",
                    secondaryCallback: EnterPracticeArtilleryTrial);
                return;
            }

            if (!progression.HasHeardSamuraiIntro)
            {
                progression.MarkSamuraiIntroHeard();
                progression.CompleteQuarry3CompassReturn();
                pendingQuestGiver = this;
                MinerTurnInPopupDisplay.Show(
                    IntroDialogue,
                    dismissCallback: ShowArtilleryTutorial,
                    okOnly: true);
                return;
            }

            if (progression.ArtilleryTrialLost)
            {
                pendingQuestGiver = this;
                MinerTurnInPopupDisplay.Show(
                    RebuildCatapultDialogue,
                    centerBody: true,
                    dismissCallback: ShowRebuildPaymentPromptIfAffordable,
                    okOnly: true,
                    secondaryButtonLabel: "Practice",
                    secondaryCallback: EnterPracticeArtilleryTrial);
                return;
            }

            ArtillerySession.Enter(this);
        }

        static void ShowRebuildPaymentPromptIfAffordable()
        {
            var ctx = GameContext.Instance;
            if (ctx?.Wallet == null || !ctx.Wallet.CanAfford(RebuildCost))
                return;

            MinerTurnInPopupDisplay.ShowConfirmation(
                RebuildConfirmDialogue,
                centerBody: true,
                confirmCallback: ConfirmCatapultRebuild);
        }

        static void ConfirmCatapultRebuild()
        {
            var ctx = GameContext.Instance;
            if (ctx?.Wallet == null || !ctx.Wallet.TrySpend(RebuildCost))
                return;

            ctx.CaveProgression?.ClearArtilleryTrialLost();
            EnterPendingArtilleryTrial();
        }

        static void EnterPracticeArtilleryTrial()
        {
            var questGiver = pendingQuestGiver;
            pendingQuestGiver = null;
            if (questGiver != null)
                ArtillerySession.EnterForPractice(questGiver);
        }

        static void ShowArtilleryTutorial()
        {
            MinerTurnInPopupDisplay.Show(
                TutorialDialogue,
                dismissCallback: EnterPendingArtilleryTrial,
                okOnly: true);
        }

        static void EnterPendingArtilleryTrial()
        {
            var questGiver = pendingQuestGiver;
            pendingQuestGiver = null;
            if (questGiver != null)
                ArtillerySession.Enter(questGiver);
        }

        public static void ShowVictoryOfframpDialogue()
        {
            MinerTurnInPopupDisplay.Show(
                VictoryDialogue,
                centerBody: true,
                dismissCallback: ShowVictoryLetterDialogue,
                okOnly: true);
        }

        static void ShowVictoryLetterDialogue()
        {
            MinerTurnInPopupDisplay.Show(
                VictoryLetterDialogue,
                centerBody: true,
                okOnly: true);
        }

        public bool TryGetPromptScreenRect(Camera camera, out Rect guiRect)
        {
            var collider = GetComponent<Collider>();
            return InteractionPromptBoundsUtility.TryGetColliderScreenRect(camera, collider, out guiRect);
        }

        public bool TryGetDialogueAnchorScreenPoint(Camera camera, out Vector2 guiPoint)
        {
            guiPoint = default;
            if (camera == null)
                return false;

            Vector3 shoulderWorld = transform.position
                + transform.right * 0.32f
                + Vector3.up * 1.42f;
            Vector3 screenPoint = camera.WorldToScreenPoint(shoulderWorld);
            if (screenPoint.z <= 0f)
                return false;

            guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
            return true;
        }
    }
}
