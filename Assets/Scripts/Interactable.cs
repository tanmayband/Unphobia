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
    private Animator attentionAnim;
    [SerializeField]
    private ParticleSystem highlightFX;
    [SerializeField]
    private Animator interactAnim;
    [SerializeField]
    private AudioSource audioSource;
    [SerializeField]
    private BetterCollider2D influenceSphere;
    private INTERACTABLE_STATE currentState = INTERACTABLE_STATE.IDLE;
    private IScareable scareableEntity;

    private void Start()
    {
        SetState(INTERACTABLE_STATE.IDLE);
        Unhighlight();
        influenceSphere.OnTriggerEnterEvent += OnInflunceEnter;
        influenceSphere.OnTriggerExitEvent += OnInflunceExit;
    }

    private void SetState(INTERACTABLE_STATE newState)
    {
        currentState = newState;
        // Debug.Log(currentState);
        ToggleAttentionFX(currentState == INTERACTABLE_STATE.IDLE);
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

    private void ToggleAttentionFX(bool show)
    {
        attentionAnim.gameObject.SetActive(show);
    }

    public bool IsAvailable()
    {
        return currentState == INTERACTABLE_STATE.IDLE;
    }

    IEnumerator InteractCooldown()
    {
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
        Debug.Log("StartEffect");
        SetState(INTERACTABLE_STATE.ONGOING);
        interactAnim.SetTrigger("TriggerInteractable");
    }

    public virtual void StopEffect()
    {
        Debug.Log("StopEffect");
        StartCoroutine(InteractCooldown());
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
