using MonsterMiner.Core;
using MonsterMiner.Economy;
using MonsterMiner.Interaction;
using MonsterMiner.UI;
using UnityEngine;

namespace MonsterMiner.UI
{
    public class HudController : MonoBehaviour
    {
        string message = string.Empty;
        float messageTimer;
        string hatchingMessage = string.Empty;

        public void Build() { RefreshSubscriptions(); }

        public void ShowHatchingMessage(string text) => hatchingMessage = text;

        public void ShowEggHit() => HudEggHitDisplay.ShowRandomHit();

        public void ClearHatchingMessage() => hatchingMessage = string.Empty;

        void RefreshSubscriptions()
        {
            var ctx = GameContext.Instance;
            if (ctx?.Wallet != null)
                ctx.Wallet.OnBalanceChanged += _ => { };
            if (ctx?.Inventory != null)
            {
                ctx.Inventory.OnInventoryChanged += () => { };
                ctx.Inventory.OnSelectedChanged += _ => { };
            }
            if (ctx?.PlayerHealth != null)
                ctx.PlayerHealth.OnHealthChanged += (_, __) => { };
            if (ctx?.PlayerThirst != null)
                ctx.PlayerThirst.OnThirstChanged += (_, __) => { };
        }

        public void ShowMessage(string text)
        {
            message = text;
            messageTimer = 3f;
        }

        void Update()
        {
            DeathScreenDisplay.HandleInput();
            HudEggHitDisplay.Tick(Time.deltaTime);
            CombatHitFeedbackDisplay.Tick(Time.deltaTime);

            if (messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
                if (messageTimer <= 0f)
                    message = string.Empty;
            }
        }

        void OnGUI()
        {
            var ctx = GameContext.Instance;
            if (ctx == null)
                return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.white }
            };
            var center = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter, fontSize = 18, wordWrap = true };
            var interactPrompt = new GUIStyle(center)
            {
                fontSize = InteractPromptDisplay.PromptFontSize,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            var bigCenter = new GUIStyle(center) { fontSize = 22 };

            GUI.Label(new Rect(12, 10, 300, 28), $"$ {ctx.Wallet.Balance}", style);
            HeartHealthDisplay.Draw(ctx.PlayerHealth.CurrentHealth, ctx.PlayerHealth.MaxHealth);
            if (ctx.PlayerThirst != null)
                ThirstDisplay.Draw(ctx.PlayerThirst.CurrentThirst, ctx.PlayerThirst.MaxThirst);
            HudHatchingDisplay.Draw(hatchingMessage);
            HudEggHitDisplay.Draw();
            CombatHitFeedbackDisplay.Draw(ctx.Player?.ViewCamera);

            if (DeathScreenDisplay.IsActive)
            {
                DeathScreenDisplay.Draw();
            }
            else if (ctx.Shop != null && ctx.Shop.IsMenuOpen)
            {
                ShopBuyMenuDisplay.Draw(ctx.Shop);
            }
            else if (SellConfirmationDisplay.IsActive)
            {
                SellConfirmationDisplay.Draw();
            }
            else if (!ctx.IsPlayerDead)
            {
                var interactor = ctx.Player != null ? ctx.Player.GetComponent<Player.Interactor>() : null;
                if (interactor != null && interactor.HasCenterTarget)
                {
                    var camera = ctx.Player.ViewCamera;
                    string prompt = InteractPromptDisplay.FormatPrompt(interactor.CurrentTarget.GetPrompt());
                    if (!string.IsNullOrEmpty(prompt))
                    {
                        if (interactor.CurrentTarget is MinerQuestNpc miner)
                        {
                            MinerDialogueDisplay.Draw(miner, camera);
                        }
                        else
                        {
                            DrawInteractPrompt(
                                interactPrompt,
                                prompt,
                                camera,
                                interactor.CurrentTarget as IInteractPromptBounds);
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(message))
                GUI.Label(new Rect(Screen.width * 0.5f - 300f, Screen.height - 120f, 600f, 40f), message, bigCenter);

            InventoryHotbarDisplay.Draw(ctx);
        }

        static void DrawInteractPrompt(GUIStyle style, string prompt, Camera camera, IInteractPromptBounds boundsTarget)
        {
            const float maxPromptWidth = 720f;
            var promptContent = new GUIContent(prompt);

            if (boundsTarget != null
                && boundsTarget.TryGetPromptScreenRect(camera, out var boundsRect))
            {
                float promptWidth = Mathf.Min(maxPromptWidth, Mathf.Max(boundsRect.width - 12f, 160f));
                float promptHeight = style.CalcHeight(promptContent, promptWidth);
                float promptX = boundsRect.center.x - promptWidth * 0.5f;
                float promptY = boundsRect.center.y - promptHeight * 0.5f;
                GUI.Label(new Rect(promptX, promptY, promptWidth, promptHeight), prompt, style);
                return;
            }

            float fallbackWidth = maxPromptWidth;
            float fallbackHeight = style.CalcHeight(promptContent, fallbackWidth);
            GUI.Label(
                new Rect(
                    Screen.width * 0.5f - fallbackWidth * 0.5f,
                    Screen.height * 0.5f + 34f,
                    fallbackWidth,
                    fallbackHeight),
                prompt,
                style);
        }
    }
}
