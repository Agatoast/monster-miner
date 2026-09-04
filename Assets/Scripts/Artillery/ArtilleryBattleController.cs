using System.Collections;
using MonsterMiner.UI;
using UnityEngine;

namespace MonsterMiner.Artillery
{
    public class ArtilleryBattleController : MonoBehaviour
    {
        const float EnemyThinkSeconds = 1.2f;
        const float PlayerShotDelaySeconds = 1f;
        const float EnemyShotDelaySeconds = 1f;

        enum Phase
        {
            WaitingForPlayer,
            PlayerAiming,
            PlayerShotDelay,
            Firing,
            WaitingForProjectile,
            CavalryAdvance,
            EnemyTurn,
            EnemyShotDelay,
            EnemyFiring,
            BattleOver,
            Resolution
        }

        const string VictoryMessage =
            "The dust clears and you have defeated the foe.\n\n"
            + "Return to the Daimyo and report your victory.";
        const string DefeatMessage =
            "The dust clears and the enemy forces advance one step closer to victory.\n\n"
            + "You need to regroup and get back to the line as soon as possible!";

        ArtilleryField field;
        ArtilleryProjectile projectile;
        Phase phase = Phase.WaitingForPlayer;
        ArtilleryCatapult selectedCatapult;
        ArtillerySide playerSide = ArtillerySide.Left;
        ArtillerySide enemySide = ArtillerySide.Right;
        float wind;
        bool lastShotWasPlayer;
        Coroutine enemyRoutine;
        Coroutine playerShotRoutine;
        ArtilleryHitTarget enemyFocusTarget;
        GUIStyle hudStyle;
        string centerMessage;
        float centerMessageTimer;
        bool centerMessageCentered;
        GUIStyle centerMessageStyle;
        GUIStyle centerMessageCenteredStyle;
        GUIStyle windMphStyle;
        GUIStyle yourTurnStyle;
        GUIStyle fireHintStyle;
        GUIStyle targetNameStyle;
        GUIStyle targetHitsStyle;

        const float TargetNameGapPixels = 6f;
        const float TargetHitsGapPixels = 1f;
        const float TargetHitsBoxPaddingX = 2f;
        const float TargetHitsBoxPaddingY = 0f;
        const float WindHudScreenOffsetY = 5f;

        public float Wind => wind;

        public void Initialize(ArtilleryField artilleryField)
        {
            field = artilleryField;
            projectile = CreateProjectile();
            field.BindWindFlag(this);
            enemyFocusTarget = null;
            RollWind();
            phase = Phase.WaitingForPlayer;
        }

        public void HandleInput()
        {
            if (field == null || !ArtillerySession.IsActive || phase == Phase.BattleOver || phase == Phase.Resolution)
                return;

            if (ArtilleryPauseDisplay.IsOpen || ArtilleryShotPanel.IsOpen)
                return;

            if (!Input.GetKeyDown(KeyCode.F))
                return;

            if (phase != Phase.WaitingForPlayer)
                return;

            var playerCatapult = field.GetCatapult(playerSide);
            if (playerCatapult?.Animator == null)
                return;

            selectedCatapult = playerCatapult;
            ArtilleryShotPanel.Open();
            phase = Phase.PlayerAiming;
        }

        public void DrawHud()
        {
            if (field == null || !ArtillerySession.IsActive)
                return;

            EnsureHudStyle();

            string turnText = phase switch
            {
                Phase.PlayerAiming => string.Empty,
                Phase.PlayerShotDelay => string.Empty,
                Phase.Firing => "Catapult firing...",
                Phase.WaitingForProjectile => "Projectile in flight",
                Phase.EnemyTurn => "Enemy turn",
                Phase.EnemyShotDelay => string.Empty,
                Phase.EnemyFiring => "Enemy firing...",
                Phase.CavalryAdvance => string.Empty,
                Phase.BattleOver => string.Empty,
                Phase.Resolution => string.Empty,
                _ => string.Empty
            };

            var rect = new Rect(16f, 16f, 560f, 28f);
            if (!string.IsNullOrEmpty(turnText))
                GUI.Label(rect, turnText, hudStyle);
        }

