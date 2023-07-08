using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class DetectiveDestination : MonoBehaviour
{
    public float secondsToSpend;
    [SerializeField]
    private DETECTIVE_STATE stateWhenReached;
    [SerializeField]
    private TextMeshPro timeLeftText;

    public float secondsLeft {get; private set;}

    public void SetupDestination()
    {
        secondsLeft = secondsToSpend;
        timeLeftText.text = secondsLeft.ToString();
    }

    public Vector3 GetDestinationLocation()
    {
        return transform.position;
    }

    public DETECTIVE_STATE GetDestinationState()
    {
        return stateWhenReached;
    }

    public void DestinationUpdate(float secondsDone)
    {
        secondsLeft -= secondsDone;
        timeLeftText.text = secondsLeft.ToString();
    }

    public void ResetDestination()
    {
        SetupDestination();
    }
}

public enum DETECTIVE_STATE
{
    EXPLORING,
    INVESTIGATING,
    FROZEN,
    GOINGHIDING,
    HIDING,
    PURSUING,
    EXITING,
    FLEEING,
    GONE
}
