using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [SerializeField]
    private GhostController ghost;
    [SerializeField]
    private DetectiveController detective;

    // Start is called before the first frame update
    void Start()
    {
        ghost.GhostDeathEvent += GhostDeath;
        detective.DetectiveEndEvent += DetectiveEnd;
        detective.DetectiveFearEvent += DetectiveFearUpdate;
    }

    private void DetectiveFearUpdate(float newFear)
    {
        // update fear in UI
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
        // show level complete
    }
}