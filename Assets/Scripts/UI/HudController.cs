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
        string centerMessage = string.Empty;
        float centerMessageTimer;
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

        public void ShowCenterMessage(string text)
        {
            centerMessage = text;
            centerMessageTimer = Mathf.Max(centerMessageTimer, 2f);
        }

        void Update()
        {
            DeathScreenDisplay.HandleInput();
            MinerTurnInPopupDisplay.HandleInput();
            WorldMapDisplay.HandleInput();
            HudEggHitDisplay.Tick(Time.deltaTime);
            CombatHitFeedbackDisplay.Tick(Time.deltaTime);

            if (messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
                if (messageTimer <= 0f)
                    message = string.Empty;
            }

            if (centerMessageTimer > 0f)
            {
                centerMessageTimer -= Time.deltaTime;
                if (centerMessageTimer <= 0f)
                    centerMessage = string.Empty;
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

            if (!DeathScreenDisplay.IsActive
                && (ctx.Shop == null || !ctx.Shop.IsMenuOpen)
                && !SellConfirmationDisplay.IsActive
                && !MinerTurnInPopupDisplay.IsActive
                && !WorldMapDisplay.IsActive
                && !ctx.IsPlayerDead)
            {
                RangedCrosshairDisplay.Draw(ctx);
            }

            if (DeathScreenDisplay.IsActive)
            {
                DeathScreenDisplay.Draw();
            }
            else if (MinerTurnInPopupDisplay.IsActive)
            {
                MinerTurnInPopupDisplay.Draw();
            }
            else if (WorldMapDisplay.IsActive)
            {
                WorldMapDisplay.Draw();
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
                var creatureCarrier = ctx.Player != null
                    ? ctx.Player.GetComponent<Player.PlayerCreatureCarrier>()
                    : null;
                if (creatureCarrier != null && creatureCarrier.IsCarrying)
                {
                    DrawInteractPrompt(
                        interactPrompt,
                        InteractPromptDisplay.FormatPrompt("Throw it! [E]"),
                        ctx.Player.ViewCamera,
                        null,
                        ctx);
                }
                else
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
                                    interactor.CurrentTarget as IInteractPromptBounds,
                                    ctx);
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(message))
                GUI.Label(new Rect(Screen.width * 0.5f - 300f, Screen.height - 120f, 600f, 40f), message, bigCenter);

            InventoryHotbarDisplay.Draw(ctx);

            if (!string.IsNullOrEmpty(centerMessage))
            {
                var warningStyle = new GUIStyle(bigCenter)
                {
                    fontSize = 24,
                    normal = { textColor = new Color(1f, 0.92f, 0.55f) }
                };
                GUI.Label(
                    new Rect(Screen.width * 0.5f - 320f, Screen.height * 0.5f - 24f, 640f, 48f),
                    centerMessage,
                    warningStyle);
            }
        }

        static void DrawInteractPrompt(GUIStyle style, string prompt, Camera camera, IInteractPromptBounds boundsTarget, GameContext ctx)
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

                if (RangedCrosshairDisplay.TryGetAmmoTopY(ctx, out float ammoTopY)
                    && promptY + promptHeight > ammoTopY - 8f)
                {
                    promptY = RangedCrosshairDisplay.GetPromptYAboveRangedHud(ctx, promptHeight);
                }

                GUI.Label(new Rect(promptX, promptY, promptWidth, promptHeight), prompt, style);
                return;
            }

            float fallbackWidth = maxPromptWidth;
            float fallbackHeight = style.CalcHeight(promptContent, fallbackWidth);
            float fallbackY = RangedCrosshairDisplay.GetPromptYAboveRangedHud(ctx, fallbackHeight);
            GUI.Label(
                new Rect(
                    Screen.width * 0.5f - fallbackWidth * 0.5f,
                    fallbackY,
                    fallbackWidth,
                    fallbackHeight),
                prompt,
                style);
        }
    }
}
