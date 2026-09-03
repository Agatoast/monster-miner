using UnityEngine;

namespace MonsterMiner.Combat
{
    public class TaipanLocomotion : MonoBehaviour
    {
        const float WalkEnterSpeed = 1.1f;
        const float WalkExitSpeed = 0.35f;
        const float RunEnterSpeed = 2.4f;
        const float RunExitSpeed = 1.6f;
        const float SpeedSmoothing = 6f;
        const float CrossFadeSeconds = 0.22f;

        Animator animator;
        Vector3 lastPosition;
        string currentState = "Idle_A";
        float attackAnimTimer;
        float smoothedSpeed;
        bool isRunning;
        bool isWalking;

        void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            lastPosition = transform.position;
        }

        void LateUpdate()
        {
            if (animator == null)
                return;

            if (attackAnimTimer > 0f)
            {
                attackAnimTimer -= Time.deltaTime;
                return;
            }

            float frameSpeed = (transform.position - lastPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            lastPosition = transform.position;
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, frameSpeed, Time.deltaTime * SpeedSmoothing);

            UpdateLocomotionState(smoothedSpeed);
        }

        void UpdateLocomotionState(float speed)
        {
            if (!isRunning && speed >= RunEnterSpeed)
            {
                isRunning = true;
                isWalking = false;
            }
            else if (isRunning && speed <= RunExitSpeed)
            {
                isRunning = false;
                isWalking = speed >= WalkEnterSpeed;
            }

            if (!isWalking && !isRunning && speed >= WalkEnterSpeed)
                isWalking = true;
            else if (isWalking && !isRunning && speed <= WalkExitSpeed)
                isWalking = false;

            string nextState = isRunning
                ? "Run"
                : isWalking
                    ? "Walk"
                    : "Idle_A";
            PlayState(nextState);
        }

        public void PlayAttack()
        {
            if (animator == null)
                return;

            attackAnimTimer = 1.1f;
            isRunning = false;
            isWalking = false;
            PlayState("Attack", forceRestart: true);
        }

        void PlayState(string stateName, bool forceRestart = false)
        {
            if (!forceRestart && currentState == stateName)
                return;

            currentState = stateName;
            animator.CrossFade(stateName, CrossFadeSeconds, 0, forceRestart ? 0f : float.NegativeInfinity);
        }
    }
}
