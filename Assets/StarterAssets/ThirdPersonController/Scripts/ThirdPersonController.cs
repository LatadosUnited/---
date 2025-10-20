using UnityEngine;
﻿using Unity.Netcode;
﻿#if ENABLE_INPUT_SYSTEM
﻿using UnityEngine.InputSystem;
﻿#endif
﻿
﻿namespace StarterAssets
﻿{
﻿    [RequireComponent(typeof(CharacterController))]
﻿#if ENABLE_INPUT_SYSTEM
﻿    [RequireComponent(typeof(PlayerInput))]
﻿#endif
﻿    public class ThirdPersonController : NetworkBehaviour
﻿    {
﻿        [Header("Player")]
﻿        [Tooltip("Move speed of the character in m/s")]
﻿        public float MoveSpeed = 2.0f;
﻿        [Tooltip("Sprint speed of the character in m/s")]
﻿        public float SprintSpeed = 5.335f;
﻿        [Tooltip("How fast the character turns to face movement direction")]
﻿        [Range(0.0f, 0.3f)]
﻿        public float RotationSmoothTime = 0.12f;
﻿        [Tooltip("Acceleration and deceleration")]
﻿        public float SpeedChangeRate = 10.0f;
﻿        public AudioClip LandingAudioClip;
﻿        public AudioClip[] FootstepAudioClips;
﻿        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;
﻿
﻿        [Space(10)]
﻿        [Tooltip("The height the player can jump")]
﻿        public float JumpHeight = 1.2f;
﻿        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
﻿        public float Gravity = -15.0f;
﻿
﻿        [Space(10)]
﻿        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
﻿        public float JumpTimeout = 0.50f;
﻿        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
﻿        public float FallTimeout = 0.15f;
﻿
﻿        [Header("Player Grounded")]
﻿        public bool Grounded = true;
﻿        [Tooltip("Useful for rough ground")]
﻿        public float GroundedOffset = -0.14f;
﻿        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
﻿        public float GroundedRadius = 0.28f;
﻿        [Tooltip("What layers the character uses as ground")]
﻿        public LayerMask GroundLayers;
﻿
﻿        [Header("Standard Camera")]
﻿        [Tooltip("The distance from the player the camera will be")]
﻿        public float CameraDistance = 3.0f;
﻿        [Tooltip("The height offset of the camera's look-at point")]
﻿        public float CameraHeightOffset = 1.2f;
﻿        [Tooltip("How far in degrees can you move the camera up")]
﻿        public float TopClamp = 70.0f;
﻿        [Tooltip("How far in degrees can you move the camera down")]
﻿        public float BottomClamp = -30.0f;
﻿
﻿        // player
﻿        private float _speed;
﻿        private float _animationBlend;
﻿        private float _targetRotation = 0.0f;
﻿        private float _rotationVelocity;
﻿        private float _verticalVelocity;
﻿        private float _terminalVelocity = 53.0f;
﻿
﻿        // timeout deltatime
﻿        private float _jumpTimeoutDelta;
﻿        private float _fallTimeoutDelta;
﻿
﻿        // animation IDs
﻿        private int _animIDSpeed;
﻿        private int _animIDGrounded;
﻿        private int _animIDJump;
﻿        private int _animIDFreeFall;
﻿        private int _animIDMotionSpeed;
﻿
﻿        // camera
﻿        private float _cameraYaw;
﻿        private float _cameraPitch;
﻿
﻿#if ENABLE_INPUT_SYSTEM
﻿        private PlayerInput _playerInput;
﻿#endif
﻿        private Animator _animator;
﻿        private CharacterController _controller;
﻿        private StarterAssetsInputs _input;
﻿        private GameObject _mainCamera;
﻿
﻿        private const float _threshold = 0.01f;
﻿
﻿        private bool _hasAnimator;
﻿
﻿        // --- NETCODE VARIABLES ---
﻿        private NetworkVariable<float> _netAnimSpeed = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
﻿        private NetworkVariable<float> _netAnimMotionSpeed = new NetworkVariable<float>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
﻿        private NetworkVariable<bool> _netAnimGrounded = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
﻿        private NetworkVariable<bool> _netAnimJump = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
﻿        private NetworkVariable<bool> _netAnimFreeFall = new NetworkVariable<bool>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
﻿
﻿        private bool IsCurrentDeviceMouse
﻿        {
﻿            get
﻿            {
﻿#if ENABLE_INPUT_SYSTEM
﻿                return _playerInput.currentControlScheme == "KeyboardMouse";
﻿#else
﻿                return false;
﻿#endif
﻿            }
﻿        }
﻿
﻿        public override void OnNetworkSpawn()
﻿        {
﻿            if (IsOwner)
﻿            {
﻿                // get a reference to our main camera
﻿                if (_mainCamera == null)
﻿                {
﻿                    _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
﻿                }
﻿            }
﻿            else
﻿            {
﻿                GetComponent<PlayerInput>().enabled = false;
                GetComponent<CharacterController>().enabled = false;
﻿            }
﻿        }
﻿
﻿        private void Start()
﻿        {
﻿            _hasAnimator = TryGetComponent(out _animator);
﻿            _controller = GetComponent<CharacterController>();
﻿            _input = GetComponent<StarterAssetsInputs>();
﻿#if ENABLE_INPUT_SYSTEM
﻿            _playerInput = GetComponent<PlayerInput>();
﻿#else
﻿            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
﻿#endif
﻿
﻿            AssignAnimationIDs();
﻿
﻿            // reset our timeouts on start
﻿            _jumpTimeoutDelta = JumpTimeout;
﻿            _fallTimeoutDelta = FallTimeout;
﻿
﻿            // Initialize camera rotation
﻿            _cameraYaw = transform.eulerAngles.y;
﻿        }
﻿
﻿        private void Update()
﻿        {
﻿            _hasAnimator = TryGetComponent(out _animator);
﻿
﻿            if (IsOwner)
﻿            {
﻿                JumpAndGravity();
﻿                GroundedCheck();
﻿                Move();
﻿            }
﻿
﻿            if (_hasAnimator)
﻿            {
﻿                _animator.SetBool(_animIDGrounded, _netAnimGrounded.Value);
﻿                _animator.SetFloat(_animIDSpeed, _netAnimSpeed.Value);
﻿                _animator.SetFloat(_animIDMotionSpeed, _netAnimMotionSpeed.Value);
﻿                _animator.SetBool(_animIDJump, _netAnimJump.Value);
﻿                _animator.SetBool(_animIDFreeFall, _netAnimFreeFall.Value);
﻿
﻿                if (!IsOwner)
﻿                {
﻿                    if (_netAnimJump.Value) _animator.SetBool(_animIDJump, true);
﻿                    if (_netAnimFreeFall.Value) _animator.SetBool(_animIDFreeFall, true);
﻿                }
﻿            }
﻿        }
﻿
﻿        private void LateUpdate()
﻿        {
﻿            if (IsOwner && _mainCamera != null)
﻿            {
﻿                CameraLogic();
﻿            }
﻿        }
﻿
﻿        private void AssignAnimationIDs()
﻿        {
﻿            _animIDSpeed = Animator.StringToHash("Speed");
﻿            _animIDGrounded = Animator.StringToHash("Grounded");
﻿            _animIDJump = Animator.StringToHash("Jump");
﻿            _animIDFreeFall = Animator.StringToHash("FreeFall");
﻿            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
﻿        }
﻿
﻿        private void GroundedCheck()
﻿        {
﻿            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
﻿            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
﻿            _netAnimGrounded.Value = Grounded;
﻿        }
﻿
﻿        private void CameraLogic()
﻿        {
﻿            if (_input.look.sqrMagnitude >= _threshold)
﻿            {
﻿                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
﻿                _cameraYaw += _input.look.x * deltaTimeMultiplier;
﻿                _cameraPitch += _input.look.y * deltaTimeMultiplier; // Pitch não invertido
﻿            }
﻿
﻿            _cameraYaw = ClampAngle(_cameraYaw, float.MinValue, float.MaxValue);
﻿            _cameraPitch = ClampAngle(_cameraPitch, BottomClamp, TopClamp);
﻿
﻿            Vector3 targetPosition = transform.position + Vector3.up * CameraHeightOffset;
﻿            Quaternion cameraRotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0.0f);
﻿            Vector3 cameraPosition = targetPosition - cameraRotation * Vector3.forward * CameraDistance;
﻿
﻿            _mainCamera.transform.position = cameraPosition;
﻿            _mainCamera.transform.rotation = cameraRotation;
﻿        }
﻿
﻿        private void Move()
﻿        {
﻿            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
﻿            if (_input.move == Vector2.zero) targetSpeed = 0.0f;
﻿            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
﻿            float speedOffset = 0.1f;
﻿            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;
﻿
﻿            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
﻿            {
﻿                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
﻿                _speed = Mathf.Round(_speed * 1000f) / 1000f;
﻿            }
﻿            else
﻿            {
﻿                _speed = targetSpeed;
﻿            }
﻿            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
﻿            if (_animationBlend < 0.01f) _animationBlend = 0f;
﻿
﻿            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
﻿
﻿            if (_input.move != Vector2.zero)
﻿            {
﻿                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
﻿                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
﻿                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
﻿            }
﻿
﻿            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
﻿            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
﻿
﻿            _netAnimSpeed.Value = _animationBlend;
﻿            _netAnimMotionSpeed.Value = inputMagnitude;
﻿        }
﻿
﻿        private void JumpAndGravity()
﻿        {
﻿            if (Grounded)
﻿            {
﻿                _fallTimeoutDelta = FallTimeout;
﻿                _netAnimJump.Value = false;
﻿                _netAnimFreeFall.Value = false;
﻿
﻿                if (_verticalVelocity < 0.0f)
﻿                {
﻿                    _verticalVelocity = -2f;
﻿                }
﻿
﻿                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
﻿                {
﻿                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
﻿                    _netAnimJump.Value = true;
﻿                }
﻿
﻿                if (_jumpTimeoutDelta >= 0.0f)
﻿                {
﻿                    _jumpTimeoutDelta -= Time.deltaTime;
﻿                }
﻿            }
﻿            else
﻿            {
﻿                _jumpTimeoutDelta = JumpTimeout;
﻿                if (_fallTimeoutDelta >= 0.0f)
﻿                {
﻿                    _fallTimeoutDelta -= Time.deltaTime;
﻿                }
﻿                else
﻿                {
﻿                    _netAnimFreeFall.Value = true;
﻿                }
﻿                _input.jump = false;
﻿            }
﻿
﻿            if (_verticalVelocity < _terminalVelocity)
﻿            {
﻿                _verticalVelocity += Gravity * Time.deltaTime;
﻿            }
﻿        }
﻿
﻿        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
﻿        {
﻿            if (lfAngle < -360f) lfAngle += 360f;
﻿            if (lfAngle > 360f) lfAngle -= 360f;
﻿            return Mathf.Clamp(lfAngle, lfMin, lfMax);
﻿        }
﻿
﻿        private void OnDrawGizmosSelected()
﻿        {
﻿            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
﻿            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);
﻿
﻿            if (Grounded) Gizmos.color = transparentGreen;
﻿            else Gizmos.color = transparentRed;
﻿
﻿            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
﻿        }
﻿
﻿        private void OnFootstep(AnimationEvent animationEvent)
﻿        {
﻿            if (animationEvent.animatorClipInfo.weight > 0.5f)
﻿            {
﻿                if (FootstepAudioClips.Length > 0)
﻿                {
﻿                    var index = Random.Range(0, FootstepAudioClips.Length);
﻿                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
﻿                }
﻿            }
﻿        }
﻿
﻿        private void OnLand(AnimationEvent animationEvent)
﻿        {
﻿            if (animationEvent.animatorClipInfo.weight > 0.5f)
﻿            {
﻿                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
﻿            }
﻿        }
﻿    }
﻿}
﻿