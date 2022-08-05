using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(CapsuleCollider))]
[AddComponentMenu("Custom Character Controller")]
public class PlayerStateMachine : MonoBehaviour
{

    #region Member Variables
    private PlayerInputActions _playerInputActions;
    private PlayerStateFactory _movementStates;
    private PlayerBaseState _movementState;

    private Rigidbody _rigidbody;
    private CapsuleCollider _collider;
    private RaycastHit _slopeHit;
    private RaycastHit _shootHit;
    private Animator _animator;

    [SerializeField] private float _standingHeight = 1f;

    #region Movement Values
    [SerializeField] private LayerMask _whatIsGround;
    [SerializeField] private float _groundDrag;
    private float _currentSpeed;
    private float _currentAcceleration;

    private Vector3 _lastMovementDirection;
    private Vector2 _moveInput = new Vector2(0, 0);

    #region Jumping
    private bool _isJumpPressed;
    private bool _jumpReady;
    private bool _isJumping;

    [SerializeField] private bool _jumpEnabled;
    [SerializeField] private float _jumpHeight;
    [SerializeField] private float _airControlFactor;
    [SerializeField] private float _jumpDescentMultiplier;
    [SerializeField] private float _wallrunDescentMultiplier;
    [SerializeField] private float _jumpCooldown;
    #endregion

    #region Running
    private bool _isRunPressed;

    [SerializeField] private float _runningSpeed;
    [SerializeField] private float _runningAcceleration;
    [SerializeField] private bool _canRun;
    #endregion 

    #region Crouching
    private bool _isCrouchPressed;

    [SerializeField] private float _crouchSpeed;
    [SerializeField] private bool _canCrouch;
    [SerializeField] private float _crouchHeight = .5f;
    [SerializeField] private float _crouchAcceleration;
    #endregion

    #region Walking
    bool _isMovementPressed = false;

    [SerializeField] private float _walkingSpeed;
    [SerializeField] private float _walkingAcceleration;
    [SerializeField] private bool _canWalk;
    #endregion

    #region Grappling
    private bool _isGrapplePressed;
    private List<GameObject> _grappleLines;
    private SpringJoint joint;
    private RaycastHit _grappleHit;

    [SerializeField] private float _minGrappleLength;
    [SerializeField] private float _maxGrappleLength;
    [SerializeField] private float _grappleWidth = .1f;
    [SerializeField] private float _maxGrappleDistance;
    [SerializeField] private float _grappleSpringConst;
    [SerializeField] private float _grappleReelForce;
    [SerializeField] private float _grappleSpeed;
    [SerializeField] private Material _grappleMaterial;
    [SerializeField] private AnimationCurve _grappleWidthCurve;
    [SerializeField] private float _numberOfGrappleLines = 5;
    [SerializeField] private int _numberOfGrapplePoints = 20;
    [SerializeField] private bool _canGrapple;
    #endregion

    #region Wallrunning
    [SerializeField] private float _wallRunSpeed = 4;
    [SerializeField] private float _wallRunTime = 2;
    [SerializeField] private float _wallRunTransitionTime = .5f;
    [SerializeField] private float _maxWallRunDistance = .5f;
    [SerializeField] private float _cameraAngleClamp;
    [SerializeField] private AnimationCurve _wallrunDecayCurve;
    [SerializeField] private AnimationCurve _wallrunFallCurve;
    [SerializeField] private float _minWallrunHeight;
    [SerializeField] private LayerMask _wallRunSurface = 7;
    [SerializeField] private float _wallTiltAngle;
    [SerializeField] private float _wallTiltSpeed;
    [SerializeField] private Vector2 _wallJumpForce;
    [Range(80.0f, 0.0f)]
    [SerializeField] private float _wallrunMaxAngle = 85.0f;
    
    private int _lastWallrunCheckIndex = 0;
    private bool _transitioningToWallrun = false;
    private bool _wallrunHitFound;
    private GameObject _currentWallrunColliderID;
    private GameObject _lastWallrunColliderID;
    private RaycastHit _wallrunHit;
    #endregion

    #region Air Movement
    [SerializeField] private float _gravity;
    [SerializeField] private float _maxAirSpeed;
    #endregion

    #region Sliding
    [SerializeField] private float _slideTime;
    [SerializeField] private float _maxSlideSpeed;
    [SerializeField] private float _downSlideForce;
    [SerializeField] private float _slideSpeed;
    [SerializeField] private float _minSlideStartSpeed;
    [SerializeField] private float _slideStopSpeed;
    [SerializeField] private float _slideFOV;
    [SerializeField] private float _crouchJumpDistance;
    [SerializeField] private float _crouchJumpForce;
    [SerializeField] private AnimationCurve _slideDecayCurve;
    [SerializeField] private AnimationCurve _wallrunFallRate;

