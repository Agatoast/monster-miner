using UnityEngine;

namespace MonsterMiner.Combat
{
    public class IguanaLocomotion : MonoBehaviour
    {
        Animator animator;
        Vector3 lastPosition;
        float smoothedForward;
        float smoothedTurn;

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

            Vector3 delta = transform.position - lastPosition;
            lastPosition = transform.position;

            Vector3 localDelta = transform.InverseTransformDirection(delta);
            float forward = localDelta.z / Mathf.Max(Time.deltaTime, 0.0001f);
            float turn = localDelta.x / Mathf.Max(Time.deltaTime, 0.0001f);

            smoothedForward = Mathf.Lerp(smoothedForward, forward, Time.deltaTime * 8f);
            smoothedTurn = Mathf.Lerp(smoothedTurn, turn, Time.deltaTime * 8f);

            animator.SetFloat("Forward", Mathf.Clamp(smoothedForward, -1f, 1f));
            animator.SetFloat("Turn", Mathf.Clamp(smoothedTurn, -1f, 1f));
        }
    }
}
