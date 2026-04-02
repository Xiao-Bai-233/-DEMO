using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float smoothTime = 0.3f;
    public float xDistance;
    public float yDistance;
    void Start()
    {
        
    }


    void LateUpdate()
    {
        if (player != null)
        {
            if (transform.position != player.position)
            {
                Vector3 playerPos = new Vector3(player.position.x+xDistance, player.position.y+yDistance, transform.position.z);
                transform.position = Vector3.Lerp(transform.position, playerPos, Time.deltaTime * smoothTime);
            }
        }
    }

    void Update()
    {
        
    }
}
