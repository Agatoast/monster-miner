using UnityEngine;

namespace MonsterMiner.Combat
{
    public class GremlinLocomotion : MonoBehaviour
    {
        const float RunSpeedThreshold = 0.35f;

        Animator animator;
        Vector3 lastPosition;
        string currentState = "idle";
        float attackAnimTimer;

        void Awake()
        {
            animator = GetComponent<Animator>();
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
            PlayState(speed >= RunSpeedThreshold ? "Run" : "idle");
        }

        public void PlayAttack()
        {
            if (animator == null)
                return;

            attackAnimTimer = 0.75f;
            PlayState("Attack");
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
