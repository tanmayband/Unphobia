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
    [SerializeField]
    private TextMeshPro fearAmountText;

    public delegate void DetectiveEndDelegate(bool succeeded);
    public event DetectiveEndDelegate DetectiveEndEvent;
    public delegate void DetectiveFearDelegate(float currentFear);
    public event DetectiveFearDelegate DetectiveFearEvent;

    private DetectiveDestination currentDestination;
    private DETECTIVE_STATE currentState;
    private DETECTIVE_STATE previousState;
    private Coroutine timeSpendCoroutine;
    private float detectiveFear = 0;    // 0-100 (slow climbing)
    private Coroutine fearCooldownCoroutine;
    private Coroutine frozenCoroutine;
    private DETECTIVE_FEAR_LEVEL currentFearLevel = DETECTIVE_FEAR_LEVEL.FREEZE;
    private IGhost ghostObject;
    private float pursuitWarmup = 5;

    // Start is called before the first frame update
    void Start()
    {
        navMeshAgent.updateRotation = false;
		navMeshAgent.updateUpAxis = false;
        SetupAllDestinations();

        detectiveVisibility.DestinationFound += NewDestinationFound;
        detectiveVisibility.KillableFound += NewKillableFound;

        SetDetectiveState(DETECTIVE_STATE.EXPLORING);
        GoToDestination(investigationSpots[0]);
        
        // DEBUG:
        fearText.text = $"Fear: {Mathf.RoundToInt(detectiveFear)}";
    }

    // Update is called once per frame
    void Update()
    {
        if(currentState != DETECTIVE_STATE.PURSUING)
        {
            // agent has reached a destination
            if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance < 0.5f)
            {
                DestinationReached();
            }
        }
        else
        {
            Pursuit();
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
        previousState = currentState;
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
                DetectiveEndEvent?.Invoke(true);
                break;
            }
            case DETECTIVE_STATE.FLEEING:
            {
                DetectiveEndEvent?.Invoke(false);
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

    private void NewKillableFound(IGhost killable)
    {
        ghostObject = killable;
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
        float fearDelta = fearAmount;
        SetFear(fearDelta);
    }

    private void SetFear(float fearDelta)
    {
        detectiveFear += fearDelta;
        fearText.text = $"Fear: {detectiveFear.ToString("F2")}";
        fearAmountText.text = $"Fear Amount: {fearDelta}";
        DetectiveFearEvent?.Invoke(detectiveFear);

        DETECTIVE_FEAR_LEVEL previousFearLevel = currentFearLevel;

        if(detectiveFear <= 30)
            currentFearLevel = DETECTIVE_FEAR_LEVEL.FREEZE;
        else if(detectiveFear <= 60)
            currentFearLevel = DETECTIVE_FEAR_LEVEL.HIDE;
        else if(detectiveFear <= 90)
            currentFearLevel = DETECTIVE_FEAR_LEVEL.ATTACK;
        else
            currentFearLevel = DETECTIVE_FEAR_LEVEL.FLEE;

        if(fearCooldownCoroutine == null)
        {
            fearCooldownCoroutine = StartCoroutine(FearCooldown());
        }

        ProcessFear();
    }

    private void ProcessFear()
    {
        switch (currentFearLevel)
        {
            case DETECTIVE_FEAR_LEVEL.FREEZE:
            {
                frozenCoroutine = StartCoroutine(FreezeCountdown());
                break;
            }
            case DETECTIVE_FEAR_LEVEL.HIDE:
            {
                GoToHidingSpot();
                break;
            }
            case DETECTIVE_FEAR_LEVEL.ATTACK:
            {
                pursuitWarmup = 5;
                StopCoroutine(timeSpendCoroutine);
                SetDetectiveState(DETECTIVE_STATE.PURSUING);
                break;
            }
            case DETECTIVE_FEAR_LEVEL.FLEE:
            {
                SetDetectiveState(DETECTIVE_STATE.FLEEING);
                GoToDestination(houseEntrance);
                break;
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

    private void GoToHidingSpot()
    {
        StopCoroutine(timeSpendCoroutine);
        SetDetectiveState(DETECTIVE_STATE.GOINGHIDING);
        GoToDestination(GetClosestHidingSpot());
    }

    IEnumerator FreezeCountdown()
    {
        if(timeSpendCoroutine != null)
            StopCoroutine(timeSpendCoroutine);
        SetDetectiveState(DETECTIVE_STATE.FROZEN);
        navMeshAgent.isStopped = true;

        float freezeForSecs = freezeForSeconds;
        while(freezeForSecs > 0)
        {
            freezeForSecs -= 1;
            yield return new WaitForSeconds(1);
        }

        navMeshAgent.isStopped = false;

        ResumeActivity();
        frozenCoroutine = null;
    }

    private void ResumeActivity()
    {
        SetDetectiveState(previousState);
        switch (currentState)
        {
            case DETECTIVE_STATE.EXPLORING:
            {
                GoToNextInvestigation();
                break;
            }
            case DETECTIVE_STATE.INVESTIGATING:
            {
                timeSpendCoroutine = StartCoroutine(SpendTimeOnDestination());
                break;
            }
        }
    }

    private void Pursuit()
    {
        navMeshAgent.destination = ghostObject.GetPosition();
        pursuitWarmup -= Time.deltaTime;
        
        if(navMeshAgent.remainingDistance < 2f)
        {
            if(pursuitWarmup <= 0)
            {
                if(Random.value > 0.5)
                    ghostObject.Kill();
            }
        }
        else if(navMeshAgent.remainingDistance >= 4f)
        {
            ResumeActivity();
        }
    }

    public void GameOver()
    {
        navMeshAgent.isStopped = true;
        StopAllCoroutines();
        enabled = false;
    }

}

public interface IScareable
{
    public void Scare(float fearAmount);
}
