using UnityEngine;

namespace MonsterMiner.Combat
{
    public class SkyMetalAlienLocomotion : MonoBehaviour
    {
        const float WalkSpeedThreshold = 0.35f;
        const float WalkRetriggerSeconds = 0.55f;
        const float AttackLockSeconds = 1.05f;

        static readonly string[] AttackTriggers =
        {
            "Attack_1",
            "Attack_2",
            "Attack_3",
            "Attack_4",
            "Attack_5"
        };

        Animator animator;
        Vector3 lastPosition;
        bool isWalking;
        bool useAlternateWalkCycle;
        float walkRetriggerTimer;
        float attackAnimTimer;
        int nextAttackIndex;

        void Awake()
        {
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
            }

            lastPosition = transform.position;
        }

        void Start()
        {
            SetTrigger("Intimidate_1");
        }

        void LateUpdate()
        {
            if (animator == null || !animator.enabled)
                return;

            if (attackAnimTimer > 0f)
            {
                attackAnimTimer -= Time.deltaTime;
                return;
            }

            float speed = (transform.position - lastPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            lastPosition = transform.position;

            if (speed >= WalkSpeedThreshold)
            {
                walkRetriggerTimer -= Time.deltaTime;
                if (!isWalking || walkRetriggerTimer <= 0f)
                {
                    isWalking = true;
                    walkRetriggerTimer = WalkRetriggerSeconds;
                    SetTrigger(useAlternateWalkCycle ? "Walk_Cycle_2" : "Walk_Cycle_1");
                    useAlternateWalkCycle = !useAlternateWalkCycle;
                }

                return;
            }

            if (isWalking)
            {
                isWalking = false;
                SetTrigger("Fight_Idle_1");
            }
        }

        public void PlayAttack()
        {
            if (animator == null)
                return;

            attackAnimTimer = AttackLockSeconds;
            isWalking = false;
            SetTrigger(AttackTriggers[nextAttackIndex % AttackTriggers.Length]);
            nextAttackIndex++;
        }

        void SetTrigger(string triggerName)
        {
            animator.ResetTrigger(triggerName);
            animator.SetTrigger(triggerName);
        }
    }
}
