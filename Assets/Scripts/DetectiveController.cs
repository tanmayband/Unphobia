using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DetectiveController : MonoBehaviour, IScareable
{
    [SerializeField]
    private NavMeshAgent navMeshAgent;
    [SerializeField]
    private DetectiveVisibility detectiveVisibility;
    [SerializeField]
    private DetectiveDestination houseEntrance;
    [SerializeField]
    private List<DetectiveDestination> investigationSpots;
    [SerializeField]
    private List<DetectiveDestination> hidingSpots;

    private DetectiveDestination currentDestination;
    private DETECTIVE_STATE currentState;
    private Coroutine TimeSpendCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        navMeshAgent.updateRotation = false;
		navMeshAgent.updateUpAxis = false;
        SetupAllDestinations();

        detectiveVisibility.DestinationFound += NewDestinationFound;

        GoToDestination(investigationSpots[0]);
    }

    // Update is called once per frame
    void Update()
    {
        // agent has reached a destination
        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.5f)
        {
            DestinationReached();
        }
    }

    private void SetupAllDestinations()
    {
        foreach (DetectiveDestination dest in investigationSpots)
            dest.SetupDestination();
        
        foreach (DetectiveDestination dest in hidingSpots)
            dest.SetupDestination();

        houseEntrance.SetupDestination();
    }

    private void SetDetectiveState(DETECTIVE_STATE newState)
    {
        currentState = newState;
        Debug.Log($"Now state: {currentState}");
    }

    private void GoToDestination(DetectiveDestination destination)
    {
        currentDestination = destination;
        navMeshAgent.destination = currentDestination.GetDestinationLocation();
    }

    private void DestinationReached()
    {
        switch (currentState)
        {
            case DETECTIVE_STATE.EXPLORING:
            case DETECTIVE_STATE.GOINGHIDING:
            {
                TimeSpendCoroutine = StartCoroutine(SpendTimeOnDestination());
                break;
            }
            case DETECTIVE_STATE.EXITING:
            {
                SetDetectiveState(DETECTIVE_STATE.GONE); 
                Debug.Log("boo GAME OVER");
                break;
            }
            case DETECTIVE_STATE.FLEEING:
            {
                Debug.Log("LEVEL FINISHED!");
                break;
            }
        }
    }

    IEnumerator SpendTimeOnDestination()
    {
        SetDetectiveState(currentDestination.GetDestinationState());
        while(currentDestination.secondsLeft > 0)
        {
            yield return new WaitForSeconds(1f);
            currentDestination.DestinationUpdate(1);
        }

        DestinationDone();
    }

    private void DestinationDone()
    {
        StopCoroutine(TimeSpendCoroutine);
        
        switch (currentState)
        {
            case DETECTIVE_STATE.INVESTIGATING:
            {
                investigationSpots.RemoveAt(0);
                GoToNextInvestigation();
                break;
            }
            case DETECTIVE_STATE.HIDING:
            {
                currentDestination.ResetDestination();
                GoToNextInvestigation();
                break;
            }
        }
    }

    private void GoToNextInvestigation()
    {
        if(investigationSpots.Count > 0)
        {
            SetDetectiveState(DETECTIVE_STATE.EXPLORING);
            GoToDestination(investigationSpots[0]);
        }
        else
        {
            // everything investigated
            SetDetectiveState(DETECTIVE_STATE.EXITING);
            GoToDestination(houseEntrance);
        }
    }

    private void GoToHidingSpot()
    {
        StopCoroutine(TimeSpendCoroutine);
        SetDetectiveState(DETECTIVE_STATE.GOINGHIDING);
        GoToDestination(GetClosestHidingSpot());
    }

    private DetectiveDestination GetClosestHidingSpot()
    {
        DetectiveDestination hidingSpot = hidingSpots[0];
        float distance = Vector3.Distance(transform.position, hidingSpot.GetDestinationLocation());
        for (int i = 1; i < hidingSpots.Count; i++)
        {
            float destDistance = Vector3.Distance(transform.position, hidingSpots[i].GetDestinationLocation());
            if(destDistance < distance)
            {
                distance = destDistance;
                hidingSpot = hidingSpots[i];
            }
        }
        return hidingSpot;
    }

    private void NewDestinationFound(DetectiveDestination destination)
    {
        if(destination.GetDestinationState() == DETECTIVE_STATE.HIDING && !hidingSpots.Contains(destination))
        {
            destination.SetupDestination();
            hidingSpots.Add(destination);
        }
    }

    // STATE RESPONSES
    public void Scare()
    {
        switch (currentState)
        {
            case DETECTIVE_STATE.EXPLORING:
            case DETECTIVE_STATE.INVESTIGATING:
            {
                Debug.Log("BOO!");
                GoToHidingSpot();
                break;
            }
        }
    }
}

public interface IScareable
{
    public void Scare();
}
