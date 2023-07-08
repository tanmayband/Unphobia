using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

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
    [SerializeField]
    private float detectiveFearCooldownRate = 3f;    // -1 fear every x seconds
    [SerializeField]
    private float detectiveResistanceCooldownRate = 1f; // -1 resistance every x seconds
    [SerializeField]
    private float freezeForSeconds = 5f;
    [SerializeField]
    private float hideForSeconds = 5f;

    [Header("Debug")]
    [SerializeField]
    private TextMeshPro fearText;
    [SerializeField]
    private TextMeshPro resistanceText;
    [SerializeField]
    private TextMeshPro stateText;

    private DetectiveDestination currentDestination;
    private DETECTIVE_STATE currentState;
    private Coroutine timeSpendCoroutine;
    private float detectiveFear = 0;    // 0-100 (slow climbing)
    private float detectiveResistance = 0;   // 0-100 (fast falling)
    private Coroutine fearCooldownCoroutine;
    private Coroutine resistanceCooldownCoroutine;
    private Coroutine frozenCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        navMeshAgent.updateRotation = false;
		navMeshAgent.updateUpAxis = false;
        SetupAllDestinations();

        detectiveVisibility.DestinationFound += NewDestinationFound;

        SetDetectiveState(DETECTIVE_STATE.EXPLORING);
        GoToDestination(investigationSpots[0]);
        
        // DEBUG:
        fearText.text = $"Fear: {Mathf.RoundToInt(detectiveFear)}";
        resistanceText.text = $"Resistance: {Mathf.RoundToInt(detectiveResistance)}";
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
        {
            dest.secondsToSpend = hideForSeconds;
            dest.SetupDestination();
        }

        houseEntrance.SetupDestination();
    }

    private void SetDetectiveState(DETECTIVE_STATE newState)
    {
        currentState = newState;
        Debug.Log($"Now state: {currentState}");
        stateText.text = $"State: {currentState}";
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
                timeSpendCoroutine = StartCoroutine(SpendTimeOnDestination());
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
        StopCoroutine(timeSpendCoroutine);
        
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

    // SCARE RESPONSES
    public void Scare(float fearAmount)
    {
        switch (currentState)
        {
            case DETECTIVE_STATE.EXPLORING:
            case DETECTIVE_STATE.INVESTIGATING:
            {
                UpdateFear(fearAmount);
                break;
            }
        }
    }

    private void UpdateFear(float fearAmount)
    {
        float fearDelta = fearAmount - (fearAmount * (detectiveResistance / 100));
        Debug.Log($"BOO with {fearDelta}");
        SetFear(fearDelta);
        if(detectiveResistance <= 0)
        {
            resistanceCooldownCoroutine = StartCoroutine(ResistanceCooldown());
        }
    }

    private void SetFear(float fearDelta)
    {
        detectiveFear += fearDelta;
        if(fearCooldownCoroutine == null)
        {
            fearCooldownCoroutine = StartCoroutine(FearCooldown());
        }

        if(fearDelta >= 10)
        {
            if(detectiveFear <= 30)
            {
                frozenCoroutine = StartCoroutine(FreezeCountdown());
            }
            else if(detectiveFear <= 60)
            {
                GoToHidingSpot();
            }
            else if(detectiveFear <= 90)
            {
                Debug.Log("ATTACK");
            }
            else
            {
                SetDetectiveState(DETECTIVE_STATE.FLEEING);
                GoToDestination(houseEntrance);
            }
        }
    }

    IEnumerator FearCooldown()
    {
        while(detectiveFear > 0)
        {
            detectiveFear -= 1;
            fearText.text = $"Fear: {detectiveFear.ToString("F2")}";
            yield return new WaitForSeconds(detectiveFearCooldownRate);
        }
        fearCooldownCoroutine = null;
    }

    IEnumerator ResistanceCooldown()
    {
        detectiveResistance = 100;
        while(detectiveResistance > 0)
        {
            detectiveResistance -= 1;
            resistanceText.text = $"Resistance: {detectiveResistance.ToString("F2")}"; // Mathf.RoundToInt
            yield return new WaitForSeconds(detectiveResistanceCooldownRate);
        }
        resistanceCooldownCoroutine = null;
    }

    private void GoToHidingSpot()
    {
        StopCoroutine(timeSpendCoroutine);
        SetDetectiveState(DETECTIVE_STATE.GOINGHIDING);
        GoToDestination(GetClosestHidingSpot());
    }

    IEnumerator FreezeCountdown()
    {
        StopCoroutine(timeSpendCoroutine);
        DETECTIVE_STATE previousState = currentState;
        SetDetectiveState(DETECTIVE_STATE.FROZEN);
        navMeshAgent.isStopped = true;

        float freezeForSecs = freezeForSeconds;
        while(freezeForSecs > 0)
        {
            freezeForSecs -= 1;
            yield return new WaitForSeconds(1);
        }

        navMeshAgent.isStopped = false;

        SetDetectiveState(previousState);
        switch (currentState)
        {
            // case DETECTIVE_STATE.EXPLORING:
            // {

            // }
            case DETECTIVE_STATE.INVESTIGATING:
            {
                timeSpendCoroutine = StartCoroutine(SpendTimeOnDestination());
                break;
            }
        }

        frozenCoroutine = null;
    }
}

public interface IScareable
{
    public void Scare(float fearAmount);
}
