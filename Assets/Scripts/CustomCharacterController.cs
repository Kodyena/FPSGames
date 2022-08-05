using DG.Tweening;
using DG.Tweening.Core;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;
using Cinemachine;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(CapsuleCollider))]
[AddComponentMenu("Custom Character Controller")]
public class CustomCharacterController : MonoBehaviour
{

    #region Variables 
    private enum MovementState { Walking, Running, WallRunning, Jumping, Crouching, Grappling, Sliding };
    private enum StanceState { Standing, Crouching };

    private PlayerInput m_playerInput;
    private PlayerInputActions m_playerInputActions;

    #region Camera
    public enum MouseInversionMode { None, X, Y, Both };
    public MouseInversionMode mouseInversion = MouseInversionMode.None;
    public bool enableCameraControl;
    public GameObject cameraHolder;
    public Sprite crosshair;

    public float sensitivity = 8;
    public float rotationWeight;
    public float verticalAngleRange;
    public float standingHeight;
    public float crouchingHeight;
    public float verticalRotationRange = 170f;
    public float walkFOV;
    public float runFOV;
    public float zoomFOVMultiplier;
    public float zoomTime;
    public float fovTransitionSpeed;

    #region HeadBob

    public bool enableHeadbob = true;
    public float headbobSpeed;
    public float headbobPower;
    public float ZTilt;

    DG.Tweening.Sequence headbobTween;

    float yBob;
    Quaternion headbobCameraRotation = Quaternion.identity;

    #endregion

    //Internal
    Vector2 MouseXY;
    Vector2 viewRotVelRef;
    RaycastHit focalPoint;
    float initialCameraFOV;
    private CinemachineBrain cinemachineBrain;
    private CinemachineVirtualCamera currentCMCamera;

    #endregion

    #region Movement

    public float walkingSpeed, runningSpeed, crouchingSpeed;
    public bool canRun = true, canWallRun = true, canCrouch = true, canJump = true, canGrapple = true;
    public LayerMask whatIsGround;
    public float groundDrag;
    public float gravity;
    public float maxAirSpeed;


    //Jumping
    public float jumpHeight;
    public float airControlFactor;
    public float jumpDescentMultiplier;
    public float wallrunDescentMultiplier;
    public float jumpCooldown;
    bool jumpReady;
    bool isJumping;

    //Grappling
    public float minGrappleLength;
    public float maxGrappleLength;
    public float grappleWidth = .1f;
    public float grappleSpringConst;
    public float grappleReelForce;
    public float grappleSpeed;
    public Material grappleMaterial;
    public AnimationCurve grappleWidthCurve;
    SpringJoint joint;

    float numberOfGrappleLines = 5;
    int numberOfGrapplePoints = 20;
    List<GameObject> grappleLines;

    //Wall Running 
    public float wallRunSpeed = 4;
    public float wallRunTime = 2;
    public float wallRunTransitionTime = .5f;
    public float maxWallRunDistance = .5f;
    public float cameraAngleClamp;
    public AnimationCurve wallrunDecayCurve;
    public float minWallrunHeight;
    public LayerMask wallRunSurface = 7;
    public float tiltAngle;
    public float tiltSpeed;
    public Vector2 wallJumpForce;
    [Range(80.0f, 0.0f)]
    public float wallrunMaxAngle = 90.0f;
    int lastWallrunCheckIndex = 0;
    float currentWallrunLength;
    bool transitioningToWallrun = false;
    bool wallrunReady;
    bool isWallRunning;
    GameObject currentWallrunColliderID;
    GameObject lastWallrunColliderID;
    RaycastHit wallrunHit;


    //Sliding
    public float slideTime;
    public float maxSlideSpeed;
    public float downSlideForce;
    public float slideSpeed;
    public float minSlideStartSpeed;
    public float slideStopSpeed;
    public float slideFOV;
    public float crouchJumpDistance;
    public float crouchJumpForce;
    public AnimationCurve slideDecayCurve;
    public AnimationCurve wallrunFallRate;

    Vector3 slideStartVelocity;
    float currentSlideTime;
    bool isSliding;

    //Melee
    public float forwardMeleeMomentum;
    public float meleeForce;
    public float meleeInfluenceRadius;
    public float meleeCooldownTime;

    private bool meleeReady;

    //KeyCodes
    public KeyCode grappleKey = KeyCode.Mouse0;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    //Slopes
    public float slopeLimit;
    public float slopeSpeedMultiplier;
    public float maxStairRise;
    public float stepSpeed;

    float currentSlopeAngle;

