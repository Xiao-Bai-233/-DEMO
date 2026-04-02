using UnityEngine;

public class PlayerAnim : MonoBehaviour
{
    private enum Anim
    {
        Idle,
        Run,
        Jump,
        Fall,
        DoubleJump
    };
    
    private Anim state;
    private Animator anim;
    public int jumpCount=0;
    private PlayerMove playerMove;
    
    void Start()
    {
        anim = GetComponent<Animator>();
        playerMove = GetComponent<PlayerMove>();
    }
    
    void Update()
    {
        StateChange();

        anim.SetInteger("States", (int)state);

        if (Input.GetButtonDown("Jump"))
        {
            jumpCount++;
            if (jumpCount > 2)
            {
                jumpCount = 0;
            }
        }
        if (playerMove.isGround)
        {
            jumpCount = 0;
        }
    }

    private void StateChange()
    {
        if (playerMove.Xmove != 0&&playerMove.isGround)
        {
            state = Anim.Run;
        }
        else if(playerMove.Xmove == 0&&playerMove.isGround)
        {
            state = Anim.Idle;
        }
        
        if (jumpCount == 0&&playerMove.rb.linearVelocity.y > 2f)
        {
            state = Anim.Jump;
        }else if (jumpCount == 1&&playerMove.rb.linearVelocity.y > 0.2f)
        {
            state = Anim.DoubleJump;
        }else if (playerMove.rb.linearVelocity.y < -2f)
        {
            state = Anim.Fall;
        }
    }
}
