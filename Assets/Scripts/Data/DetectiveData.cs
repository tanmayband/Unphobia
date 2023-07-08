using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DetectiveDestination
{
    [SerializeField]
    private Transform location;
    [SerializeField]
    private float secondsToSpend;
    [SerializeField]
    private DETECTIVE_STATE stateWhenReached;
    public float secondsLeft {get; private set;}

    public void SetupDestination()
    {
        secondsLeft = secondsToSpend;
    }

    public Vector3 GetDestinationLocation()
    {
        return location.position;
    }

    public DETECTIVE_STATE GetDestinationState()
    {
        return stateWhenReached;
    }

    public void DestinationUpdate(float secondsDone)
    {
        secondsLeft -= secondsDone;
    }
}

public enum DETECTIVE_STATE
{
    EXPLORING,
    INVESTIGATING,
    HIDING,
    PURSUING,
    EXITING,
    FLEEING,
    GONE
}
