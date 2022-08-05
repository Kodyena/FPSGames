using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(CapsuleCollider))]
[AddComponentMenu("Custom Character Controller")]
public class CustomCharacterController : MonoBehaviour
{

    #region Variables 
    private enum MovementState { Walking, Running, WallRunning, Jumping, Crouching, Grappling, Sliding };
    private enum StanceState { Standing, Crouching };

    #region Camera
    public enum MouseInversionMode { None, X, Y, Both };
    public MouseInversionMode mouseInversion = MouseInversionMode.None;
    public bool enableCameraControl;
    public Camera playerCamera;
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
    public float fovTransitionSpeed;

    #region HeadBob

    public bool enableHeadbob = true;
    public float headbobSpeed;
    public float headbobPower;
    public float ZTilt;

    float yBob;
    Quaternion headbobCameraRotation = Quaternion.identity;
    #endregion

    //Internal
    Vector2 MouseXY;
    Vector2 viewRotVelRef;
    RaycastHit focalPoint;
    float initialCameraFOV;

    #endregion

    #region Movement

    public float walkingSpeed, runningSpeed, crouchingSpeed;
    public bool canRun = true, canWallRun = true, canCrouch = true, canJump = true, canGrapple = true;
    public LayerMask whatIsGround;
    public float groundDrag;
    public float gravity;
    public float maxAirSpeed;


    //Jumping
    public float jumpPower;
    public float airControlFactor;
    public float jumpDescentMultiplier;
    public float wallrunDescentMultiplier;
    public float jumpCooldown;
    bool jumpReady;
    bool jumping;

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
    public float maxWallRunDistance = .5f;
    [Range(0, 1)]
    public float wallrunDecayFactor;
    public float minWallrunHeight;
    public LayerMask wallRunSurface = 7;
    public float tiltAngle;
    public float tiltSpeed;
    public float horizontalWallJumpForce;
    public float verticalWallJumpForce;

    bool wallrunReady;
    bool isWallRunning;
    RaycastHit wallrunHit;


    //Sliding
    public float slideTime;
    public float maxSlideSpeed;
    public float downSlideForce;
    public float slideSpeed;
    public float minSlideStartSpeed;
    public float slideStopSpeed;
    public AnimationCurve slideDecayCurve;

    Vector3 slideStartVelocity;
    float currentSlideTime;
    bool isSliding;

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
    Vector2 movInput;
    Vector2 movInputSmooth;
    bool jumpInput, jumpInput_Instant;
    bool crouchInput, crouchInput_Instant;
    bool runInput, runInput_Instant;
    bool isCrouching, isRunning, isGrappling, isWalking;
    bool grappleInput, grappleInput_Instant;
    Vector3 movInputDir;

    #endregion

    #endregion


    // Start is called before the first frame update
    void Start()
    {
        p_Rigidbody = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        Cursor.lockState = CursorLockMode.Locked;
        movementState = MovementState.Walking;
        stanceState = StanceState.Standing;
        capsule.height = standingHeight;
        currentSpeed = walkingSpeed;
        jumping = false;
        jumpReady = true;
        wallrunReady = true;
        p_Rigidbody.useGravity = false;
        grappleLines = new List<GameObject>();    
    }

    // Update is called once per frame
    void Update()
    {

        //Get Inputs
        MouseXY.x = Input.GetAxis("Mouse Y");
        MouseXY.y = Input.GetAxis("Mouse X");

        jumpInput = Input.GetKey(jumpKey);
        jumpInput_Instant = Input.GetKeyDown(jumpKey);
        grappleInput = Input.GetKey(grappleKey);
        grappleInput_Instant = Input.GetKeyDown(grappleKey);
        crouchInput = Input.GetKey(crouchKey);
        crouchInput_Instant = Input.GetKeyDown(crouchKey);
        runInput = Input.GetKey(runKey);
        runInput_Instant = Input.GetKeyDown(runKey);

        if (enableCameraControl)
        {
            RotateView(MouseXY, sensitivity, rotationWeight);
        }

        isGrounded = Physics.Raycast(transform.position, Vector3.down, capsule.height * .5f + .2f);

        if (isGrounded && canJump && jumpInput && jumpReady)
        {
            Jump(jumpPower);
        }

        if (isGrounded)
        {
            p_Rigidbody.drag = groundDrag;
            jumping = false;
        }
        else
        {
            p_Rigidbody.drag = 0;
        }

        movInput.x = Input.GetAxisRaw("Horizontal");
        movInput.y = Input.GetAxisRaw("Vertical");
        movInputDir = Vector3.ClampMagnitude(transform.forward * movInput.y + transform.right * movInput.x, 1);

        HandleMovementState();

        MovePlayer();



        Debug.Log(p_Rigidbody.velocity);
        //ApplyHeadBob();
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
        Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out focalPoint, Mathf.Infinity);

