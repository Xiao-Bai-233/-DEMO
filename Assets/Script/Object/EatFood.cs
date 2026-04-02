using UnityEngine;
using TMPro;

public class EatFood : MonoBehaviour
{
    private int foodCount;
    public TextMeshProUGUI foodText;
    void Start()
    {
        foodCount = 0;
    }

    
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Food"))
        {
            foodCount++;
            Destroy(collision.gameObject);
            foodText.text = ":" +foodCount;
        }
    }
}
