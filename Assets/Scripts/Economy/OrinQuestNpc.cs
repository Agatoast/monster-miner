using MonsterMiner.Artillery;
using MonsterMiner.Core;
using MonsterMiner.Data;
using MonsterMiner.Interaction;
using MonsterMiner.Inventory;
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
            "\"A meteor fell from the sky near here recently and I did not understand the portent until now. You need to find that hunk of sky-metal and then take that fancy pickaxe you are holding and dig it up. Bring it to me and I will make you the best weapon you have ever held. Get on with you now, I don't have all day to jibber-jabber.\"\n\n"
            + "\"Oh, don't forget this sky-metal detector, you are going to need it to find that heavenly rock.\"\n\n"
            + "\"It's...um...SOMEWHAT reliable. Mostly.\"";

        const string SkyMetalTurnInDialogue =
            "Orin hefts the sky-metal lump and grins like a dwarf who finally got his anvil back.\n\n"
            + "\"Now THIS is the stuff! Stand back while I work.\"\n\n"
            + "He hammers, quenches, and hammers again until a weapon gleams in his hands.\n\n"
            + "\"There. Legendary Sky-Metal Machine Gun. Try not to shoot your foot off, Hero.\"";

        public string GetPrompt()
        {
            var ctx = GameContext.Instance;
            if (ctx?.Inventory != null
                && ctx.Database?.skyMetalLumpItem != null
                && ctx.Inventory.ContainsItem(ctx.Database.skyMetalLumpItem)
                && ctx.CaveProgression != null
                && !ctx.CaveProgression.HasLegendarySkyMetalMachineGun)
                return $"{OrinVisualFactory.CharacterName} — Turn in Sky-Metal [E]";

            return $"{OrinVisualFactory.CharacterName} [E]";
        }

        public bool CanInteract(GameObject interactor)
        {
            return !ArtillerySession.IsActive && !MinerTurnInPopupDisplay.IsActive;
        }

        public void Interact(GameObject interactor)
        {
            var ctx = GameContext.Instance;
            var progression = ctx?.CaveProgression;
            if (progression == null || !progression.ArtilleryTrialWon)
                return;

            if (TryTurnInSkyMetalLump(ctx))
                return;

            MinerTurnInPopupDisplay.Show(
                LetterDialogue,
                centerBody: true,
                okOnly: true,
                dismissCallback: ShowHeroDialogue);
        }

        static bool TryTurnInSkyMetalLump(GameContext ctx)
        {
            var lump = ctx?.Database?.skyMetalLumpItem;
            var weapon = ctx?.Database?.legendarySkyMetalMachinegunItem;
            var progression = ctx?.CaveProgression;
            var inventory = ctx?.Inventory;
            if (lump == null || weapon == null || progression == null || inventory == null)
                return false;

            if (progression.HasLegendarySkyMetalMachineGun || !inventory.ContainsItem(lump))
                return false;

            MinerTurnInPopupDisplay.Show(
                SkyMetalTurnInDialogue,
                centerBody: true,
                okOnly: true,
                dismissCallback: () => CompleteSkyMetalTurnIn(ctx, lump, weapon));

            return true;
        }

        static void CompleteSkyMetalTurnIn(GameContext ctx, ItemDefinition lump, ItemDefinition weapon)
        {
            if (ctx?.Inventory == null || ctx.CaveProgression == null)
                return;

            if (!ctx.Inventory.TryRemove(lump, 1))
                return;

            if (!ctx.Inventory.TryAdd(weapon, 1))
            {
                ctx.Inventory.TryAdd(lump, 1);
                ctx.Hud?.ShowMessage("Need an empty inventory slot for the Legendary Sky-Metal Machine Gun.");
                return;
            }

            ctx.CaveProgression.CompleteSkyMetalMachineGunTurnIn();
            ctx.Hud?.ShowMessage("Orin forged the Legendary Sky-Metal Machine Gun!");
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
            MinerTurnInPopupDisplay.Show(
                SkyMetalDialogue,
                centerBody: true,
                okOnly: true,
                dismissCallback: GrantSkyMetalDetector);
        }

        static void GrantSkyMetalDetector()
        {
            GameContext.Instance?.CaveProgression?.GrantSkyMetalDetector();
        }

        public bool TryGetPromptScreenRect(Camera camera, out Rect guiRect)
        {
            var collider = GetComponent<Collider>();
            return InteractionPromptBoundsUtility.TryGetColliderScreenRect(camera, collider, out guiRect);
        }
    }
}
