using UnityEngine;

public class PlayerDead : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    public Vector2 pos;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        pos = transform.position;
    }
    
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("tarps"))
        {
            anim.SetTrigger("isDead");
            rb.bodyType = RigidbodyType2D.Static;
        }
    }

    private void Revive_1()
    {
        transform.position = pos;
        
    }
    
    private void Revive_2()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
    }
}