    //Internal
    bool isGrounded;
    MovementState movementState;
    StanceState stanceState;
    float horizontalInput, verticalInput;
    float currentSpeed;
    Rigidbody p_Rigidbody;
    CapsuleCollider capsule;
    RaycastHit slopeHit;
    RaycastHit shootHit;
    Vector2 lastMoveInput;
    Vector2 movInputSmooth;
    bool jumpInput, jumpInput_Instant;
    bool crouchInput, crouchInput_Instant;
    bool runInput, runInput_Instant;
    bool isCrouching, isRunning, isGrappling, isWalking;
    bool grappleInput, grappleInput_Instant;
    Vector3 lastMoveDir;

    //Debugging 
    public bool wallrunDebug;

    #endregion

    #endregion

    public Texture2D _crosshairTex;
    public float _crosshairScale;

    void OnGUI()
    {
        if (Time.timeScale != 0 && _crosshairTex != null)
        {
            GUI.DrawTexture(new Rect((Screen.width - _crosshairTex.width * _crosshairScale) / 2, (Screen.height - _crosshairTex.height * _crosshairScale) / 2, _crosshairTex.width * _crosshairScale, _crosshairTex.height * _crosshairScale), _crosshairTex);
        }
    }

    private void Awake()
    {
        m_playerInputActions = new PlayerInputActions();
        m_playerInputActions.Player.Enable();

        m_playerInputActions.Player.Jump.started += OnJump;

        m_playerInputActions.Player.Look.performed += OnLook;
        m_playerInputActions.Player.Look.canceled += OnLook;

        m_playerInputActions.Player.Crouch.started += OnCrouch;
        m_playerInputActions.Player.Crouch.canceled += OnCrouch;

        m_playerInputActions.Player.Movement.performed += OnMovement;
        m_playerInputActions.Player.Movement.canceled += OnMovement;

        m_playerInputActions.Player.Melee.started += OnMelee;

        m_playerInputActions.Player.Zoom.started += OnZoom;
        m_playerInputActions.Player.Zoom.canceled += OnZoom;

        //m_playerInputActions.Player.Grapple.started += OnGrapple;



    }

    private void OnDisable()
    {
        m_playerInputActions.Player.Jump.started -= OnJump;

        m_playerInputActions.Player.Look.performed -= OnLook;
        m_playerInputActions.Player.Look.canceled -= OnLook;

        m_playerInputActions.Player.Crouch.started -= OnCrouch;
        m_playerInputActions.Player.Crouch.canceled -= OnCrouch;

        m_playerInputActions.Player.Movement.performed -= OnMovement;
        m_playerInputActions.Player.Movement.canceled -= OnMovement;

        m_playerInputActions.Player.Zoom.started -= OnZoom;
        m_playerInputActions.Player.Zoom.canceled -= OnZoom;

        m_playerInputActions.Player.Melee.started -= OnMelee;


        m_playerInputActions.Player.Disable();
    }

    // Start is called before the first frame update
    void Start()
    {
        Camera.main.gameObject.TryGetComponent<CinemachineBrain>(out cinemachineBrain);
        if (cinemachineBrain == null) cinemachineBrain = Camera.main.gameObject.AddComponent<CinemachineBrain>();
        currentCMCamera = cinemachineBrain.ActiveVirtualCamera as CinemachineVirtualCamera;
        m_playerInputActions.Player.Enable();
        p_Rigidbody = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        Cursor.lockState = CursorLockMode.Locked;
        movementState = MovementState.Walking;
        stanceState = StanceState.Standing;
        capsule.height = standingHeight;
        currentSpeed = walkingSpeed;
        isJumping = false;
        jumpReady = true;
        meleeReady = true;
        wallrunReady = true;
        p_Rigidbody.useGravity = false;
        grappleLines = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        currentCMCamera = cinemachineBrain.ActiveVirtualCamera as CinemachineVirtualCamera;
        grappleInput = m_playerInputActions.Player.Grapple.enabled;
        grappleInput_Instant = Input.GetKeyDown(grappleKey);
        runInput = Input.GetKey(runKey);
        runInput_Instant = Input.GetKeyDown(runKey);

        isGrounded = Physics.Raycast(transform.position, Vector3.down, capsule.height * .5f + .2f);

        if (isGrounded)
        {
            lastWallrunColliderID = null;
            p_Rigidbody.drag = groundDrag;

            if (isJumping)
            {
                //TODO Play ground animation

                isJumping = false;
            }
        }
        else
        {
            p_Rigidbody.drag = 0;
        }


        lastMoveInput = m_playerInputActions.Player.Movement.ReadValue<Vector2>();
        Vector3 flatForward = cinemachineBrain.gameObject.transform.forward;
        flatForward.y = 0;
        flatForward.Normalize();
        lastMoveDir = Vector3.ClampMagnitude(flatForward * lastMoveInput.y + cinemachineBrain.gameObject.transform.right * lastMoveInput.x, 1);

        MovePlayer();

        if (enableHeadbob && lastMoveInput.magnitude > 0 && isGrounded)
        {
            //TODO Change headbob to work with cinemachine
            //ApplyHeadBob();
        }

        HandleMovementState();
        Vector3 camPos = cameraHolder.transform.localPosition;

        camPos.y = Mathf.Lerp(camPos.y, 0, Time.deltaTime);
        cameraHolder.transform.localPosition = camPos;
    }

