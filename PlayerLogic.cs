using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


[RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
public class PlayerLogic : MonoBehaviour
{
    public static PlayerLogic Instance { get; private set; }
    private PlayerInput playerInput;
    private PlayerVisual playerVisual;
    [Header("Terrain LayerMask"), SerializeField] private LayerMask TERRAIN;
    [Header("Player LayerMask"), SerializeField] private LayerMask PLAYER;

    // Game Input
    private CharacterController controller;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction interactAction;
    private InputAction crouchAction;


    // Movement
    //vector to store the movement values
    Vector3 move;
    Vector3 cameraForwardXZ;
    Quaternion targetRotation;
    [Header("Movement"), SerializeField] private float movementSpeed;
    [SerializeField] private float baseMovementSpeed = 2f;
    [SerializeField] private float sprintMultiplier = 3f;
    private float currentSpeed;
    private float speedSmoothVelocity;
    private float speedSmoothTime = 0.1f;
    private bool isSprinting;

    private Vector3 playerVelocity;
    [SerializeField] private float rotSpeed = 10f;

    // Interactibles
    [Header("Interactible"), SerializeField] private LayerMask interactibleLayerMask;

    // Jump
    [Header("Jump"), SerializeField] private float groundCheckDistance = 0.5f;
    [SerializeField] private float jumpHeight = 0.6f;
    [SerializeField, Tooltip("-9.81 is the base Gravity Acceleration")]
    private float gravityCurrent;
    private float gravityValue = -9.81f;
    private float gravityMultiplier = 2f;
    private float groundedTimer = 0f;

    //Crouching
    private bool isCrouching;
    [Header("Crouch"), SerializeField] private float crouchMultiplier = 0.5f;

    //Ledge Climb
    [Header("Ledge Climb"), SerializeField, Range(0f, 2f)] private float hangCheckStartingPointOffset = 1.5f;
    [SerializeField, Range(0f, 2f)] private float hangCheckEndingPointOffset = 0.97f, kneeHeightOffset = 0.8f, contactPointOffset = 0.4f, pullUpOffset = 0.4f;
    private bool isHanging = false;

    // Camera
    private Transform cameraTransform;

    private void Awake()
    {
        movementSpeed = baseMovementSpeed;
        gravityCurrent = gravityValue;
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        playerVisual = GetComponentInChildren<PlayerVisual>();
        cameraTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        sprintAction = playerInput.actions["Sprint"];
        interactAction = playerInput.actions["Interact"];
        crouchAction = playerInput.actions["Crouch"];

        isHanging = false;
    }

    private void Update()
    {
        Debug.DrawLine(transform.position, Vector3.zero, Color.white);
        //Gravity
        //if the player is on the ground and was falling, reset velocity and increase the grounded timer
        if (GroundCheck(groundCheckDistance) && playerVelocity.y < -6)
        {
            playerVelocity.y = -3f;
            groundedTimer += Time.deltaTime;
        }
        else //player is currently falling
        {
            playerVelocity.y += gravityCurrent * Time.deltaTime;
            groundedTimer = 0f;
        } 

        // Check if the player has been grounded for at least 1 second
        if (groundedTimer >= 0.5f)
        {
            // Adjust gravity to be stronger when jump has reached the highest jump time
            gravityCurrent = gravityValue * gravityMultiplier; 
        }

        SlopeCheck();

        // Vertical Movement affected by Gravity
        //controller.Move(playerVelocity * Time.deltaTime);

        if (!isHanging)
        {

            // Movement
            Vector2 input = moveAction.ReadValue<Vector2>();

            // Get the camera's forward direction in the XZ plane so that the movement won't be affected bu the camera's Y component of the .forward
            cameraForwardXZ = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;

            move = new Vector3(input.x, 0, input.y).normalized;

            // Calculate the movement direction by combining the camera's forward direction in the XZ plane and input
            move = move.x * cameraTransform.right.normalized + move.z * cameraForwardXZ;
            move.y = 0;

            // Smoothing of the movement, simulate acceleration
            float targetSpeed = move.magnitude * movementSpeed;
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, speedSmoothTime);
            move *= currentSpeed;

            // Calculate currentSpeedX and currentSpeedZ to use in the Animator
            float currentSpeedX = Vector3.Dot(move, cameraTransform.right.normalized);
            float currentSpeedZ = Vector3.Dot(move, cameraForwardXZ);

            // Move the player
            controller.Move((move + playerVelocity) * Time.deltaTime);

            // Animator pass speed for the blending tree
            playerVisual.SetPlayerSpeed(currentSpeedX, currentSpeedZ);

            // Rotation for the movement using the camera
            float targetAngle = cameraTransform.eulerAngles.y;
            targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotSpeed * Time.deltaTime);
        }
        else
        {
            playerVelocity = Vector3.zero;
            move = Vector3.right.x * cameraTransform.right.normalized + Vector3.forward.z * cameraForwardXZ;
        }

        HangCheck();

       
    }

    

    private void HangCheck()
    {
        if (isHanging)
            return;
       
        //Camera reference for the direction
        Vector3 cameraForwardXZ = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;

        //GET VERTICAL VECTOR PART        
        //Start position of the vector
        Vector3 startPosition = transform.position + Vector3.up * hangCheckStartingPointOffset + cameraForwardXZ;
        //End Position of the vector
        Vector3 endPosition = startPosition - Vector3.up * hangCheckEndingPointOffset;
        //vector difference to get the vertical vector
        Vector3 ledgeVectorVertical = endPosition - startPosition;

        //Store Raycast info and distance
        RaycastHit hit;
        //float hangCheckRaycastDistance = 3f;

        //Ray Debug, see the ray
        Debug.DrawRay(startPosition, ledgeVectorVertical, Color.yellow);
       
        Debug.DrawRay(Vector3.zero, ledgeVectorVertical, Color.red);
        //Vertical Ray Check
        if (Physics.Raycast(startPosition, ledgeVectorVertical, out hit, ledgeVectorVertical.magnitude, TERRAIN, QueryTriggerInteraction.Ignore))
        {

            //Does it have the climbable Script?
            if (hit.transform.gameObject.GetComponent<Climbable>() == null)
                return;
            

            //it does, cool, let's get the vector of the point of contact and make it realtive to the player
            Vector3 CONTACT_POINT = hit.point;
            Vector3 contactPointOffsetVector = CONTACT_POINT + new Vector3(0, -contactPointOffset, 0);
            Vector3 kneePosition = transform.position + new Vector3(0, kneeHeightOffset, 0);

            //We got the height, now check the Y point distance between the Player and the HitPoint.
            if (kneePosition.y >= contactPointOffsetVector.y)
            {
                //the distance is right to do the step hop animation
                //knee animation
                Debug.DrawLine(kneePosition, contactPointOffsetVector, Color.magenta, 5f);
                Debug.Log("knee animation logic");
                isHanging = true;


                StartCoroutine(ClimbUP(transform.position, transform.position + new Vector3(0, pullUpOffset, 0), CONTACT_POINT, false));
                return;
            }
            else if (kneePosition.y + (kneeHeightOffset*2) >= contactPointOffsetVector.y) 
            {
                //waist animation
                Debug.DrawLine(kneePosition + new Vector3(0, kneeHeightOffset, 0), contactPointOffsetVector, Color.cyan, 5f);
                Debug.Log("too high for the knee, grab onto it logic");
                isHanging = true;

                StartCoroutine(ClimbUP(transform.position, transform.position + new Vector3(0, pullUpOffset, 0), CONTACT_POINT, true));
                
            }
        }
    }
    private IEnumerator ClimbUP(Vector3 startTransformPosition, Vector3 heightTransformDestination, Vector3 forwardTransformDestination, bool isWaistAnimation)
    {
        playerVisual.isWaistAnimation = isWaistAnimation;
        playerVisual.isHanging = true;
        if (isWaistAnimation)
        {
            float startTime = Time.time; // Time.time contains current frame time, so remember starting point
            while (Time.time - startTime <= 1.1f)
            { // until 1.1 seconds passed
                transform.position = Vector3.Lerp(startTransformPosition, heightTransformDestination, Time.time - startTime); // lerp from A to B in one second
                yield return 1; // wait for next frame
            }

            forwardTransformDestination.y = transform.position.y;

            startTime = Time.time;
            while (Time.time - startTime <= 0.3f)
            { // until 0.3 seconds passed
                transform.position = Vector3.Lerp(transform.position, forwardTransformDestination, Time.time - startTime); // lerp from A to B in one second
                yield return 1; // wait for next frame
            }
        }
        else
        {
            float startTime = Time.time; // Time.time contains current frame time, so remember starting point
            while (Time.time - startTime <= 0.2f)
            { // until 0.2 seconds passed
                transform.position = Vector3.Lerp(startTransformPosition, heightTransformDestination, Time.time - startTime); // lerp from A to B in one second
                yield return 1; // wait for next frame
            }

            forwardTransformDestination.y = transform.position.y;

            startTime = Time.time;
            while (Time.time - startTime <= 0.5f)
            { // until 0.5 seconds passed
                transform.position = Vector3.Lerp(transform.position, forwardTransformDestination, Time.time - startTime); // lerp from A to B in one second
                yield return 1; // wait for next frame
            }
        }

        isHanging = false;

    }


        #region Event Supscripton

    private void OnEnable()
    {
        jumpAction.performed += OnJumpPerformed;
        jumpAction.canceled += OnJumpCanceled;

        sprintAction.performed += OnSprintPerformed;
        sprintAction.canceled += OnSprintCanceled;

        interactAction.performed += OnInteractPerformed;
        interactAction.canceled += OnInteractCanceled;

        crouchAction.performed += OnCrouchPerformed;
        crouchAction.canceled += OnCrouchCanceled;
    }
    private void OnDisable()
    {
        jumpAction.performed -= OnJumpPerformed;
        jumpAction.canceled -= OnJumpCanceled;

        sprintAction.performed -= OnSprintPerformed;
        sprintAction.canceled -= OnSprintCanceled;

        interactAction.performed -= OnInteractPerformed;
        interactAction.canceled -= OnInteractCanceled;

        crouchAction.performed -= OnCrouchPerformed;
        crouchAction.canceled -= OnCrouchCanceled;
    }

    #endregion

    #region Sprint Logic
    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        if (!isCrouching)
        {
            isSprinting = true;
            movementSpeed = baseMovementSpeed * sprintMultiplier;
            Debug.Log(movementSpeed);
        }
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        if (!isCrouching)
        {
            isSprinting = false;
            movementSpeed = baseMovementSpeed;
            Debug.Log(movementSpeed);
        }
    }

    #endregion

    #region Interaction Logic
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + new Vector3(0, 2f, 0), transform.forward, out hit, 5f, interactibleLayerMask))
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();
            if (interactable != null)
            {
                interactable.Interact();
            }
        }
        else
        {
            Debug.Log("No interactible");
        }

        Debug.DrawRay(transform.position + new Vector3(0,2f,0), cameraTransform.forward * 3f, Color.blue, 0.3f);
    }


    private void OnInteractCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("Interaction Canceled");
    }
    #endregion

    #region Jump Logic and Slope Slide

    private bool GroundCheck( float groundCheckDistance)
    {
        //Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, Color.red, 0.2f);
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, TERRAIN);
    }

    private void SlopeCheck()
    {
        if (isHanging)
            return;

        RaycastHit hit;
        float distance = 2f;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, distance, ~PLAYER, QueryTriggerInteraction.Ignore))
        {
            //Debug.Log("Slope detected");
            //var collider = hit.collider;
            var angle = Vector3.Angle(Vector3.up, hit.normal);
            //Debug.Log(angle);
            if (angle > controller.slopeLimit)
            {
                var normal = hit.normal;
                var yInverse = 1f - normal.y;

                controller.Move(new Vector3(yInverse * normal.x, 0, yInverse * normal.z) * 5f * Time.deltaTime);

            }
        }
    }
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        isHanging = false;

        if (GroundCheck(groundCheckDistance) && !isCrouching)
        {
            playerVisual.isJumping = true;
            // Calculate the initial vertical velocity to reach the highest point in 0.5 seconds
            float initialVerticalVelocity = jumpHeight / 0.5f;

            // Apply the initial velocity to the player's vertical velocity
            playerVelocity.y = initialVerticalVelocity;

            // Reset the grounded timer
            groundedTimer = 0f;
        }
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        // Adjust gravity to be stronger when jump is canceled
        gravityCurrent = gravityValue * gravityMultiplier; 
    }
    #endregion

    #region Crouch Logic

    private void OnCrouchPerformed(InputAction.CallbackContext obj)
    {
        if (!isSprinting)
        {
            if (GroundCheck(groundCheckDistance) == true) 
            {
                isCrouching = true;
                playerVisual.isCrouching = true;
                movementSpeed = baseMovementSpeed * crouchMultiplier;
            }   
        }
        Debug.Log(isCrouching);
    }

    private void OnCrouchCanceled(InputAction.CallbackContext obj)
    {
        if (!isSprinting)
        {
            isCrouching = false;
            playerVisual.isCrouching = false;
            movementSpeed = baseMovementSpeed;
            //trigger exit if it's played
            playerVisual.ExitCrouchTrigger();
        }
        Debug.Log(isCrouching);
    }
    #endregion


}
