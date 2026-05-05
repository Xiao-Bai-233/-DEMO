using UnityEngine;
using TMPro;

public class EatFood : MonoBehaviour
{
    private int _foodCount;
    public TextMeshProUGUI foodText;

    private void Start()
    {
        _foodCount = 0;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Food")) return;

        _foodCount++;
        Destroy(collision.gameObject);
        foodText.text = ":" + _foodCount;
    }
}
