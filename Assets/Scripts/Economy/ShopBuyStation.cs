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

        public bool IsJarlLandShop { get; set; }
        public bool IsQuarry3Shop { get; set; }
        public bool IsQuarry4Shop { get; set; }

        public void Initialize(ShopManager manager)
        {
            shop = manager;
            EnsureBoardCollider();
        }

        void EnsureBoardCollider()
        {
            boardCollider = GetComponent<Collider>();
            if (boardCollider != null)
                return;

            var box = gameObject.AddComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = Vector3.one;
            boardCollider = box;
        }

        public void ConfigureHitbox(Camera camera)
        {
            EnsureBoardCollider();

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
            shop?.OpenMenu(this);
        }
    }
}
