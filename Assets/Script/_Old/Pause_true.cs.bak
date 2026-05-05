using UnityEngine;
using UnityEngine.UI;

public class Pause_true : MonoBehaviour
{
    private bool isPaused = false;
    [SerializeField]private Canvas pauseCanvas;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
        isPaused = true;
        pauseCanvas.gameObject.SetActive(true);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        isPaused = false;
        pauseCanvas.gameObject.SetActive(false);
    }
}