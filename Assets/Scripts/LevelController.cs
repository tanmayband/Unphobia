using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelController : MonoBehaviour
{
    [SerializeField]
    private GhostController ghost;
    [SerializeField]
    private DetectiveController detective;
    [SerializeField]
    private float detectiveEntersHouseAfter = 10;
    [SerializeField]
    private Slider fearBar;

    private CustomInput customInputActions;

    private void Awake()
    {
        customInputActions = new CustomInput();
        customInputActions.Admin.Enable();

        customInputActions.Admin.RestartLevel.performed += RestartLevel;
        customInputActions.Admin.MainMenu.performed += GoToMainMenu;
    }

    // Start is called before the first frame update
    void Start()
    {
        fearBar.value = 0;
        ghost.GhostDeathEvent += GhostDeath;
        detective.DetectiveEndEvent += DetectiveEnd;
        detective.DetectiveFearEvent += DetectiveFearUpdate;

        StartCoroutine(WaitBeforeStartingDetective());
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
            Debug.Log("boo GAME OVER");
            GameOver();
        }
        else
        {
            Debug.Log("LEVEL FINISHED!");
            LevelComplete();
        }
        
    }

    private void GameOver()
    {
        // show relevent game over screen

        ghost.GameOver();
        detective.GameOver();
    }

    private void LevelComplete()
    {
        // show level complete OR GoToNextLevel();
    }

    private void RestartLevel(InputAction.CallbackContext context)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoToNextLevel()
    {

    }

    private void GoToMainMenu(InputAction.CallbackContext context)
    {
        //SceneManager.LoadScene();
    }
}