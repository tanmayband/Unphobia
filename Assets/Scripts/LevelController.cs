using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ConstantUtils;
public class LevelController : MonoBehaviour
{
    [SerializeField]
    private GhostController ghost;
    [SerializeField]
    private DetectiveController detective;
    [SerializeField]
    private float detectiveEntersHouseAfter = 10;
    [SerializeField]
    private string nextLevelName;

    [Header("UI")]
    [SerializeField]
    private Slider fearBar;
    [SerializeField]
    private GameObject PauseScreen;
    [SerializeField]
    private GameObject LevelCompleteScreen;
    [SerializeField]
    private GameObject GameOverScreen;

    private CustomInput customInputActions;
    private bool isPaused;

    private void Awake()
    {
        customInputActions = new CustomInput();
        customInputActions.Admin.Enable();

        customInputActions.Admin.RestartLevel.performed += RestartLevel;
        customInputActions.Admin.PauseMenu.performed += PausePressed;
    }

    // Start is called before the first frame update
    void Start()
    {
        InitializeUI();
        ghost.GhostDeathEvent += GhostDeath;
        detective.DetectiveEndEvent += DetectiveEnd;
        detective.DetectiveFearEvent += DetectiveFearUpdate;

        StartCoroutine(WaitBeforeStartingDetective());
    }

    private void InitializeUI()
    {
        fearBar.value = 0;
        TogglePauseMenu(false);
        LevelCompleteScreen.SetActive(false);
        GameOverScreen.SetActive(false);
    }

    IEnumerator WaitBeforeStartingDetective()
    {
        float timeLeft = detectiveEntersHouseAfter;
        while(timeLeft > 0)
        {
            yield return new WaitForSeconds(1);
            timeLeft -= 1;
        }
        detective.EnterHouse();
    }

    private void DetectiveFearUpdate(float newFearRatio)
    {
        fearBar.value = newFearRatio;
    }

    private void GhostDeath()
    {
        Debug.Log("ded GAME OVER");
        GameOver();
    }

    private void DetectiveEnd(bool detectiveSucceeded)
    {
        if(detectiveSucceeded)
        {
            GameOver();
        }
        else
        {
            LevelComplete();
        }
        
    }

    private void GameOver()
    {
        GameOverScreen.SetActive(true);
        ghost.GameOver();
        detective.GameOver();
    }

    private void LevelComplete()
    {
        // LevelCompleteScreen.SetActive(true);
        GoToNextLevel();
    }

    private void RestartLevel(InputAction.CallbackContext context)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoToNextLevel()
    {
        SceneManager.LoadScene(nextLevelName);
    }

    private void PausePressed(InputAction.CallbackContext context)
    {
        TogglePauseMenu(!isPaused);
    }

    public void TogglePauseMenu(bool show)
    {
        isPaused = show;
        Time.timeScale = isPaused ? 0f : 1f;
        PauseScreen.SetActive(show);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(Constants.SCENE_MAIN_MENU);
    }
}