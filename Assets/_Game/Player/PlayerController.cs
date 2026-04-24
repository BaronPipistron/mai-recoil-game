using UnityEngine;

namespace RecoilArena.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Camera playerCamera;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8.5f;
        [SerializeField] private float groundAcceleration = 22f;
        [SerializeField] private float airAcceleration = 10f;
        [SerializeField] private float jumpImpulse = 6f;
        [SerializeField] private float extraGravity = 20f;
        [SerializeField] private float groundCheckDistance = 1.2f;
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("Look")]
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float maxLookPitch = 85f;

        [Header("Dash")]
        [SerializeField] private bool dashUnlocked;
        [SerializeField] private float dashForce = 12f;
        [SerializeField] private float dashVerticalBoost = 0.8f;
        [SerializeField] private float dashCooldown = 1.6f;

        [Header("Recoil")]
        [SerializeField] private float recoilMultiplier = 1f;

        private Rigidbody _rb;
        private Vector2 _moveInput;
        private bool _jumpQueued;
        private bool _dashQueued;
        private float _yaw;
        private float _pitch;
        private float _lastDashTime = -99f;
        private float _baseMoveSpeed;
        private float _baseDashForce;
        private float _baseRecoilMultiplier;
        private bool _baseDashUnlocked;

        public bool ControlsEnabled { get; private set; }
        public bool IsGrounded { get; private set; }
        public Camera PlayerCamera => playerCamera;
        public float RecoilMultiplier => recoilMultiplier;
        public bool DashUnlocked => dashUnlocked;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.freezeRotation = true;

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (cameraPivot == null && playerCamera != null)
            {
                cameraPivot = playerCamera.transform.parent;
            }

            _yaw = transform.eulerAngles.y;
            _pitch = 0f;

            _baseMoveSpeed = moveSpeed;
            _baseDashForce = dashForce;
            _baseRecoilMultiplier = recoilMultiplier;
            _baseDashUnlocked = dashUnlocked;
        }

        private void Update()
        {
            UpdateGrounded();

            if (!ControlsEnabled)
            {
                _moveInput = Vector2.zero;
                return;
            }

            ReadInput();
            UpdateLook();
        }

        private void FixedUpdate()
        {
            if (!ControlsEnabled)
            {
                return;
            }

            ApplyMovement();
            ApplyJumpAndDash();
            ApplyExtraGravity();
        }

        public void SetControlEnabled(bool enabled)
        {
            ControlsEnabled = enabled;
            Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !enabled;

            if (!enabled)
            {
                _moveInput = Vector2.zero;
                _jumpQueued = false;
                _dashQueued = false;
            }
        }

        public void WarpTo(Vector3 worldPosition)
        {
            _rb.position = worldPosition;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _yaw = transform.eulerAngles.y;
            _pitch = 0f;
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.identity;
            }
        }

        public void SetRecoilMultiplier(float multiplier)
        {
            recoilMultiplier = Mathf.Clamp(multiplier, 0.35f, 2.8f);
        }

        public void AddRecoilMultiplier(float delta)
        {
            SetRecoilMultiplier(recoilMultiplier + delta);
        }

        public void EnableOrBoostDash(float forceBonus)
        {
            dashUnlocked = true;
            dashForce = Mathf.Clamp(dashForce + forceBonus, 8f, 32f);
        }

        public void AddMoveSpeed(float delta)
        {
            moveSpeed = Mathf.Clamp(moveSpeed + delta, 6f, 14f);
        }

        public void ResetRuntimeTuning()
        {
            moveSpeed = _baseMoveSpeed;
            dashForce = _baseDashForce;
            recoilMultiplier = _baseRecoilMultiplier;
            dashUnlocked = _baseDashUnlocked;
            _lastDashTime = -99f;
        }

        public void ApplyShotgunRecoil(Vector3 shotDirection, float recoilImpulse, float verticalLift)
        {
            Vector3 pushDirection = -shotDirection.normalized;

            // No artificial upward boost while grounded: recoil should feel like a backward blast.
            if (IsGrounded && pushDirection.y > 0f)
            {
                pushDirection.y *= 0.1f;
            }

            // Optional air assist for upgrades/balance tuning.
            if (!IsGrounded && verticalLift > 0.001f)
            {
                pushDirection += Vector3.up * verticalLift;
            }

            pushDirection.Normalize();
            _rb.AddForce(pushDirection * recoilImpulse * recoilMultiplier, ForceMode.Impulse);
        }

        private void UpdateGrounded()
        {
            IsGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
        }

        private void ReadInput()
        {
            _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (_moveInput.sqrMagnitude > 1f)
            {
                _moveInput.Normalize();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                _jumpQueued = true;
            }

            if (dashUnlocked && Input.GetKeyDown(KeyCode.LeftShift))
            {
                _dashQueued = true;
            }
        }

        private void UpdateLook()
        {
            _yaw += Input.GetAxis("Mouse X") * mouseSensitivity * 10f;
            _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * 10f;
            _pitch = Mathf.Clamp(_pitch, -maxLookPitch, maxLookPitch);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void ApplyMovement()
        {
            Vector3 desiredDirection = (transform.forward * _moveInput.y + transform.right * _moveInput.x).normalized;
            Vector3 desiredVelocity = desiredDirection * moveSpeed;

            Vector3 velocity = _rb.linearVelocity;
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 delta = desiredVelocity - horizontalVelocity;

            float accel = IsGrounded ? groundAcceleration : airAcceleration;
            _rb.AddForce(delta * accel, ForceMode.Acceleration);
        }

        private void ApplyJumpAndDash()
        {
            if (_jumpQueued && IsGrounded)
            {
                _rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
            }

            _jumpQueued = false;

            if (!_dashQueued)
            {
                return;
            }

            _dashQueued = false;
            if (!dashUnlocked || Time.time < _lastDashTime + dashCooldown)
            {
                return;
            }

            Vector3 dashDirection;
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                dashDirection = (transform.forward * _moveInput.y + transform.right * _moveInput.x).normalized;
            }
            else
            {
                Vector3 forward = cameraPivot != null ? cameraPivot.forward : transform.forward;
                dashDirection = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;
                if (dashDirection.sqrMagnitude < 0.1f)
                {
                    dashDirection = transform.forward;
                }
            }

            _rb.AddForce(dashDirection * dashForce + Vector3.up * dashVerticalBoost, ForceMode.Impulse);
            _lastDashTime = Time.time;
        }

        private void ApplyExtraGravity()
        {
            if (!IsGrounded)
            {
                _rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
            }
        }
    }
}