    private void FixedUpdate()
    {

        //apply gravity
        if (CheckSlope())
        {
            p_Rigidbody.AddForce(-slopeHit.normal * gravity);
        }
        else
        {
            p_Rigidbody.AddForce(Vector3.down * gravity);
        }

    }

    #region Camera Functions

    public void ApplyHeadBob()
    {
        //Get focal point
        Physics.Raycast(currentCMCamera.transform.position, currentCMCamera.transform.forward, out focalPoint, Mathf.Infinity, ~LayerMask.GetMask("Player"));

        Vector3 camPos = cameraHolder.transform.localPosition;

        camPos.y -= yBob;
        yBob = headbobPower * Mathf.Sin(headbobSpeed * Time.time * Mathf.PI * 2);

        camPos.y += yBob;
        cameraHolder.transform.localPosition = camPos;

        if (focalPoint.collider != null)
        {
            //Look at focal point
            currentCMCamera.transform.LookAt(focalPoint.point);
        }
        else
        {
            Vector3 horizonPoint = transform.position + currentCMCamera.transform.forward * 1000;
            horizonPoint.y -= yBob;

            //Look at horizon
            currentCMCamera.transform.LookAt(horizonPoint);
        }
    }

    public void RotateView(Vector2 yawPitch, float sensitivity, float cameraWeight)
    {
        (yawPitch.x, yawPitch.y) = (yawPitch.y, yawPitch.x);

        Vector2 targetAngles = ((Vector2.right * cameraHolder.transform.localEulerAngles.x) + (Vector2.up * transform.localEulerAngles.y));
        targetAngles = Vector2.SmoothDamp(targetAngles, targetAngles + (yawPitch * (sensitivity * Mathf.Pow(cameraWeight, 2))), ref viewRotVelRef, (Mathf.Pow(cameraWeight, 2)) * Time.deltaTime);
        targetAngles.x += targetAngles.x > 180 ? -360 : targetAngles.x < -180 ? 360 : 0;
        targetAngles.x = Mathf.Clamp(targetAngles.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);


        //cameraHolder.transform.localEulerAngles = (Vector3.right * targetAngles.x) + Vector3.forward;

        transform.localEulerAngles = Vector3.up * targetAngles.y;

        //limit camera if wall running
        if (isWallRunning)
            transform.forward = ClampCamera2(transform.forward);
    }

    public Vector3 ClampCamera2(Vector3 cameraDir)
    {
        Vector3 normalDir = wallrunHit.normal;
        Vector3 tangentDir = Vector3.Cross(Vector3.up, normalDir);
        tangentDir *= Vector3.Dot(tangentDir, cameraDir) > 0 ? 1.0f : -1.0f;

        //Rotate normal toward tangent for desired sight cone
        normalDir = Quaternion.AngleAxis(90 - wallrunMaxAngle, Vector3.Cross(normalDir, tangentDir)) * normalDir;

        //check rotations from norm to cam to tan are same and from tan to cam to norm are same
        if (Vector3.Dot(Vector3.Cross(tangentDir, cameraDir), Vector3.Cross(tangentDir, normalDir)) < 0 ||
            Vector3.Dot(Vector3.Cross(normalDir, cameraDir), Vector3.Cross(normalDir, tangentDir)) < 0)
        {
            cameraDir = (Vector3.AngleBetween(tangentDir, cameraDir) < Vector3.AngleBetween(normalDir, cameraDir) ? tangentDir : normalDir);
        }

        return cameraDir;
    }

