using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [SerializeField] private Animator animator; // Reference to the Animator component.
    private const string HORIZONTAL_SPEED_X = "PlayerSpeedX"; // Name of the float parameter in the Animator controller.
    private const string HORIZONTAL_SPEED_Z = "PlayerSpeedZ";


    public bool isJumping;
    public bool isCrouching;
    public bool isHanging;
        public bool isWaistAnimation;
    private Vector2 horizontalSpeed;





    //Animator states to hash
    int GROUND_TO_JUMP;
    int GROUND_TO_CROUCH;
    int CLIMB_WAIST;
    int CLIMB_KNEE;



    //private Vector2 moveDirection; // Stores the movement input direction.

    private void Awake()
    {
        animator= GetComponent<Animator>();
        Animator.StringToHash(HORIZONTAL_SPEED_Z);

        GROUND_TO_JUMP = Animator.StringToHash("GroundToJump");
        GROUND_TO_CROUCH = Animator.StringToHash("GroundToCrouch");
        CLIMB_WAIST = Animator.StringToHash("ClimbWaist");
        CLIMB_KNEE = Animator.StringToHash("ClimbKnee");
    }
    
    
    //public void SetMoveDirection(Vector2 direction) => moveDirection = direction;

    //public Vector2 GetPlayerDirection()
    //{
    //    return moveDirection;
    //}


    

    


    private void Update()
    {
        //Set the animation parameters for ground speed
        float moveSpeedX = GetPlayerSpeedX();
        float moveSpeedZ = GetPlayerSpeedZ();
        animator.SetFloat(HORIZONTAL_SPEED_X, moveSpeedX);
        animator.SetFloat(HORIZONTAL_SPEED_Z, moveSpeedZ);

        //check which special animation has to be played
        if (isHanging)
        {
            if (isWaistAnimation)
                animator.CrossFade(CLIMB_WAIST, 0.2f);
            else
                animator.CrossFade(CLIMB_KNEE, 0.2f);

            isHanging= false;
        }
        else if (isJumping)
        {
            animator.CrossFade(GROUND_TO_JUMP, 0.2f);
            isJumping = false;
        }
        else if (isCrouching)
        {
            animator.Play(GROUND_TO_CROUCH);
        }
        else isCrouching = false;


    }




    //SET in the player logic in the PlayerLogic
    public void SetPlayerSpeed(float speedX, float speedZ)
    {
        horizontalSpeed = new Vector2(speedX, speedZ);
    }

    //GETTER within the animator of the movement speed parameters for the animations
    public float GetPlayerSpeedX()
    {
        return horizontalSpeed.x;
    }
    public float GetPlayerSpeedZ()
    {
        return horizontalSpeed.y;
    }

    public void ExitCrouchTrigger()
    {
        animator.SetTrigger("CrouchEXIT");
    }
}
