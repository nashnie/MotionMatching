using UnityEngine;

//[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(MotionMatcher))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float m_MovingTurnSpeed = 360;
    [SerializeField] float m_StationaryTurnSpeed = 180;
    [SerializeField] float m_JumpPower = 12f;
    [Range(1f, 4f)] [SerializeField] float m_GravityMultiplier = 2f;
    [SerializeField] float m_RunCycleLegOffset = 0.2f; //specific to the character in sample assets, will need to be modified to work with others
    [SerializeField] float m_MoveSpeedMultiplier = 1f;
    [SerializeField] float m_AnimSpeedMultiplier = 1f;
    [SerializeField] float m_GroundCheckDistance = 0.1f;
    [SerializeField] AnimationClip[] animationClips;
    private MotionMatcher motionMatcher;
    private string matchedMotionName = "HumanoidIdle";

    //Rigidbody m_Rigidbody;
    Animator m_Animator;
    bool m_IsGrounded;
    float m_OrigGroundCheckDistance;
    const float k_Half = 0.5f;
    float m_TurnAmount;
    float m_ForwardAmount;
    Vector3 m_GroundNormal;
    float m_CapsuleHeight;
    Vector3 m_CapsuleCenter;
    CapsuleCollider m_Capsule;
    bool m_Crouching;
    float fps = 30.0f;

    void Start()
    {
        m_Animator = GetComponent<Animator>();
        //m_Rigidbody = GetComponent<Rigidbody>();
        m_Capsule = GetComponent<CapsuleCollider>();
        motionMatcher = GetComponent<MotionMatcher>();

        m_CapsuleHeight = m_Capsule.height;
        m_CapsuleCenter = m_Capsule.center;

        //m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        m_OrigGroundCheckDistance = m_GroundCheckDistance;

        m_IsGrounded = true;
        m_Animator.applyRootMotion = true;
    }

    public void Move(Vector3 move, bool crouch, bool jump)
    {
        // convert the world relative moveInput vector into a local-relative
        // turn amount and forward amount required to head in the desired
        // direction.
        if (move.magnitude > 1f) move.Normalize();
        move = transform.InverseTransformDirection(move);

        move = Vector3.ProjectOnPlane(move, m_GroundNormal);
        m_TurnAmount = Mathf.Atan2(move.x, move.z);
        m_ForwardAmount = move.z;

        ApplyExtraTurnRotation();

        m_Crouching = crouch;

        // send input and other state parameters to the animator
        UpdateAnimator(move);
    }

    void UpdateAnimator(Vector3 move)
    {
        // update the animator parameters
        //m_Animator.SetFloat("Forward", m_ForwardAmount, 0.1f, Time.deltaTime);

        //m_ForwardAmount
        //m_TurnAmount
        //m_Crouching

        float normalizedTime = m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        normalizedTime = normalizedTime - Mathf.FloorToInt(normalizedTime);
        PlayerInput playerInput = new PlayerInput();
        if (m_ForwardAmount >= 1)
        {
            playerInput.velocity = move.magnitude;
            playerInput.direction = move.normalized;
            playerInput.angularVelocity = 0.0f;
        }
        else
        {
            playerInput.velocity = 0.0f;
            playerInput.direction = Vector3.zero;
            playerInput.angularVelocity = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount) * m_TurnAmount;
        }

        playerInput.acceleration = 0.0f;
        playerInput.brake = 0.0f;
        playerInput.crouch = m_Crouching;

        string matchedMotionName = motionMatcher.AcquireMatchedMotion(playerInput, this.matchedMotionName, normalizedTime);
        if (this.matchedMotionName != matchedMotionName)
        {
            this.matchedMotionName = matchedMotionName;
            //float bestMotionFrameIndex = motionMatcher.bestMotionFrameIndex;
            float bestMotionFrameIndex = 0;
            float bestMotionTime = bestMotionFrameIndex / (m_Animator.GetCurrentAnimatorStateInfo(0).length * fps);
            //m_Animator.Play(matchedMotionName, 0, bestMotionTime);
            m_Animator.CrossFade(matchedMotionName, 0.1f);
            Debug.Log("Play matchedMotionName " + matchedMotionName);
        }
    }

    void ApplyExtraTurnRotation()
    {
        //help the character turn faster(this is in addition to root rotation in the animation)
        float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
        transform.Rotate(0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);
    }


    public void OnAnimatorMove()
    {
        // we implement this function to override the default root motion.
        // this allows us to modify the positional speed before it's applied.
        if (m_IsGrounded && Time.deltaTime > 0)
        {
            Vector3 v = (m_Animator.deltaPosition * m_MoveSpeedMultiplier) / Time.deltaTime;

            // we preserve the existing y part of the current velocity.
            //v.y = m_Rigidbody.velocity.y;
            //m_Rigidbody.velocity = v;
        }
    }
}