    public void ClampCamera(ref float cameraAngle)
    {
        //TODO cleanup code
        if (cameraAngle < 271)
        {
            Debug.Log("change ");
        }
        Vector3 wallrunDir = Vector3.Cross(Vector3.up, wallrunHit.normal).normalized;
        wallrunDir *= (Vector3.Dot(wallrunDir, transform.forward) > 0) ? 1.0f : -1.0f;
        float tangentAngle = Vector3.SignedAngle(Vector3.forward, wallrunDir, Vector3.up);
        float normalAngle = Vector3.SignedAngle(Vector3.forward, wallrunHit.normal, Vector3.up);

        if (cameraAngle <= 0) cameraAngle += 360;
        if (tangentAngle <= 0) tangentAngle += 360;
        if (normalAngle <= 0) normalAngle += 360;

        normalAngle += tangentAngle - normalAngle > 0 ? 90 - wallrunMaxAngle : wallrunMaxAngle - 90;
        float minAngle = Mathf.Min(tangentAngle, normalAngle);
        float maxAngle = Mathf.Max(tangentAngle, normalAngle);

        bool checkAcrossZero = maxAngle - minAngle > 180;



        if (wallrunDebug) Debug.Log("tangent angle: " + tangentAngle);
        if (wallrunDebug) Debug.Log("normal angle: " + normalAngle);
        if (wallrunDebug) Debug.Log("camear angle" + cameraAngle);

        if ((!checkAcrossZero && !(cameraAngle - minAngle > 0 && maxAngle - cameraAngle < 0)) || (checkAcrossZero && !(cameraAngle > maxAngle || cameraAngle < minAngle)))
        {
            float minAngleDiff = Mathf.Abs(cameraAngle - minAngle);
            float maxAngleDiff = Mathf.Abs(cameraAngle - maxAngle);

            if (minAngleDiff > 180) minAngleDiff = Mathf.Abs(minAngleDiff - 360);
            if (maxAngleDiff > 180) maxAngleDiff = Mathf.Abs(maxAngleDiff - 360);

            cameraAngle = (minAngleDiff < maxAngleDiff) ? minAngle : maxAngle;
        }
    }

    #endregion

