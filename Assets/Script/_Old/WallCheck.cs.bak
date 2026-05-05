using UnityEngine;

public class WallCheck : MonoBehaviour
{
    public bool inWall;
    private Animator anim;
    void Start()
    {
        anim = GetComponentInParent<Animator>();
    }


    void Update()
    {
        anim.SetBool("inWall", inWall);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Wall")
        {
            inWall = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Wall")
        {
            inWall = false;
        }
    }
}
