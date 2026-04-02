using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    private Animator anim;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerDead pd = collision.GetComponent<PlayerDead>();
            anim.SetTrigger("isTouch");
            pd.pos=transform.position;
        }
    }
}
