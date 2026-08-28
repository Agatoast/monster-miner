using UnityEngine;

namespace MonsterMiner.Combat
{
    public class RabbitLocomotion : MonoBehaviour
    {
        const float RunSpeedThreshold = 0.35f;

        Animator animator;
        Vector3 lastPosition;
        string currentState = "Idle";

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

            float speed = (transform.position - lastPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            lastPosition = transform.position;
            PlayState(speed >= RunSpeedThreshold ? "Run" : "Idle");
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