        Vector3 camPos = cameraHolder.transform.position;

        camPos.y -= yBob;

        if (movInput.magnitude > 0)
        {
            yBob = headbobPower * Mathf.Sin(headbobSpeed * Time.time * Mathf.PI * 2);
        }
        else
        {
            yBob = 0;
        }
        
        camPos.y += yBob;
        playerCamera.transform.position = camPos;

        if(focalPoint.collider != null)
        {
            //Look at focal point
            playerCamera.transform.LookAt(focalPoint.point);
        }
        else
        {
            Vector3 horizonPoint = transform.position + playerCamera.transform.forward * 1000;
            horizonPoint.y -= yBob;

            //Look at horizon
            playerCamera.transform.LookAt(horizonPoint);
        }
    }

    public void RotateView(Vector2 yawPitch, float sensitivity, float cameraWeight)
    {
        //Invert controls
        yawPitch.x *= ((mouseInversion == MouseInversionMode.X || mouseInversion == MouseInversionMode.Both)) ? 1 : -1;
        yawPitch.y *= ((mouseInversion == MouseInversionMode.Y || mouseInversion == MouseInversionMode.Both)) ? 1 : -1;

        Vector2 targetAngles = ((Vector2.right * cameraHolder.transform.localEulerAngles.x) + (Vector2.up * transform.localEulerAngles.y)); 
        targetAngles = Vector2.SmoothDamp(targetAngles, targetAngles + (yawPitch * (sensitivity * Mathf.Pow(cameraWeight, 2))), ref viewRotVelRef, (Mathf.Pow(cameraWeight, 2)) * Time.deltaTime);
        targetAngles.x += targetAngles.x > 180 ? -360 : targetAngles.x < -180 ? 360 : 0;
        targetAngles.x = Mathf.Clamp(targetAngles.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);
        cameraHolder.transform.localEulerAngles = (Vector3.right * targetAngles.x) + Vector3.forward;
        transform.localEulerAngles = new Vector3(0f, targetAngles.y, 0);
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
            case MovementState.Walking:

                if (!isWalking)
                {
                    isCrouching = false;
                    isRunning = false;
                    isGrappling = false;
                    isWalking = true;
                    isWallRunning = false;

                    playerCamera.DOFieldOfView(walkFOV, fovTransitionSpeed);

                    ApplyStance(StanceState.Standing);
                }

                if (canCrouch && crouchInput && isGrounded)
                {
                    movementState = MovementState.Crouching;
                    ApplyStance(StanceState.Crouching);
                }
                
                if (canRun && runInput )
                {
                    movementState = MovementState.Running;
                }

                if (grappleInput_Instant)
                {
                    movementState = MovementState.Grappling;
                }

                break;

            case MovementState.Running:

                if (!isRunning)
                {
                    isCrouching = false;
                    isRunning = true;
                    isGrappling = false;
                    isWalking = false;
                    isWallRunning = false;

                    currentSpeed = runningSpeed;
                    playerCamera.DOFieldOfView(runFOV, fovTransitionSpeed);

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
                
                if (!isWallRunning && WallRunCheck() && wallrunReady)
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

            case MovementState.Crouching:

                if (!isCrouching)
                {
                    isCrouching = true;
                    isRunning = false;
                    isGrappling = false;
                    isWalking = false;
                    isWallRunning = false;

                    currentSpeed = crouchingSpeed;
                    playerCamera.DOFieldOfView(walkFOV, fovTransitionSpeed);

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
                    isWalking=false;
                    isWallRunning = false;

                    currentSpeed = grappleSpeed;
                    playerCamera.DOFieldOfView(runFOV, fovTransitionSpeed);

                    StartGrapple();
                }

                RaycastHit hit;
                Physics.Raycast(playerCamera.transform.position, shootHit.point  - transform.position, out hit, Mathf.Infinity);
                
                if (!grappleInput || (hit.collider != shootHit.collider && shootHit.collider != null && hit.collider != null))
                {
                    Destroy(joint);

                    foreach(GameObject grappleLine in grappleLines)
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

            case MovementState.WallRunning:

                Debug.Log("Wall Running");
                Vector3 wallRunRotation = new Vector3(0f, 0f, tiltAngle);
                if (!isWallRunning)
                {
                    isWallRunning = true;
                    isCrouching = false;
                    isRunning = false;
                    isGrappling = false;
                    isWalking = false;

                    wallrunReady = false;

                    p_Rigidbody.velocity = Vector3.zero;
                    currentSpeed = wallRunSpeed;
                    playerCamera.DOFieldOfView(runFOV, fovTransitionSpeed);

                    if (Vector3.Dot(wallrunHit.normal, transform.right) > 0)
                    {
                        playerCamera.transform.DOLocalRotate(-wallRunRotation, tiltSpeed);
                    }
                    else
                    {
                        playerCamera.transform.DOLocalRotate(wallRunRotation, tiltSpeed);
                    }

                    Invoke(nameof(StopWallRunning), wallRunTime);
                }

                if(stanceState != StanceState.Standing)
                {
                    ApplyStance(StanceState.Standing);
                }

                //Stop wallrunning 
                if (jumpInput_Instant || !WallRunCheck() || isGrounded)
                {

                    StopWallRunning();

                    if (jumpInput_Instant)
                    {
                        Vector3 direction = playerCamera.transform.forward;
                        direction.y = 0f;

                        p_Rigidbody.velocity = Vector3.zero;
                        p_Rigidbody.AddForce(direction * horizontalWallJumpForce * 10 + Vector3.up * verticalWallJumpForce * 10, ForceMode.Force);
                    }
                }

                break;

            case MovementState.Sliding:

                if (!isSliding)
                {
                    currentSpeed = slideSpeed;
                    isSliding = true;
                    currentSlideTime = 0;
                    slideStartVelocity = p_Rigidbody.velocity; 
                    //Invoke(nameof(StopSliding), slideTime);
                }
                else
                {
                    currentSlideTime += Time.deltaTime;
                }

                if (!crouchInput || p_Rigidbody.velocity.magnitude < slideStopSpeed)
                {
                    StopSliding();
                }

                break;
        }
    }

    private void StartGrapple()
    {
        Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out shootHit, maxGrappleLength);

        if (shootHit.collider != null)
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

    private void WallrunReady()
    {
        wallrunReady = true;
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
        playerCamera.transform.localPosition = new Vector3(0, capsule.height * 2f / 3f, 0);
        if (stanceState == StanceState.Crouching) { p_Rigidbody.AddForce(Vector3.down); }
    }

    public void MovePlayer()
    {
        if (isWallRunning)
        {
            MoveWallRun();
        }
        else if(isSliding)
        {
            MoveSlide();
        }
        else if (CheckSlope())
        {
            if (movInputDir.magnitude != 0)
            {
                Vector3 dirAlongSlope = Vector3.ProjectOnPlane(movInputDir, slopeHit.normal).normalized;
                float slopeAngle = 90 - (Mathf.Acos(Mathf.Clamp(Vector3.Dot(dirAlongSlope, Vector3.up), -1, 1))) * 60;

                Debug.DrawRay(transform.position, dirAlongSlope, Color.red);

                if (slopeAngle <= slopeLimit)
                {
                    p_Rigidbody.AddForce(dirAlongSlope * currentSpeed * 10f, ForceMode.Force);
                }
            }

            //p_Rigidbody.AddForce(-slopeHit.normal * 10, ForceMode.Force);

        }
        else
        {
            p_Rigidbody.AddForce(movInputDir * currentSpeed * 10f * (isGrounded ? 1 : airControlFactor), ForceMode.Force);
        }


        Vector3 flatVelocity = new Vector3(p_Rigidbody.velocity.x, 0f, p_Rigidbody.velocity.z);

        //clamp velocity
        if ((isGrounded && flatVelocity.magnitude > currentSpeed) || (!isGrounded && flatVelocity.magnitude > maxAirSpeed))
        {
            Vector3 clampVelocity = flatVelocity.normalized * (isGrounded ? currentSpeed : maxAirSpeed);
            clampVelocity.y = p_Rigidbody.velocity.y;
            p_Rigidbody.velocity = Vector3.Lerp(p_Rigidbody.velocity,clampVelocity, clampVelocity.magnitude / p_Rigidbody.velocity.magnitude);
        }
    }

    private void Jump(float jumpForce)
    {
        Debug.Log("jumping");

        //reset y vel
        p_Rigidbody.velocity = new Vector3(p_Rigidbody.velocity.x, 0f, p_Rigidbody.velocity.z);

        p_Rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Force);
        jumping = true;
        jumpReady = false;

        Invoke(nameof(ResetJump), jumpCooldown);
    }

    private void ResetJump()
    {
        jumpReady = true;
    }

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
                Debug.DrawRay(transform.position, dir * maxWallRunDistance, Color.red);
                return true;
            }
            else
            { 
                Debug.DrawRay(transform.position, dir * maxWallRunDistance, Color.green);
            }
        }

        return false;
    }

    private void StopWallRunning()
    {
        if (isWallRunning)
        {
            playerCamera.transform.DOLocalRotate(Vector3.zero, tiltSpeed);
            movementState = (runInput ? MovementState.Running : MovementState.Walking);
            isWallRunning = false;
            Invoke(nameof(WallrunReady), .1f);
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
        Debug.Log("WallRunning");
        Vector3 wallNormal = wallrunHit.normal;

        //Apply Force Toward Wall
        p_Rigidbody.AddForce(-wallNormal * 2, ForceMode.Force);

        Vector3 wallrunDir = Vector3.Cross(Vector3.up, wallNormal).normalized;

        Debug.DrawRay(transform.position, wallrunDir, Color.red);

        Vector3 wallVelocity = wallrunDir * wallRunSpeed * ((Vector3.Dot(wallrunDir, transform.forward) > 0) ? 1.0f : -1.0f);
        p_Rigidbody.velocity = new Vector3(wallVelocity.x, p_Rigidbody.velocity.y * wallrunDecayFactor, wallVelocity.z);
    }

    public void MoveSlide()
    {

        if (CheckSlope())
        {
            Vector3 dirAlongSlope = Vector3.ProjectOnPlane(movInputDir, slopeHit.normal).normalized;
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

        foreach (GameObject grappleLineObject in grappleLines) {
            LineRenderer grappleLine = grappleLineObject.GetComponent<LineRenderer>();
            grappleLine.SetPosition(0, lineStart);
            grappleLine.SetPosition(numberOfGrapplePoints - 1, shootHit.point);

            Vector3 lineDir = shootHit.point - lineStart;
            Vector3 lineNorm = Vector3.Cross(playerCamera.transform.right, lineDir).normalized;
            float lineLength = lineDir.magnitude;
            float stepSize = lineDir.magnitude / (numberOfGrapplePoints - 2);
            lineDir.Normalize();
            float noiseAmount = .6f;
            float arcHeight = .3f;
            float c = ((Time.time + ((float)grappleLines.IndexOf(grappleLineObject) / (float) numberOfGrapplePoints)) % .5f) / .5f;
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

        if(hit.collider == null)
        {
            Debug.Log("null");
        }

        if (hit.normal != Vector3.up && hit.collider != null)
        {
            slopeHit = hit;
            Debug.Log("center slope");
            return true;
        }

        Physics.Raycast(transform.position + movInputDir.normalized * capsule.radius, Vector3.down, out hit, capsule.height * .5f + .05f);

        Debug.DrawRay(transform.position + movInputDir.normalized * capsule.radius, Vector3.down * (capsule.height * .5f + .05f), Color.red);
        if(hit.normal != Vector3.up && hit.collider != null)
        {
            slopeHit = hit;
            Debug.Log("forward slope");
            return true;
        }

        Debug.Log("no slope");
        return false;
    }

    private float SlopeAngle()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position, Vector3.down, out hit, capsule.height * .5f + .2f);

        if(hit.collider == null)
        {
            return 0f;
        }


        Vector3 dirAlongSlope = Vector3.ProjectOnPlane(movInputDir, slopeHit.normal).normalized;
        float slopeAngle = 90 - (Mathf.Acos(Mathf.Clamp(Vector3.Dot(dirAlongSlope, Vector3.up), -1, 1))) * 60;

        return slopeAngle;
    }
}
