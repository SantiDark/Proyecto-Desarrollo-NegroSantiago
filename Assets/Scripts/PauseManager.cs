using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject pauseMenu; // Panel de pausa opcional

    bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        Time.timeScale = isPaused ? 0f : 1f;

        if (pauseMenu)
            pauseMenu.SetActive(isPaused);

        // opcional: manejar el cursor
        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
