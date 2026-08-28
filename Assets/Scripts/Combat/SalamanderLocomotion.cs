using UnityEngine;

namespace MonsterMiner.Combat
{
    public class SalamanderLocomotion : MonoBehaviour
    {
        const float WalkSpeedThreshold = 0.35f;
        const string IdleState = "Creep|Idle1_Action";
        const string WalkState = "Creep|Walk1_Action";
        const string AttackState = "Creep|Bite_Action";

        Animator animator;
        Vector3 lastPosition;
        string currentState = IdleState;
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
            PlayState(speed >= WalkSpeedThreshold ? WalkState : IdleState);
        }

        public void PlayAttack()
        {
            if (animator == null)
                return;

            attackAnimTimer = 0.9f;
            PlayState(AttackState);
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