    #region Movement Functions
    private void HandleMovementState()
    {

        //Check Above for Blocks
        if (isCrouching && movementState != MovementState.Crouching)
        {
            RaycastHit hit;
            Physics.Raycast(transform.position, Vector3.up, out hit, standingHeight - crouchingHeight / 2.0f);
            if (hit.collider != null)
            {
                movementState = MovementState.Crouching;
                HandleMovementState();
                return;
            }
        }

        switch (movementState)
        {
            #region WalkingState
            case MovementState.Walking:

                if (!isWalking)
                {
                    isCrouching = false;
                    isRunning = false;
                    isGrappling = false;
                    isWalking = true;
                    isWallRunning = false;

                    currentSpeed = walkingSpeed;
                    Camera.main.DOFieldOfView(walkFOV, fovTransitionSpeed);

                    ApplyStance(StanceState.Standing);
                }

                if (canCrouch && crouchInput && isGrounded)
                {
                    movementState = MovementState.Crouching;
                    ApplyStance(StanceState.Crouching);
                }

                if (canRun && runInput)
                {
                    movementState = MovementState.Running;
                }

                if (grappleInput_Instant)
                {
                    movementState = MovementState.Grappling;
                }

                break;
            #endregion
            #region RunningState
            case MovementState.Running:

                if (!isRunning)
                {
                    isCrouching = false;
                    isRunning = true;
                    isGrappling = false;
                    isWalking = false;
                    isWallRunning = false;

                    currentSpeed = runningSpeed;
                    Camera.main.DOFieldOfView(runFOV, fovTransitionSpeed);

                    ApplyStance(StanceState.Standing);
                }

                if (canCrouch && crouchInput && isGrounded)
                {
                    ApplyStance(StanceState.Crouching);

                    if (p_Rigidbody.velocity.magnitude > minSlideStartSpeed)
                    {
                        movementState = MovementState.Sliding;
                    }
                    else
                    {
                        movementState = MovementState.Crouching;
                    }
                }

                if (!isWallRunning && WallRunCheck() && wallrunReady && wallrunHit.collider.gameObject != lastWallrunColliderID)
                {
                    movementState = MovementState.WallRunning;
                }

                if (grappleInput_Instant)
                {
                    movementState = MovementState.Grappling;
                }

                if (!runInput)
                {
                    movementState = MovementState.Walking;
                }


                break;
            #endregion
            #region CrouchingState
            case MovementState.Crouching:

                if (!isCrouching)
                {
                    isCrouching = true;
                    isRunning = false;
                    isGrappling = false;
                    isWalking = false;
                    isWallRunning = false;

                    currentSpeed = crouchingSpeed;
                    Camera.main.DOFieldOfView(walkFOV, fovTransitionSpeed);

                    ApplyStance(StanceState.Crouching);
                }

                if (canRun && runInput && isGrounded && !crouchInput)
                {
                    movementState = MovementState.Running;
                    ApplyStance(StanceState.Standing);
                }

                if (!crouchInput)
                {
                    ApplyStance(StanceState.Standing);
                    movementState = MovementState.Walking;
                }

                break;
            #endregion
            #region GrapplingState
            case MovementState.Grappling:

                if (!isGrappling)
                {
                    if (stanceState != StanceState.Standing)
                    {
                        ApplyStance(StanceState.Standing);
                    }

                    isCrouching = false;
                    isRunning = false;
                    isGrappling = true;
                    isWalking = false;
                    isWallRunning = false;
                    lastWallrunColliderID = null;
                    currentSpeed = grappleSpeed;

                    StartGrapple();
                }

                RaycastHit hit;
                Physics.Raycast(currentCMCamera.transform.position, shootHit.point - transform.position, out hit, Mathf.Infinity);

                if (!grappleInput || (hit.collider != shootHit.collider && shootHit.collider != null && hit.collider != null))
                {
                    Destroy(joint);

                    foreach (GameObject grappleLine in grappleLines)
                    {
                        Destroy(grappleLine);
                    }

                    grappleLines.Clear();

                    Debug.Log("was colliding with: " + shootHit.collider + " now colliding with: " + hit.collider);
                    if (runInput)
                    {
                        movementState = MovementState.Running;
                    }
                    else
                    {
                        movementState = MovementState.Walking;
                    }

                    isGrappling = false;
                }

                else
                {
                    if (Input.GetKey(KeyCode.Mouse1) && joint)
                    {
                        p_Rigidbody.velocity = Vector3.zero;
                        p_Rigidbody.AddForce((shootHit.point - transform.position).normalized * grappleReelForce, ForceMode.Force);
                        joint.maxDistance = Vector3.Distance(transform.position, shootHit.point);
                    }

                    DrawGrappleLine();
                }

                break;
            #endregion
            #region WallrunningState
            case MovementState.WallRunning:
                Vector3 wallRunRotation = new Vector3(0f, 0f, tiltAngle);
                if (!isWallRunning)
                {
                    isWallRunning = true;
                    isCrouching = false;
                    isRunning = false;
                    isGrappling = false;
                    isWalking = false;
                    wallrunReady = false;

                    currentWallrunLength = 0;
                    p_Rigidbody.velocity = Vector3.zero;
                    currentSpeed = wallRunSpeed;
                    Camera.main.DOFieldOfView(runFOV, fovTransitionSpeed);

                    if (Vector3.Dot(wallrunHit.normal, transform.right) > 0)
                    {
                        currentCMCamera.transform.DOLocalRotate(-wallRunRotation, tiltSpeed);
                    }
                    else
                    {
                        currentCMCamera.transform.DOLocalRotate(wallRunRotation, tiltSpeed);
                    }

                }
                else
                {
                    currentWallrunLength += Time.deltaTime;
                    currentWallrunColliderID = wallrunHit.collider.gameObject;
                }

                if (stanceState != StanceState.Standing)
                {
                    ApplyStance(StanceState.Standing);
                }

                //Stop wallrunning 
                if (jumpInput_Instant || !WallRunCheck() || isGrounded || currentWallrunLength > wallRunTime)
                {

                    StopWallRunning();

                    if (jumpInput_Instant)
                    {
                        Vector3 direction = currentCMCamera.transform.forward;
                        direction.y = 0f;

                        p_Rigidbody.velocity = Vector3.zero;
                        p_Rigidbody.AddForce(direction * wallJumpForce.x * 10 + Vector3.up * wallJumpForce.y * 10, ForceMode.Force);
                    }

                    if (wallrunDebug) lastWallrunColliderID = currentWallrunColliderID;
                }

                break;
            #endregion
            #region SlidingState
            case MovementState.Sliding:

                if (!isSliding)
                {
                    currentSpeed = slideSpeed;
                    isSliding = true;
                    isWallRunning = false;
                    isCrouching = false;
                    isRunning = false;
                    isGrappling = false;
                    isWalking = false;

                    currentSlideTime = 0;
                    slideStartVelocity = p_Rigidbody.velocity;
                    Camera.main.DOFieldOfView(slideFOV, fovTransitionSpeed);
                    currentCMCamera.transform.DOLocalRotate(new Vector3(0, 0, -10), .5f);
                    Invoke(nameof(StopSliding), slideTime);
                }
                else
                {
                    currentSlideTime += Time.deltaTime;
                }

                if (jumpInput)
                {
                    Vector3 jumpDir = transform.forward;
                    StopSliding();
                    movementState = MovementState.Running;
                }

                if (!crouchInput || p_Rigidbody.velocity.magnitude < slideStopSpeed)
                {
                    StopSliding();
                }

                break;
                #endregion
        }
    }

