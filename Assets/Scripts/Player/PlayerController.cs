using MonsterMiner.Core;
using MonsterMiner.UI;
using UnityEngine;

namespace MonsterMiner.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 6f;
        [SerializeField] float jumpForce = 5f;
        [SerializeField] float mouseSensitivity = 2f;
        [SerializeField] float landingBounce = 0.35f;
        [SerializeField] float groundCheckDistance = 0.2f;

        Rigidbody rb;
        Camera viewCamera;
        Transform head;
        float pitch;
        bool groundedLastFrame;
        bool gameplayCursorLocked = true;

        public Camera ViewCamera => viewCamera;
        public Transform Head => head;
        public bool IsGameplayCursorLocked => gameplayCursorLocked;

        public void Initialize(Vector3 spawnPoint)
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            transform.position = spawnPoint;
            transform.rotation = Quaternion.identity;
            pitch = 0f;

            head = new GameObject("Head").transform;
            head.SetParent(transform, false);
            head.localPosition = new Vector3(0f, 0.65f, 0f);
            head.localRotation = Quaternion.identity;

            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(head, false);
            camGo.transform.localRotation = Quaternion.identity;
            viewCamera = camGo.AddComponent<Camera>();
            viewCamera.nearClipPlane = 0.05f;
            RenderPipelineSetup.ConfigureCamera(viewCamera);
            var listener = camGo.AddComponent<AudioListener>();
            DisableExtraAudioListeners(listener);

            LockCursorToCenter();
        }

        static void DisableExtraAudioListeners(AudioListener keep)
        {
            foreach (var other in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (other != keep)
                    other.enabled = false;
            }
        }

        void Update()
        {
            HandleCursorToggle();

            if (!IsUiCursorMode()
                && gameplayCursorLocked
                && Cursor.lockState != CursorLockMode.Locked
                && Input.GetMouseButtonDown(0))
                LockCursorToCenter();

            ApplyCursorState();

            if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
                rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }

        void LateUpdate()
        {
            ApplyCursorState();
            HandleLook();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && gameplayCursorLocked && !IsUiCursorMode())
                LockCursorToCenter();
        }

        void HandleCursorToggle()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKeyDown(KeyCode.Numlock))
                gameplayCursorLocked = !gameplayCursorLocked;
#else
            gameplayCursorLocked = true;
#endif
        }

        void ApplyCursorState()
        {
            if (IsUiCursorMode())
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (gameplayCursorLocked)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void LockCursorToCenter()
        {
            gameplayCursorLocked = true;
            ApplyCursorState();
        }

        static bool IsShopMenuOpen()
        {
            return GameContext.Instance?.Shop != null && GameContext.Instance.Shop.IsMenuOpen;
        }

        static bool IsUiCursorMode()
        {
            return IsShopMenuOpen() || SellConfirmationDisplay.IsActive || DeathScreenDisplay.IsActive;
        }

        public static bool IsGameplayBlocked()
        {
            var ctx = GameContext.Instance;
            return ctx != null && (ctx.IsPlayerDead || DeathScreenDisplay.IsActive);
        }

        void FixedUpdate()
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            var move = transform.TransformDirection(input) * moveSpeed;
            var velocity = rb.linearVelocity;
            velocity.x = move.x;
            velocity.z = move.z;
            rb.linearVelocity = velocity;

            bool grounded = IsGrounded();
            if (grounded && !groundedLastFrame && rb.linearVelocity.y < -2f)
                rb.AddForce(Vector3.up * landingBounce, ForceMode.VelocityChange);
            groundedLastFrame = grounded;
        }

        void HandleLook()
        {
            if (head == null || IsUiCursorMode() || !gameplayCursorLocked || Cursor.lockState != CursorLockMode.Locked)
                return;

            float yaw = Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -85f, 85f);
            transform.Rotate(0f, yaw, 0f);
            head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        bool IsGrounded()
        {
            return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance + 0.15f);
        }

        public void Respawn(Vector3 spawnPoint)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = spawnPoint;
            transform.rotation = Quaternion.identity;
            pitch = 0f;
            if (head != null)
                head.localRotation = Quaternion.identity;
            LockCursorToCenter();
        }
    }
}
