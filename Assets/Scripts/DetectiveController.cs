using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DetectiveController : MonoBehaviour
{
    [SerializeField]
    private NavMeshAgent navMeshAgent;
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

        if(Input.GetKeyDown(KeyCode.H))
        {
            GoToHidingSpot();
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
                currentState = DETECTIVE_STATE.GONE; 
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
        currentState = currentDestination.GetDestinationState();
        Debug.Log($"Started count down on state {currentState}");
        while(currentDestination.secondsLeft > 0)
        {
            yield return new WaitForSeconds(1f);
            currentDestination.DestinationUpdate(1);
            Debug.Log($"Counting down 1s on state {currentState}");
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
            currentState = DETECTIVE_STATE.EXPLORING;
            GoToDestination(investigationSpots[0]);
        }
        else
        {
            // everything investigated
            currentState = DETECTIVE_STATE.EXITING;
            GoToDestination(houseEntrance);
        }
    }

    private void GoToHidingSpot()
    {
        StopCoroutine(TimeSpendCoroutine);
        currentState = DETECTIVE_STATE.GOINGHIDING;
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
}