    private void StartGrapple()
    {
        Physics.Raycast(currentCMCamera.transform.position, currentCMCamera.transform.forward, out shootHit, maxGrappleLength);

        if (shootHit.collider != null && false) //TODO remove false
        {
            currentSpeed = grappleSpeed;
            joint = gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = shootHit.point;
            joint.spring = grappleSpringConst;

            for (int i = 0; i < numberOfGrappleLines; i++)
            {
                grappleLines.Add(new GameObject("line renderer"));
                LineRenderer grappleLine = grappleLines[i].AddComponent<LineRenderer>();
                grappleLine.material = grappleMaterial;
                grappleLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //grappleLine.widthCurve = grappleWidthCurve;
                grappleLine.widthMultiplier = UnityEngine.Random.value * grappleWidth;
                grappleLine.positionCount = numberOfGrapplePoints;
            }

            float targetDist = Vector3.Distance(transform.position, shootHit.point);

            joint.maxDistance = targetDist * .8f;
            joint.minDistance = minGrappleLength;

            joint.spring = 4.5f;
            joint.damper = 7f;
            joint.massScale = 4.5f;
        }
    }

    private void ApplyStance(StanceState newStance)
    {
        if (capsule.height == crouchingHeight)
        {
            RaycastHit hit;
            //check above 
            Physics.Raycast(transform.position, Vector3.up, out hit, standingHeight - crouchingHeight / 2.0f);
            if (hit.collider != null)
            {
                movementState = MovementState.Crouching;
                return;
            }
        }

        stanceState = newStance;
        capsule.transform.DOScaleY((stanceState == StanceState.Standing) ? standingHeight : crouchingHeight, .2f);
        currentCMCamera.transform.localPosition = new Vector3(0, capsule.height * 2f / 3f, 0);
        if (stanceState == StanceState.Crouching) { p_Rigidbody.AddForce(Vector3.down); }
    }

    public void MovePlayer()
    {
        if (isWallRunning)
        {
            MoveWallRun();
        }
        else if (isSliding)
        {
            MoveSlide();
        }
        else if (CheckSlope())
        {
            //move along slope
            if (lastMoveDir.magnitude != 0)
            {
                Vector3 dirAlongSlope = Vector3.ProjectOnPlane(lastMoveDir, slopeHit.normal).normalized;
                float slopeAngle = 90 - (Mathf.Acos(Mathf.Clamp(Vector3.Dot(dirAlongSlope, Vector3.up), -1, 1))) * 60;

                if (wallrunDebug) Debug.DrawRay(transform.position, dirAlongSlope, Color.red);

                if (slopeAngle <= slopeLimit)
                {
                    p_Rigidbody.AddForce(dirAlongSlope * currentSpeed * 10f, ForceMode.Force);
                }
            }

            p_Rigidbody.AddForce(-slopeHit.normal * 10, ForceMode.Force);

        }
        else
        {
            p_Rigidbody.AddForce(lastMoveDir * currentSpeed * 10f * (isGrounded ? 1 : airControlFactor), ForceMode.Force);
        }


        Vector3 flatVelocity = new Vector3(p_Rigidbody.velocity.x, 0f, p_Rigidbody.velocity.z);

        //clamp velocity
        if ((isGrounded && flatVelocity.magnitude > currentSpeed) || (!isGrounded && flatVelocity.magnitude > maxAirSpeed))
        {
            Vector3 clampVelocity = flatVelocity.normalized * (isGrounded ? currentSpeed : maxAirSpeed);
            clampVelocity.y = p_Rigidbody.velocity.y;
            p_Rigidbody.velocity = Vector3.Lerp(p_Rigidbody.velocity, clampVelocity, clampVelocity.magnitude / p_Rigidbody.velocity.magnitude);
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded && canJump && jumpReady && !isWallRunning)
        {
            Jump(Vector3.up, jumpHeight);
        }
        else if (isWallRunning)
        {
            Jump(Vector3.forward + Vector3.up, jumpHeight);
            StopWallRunning();
        }
    }