        public void DrawWindMph()
        {
            if (field == null || !ArtillerySession.IsActive || field.ViewCamera == null)
                return;

            if (!field.TryGetWindLabelWorldPosition(out Vector3 worldPosition))
                return;

            EnsureWindMphStyle();
            EnsureYourTurnStyle();
            EnsureFireHintStyle();

            string yourTurnText = phase == Phase.WaitingForPlayer ? "Your Turn" : string.Empty;
            string text = $"{ArtilleryRockPhysics.WindSpeedToMph(wind):0} MPH";
            string fireHint = phase == Phase.BattleOver || phase == Phase.Resolution
                ? string.Empty
                : "F to fire catapult";
            var yourTurnSize = string.IsNullOrEmpty(yourTurnText)
                ? Vector2.zero
                : yourTurnStyle.CalcSize(new GUIContent(yourTurnText));
            var content = new GUIContent(text);
            var size = windMphStyle.CalcSize(content);
            var hintSize = string.IsNullOrEmpty(fireHint)
                ? Vector2.zero
                : fireHintStyle.CalcSize(new GUIContent(fireHint));

            if (!string.IsNullOrEmpty(yourTurnText)
                && field.TryGetYourTurnLabelWorldPosition(out Vector3 yourTurnWorld))
            {
                Vector3 yourTurnScreen = field.ViewCamera.WorldToScreenPoint(yourTurnWorld);
                float yourTurnGuiX = yourTurnScreen.x - yourTurnSize.x * 0.5f;
                float yourTurnGuiY = Screen.height - yourTurnScreen.y - yourTurnSize.y * 0.5f - WindHudScreenOffsetY;
                GUI.Label(
                    new Rect(yourTurnGuiX, yourTurnGuiY, yourTurnSize.x, yourTurnSize.y),
                    yourTurnText,
                    yourTurnStyle);
            }

            Vector3 screen = field.ViewCamera.WorldToScreenPoint(worldPosition);
            float blockWidth = Mathf.Max(size.x, hintSize.x);
            float blockHeight = size.y + (hintSize.y > 0f ? hintSize.y + 6f : 0f);
            float guiX = screen.x - blockWidth * 0.5f;
            float guiY = Screen.height - screen.y - blockHeight * 0.5f - WindHudScreenOffsetY;
            GUI.Label(
                new Rect(guiX + (blockWidth - size.x) * 0.5f, guiY, size.x, size.y),
                text,
                windMphStyle);
            if (!string.IsNullOrEmpty(fireHint))
            {
                GUI.Label(
                    new Rect(guiX + (blockWidth - hintSize.x) * 0.5f, guiY + size.y + 6f, hintSize.x, hintSize.y),
                    fireHint,
                    fireHintStyle);
            }
        }

        public void DrawTargetLabels()
        {
            if (field == null || !ArtillerySession.IsActive || field.ViewCamera == null)
                return;

            EnsureTargetLabelStyles();
            var camera = field.ViewCamera;

            for (int i = 0; i < field.HitTargetCount; i++)
            {
                var target = field.GetHitTarget(i);
                if (target == null || target.IsDestroyed)
                    continue;

                Vector3 topWorld = field.GetTargetWorldPoint(target, target.CenterX, target.TopY);
                Vector3 bottomWorld = field.GetTargetWorldPoint(target, target.CenterX, target.BottomY);
                Vector3 topScreen = camera.WorldToScreenPoint(topWorld);
                Vector3 bottomScreen = camera.WorldToScreenPoint(bottomWorld);
                if (topScreen.z < 0f || bottomScreen.z < 0f)
                    continue;

                string name = target.GetDisplayName();
                var nameContent = new GUIContent(name);
                var nameSize = targetNameStyle.CalcSize(nameContent);
                float nameGuiX = topScreen.x - nameSize.x * 0.5f;
                float nameGuiY = Screen.height - topScreen.y - nameSize.y - TargetNameGapPixels;
                GUI.Label(new Rect(nameGuiX, nameGuiY, nameSize.x, nameSize.y), name, targetNameStyle);

                if (target.Kind == ArtilleryTargetKind.Catapult)
                    continue;

                string hitsText = $"{target.HitsRemaining}/{target.MaxHits}";
                var hitsContent = new GUIContent(hitsText);
                var hitsSize = targetHitsStyle.CalcSize(hitsContent);
                float boxWidth = hitsSize.x + TargetHitsBoxPaddingX * 2f;
                float boxHeight = hitsSize.y + TargetHitsBoxPaddingY * 2f;
                float boxGuiX = bottomScreen.x - boxWidth * 0.5f;
                float boxGuiY = Screen.height - bottomScreen.y + TargetHitsGapPixels;
                var boxRect = new Rect(boxGuiX, boxGuiY, boxWidth, boxHeight);
                GUI.color = Color.black;
                GUI.DrawTexture(boxRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(
                    new Rect(boxRect.x + TargetHitsBoxPaddingX, boxRect.y + TargetHitsBoxPaddingY, hitsSize.x, hitsSize.y),
                    hitsText,
                    targetHitsStyle);
            }
        }

        void EnsureTargetLabelStyles()
        {
            if (targetNameStyle != null)
                return;

            targetNameStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 25,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            targetHitsStyle = new GUIStyle(targetNameStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                normal = { textColor = Color.white }
            };
        }

        void EnsureYourTurnStyle()
        {
            if (yourTurnStyle != null)
                return;

            yourTurnStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 50,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black }
            };
        }

