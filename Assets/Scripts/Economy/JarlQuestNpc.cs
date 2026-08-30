using MonsterMiner.Interaction;
using MonsterMiner.UI;
using UnityEngine;

namespace MonsterMiner.Economy
{
    public class JarlQuestNpc : MonoBehaviour, IInteractable, IInteractPromptBounds
    {
        const string DialogueBody =
            "Greetings Stranger and well met. Forgive me but I am a little too busy at the moment to help you find a Dragon Stone. My assistant can help you with any of your supply needs.\n\n"
            + "On second thought, maybe you can help me.\n\n"
            + "I need the skull of an ogre. Ogres are only found on the island in the middle of the lake. If you bring me one, I can give you something to help with your quest.\n\n"
            + "Warrenson has a boat on which you can book passage to the island and you can buy a ticket from my assistant. Good luck to you";

        public string GetDialogueBody() => DialogueBody;

        public string GetPrompt() => "Jarl Jorgenson [E]";

        public bool CanInteract(GameObject interactor) => true;

        public void Interact(GameObject interactor)
        {
            MinerTurnInPopupDisplay.Show(DialogueBody);
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