    private Vector3 slideStartVelocity;
    private float currentSlideTime;
    private bool isSliding;
    #endregion

    #endregion

    #region Camera Values
    private RaycastHit _focalPoint;
    private float _initialCameraFOV;
    private CinemachineBrain _cinemachineBrain;
    private CinemachineVirtualCamera _currentCMCamera;
    private Vector2 _viewRotVelRef;
    private bool _isZoomPressed;

    [SerializeField] private Texture2D _crosshairTexture;
    [SerializeField] private float _crosshairScale;
    [SerializeField] private float _verticalRotationRange;
    [SerializeField] private float _sensitivity;
    [SerializeField] private float _cameraWeight;
    #endregion

    #region Combat

    #region Melee
    private bool _isMeleePressed;
    private bool _meleeReady;
    private List<GameObject> _meleeHitObjects;

    [SerializeField] private float _meleeSpeed;
    [SerializeField] private float _meleeForce;
    [SerializeField] private float _meleeInfluenceRadius;
    [SerializeField] private float _meleeCooldownTimer;
    #endregion

    #endregion

    #region Animator
    int _isIdleHash = Animator.StringToHash("isIdle");
    int _isWalkingHash = Animator.StringToHash("isWalking");
    int _isRunningHash = Animator.StringToHash("isRunning");
    int _isSlidingHash = Animator.StringToHash("isSliding");
    int _isJumpingHash = Animator.StringToHash("isJumping");
    int _isFallingHash = Animator.StringToHash("isFalling");
    int _isWallRunningHash = Animator.StringToHash("isWallRunning");
    int _resetStateHash = Animator.StringToHash("stateReset");

    #endregion

    #region Getters and Setters
    public PlayerBaseState CurrentMovementState { get { return _movementState; } set { _movementState = value; } }
    public bool IsJumpPressed { get { return _isJumpPressed; } }
    public bool IsCrouchPressed { get { return _isCrouchPressed; } }
    public bool IsRunPressed { get { return _isRunPressed; } }
    public bool IsMovementPressed { get { return _isMovementPressed; } }
    public bool IsMeleePressed { get { return _isMeleePressed; } }
    public bool IsGrounded { get { return Physics.Raycast(transform.position, Vector3.down, Collider.height * .5f + .3f); } }
    public bool IsZoomPressed { get { return _isZoomPressed; } }
    public bool IsGrapplePressed { get { return _isGrapplePressed; } }
    public float RunningSpeed { get => _runningSpeed;  }
    public float CurrentSpeed { get => _currentSpeed; set => _currentSpeed = value; }
    public bool JumpEnabled { get => _jumpEnabled;  }
    public Rigidbody Rigidbody { get => _rigidbody; }
    public float JumpHeight { get => _jumpHeight;}
    public float Gravity { get => _gravity;}
    public float WalkingSpeed { get => _walkingSpeed;}
    public float CrouchSpeed { get => _crouchSpeed; }
    public CapsuleCollider Collider { get => _collider; set => _collider = value; }
    public float CrouchHeight { get => _crouchHeight; }
    public CinemachineVirtualCamera CurrentCMCamera { get => _currentCMCamera; set => _currentCMCamera = value; }
    public float StandingHeight { get => _standingHeight;  }
    public Vector3 LastMovementDirection { get => _lastMovementDirection;  }
    public float WalkingAcceleration { get => _walkingAcceleration; }
    public float RunningAcceleration { get => _runningAcceleration; }
    public float CurrentAcceleration { get => _currentAcceleration; set => _currentAcceleration = value; }
    public float AirControlFactor { get => _airControlFactor; }
    public float CrouchAcceleration { get => _crouchAcceleration; }
    public float SlideSpeed { get => _slideSpeed; }
    public float MaxSlideTime { get => _slideTime; }
    public AnimationCurve SlideDecayCurve { get => _slideDecayCurve; }
    public float MaxWallRunTime { get => _wallRunTime; }
    public float WallRunSpeed { get => _wallRunSpeed;  }
    public RaycastHit WallHit { get => _wallrunHit; }
    public AnimationCurve WallRunDecayCurve { get => _wallrunDecayCurve;  }
    public AnimationCurve WallRunFallCurve { get => _wallrunFallCurve;  }
    public float MaxWallRunDistance { get => _maxWallRunDistance;  }
    public bool WallrunHitFound { get => _wallrunHitFound; }
    public float WallRunMaxAngle { get => _wallrunMaxAngle; }
    public CinemachineBrain CinemachineBrain { get => _cinemachineBrain; set => _cinemachineBrain = value; }
    public RaycastHit GrappleHit { get => _grappleHit; }
    public float GrappleSpringConst { get => _grappleSpringConst; }
    public Material GrappleMaterial { get => _grappleMaterial; }
    public float GrappleSpeed { get => _grappleSpeed; }
    public float WallTiltAngle { get => _wallTiltAngle; }
    public float WallTiltSpeed { get => _wallTiltSpeed;  }
    public float MeleeForce { get => _meleeForce;  }
    public float MeleeInfluenceRadius { get => _meleeInfluenceRadius;  }
    public float MaxGrappleLength { get => _maxGrappleLength; }
    public float MinGrappleLength { get => _minGrappleLength; }
    public Animator Animator { get => _animator; }
    public int IsIdleHash { get => _isIdleHash; }
    public int IsWalkingHash { get => _isWalkingHash;  }
    public int IsRunningHash { get => _isRunningHash;  }
    public int IsSlidingHash { get => _isSlidingHash; }
    public int IsJumpingHash { get => _isJumpingHash; }
    public int IsFallingHash { get => _isFallingHash;  }
    public int IsWallRunningHash { get => _isWallRunningHash;  }
    public int ResetStateHash { get => _resetStateHash; }
    public float MeleeSpeed { get => _meleeSpeed; }
    #endregion
    #endregion

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _movementStates = new PlayerStateFactory(this);
        _movementState = _movementStates.Grounded();
        _movementState.EnterState();
        _playerInputActions = new PlayerInputActions();