        void EnsureFireHintStyle()
        {
            if (fireHintStyle != null)
                return;

            fireHintStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 40,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        void EnsureWindMphStyle()
        {
            if (windMphStyle != null)
                return;

            windMphStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 50,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        public void DrawShotPanel()
        {
            if (phase == Phase.BattleOver || phase == Phase.Resolution)
                return;

            ArtilleryShotPanel.Draw(ConfirmPlayerShot);
        }

        void ConfirmPlayerShot(float angleDegrees, float power)
        {
            if (selectedCatapult == null)
                selectedCatapult = field?.GetCatapult(playerSide);
            if (selectedCatapult == null)
            {
                phase = Phase.WaitingForPlayer;
                return;
            }

            ArtilleryShotPanel.Close();
            phase = Phase.PlayerShotDelay;

            if (playerShotRoutine != null)
                StopCoroutine(playerShotRoutine);
            playerShotRoutine = StartCoroutine(DelayedPlayerShot(angleDegrees, power));
        }

        IEnumerator DelayedPlayerShot(float angleDegrees, float power)
        {
            yield return new WaitForSeconds(PlayerShotDelaySeconds);

            if (selectedCatapult == null)
            {
                phase = Phase.WaitingForPlayer;
                playerShotRoutine = null;
                yield break;
            }

            BeginShot(selectedCatapult, angleDegrees, power, Phase.Firing, wind);
            playerShotRoutine = null;
        }

        void BeginShot(ArtilleryCatapult catapult, float angleDegrees, float power, Phase firingPhase, float shotWind)
        {
            phase = firingPhase;
            lastShotWasPlayer = catapult.Side == playerSide;
            float speed = ArtilleryRockPhysics.LaunchSpeed(power);
            var direction = catapult.GetLaunchDirection2D(angleDegrees);
            var initialVelocity = direction * speed;

            catapult.Animator.PlayOnce(() =>
            {
                if (projectile == null)
                    projectile = CreateProjectile();

                projectile.Launch(
                    field,
                    catapult.Side,
                    catapult.GetLaunchWorldPosition(),
                    initialVelocity,
                    shotWind);
                phase = Phase.WaitingForProjectile;
            }, null);
        }

        void ShowCenterMessage(string message, float durationSeconds = 2.75f, bool centerText = false)
        {
            centerMessage = message;
            centerMessageTimer = durationSeconds;
            centerMessageCentered = centerText;
        }

        public void DrawCenterMessage()
        {
            if (centerMessageTimer <= 0f || string.IsNullOrEmpty(centerMessage))
                return;

            EnsureCenterMessageStyle();

            var style = centerMessageCentered ? centerMessageCenteredStyle : centerMessageStyle;
            var content = new GUIContent(centerMessage);
            float textWidth = Mathf.Min(720f, Screen.width - 48f);
            float textHeight = style.CalcHeight(content, textWidth);
            float paddingX = 24f;
            float paddingY = 14f;
            var panel = new Rect(
                (Screen.width - textWidth) * 0.5f - paddingX,
                (Screen.height - textHeight) * 0.5f - paddingY,
                textWidth + paddingX * 2f,
                textHeight + paddingY * 2f);

            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(
                new Rect(panel.x + paddingX, panel.y + paddingY, textWidth, textHeight),
                centerMessage,
                style);
        }

        void EnsureCenterMessageStyle()
        {
            if (centerMessageStyle != null)
                return;

            centerMessageStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            centerMessageCenteredStyle = new GUIStyle(centerMessageStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        void Update()
        {
            if (centerMessageTimer > 0f)
                centerMessageTimer -= Time.deltaTime;

            if (field == null || phase == Phase.BattleOver || phase == Phase.Resolution)
                return;

            if (phase != Phase.WaitingForProjectile || projectile == null || projectile.IsActive)
                return;

            phase = Phase.CavalryAdvance;
            var movingSide = lastShotWasPlayer ? playerSide : enemySide;
            var cavalryResult = field.ProcessEndOfTurnCavalry(movingSide, playerSide);
            if (cavalryResult == ArtilleryCavalryTurnResult.PlayerVictory)
            {
                BeginResolution(playerVictory: true);
                return;
            }

            if (cavalryResult == ArtilleryCavalryTurnResult.PlayerDefeat)
            {
                BeginResolution(playerVictory: false);
                return;
            }

            if (TryEndBattle())
                return;

            if (lastShotWasPlayer)
            {
                BeginEnemyTurn();
                return;
            }

            BeginPlayerTurn();
        }

        void BeginPlayerTurn()
        {
            phase = Phase.WaitingForPlayer;
            RollWind();
            selectedCatapult = null;
        }

        void BeginEnemyTurn()
        {
            if (field.IsSideDefeated(enemySide))
            {
                TryEndBattle();
                return;
            }

            phase = Phase.EnemyTurn;
            RollWind();
            selectedCatapult = null;

            if (enemyRoutine != null)
                StopCoroutine(enemyRoutine);
            enemyRoutine = StartCoroutine(EnemyTurnRoutine());
        }

        IEnumerator EnemyTurnRoutine()
        {
            yield return new WaitForSeconds(EnemyThinkSeconds);

            var enemyCatapult = field.GetCatapult(enemySide);
            if (enemyCatapult == null || !enemyCatapult.gameObject.activeInHierarchy)
            {
                BeginPlayerTurn();
                enemyRoutine = null;
                yield break;
            }

            enemyFocusTarget = field.SelectFocusTarget(playerSide, enemyFocusTarget);
            if (enemyFocusTarget == null)
            {
                BeginPlayerTurn();
                enemyRoutine = null;
                yield break;
            }

            float shotWind = wind;
            if (!ArtilleryEnemyAI.TryPlanShot(field, enemyCatapult, enemyFocusTarget, shotWind, out float angle, out float power))
            {
                BeginPlayerTurn();
                enemyRoutine = null;
                yield break;
            }

            phase = Phase.EnemyShotDelay;
            yield return new WaitForSeconds(EnemyShotDelaySeconds);

            if (enemyCatapult == null || !enemyCatapult.gameObject.activeInHierarchy)
            {
                BeginPlayerTurn();
                enemyRoutine = null;
                yield break;
            }

            BeginShot(enemyCatapult, angle, power, Phase.EnemyFiring, shotWind);
            enemyRoutine = null;
        }

        bool TryEndBattle()
        {
            if (field.IsSideDefeated(enemySide))
            {
                BeginResolution(playerVictory: true);
                return true;
            }

            if (field.IsSideDefeated(playerSide))
            {
                BeginResolution(playerVictory: false);
                return true;
            }

            return false;
        }

        void BeginResolution(bool playerVictory)
        {
            phase = Phase.Resolution;
            ArtilleryShotPanel.Close();
            ArtillerySession.SetLastBattleResult(playerVictory);

            MinerTurnInPopupDisplay.Show(
                playerVictory ? VictoryMessage : DefeatMessage,
                centerBody: true,
                dismissCallback: ArtillerySession.Finish,
                okOnly: true);
        }

        void RollWind()
        {
            wind = Random.Range(ArtilleryRockPhysics.WindMin, ArtilleryRockPhysics.WindMax);
        }

        ArtilleryProjectile CreateProjectile()
        {
            var go = new GameObject("ArtilleryProjectile");
            go.transform.SetParent(field.transform, false);
            go.SetActive(false);
            return go.AddComponent<ArtilleryProjectile>();
        }

        void EnsureHudStyle()
        {
            if (hudStyle != null)
                return;

            hudStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        void OnDestroy()
        {
            if (enemyRoutine != null)
                StopCoroutine(enemyRoutine);
            if (playerShotRoutine != null)
                StopCoroutine(playerShotRoutine);
        }
    }
}
