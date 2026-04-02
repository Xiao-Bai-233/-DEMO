using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public Rigidbody2D rb;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpSpeed = 5f;
    [SerializeField]  private float WallJumpSpeed = 10f;
    [SerializeField] private float isGroundCheck;
    [SerializeField] private LayerMask GroundLayer;
    
    private Animator animator;
    private WallCheck wallCheck;
    
    public int jumpCount=0;
    public float Xmove;
    public bool isGround;
    private bool isRunScript;
    private bool isJump;
    private bool walljumping = false;
    
    public AudioSource jumpAudio;
    public AudioSource runAudio;
    void Start()
    {  
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        wallCheck = GetComponentInChildren<WallCheck>();
    }
    
    void Update()
    {
        X_Move();

        AnimatorController();
        
        RayCast();
        
        WallJump();
    }

    IEnumerator WallJumping()
    {
        walljumping = true;
        yield return new WaitForSeconds(0.1f);
        walljumping = false;
    }
    private void AnimatorController()//动画切换判定
    {
        isRunScript = Xmove != 0;
        animator.SetBool("isRun", isRunScript);
        if (rb.linearVelocity.y >2f)
        {
            isJump = true;
        }
        else
        {
            isJump = false;
        }
        animator.SetBool("isJump", isJump);
    }

    private void X_Move()//移动和跳跃
    {
        if (!walljumping)
        {
            Xmove = Input.GetAxis("Horizontal");
            if (Xmove != 0 && isGround)
            {
                if (!runAudio.isPlaying)
                {
                    runAudio.Play();
                }
            }
            else { runAudio.Stop(); }
            rb.linearVelocity = new Vector2(Xmove * moveSpeed, rb.linearVelocity.y);
        }
        
        if (Input.GetButtonDown("Jump")&& jumpCount>=0)
        {
            jumpCount++;
            if (jumpCount >= 1&&!wallCheck.inWall)
            {
                jumpCount = -1;
            }
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpSpeed);
            jumpAudio.Play();
        }
        
        if (isGround)
        {
            jumpCount = 0;
        }
        
        if (!isGround && !isJump)
        {
            rb.linearVelocity += new Vector2(0, Physics2D.gravity.y * Time.deltaTime);
        }
        
        if (Xmove > 0)
        {
            transform.localScale = new Vector2(1, 1);
        }
        else if (Xmove < 0)
        {
            transform.localScale = new Vector2(-1, 1);
        }
    }

    private void WallJump() //墙壁跳跃
    {
        if (wallCheck.inWall)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y*0.5f);
        }

        if (Input.GetButtonDown("Jump") && wallCheck.inWall&&!isGround)
        {
            rb.linearVelocity = new Vector2(WallJumpSpeed*transform.localScale.x*-1, jumpSpeed);
            StartCoroutine(WallJumping());
        }
    }
    
    private void OnDrawGizmos()//射线检测的具体实现
    {
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y - isGroundCheck));
    }
    
    private void RayCast()//射线检测
    {
        isGround = Physics2D.Raycast(transform.position,Vector2.down,isGroundCheck,GroundLayer);
        animator.SetBool("isGround", isGround);
        
    }
}