        if (_playerInputActions == null)
            _playerInputActions = new PlayerInputActions();

        _playerInputActions.Player.Enable();

        _playerInputActions.Player.Jump.started += OnJump;
        _playerInputActions.Player.Jump.canceled += OnJump;

        _playerInputActions.Player.Crouch.started += OnCrouch;
        _playerInputActions.Player.Crouch.canceled += OnCrouch;

        _playerInputActions.Player.Run.started += OnRun;
        _playerInputActions.Player.Run.canceled += OnRun;

        _playerInputActions.Player.Movement.started += OnMovement;
        _playerInputActions.Player.Movement.canceled += OnMovement;

        _playerInputActions.Player.Melee.started += OnMelee;
        _playerInputActions.Player.Melee.canceled += OnMelee;

        _playerInputActions.Player.Zoom.started += OnZoom;
        _playerInputActions.Player.Zoom.canceled += OnZoom;

        _playerInputActions.Player.Look.started += OnLook;
        _playerInputActions.Player.Look.canceled += OnLook;

        _playerInputActions.Player.Grapple.started += OnGrapple;
        _playerInputActions.Player.Grapple.canceled += OnGrapple;

    }

    private void OnDisable()
    {
        _playerInputActions.Player.Jump.started -= OnJump;
        _playerInputActions.Player.Jump.canceled -= OnJump;

        _playerInputActions.Player.Crouch.started -= OnCrouch;
        _playerInputActions.Player.Crouch.canceled -= OnCrouch;

        _playerInputActions.Player.Run.started -= OnRun;
        _playerInputActions.Player.Run.canceled -= OnRun;

        _playerInputActions.Player.Movement.started -= OnMovement;
        _playerInputActions.Player.Movement.canceled -= OnMovement;

        _playerInputActions.Player.Melee.started -= OnMelee;
        _playerInputActions.Player.Melee.canceled -= OnMelee;

        _playerInputActions.Player.Zoom.started -= OnZoom;
        _playerInputActions.Player.Zoom.canceled -= OnZoom;

        _playerInputActions.Player.Look.started -= OnLook;
        _playerInputActions.Player.Look.canceled -= OnLook;

        _playerInputActions.Player.Grapple.started -= OnGrapple;
        _playerInputActions.Player.Grapple.canceled -= OnGrapple;

        _playerInputActions.Player.Disable();
        _playerInputActions.Disable();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Camera.main.gameObject.TryGetComponent<CinemachineBrain>(out _cinemachineBrain);
        if(CinemachineBrain == null) CinemachineBrain = Camera.main.gameObject.AddComponent<CinemachineBrain>();
        _playerInputActions = new PlayerInputActions();
        _playerInputActions.Player.Enable();
        _rigidbody = GetComponent<Rigidbody>();
        Collider = GetComponent<CapsuleCollider>();
        Collider.height = StandingHeight;
        _jumpReady = true;
        _meleeReady = true;
        //Rigidbody.useGravity = false;
        _grappleLines = new List<GameObject>();
    }

    private void OnGUI()
    {
        if(Time.timeScale != 0 && _crosshairTexture != null)
        {
            GUI.DrawTexture(new Rect((Screen.width - _crosshairTexture.width * _crosshairScale) / 2, (Screen.height - _crosshairTexture.height * _crosshairScale) / 2, _crosshairTexture.width * _crosshairScale, _crosshairTexture.height * _crosshairScale), _crosshairTexture);
        }
    }

    private void Update()
    {
        //Update Movement
        _moveInput = _playerInputActions.Player.Movement.ReadValue<Vector2>();
        Vector3 flatForward = CinemachineBrain.gameObject.transform.forward;
        flatForward.y = 0;
        flatForward.Normalize();
        _lastMovementDirection = Vector3.ClampMagnitude(flatForward * _moveInput.y + CinemachineBrain.gameObject.transform.right * _moveInput.x, 1);
        _wallrunHitFound = CanWallRun();
        _movementState.UpdateStates();

        CurrentCMCamera = CinemachineBrain.ActiveVirtualCamera as CinemachineVirtualCamera;
    }

    public bool CrouchObstructedAbove()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position, Vector3.up, out hit, StandingHeight - CrouchHeight / 2.0f);
        return hit.collider != null;
    }

    public bool CanWallRun()
    {
        Physics.Raycast(transform.position, Vector3.down, out _wallrunHit, _minWallrunHeight, _wallRunSurface);

        if (_wallrunHit.collider != null)
        {
            return false;
        }

        Vector3[] wallRunCheckDirs = new Vector3[]
        {
            -transform.right,
            transform.right,
            (transform.right + transform.forward).normalized,
            (transform.right - transform.forward).normalized,
            (-transform.right + transform.forward).normalized,
            (-transform.right - transform.forward).normalized,
            -transform.forward
        };

        foreach (Vector3 dir in wallRunCheckDirs)
        {
            if (Physics.Raycast(transform.position, dir, out _wallrunHit, MaxWallRunDistance, _wallRunSurface))
            {
                Debug.DrawRay(transform.position, dir * MaxWallRunDistance, Color.red);
                _currentWallrunColliderID = _wallrunHit.collider.gameObject;
                return true;
            }
            else
            {
                Debug.DrawRay(transform.position, dir * MaxWallRunDistance, Color.green);
            }
        }

        return false;
    }

    #region Input Functions
    void OnJump(InputAction.CallbackContext ctx)
    {
        _isJumpPressed = ctx.ReadValueAsButton();
    }

    void OnCrouch(InputAction.CallbackContext ctx)
    {
        _isCrouchPressed = ctx.ReadValueAsButton();
    }

    void OnRun(InputAction.CallbackContext ctx)
    {
        _isRunPressed = ctx.ReadValueAsButton();
    }

    void OnMovement(InputAction.CallbackContext ctx)
    {
        _isMovementPressed = ctx.started ? true : false;
    }

    void OnMelee(InputAction.CallbackContext ctx)
    {
        _isMeleePressed = ctx.ReadValueAsButton();
    }

    void OnZoom(InputAction.CallbackContext ctx)
    {
        _isZoomPressed = ctx.ReadValueAsButton();
    }

    void OnLook(InputAction.CallbackContext ctx)
    {
        Vector2 mouseDelta = ctx.ReadValue<Vector2>();
        RotateView(mouseDelta, _sensitivity);
    }

    void OnGrapple(InputAction.CallbackContext ctx)
    {
        _isGrapplePressed = ctx.ReadValueAsButton();
        Physics.Raycast(_cinemachineBrain.transform.position, _cinemachineBrain.transform.forward, out _grappleHit, _maxGrappleDistance);
    }
    #endregion

    public void DestroyComponent(Object o)
    {
        Destroy(o);
    }

    public Coroutine StartExternalCoroutine(IEnumerator method)
    {
        return StartCoroutine(method);
    }

    #region Camera Functions
    public void RotateView(Vector2 yawPitch, float sensitivity)
    {
        (yawPitch.x, yawPitch.y) = (yawPitch.y, yawPitch.x);

        Vector2 targetAngles = ((Vector2.right * _currentCMCamera.transform.localEulerAngles.x) + (Vector2.up * transform.localEulerAngles.y));
        targetAngles = Vector2.SmoothDamp(targetAngles, targetAngles + (yawPitch * (sensitivity * Mathf.Pow(_cameraWeight, 2))), ref _viewRotVelRef, (Mathf.Pow(_cameraWeight, 2)) * Time.deltaTime);
        targetAngles.x += targetAngles.x > 180 ? -360 : targetAngles.x < -180 ? 360 : 0;
        targetAngles.x = Mathf.Clamp(targetAngles.x, -0.5f * _verticalRotationRange, 0.5f * _verticalRotationRange);


        //cameraHolder.transform.localEulerAngles = (Vector3.right * targetAngles.x) + Vector3.forward;

        transform.localEulerAngles = Vector3.up * targetAngles.y;

        //limit camera if wall running
        //if (isWallRunning)
        //    transform.forward = ClampCamera2(transform.forward);
    }
    #endregion
}
