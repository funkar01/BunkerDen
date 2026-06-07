using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
	[RequireComponent(typeof(PlayerInput))]
	public class FirstPersonController : MonoBehaviour
	{
		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 4.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 6.0f;
		[Tooltip("Rotation speed of the character")]
		public float RotationSpeed = 1.0f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float JumpHeight = 1.2f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.1f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;
		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.5f;
		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -90.0f;

		[Header("Mouse Movement Controls")]
		[Tooltip("Enable mouse cursor-based rotation and click-to-walk/run")]
		public bool UseMouseMovementControl = true;
		[Tooltip("Deadzone around screen center for cursor rotation")]
		public float MouseRotationDeadzone = 0.1f;
		[Tooltip("Sensitivity multiplier for cursor-based rotation")]
		public float MouseRotationSensitivity = 150f;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

		private bool _movingByMouseClick;

		[Header("Footstep Sounds")]
		[Tooltip("Audio clip for player footsteps")]
		public AudioClip FootstepAudioClip;
		[Tooltip("Volume of footstep sounds")]
		[Range(0f, 1f)] public float FootstepVolume = 0.4f;
		[Tooltip("Pitch (speed) multiplier when walking")]
		public float FootstepWalkPitch = 1.0f;
		[Tooltip("Pitch (speed) multiplier when running")]
		public float FootstepRunPitch = 1.35f;

		private AudioSource _footstepAudioSource;

	
		private PlayerInput _playerInput;
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;

		private const float _threshold = 0.01f;

		private bool IsCurrentDeviceMouse
		{
			get
			{
				return _playerInput.currentControlScheme == "KeyboardMouse";
			}
		}

		private void Awake()
		{
			// Force UseMouseMovementControl to true programmatically to prevent Unity serialization issues
			UseMouseMovementControl = true;

			// get a reference to our main camera
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
		}

		private void Start()
		{
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
			_playerInput = GetComponent<PlayerInput>();

			// reset our timeouts on start
			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;

#if UNITY_EDITOR
			// Auto-load footstep clip if not assigned in the inspector
			if (FootstepAudioClip == null)
			{
				string path = "Assets/Assets/Audios/sumaga123-shoe-steps-447693.mp3";
				FootstepAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
				if (FootstepAudioClip != null)
				{
					Debug.Log("[BunkerDen] Loaded footstep audio clip automatically.");
				}
			}
#endif

			// Initialize AudioSource for loop footsteps
			_footstepAudioSource = GetComponent<AudioSource>();
			if (_footstepAudioSource == null)
			{
				_footstepAudioSource = gameObject.AddComponent<AudioSource>();
			}

			if (FootstepAudioClip != null)
			{
				_footstepAudioSource.clip = FootstepAudioClip;
				_footstepAudioSource.loop = true;
				_footstepAudioSource.playOnAwake = false;
				_footstepAudioSource.volume = FootstepVolume;
				_footstepAudioSource.spatialBlend = 0f; // 2D sound for clear headset output
			}
		}

		private void Update()
		{
			HandleMouseMovementInputs();
			JumpAndGravity();
			GroundedCheck();
			Move();
			UpdateFootsteps();
		}

		private void LateUpdate()
		{
			CameraRotation();
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		private void HandleMouseMovementInputs()
		{
			if (!UseMouseMovementControl) return;

			bool leftClick = false;
			bool rightClick = false;

			if (Mouse.current != null)
			{
				leftClick = Mouse.current.leftButton.isPressed;
				rightClick = Mouse.current.rightButton.isPressed;
			}
			else
			{
				leftClick = Input.GetMouseButton(0);
				rightClick = Input.GetMouseButton(1);
			}

			if (leftClick)
			{
				_input.move = new Vector2(0f, 1f); // Walk forward
				_input.sprint = false;
				_movingByMouseClick = true;
				Debug.Log("[BunkerDen] Left Click Held - Walking Forward");
			}
			else if (rightClick)
			{
				_input.move = new Vector2(0f, 1f); // Run forward
				_input.sprint = true;
				_movingByMouseClick = true;
				Debug.Log("[BunkerDen] Right Click Held - Running Forward");
			}
			else
			{
				if (_movingByMouseClick)
				{
					_input.move = Vector2.zero;
					_input.sprint = false;
					_movingByMouseClick = false;
					Debug.Log("[BunkerDen] Clicks Released - Stopping Movement");
				}
			}
		}

		private void UpdateFootsteps()
		{
			if (_footstepAudioSource == null || FootstepAudioClip == null) return;

			// Check if player is grounded and actually moving on the ground
			Vector3 horizontalVelocity = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z);
			if (Grounded && horizontalVelocity.magnitude > 0.1f)
			{
				// Set pitch based on walking vs running
				_footstepAudioSource.pitch = _input.sprint ? FootstepRunPitch : FootstepWalkPitch;
				_footstepAudioSource.volume = FootstepVolume; // Update volume in case it was adjusted in inspector

				// If not playing, play/unpause
				if (!_footstepAudioSource.isPlaying)
				{
					_footstepAudioSource.UnPause();
					// If it wasn't playing or paused before, let's call Play
					if (!_footstepAudioSource.isPlaying)
					{
						_footstepAudioSource.Play();
					}
					Debug.Log($"[BunkerDen] Footsteps Resumed (pitch={_footstepAudioSource.pitch})");
				}
			}
			else
			{
				// If playing, pause immediately
				if (_footstepAudioSource.isPlaying)
				{
					_footstepAudioSource.Pause();
					Debug.Log("[BunkerDen] Footsteps Paused");
				}
			}
		}

		private void CameraRotation()
		{
			// if there is an input
			if (_input.look.sqrMagnitude >= _threshold)
			{
				//Don't multiply mouse input by Time.deltaTime
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
				
				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

				// clamp our pitch rotation
				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

				// Update Cinemachine camera target pitch
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

				// rotate the player left and right
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}

		private void Move()
		{
			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// creates curved result rather than a linear one giving a more organic speed change
				// note T in Lerp is clamped, so we don't need to clamp our speed
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

				// round speed to 3 decimal places
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			// normalise input direction
			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input.move != Vector2.zero)
			{
				// move
				inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
			}

			// move the player
			_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
		}

		private void JumpAndGravity()
		{
			if (Grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = FallTimeout;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}

				// Jump
				if (_input.jump && _jumpTimeoutDelta <= 0.0f)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = JumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}

				// if we are not grounded, do not jump
				_input.jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += Gravity * Time.deltaTime;
			}
		}

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}
	}
}