    private void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            crouchInput = true;
        }
        else if (context.canceled)
        {
            crouchInput = false;
        }
    }

    private void OnMovement(InputAction.CallbackContext context)
    {
        lastMoveInput = context.ReadValue<Vector2>();
        lastMoveDir = Vector3.ClampMagnitude(transform.forward * lastMoveInput.y + transform.right * lastMoveInput.x, 1);

        MovePlayer();

        if (enableHeadbob && lastMoveInput.magnitude > 0 && isGrounded)
        {
            ApplyHeadBob();
        }
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        if (enableCameraControl)
        {
            Vector2 mouseDelta = context.ReadValue<Vector2>();
            RotateView(mouseDelta, sensitivity, rotationWeight);
        }
    }

    private void OnZoom(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Camera.main.DOFieldOfView(Camera.main.fieldOfView / zoomFOVMultiplier, zoomTime);
        }
        else if (context.canceled)
        {
            Camera.main.DOFieldOfView(movementState == MovementState.Walking ? walkFOV : runFOV, zoomTime);
        }
    }

    private void OnMelee(InputAction.CallbackContext context)
    {
        if (meleeReady)
        {
            meleeReady = false;

            p_Rigidbody.AddForce(transform.forward * forwardMeleeMomentum);

            Collider[] overlapColliders = Physics.OverlapSphere(transform.position, meleeInfluenceRadius);

            foreach (Collider col in overlapColliders)
            {
                if (col.attachedRigidbody && !col.attachedRigidbody.isKinematic && col.gameObject != this.gameObject)
                {
                    Rigidbody rigidbody = col.attachedRigidbody;
                    Vector3 dir = col.transform.position - transform.position;

                    rigidbody.AddExplosionForce(meleeForce, transform.position, meleeInfluenceRadius);
                }
            }

            Invoke(nameof(MeleeReady), meleeCooldownTime);
        }
    }

    private void MeleeReady()
    {
        meleeReady = true;
    }

    private void Jump(Vector3 direction, float jumpHeight)
    {
        Debug.Log("jumping");

        //reset y vel
        p_Rigidbody.velocity = new Vector3(p_Rigidbody.velocity.x, 0f, p_Rigidbody.velocity.z);

        float jumpForce = MathF.Sqrt(jumpHeight * -2 * Physics.gravity.y);
        p_Rigidbody.AddForce(direction * jumpForce, ForceMode.Impulse);
        isJumping = true;
        jumpReady = false;

        Invoke(nameof(ResetJump), jumpCooldown);
    }

    private void ResetJump()
    {
        jumpReady = true;
    }

    //TODO Add extra steps to left and right reverse checks to stop wallrun redirection (if back left check back right 
    private bool WallRunCheck()
    {
        Physics.Raycast(transform.position, Vector3.down, out wallrunHit, minWallrunHeight, wallRunSurface);

        if (wallrunHit.collider != null)
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
            if (Physics.Raycast(transform.position, dir, out wallrunHit, maxWallRunDistance, wallRunSurface))
            {
                if (wallrunDebug) Debug.DrawRay(transform.position, dir * maxWallRunDistance, Color.red);
                currentWallrunColliderID = wallrunHit.collider.gameObject;
                return true;
            }
            else
            {
                if (wallrunDebug) Debug.DrawRay(transform.position, dir * maxWallRunDistance, Color.green);
            }
        }

        return false;
    }

    private void StopWallRunning()
    {
        if (isWallRunning)
        {
            currentCMCamera.transform.DOLocalRotate(Vector3.zero, tiltSpeed);
            movementState = (runInput ? MovementState.Running : MovementState.Walking);
            isWallRunning = false;
            lastWallrunColliderID = currentWallrunColliderID;
            wallrunReady = true;
        }
    }

    private void StopSliding()
    {
        if (isSliding)
        {
            isSliding = false;
            if (crouchInput)
            {
                movementState = MovementState.Crouching;
            }
            else
            {
                ApplyStance(StanceState.Standing);
                movementState = (runInput ? MovementState.Running : MovementState.Walking);
            }
        }
    }

    public void MoveWallRun()
    {
        if (wallrunDebug) Debug.Log("WallRunning");
        Vector3 wallNormal = wallrunHit.normal;

        //Apply Force Toward Wall
        p_Rigidbody.AddForce(-wallNormal * 2, ForceMode.Force);

        Vector3 wallrunDir = Vector3.Cross(Vector3.up, wallNormal).normalized;

        if (wallrunDebug) Debug.DrawRay(transform.position, wallrunDir, Color.red);

        Vector3 wallVelocity = wallrunDir * wallRunSpeed * ((Vector3.Dot(wallrunDir, transform.forward) > 0) ? 1.0f : -1.0f);
        float flatDecay = wallrunDecayCurve.Evaluate(currentWallrunLength / wallRunTime);
        float vertDecay = wallrunFallRate.Evaluate(currentWallrunLength / wallRunTime);

        p_Rigidbody.velocity = new Vector3(wallVelocity.x * flatDecay, p_Rigidbody.velocity.y * vertDecay, wallVelocity.z * flatDecay);
    }

    public void MoveSlide()
    {

        if (CheckSlope())
        {
            Vector3 dirAlongSlope = Vector3.ProjectOnPlane(lastMoveDir, slopeHit.normal).normalized;
            float slopeAngle = 90 - (Mathf.Acos(Mathf.Clamp(Vector3.Dot(dirAlongSlope, Vector3.up), -1, 1))) * 60;

            if (slopeAngle > 0)
            {
                Debug.Log("Move up slope");
                p_Rigidbody.velocity = slideStartVelocity * slideDecayCurve.Evaluate(currentSlideTime / slideTime);
            }
            else
            {
                Debug.Log("Move DownSlope");
                currentSlideTime = 0;
                p_Rigidbody.velocity = slideStartVelocity + Mathf.Pow(Time.deltaTime, 2) * downSlideForce * slideStartVelocity.normalized;
            }
        }
        else
        {
            Debug.Log("Slide horizontal");
            p_Rigidbody.velocity = slideStartVelocity * slideDecayCurve.Evaluate(currentSlideTime / slideTime);
        }

        Debug.Log("velocity = " + p_Rigidbody.velocity);
    }

    #endregion


    private void DrawGrappleLine()
    {
        if (!joint) return;

        Vector3 lineStart = transform.position + transform.forward + transform.right;

        foreach (GameObject grappleLineObject in grappleLines)
        {
            LineRenderer grappleLine = grappleLineObject.GetComponent<LineRenderer>();
            grappleLine.SetPosition(0, lineStart);
            grappleLine.SetPosition(numberOfGrapplePoints - 1, shootHit.point);

            Vector3 lineDir = shootHit.point - lineStart;
            Vector3 lineNorm = Vector3.Cross(currentCMCamera.transform.right, lineDir).normalized;
            float lineLength = lineDir.magnitude;
            float stepSize = lineDir.magnitude / (numberOfGrapplePoints - 2);
            lineDir.Normalize();
            float noiseAmount = .6f;
            float arcHeight = .3f;
            float c = ((Time.time + ((float)grappleLines.IndexOf(grappleLineObject) / (float)numberOfGrapplePoints)) % .5f) / .5f;
            float sinOffset = UnityEngine.Random.value * Mathf.PI;

            for (int i = 1; i < numberOfGrapplePoints - 1; i++)
            {
                float pointHeight = -4 / lineLength * i * stepSize * (i * stepSize / lineLength - 1) * arcHeight;
                pointHeight *= c;
                pointHeight += UnityEngine.Random.value * noiseAmount;
                Vector3 horPos = lineStart + lineDir * stepSize * i;
                grappleLine.SetPosition(i, horPos + (lineNorm * pointHeight * Mathf.Sin((Mathf.PI + sinOffset))));
            }
        }
    }

    private bool CheckSlope()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position, Vector3.down, out hit, capsule.height * .5f + .05f);

        if (hit.normal != Vector3.up && hit.collider != null)
        {
            slopeHit = hit;
            Debug.Log("center slope");
            return true;
        }

        Physics.Raycast(transform.position + lastMoveDir.normalized * capsule.radius, Vector3.down, out hit, capsule.height * .5f + .05f);

        //Debug.DrawRay(transform.position + movInputDir.normalized * capsule.radius, Vector3.down * (capsule.height * .5f + .05f), Color.red);
        if (hit.normal != Vector3.up && hit.collider != null)
        {
            slopeHit = hit;
            Debug.Log("forward slope");
            return true;
        }

        //Debug.Log("no slope");
        return false;
    }

    private float SlopeAngle()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position, Vector3.down, out hit, capsule.height * .5f + .2f);

        if (hit.collider == null)
        {
            return 0f;
        }


        Vector3 dirAlongSlope = Vector3.ProjectOnPlane(lastMoveDir, slopeHit.normal).normalized;
        float slopeAngle = 90 - (Mathf.Acos(Mathf.Clamp(Vector3.Dot(dirAlongSlope, Vector3.up), -1, 1))) * 60;

        return slopeAngle;
    }

}
