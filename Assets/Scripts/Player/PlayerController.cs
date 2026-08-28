using MonsterMiner.Core;
using MonsterMiner.UI;
using MonsterMiner.World;
using UnityEngine;

namespace MonsterMiner.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        public const float CharacterHeight = WorldScale.CharacterHeightUnits;

        [SerializeField] float moveSpeedMph = 11f;
        [SerializeField] float mouseSensitivity = 2f;
        [SerializeField] float landingBounce = 0.35f;
        [SerializeField] float groundCheckDistance = 0.2f;

        Rigidbody rb;
        Camera viewCamera;
        Transform head;
        CapsuleCollider bodyCollider;
        float pitch;
        bool groundedLastFrame;
        bool gameplayCursorLocked = true;
        bool jumpRequested;

        public Camera ViewCamera => viewCamera;
        public Transform Head => head;
        public bool IsGameplayCursorLocked => gameplayCursorLocked;

        float JumpVelocity => Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * CharacterHeight * 0.5f);

        public void Initialize(Vector3 spawnPoint)
        {
            rb = GetComponent<Rigidbody>();
            bodyCollider = GetComponent<CapsuleCollider>();
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            transform.position = SnapToFloor(spawnPoint);
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
            viewCamera.farClipPlane = 2500f;
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

            if (Input.GetKeyDown(KeyCode.Space) && !IsUiCursorMode() && !IsGameplayBlocked())
                jumpRequested = true;
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
            if (IsGameplayBlocked() || IsUiCursorMode())
            {
                jumpRequested = false;
                return;
            }

            if (GetComponent<PlayerWingsFlight>()?.IsFlying == true)
            {
                jumpRequested = false;
                return;
            }

            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            var move = transform.TransformDirection(input) * WorldScale.MilesPerHour(moveSpeedMph);
            var velocity = rb.linearVelocity;
            velocity.x = move.x;
            velocity.z = move.z;

            if (jumpRequested && IsGrounded())
            {
                if (input.sqrMagnitude < 0.01f)
                {
                    velocity.x = 0f;
                    velocity.z = 0f;
                }

                velocity.y = JumpVelocity;
                jumpRequested = false;
            }

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
            if (GrenadeThrowController.UsesMouseYForRange)
            {
                transform.Rotate(0f, yaw, 0f);
                return;
            }

            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -85f, 85f);
            transform.Rotate(0f, yaw, 0f);
            head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        bool IsGrounded()
        {
            float probeDistance = groundCheckDistance + 0.2f;
            float bottomOffset = CharacterHeight * 0.5f;
            if (bodyCollider != null)
                bottomOffset = (bodyCollider.height * 0.5f - bodyCollider.center.y) * transform.lossyScale.y;

            var origin = transform.position - Vector3.up * bottomOffset + Vector3.up * 0.08f;
            return Physics.Raycast(origin, Vector3.down, probeDistance, ~0, QueryTriggerInteraction.Ignore)
                || Physics.SphereCast(origin, 0.18f, Vector3.down, out _, probeDistance, ~0, QueryTriggerInteraction.Ignore);
        }

        public void Respawn(Vector3 spawnPoint)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = SnapToFloor(spawnPoint);
            transform.rotation = Quaternion.identity;
            pitch = 0f;
            if (head != null)
                head.localRotation = Quaternion.identity;
            LockCursorToCenter();
        }

        Vector3 SnapToFloor(Vector3 worldPos)
        {
            var bounds = GameContext.Instance?.CavernBounds;
            if (bounds == null)
                return worldPos;

            var local = bounds.transform.InverseTransformPoint(worldPos);
            float floorY;
            if (bounds.TryResolveFloorWorldPoint(local.x, local.z, out var floorPoint))
            {
                worldPos.x = floorPoint.x;
                worldPos.z = floorPoint.z;
                floorY = floorPoint.y;
            }
            else
            {
                floorY = bounds.SampleFloorWorldY(local.x, local.z);
            }

            float halfHeight = CharacterHeight * 0.5f;
            if (bodyCollider != null)
                halfHeight = (bodyCollider.height * 0.5f - bodyCollider.center.y) * transform.lossyScale.y;

            worldPos.y = floorY + halfHeight + bounds.SpawnRestHeight;
            return worldPos;
        }
    }
}
