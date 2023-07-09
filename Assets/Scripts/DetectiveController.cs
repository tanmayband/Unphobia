using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;

public class DetectiveController : MonoBehaviour, IScareable
{
    [SerializeField]
    private NavMeshAgent navMeshAgent;
    [SerializeField]
    private DetectiveDestination houseEntrance;
    [SerializeField]
    private List<DetectiveDestination> investigationSpots;
    [SerializeField]
    private List<DetectiveDestination> hidingSpots;
    [SerializeField]
    private Slider fearBar;
    public float maxFear = 50f;
    [SerializeField]
    private float detectiveFearCooldownRate = 3f;    // -1 fear every x seconds
    [SerializeField]
    private float freezeForSeconds = 5f;
    [SerializeField]
    private float hideForSeconds = 5f;
    [SerializeField]
    private float gunSeconds = 5f;
    [SerializeField]
    private float walkSpeed = 2f;
    [SerializeField]
    private float runSpeed = 4f;
    [SerializeField]
    private BetterCollider2D visibiltySphere;
    [SerializeField]
    private AudioSource fearAudio;
    [SerializeField]
    private ParticleSystem attackFX;
    [SerializeField]
    private GameObject detectiveSprite;

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
    private DETECTIVE_STATE currentState = DETECTIVE_STATE.DISABLED;
    private DETECTIVE_STATE previousState;
    private Coroutine timeSpendCoroutine;
    private float detectiveFear = 0;    // 0-100 (slow falling)
    private Coroutine fearCooldownCoroutine;
    private Coroutine frozenCoroutine;
    private DETECTIVE_FEAR_LEVEL currentFearLevel = DETECTIVE_FEAR_LEVEL.FREEZE;
    private IGhost ghostObject;
    private float pursuitWarmup = 5;
    private bool pursuitWarmupDone;

    private void Awake()
    {
        navMeshAgent.updateRotation = false;
		navMeshAgent.updateUpAxis = false;
        attackFX.Stop();
        // fearBar.value = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        SetupAllDestinations();
        navMeshAgent.speed = walkSpeed;
        visibiltySphere.OnTriggerEnterEvent += DetectiveSees;
        
        // DEBUG:
        fearText.text = $"Fear: {Mathf.RoundToInt(detectiveFear)}";
    }

    // Update is called once per frame
    void Update()
    {
        if(currentState != DETECTIVE_STATE.DISABLED)
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

    public void EnterHouse()
    {
        SetDetectiveState(DETECTIVE_STATE.EXPLORING);
        GoToDestination(investigationSpots[0]);
    }

    private void SetDetectiveState(DETECTIVE_STATE newState)
    {
        previousState = currentState;
        currentState = newState;
        // Debug.Log($"Now state: {currentState}");
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
                StartSpendingTimeOnDestination();
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
            case DETECTIVE_STATE.HIDING:
            {
                detectiveSprite.SetActive(false);
                break;
            }
        }
    }

    private void StartSpendingTimeOnDestination()
    {
        SetDetectiveState(currentDestination.GetDestinationState());
        timeSpendCoroutine = StartCoroutine(SpendTimeOnDestination());
    }

    IEnumerator SpendTimeOnDestination()
    {
        while(currentDestination.secondsLeft > 0)
        {
            yield return new WaitForSeconds(1f);
            currentDestination.DestinationUpdate(1);
        }

        DestinationDone();
    }

    private void DestinationDone()
    {
        if(timeSpendCoroutine != null)
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
                detectiveSprite.SetActive(true);
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

    private void DetectiveSees(Collider2D other)
    {
        if(other.TryGetComponent(out DetectiveDestination destination))
        {
            NewDestinationFound(destination);
        }
        else if(other.TryGetComponent(out IGhost killable))
        {
            NewKillableFound(killable);
        }
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
        fearAudio.Play();
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
        // Debug.Log(detectiveFear);
        detectiveFear = Mathf.Clamp(detectiveFear + fearDelta, 0, maxFear);
        // fearBar.value = detectiveFear / maxFear;
        fearText.text = $"Fear: {detectiveFear.ToString("F2")}";
        fearAmountText.text = $"Fear Amount: {fearDelta}";
        DetectiveFearEvent?.Invoke(detectiveFear);

        DETECTIVE_FEAR_LEVEL previousFearLevel = currentFearLevel;

        if(detectiveFear <= maxFear / 3)
            currentFearLevel = DETECTIVE_FEAR_LEVEL.FREEZE;
        else if(detectiveFear <= maxFear / 6)
            currentFearLevel = DETECTIVE_FEAR_LEVEL.HIDE;
        else if(detectiveFear <= maxFear / 9)
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
                StartPursuit();
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
        navMeshAgent.speed = runSpeed;
        if(timeSpendCoroutine != null)
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
        navMeshAgent.speed = walkSpeed;
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
                GoToNextInvestigation();
                // StartSpendingTimeOnDestination();
                break;
            }
        }
    }

    private void StartPursuit()
    {
        navMeshAgent.speed = runSpeed;
        pursuitWarmup = 5;
        if(timeSpendCoroutine != null)
            StopCoroutine(timeSpendCoroutine);
        StartCoroutine(StartGun());
        SetDetectiveState(DETECTIVE_STATE.PURSUING);
    }

    private void Pursuit()
    {
        navMeshAgent.destination = ghostObject.GetPosition();
        
        if(navMeshAgent.remainingDistance < 4f)
        {
            if(pursuitWarmupDone && Random.value > 0.8)
                ghostObject.Kill();
        }
        else if(navMeshAgent.remainingDistance >= 5f)
        {
            StopPursuit();
        }
    }

    private void StopPursuit()
    {
        attackFX.Stop();
        pursuitWarmupDone = false;
        ResumeActivity();
    }

    IEnumerator StartGun()
    {
        attackFX.Play();

        float warmupTimeLeft = pursuitWarmup;
        while(warmupTimeLeft > 0)
        {
            yield return new WaitForSeconds(1);
            warmupTimeLeft--;
        }
        pursuitWarmupDone = true;

        float timeLeft = gunSeconds;
        while(timeLeft > 0)
        {
            yield return new WaitForSeconds(1);
            timeLeft--;
        }
        StopPursuit();
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
