using UnityEngine;

public class PlatfoemMove : MonoBehaviour
{
    public Transform PosA, PosB;
    private Transform MovePos;
    [SerializeField] private float MoveSpeed;
    void Start()
    {
        MovePos = PosA;
    }

    
    void Update()
    {
        if (Vector2.Distance(transform.position, PosA.position) < 0.1f)
        {
            MovePos = PosB;
        }
        else if (Vector2.Distance(transform.position, PosB.position) < 0.1f)
        {
            MovePos = PosA;
        }
        transform.position = Vector2.MoveTowards(transform.position, MovePos.position, MoveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        collision.transform.parent = this.transform;
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        collision.transform.parent = null;
    }
}
