using MonsterMiner.Artillery;
using MonsterMiner.Core;
using MonsterMiner.Interaction;
using MonsterMiner.UI;
using MonsterMiner.Util;
using UnityEngine;

namespace MonsterMiner.Economy
{
    public class OrinQuestNpc : MonoBehaviour, IInteractable, IInteractPromptBounds
    {
        const string LetterDialogue =
            "Orin takes the letter the Shogun gave you and appears to read it intently. You see his brows knit together.\n\n"
            + "\"Well, this is very pretty paper but I never learned to read human. What does it say?\"\n\n"
            + "You take the letter back from him and read it aloud, feeling slightly uncomfortable with the word Hero when used to describe you.";

        const string HeroDialogue =
            "\"Hero, eh? Well aren't you special. It's not every day someone gets to meet a Hero,\" he says, giving you a wink that makes you think he might be teasing you.\n\n"
            + "\"OK Hero, let's get you a special weapon to help kill the Dragon.\"\n\n"
            + "Orin notices the look of confusion that crosses your face.\n\n"
            + "\"Well Hero, where else did you think DRAGON Stones came from, eh?\"\n\n"
            + "The dwarf has a mighty laugh at your expense";

        const string SkyMetalDialogue =
            "\"A meteor fell from the sky near here recently and I did not understand the portent until now. You need to find that hunk of sky-metal and I will make you the best weapon you have ever held. Get on with you now, I don't have all day to jibber-jabber.\"\n\n"
            + "\"Oh, don't forget this sky-metal detector, you are going to need it to find that heavenly rock.\"\n\n"
            + "\"It's...um...SOMEWHAT reliable. Mostly.\"";

        public string GetPrompt() => $"{OrinVisualFactory.CharacterName} [E]";

        public bool CanInteract(GameObject interactor)
        {
            return !ArtillerySession.IsActive && !MinerTurnInPopupDisplay.IsActive;
        }

        public void Interact(GameObject interactor)
        {
            var progression = GameContext.Instance?.CaveProgression;
            if (progression == null || !progression.ArtilleryTrialWon)
                return;

            MinerTurnInPopupDisplay.Show(
                LetterDialogue,
                centerBody: true,
                okOnly: true,
                dismissCallback: ShowHeroDialogue);
        }

        static void ShowHeroDialogue()
        {
            MinerTurnInPopupDisplay.Show(
                HeroDialogue,
                centerBody: true,
                okOnly: true,
                dismissCallback: ShowSkyMetalDialogue);
        }

        static void ShowSkyMetalDialogue()
        {
            MinerTurnInPopupDisplay.Show(SkyMetalDialogue, centerBody: true, okOnly: true);
        }

        public bool TryGetPromptScreenRect(Camera camera, out Rect guiRect)
        {
            var collider = GetComponent<Collider>();
            return InteractionPromptBoundsUtility.TryGetColliderScreenRect(camera, collider, out guiRect);
        }
    }
}
