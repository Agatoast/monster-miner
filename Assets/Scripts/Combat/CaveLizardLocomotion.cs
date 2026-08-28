using UnityEngine;

namespace MonsterMiner.Combat
{
    public class CaveLizardLocomotion : MonoBehaviour
    {
        const float WalkSpeedThreshold = 0.35f;
        const float RunSpeedThreshold = 1.1f;

        Animator animator;
        Vector3 lastPosition;
        string currentState = "idle";
        float attackAnimTimer;

        void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
                animator.applyRootMotion = false;
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

            float speed = (transform.position - lastPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            lastPosition = transform.position;

            string nextState = speed >= RunSpeedThreshold
                ? "run"
                : speed >= WalkSpeedThreshold
                    ? "walk"
                    : "battleidle";
            PlayState(nextState);
        }

        public void PlayAttack()
        {
            if (animator == null)
                return;

            attackAnimTimer = 0.85f;
            PlayState("attack1");
        }

        void PlayState(string stateName)
        {
            if (currentState == stateName)
                return;

            currentState = stateName;
            animator.Play(stateName, 0, 0f);
        }
    }
}
