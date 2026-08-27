using MonsterMiner.Core;
using MonsterMiner.Interaction;
using UnityEngine;

namespace MonsterMiner.Economy
{
    public class ShopBuyStation : MonoBehaviour, IInteractable, IInteractPromptBounds
    {
        const float ScreenPixelPullTowardPlayer = 50f;

        ShopManager shop;
        Collider boardCollider;
        bool configuredVisualOffset;

        public void Initialize(ShopManager manager)
        {
            shop = manager;
            boardCollider = GetComponent<Collider>();
        }

        public void ConfigureHitbox(Camera camera)
        {
            if (boardCollider == null)
                boardCollider = GetComponent<Collider>();

            if (configuredVisualOffset || boardCollider == null || camera == null)
                return;

            InteractionHitboxUtility.OffsetTransformByScreenPixels(
                boardCollider.transform,
                camera,
                new Vector3(0f, 0f, ScreenPixelPullTowardPlayer));
            configuredVisualOffset = true;
        }

        public bool TryGetPromptScreenRect(Camera camera, out Rect guiRect)
        {
            return InteractionPromptBoundsUtility.TryGetColliderScreenRect(camera, boardCollider, out guiRect);
        }

        public string GetPrompt() => "Buy from Shop [E]";

        public bool CanInteract(GameObject interactor)
        {
            return shop != null && !shop.IsMenuOpen;
        }

        public void Interact(GameObject interactor)
        {
            shop?.OpenMenu();
        }
    }
}
