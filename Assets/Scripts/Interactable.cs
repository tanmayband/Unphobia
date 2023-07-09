using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    [SerializeField]
    private float fearAmount = 5f;
    [SerializeField]
    private float cooldownTime = 10f;
    [SerializeField]
    private bool onlyOnce = true;
    [SerializeField]
    private ParticleSystem highlightFX;
    [SerializeField]
    private AudioSource audioSource;
    [SerializeField]
    private Animation animationClip;
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private BetterCollider2D influenceSphere;
    private INTERACTABLE_STATE currentState = INTERACTABLE_STATE.IDLE;
    private IScareable scareableEntity;

    private void Start()
    {
        Unhighlight();
        influenceSphere.OnTriggerEnterEvent += OnInflunceEnter;
        influenceSphere.OnTriggerExitEvent += OnInflunceExit;
    }

    private void SetState(INTERACTABLE_STATE newState)
    {
        currentState = newState;
        // Debug.Log(currentState);
    }

    public void Interact()
    {
        if(currentState == INTERACTABLE_STATE.IDLE)
        {
            SetState(INTERACTABLE_STATE.TRIGGERED);
            // play interaction
            StartEffect();
            
            // trigger scare
            scareableEntity?.Scare(fearAmount);
        }
    }

    public void Highlight()
    {
        if(currentState == INTERACTABLE_STATE.IDLE)
        {
            highlightFX.gameObject.SetActive(true);
        }
    }

    public void Unhighlight()
    {
        highlightFX.gameObject.SetActive(false);
    }

    public bool IsAvailable()
    {
        return currentState == INTERACTABLE_STATE.IDLE;
    }

    IEnumerator InteractCooldown()
    {
        SetState(INTERACTABLE_STATE.ONGOING);
        yield return new WaitWhile(() => animationClip.isPlaying);

        SetState(INTERACTABLE_STATE.COOLDOWN);
        Unhighlight();
        float timeLeft = cooldownTime;
        while(timeLeft > 0)
        {
            yield return new WaitForSeconds(1);
            timeLeft -= 1;
        }

        if(!onlyOnce)
            SetState(INTERACTABLE_STATE.IDLE);
        else
            SetState(INTERACTABLE_STATE.FINISHED);
    }

    private void OnInflunceEnter(Collider2D other)
    {
        if(other.TryGetComponent(out IScareable scareable))
        {
            scareableEntity = scareable;
        }
    }

    private void OnInflunceExit(Collider2D other)
    {
        if(other.TryGetComponent(out IScareable scareable))
        {
            scareableEntity = null;
        }
    }

    public virtual void StartEffect()
    {
        // animator.Play()
        animationClip.Rewind();
        animationClip.Play();
        StartCoroutine(InteractCooldown());
    }

    public virtual void StopEffect()
    {

    }

    public void PlayAudio()
    {
        audioSource.Play();
    }

    public void StopAudio()
    {
        audioSource.Stop();
    }
}

public enum INTERACTABLE_STATE
{
    IDLE,
    TRIGGERED,
    ONGOING,
    COOLDOWN,
    FINISHED
}
