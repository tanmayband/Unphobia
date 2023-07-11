using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GhostController : MonoBehaviour, IGhost
{
    [SerializeField]
    private Rigidbody2D ghostBody;
    [SerializeField]
    private float ghostSpeed = 5f;
    [SerializeField]
    private float booFearAmount = 10;
    [SerializeField]
    private int maxBoos = 2;
    // [SerializeField]
    // private float scareCooldownTime = 20f;
    [SerializeField]
    private TextMeshPro scareCooldownText;
    [SerializeField]
    private ParticleSystem booFX;

    public delegate void GhostDeathDelegate();
    public event GhostDeathDelegate GhostDeathEvent;

    private CustomInput customInputActions;
    private IScareable scareableEntity;
    // private bool booEnabled = true;
    private int currentBoos = 0;
    private Interactable currentInteractable;

    private void Awake()
    {
        customInputActions = new CustomInput();
        customInputActions.Ghost.Enable();

        customInputActions.Ghost.Scare.performed += Boo;
        customInputActions.Ghost.Interact.performed += Interact;
    }

    // Start is called before the first frame update
    void Start()
    {
        booFX.Stop();
        scareCooldownText.text = $"Boos left: {maxBoos - currentBoos}";
    }

    void Update()
    {
        Vector2 inputVector = customInputActions.Ghost.Movement.ReadValue<Vector2>();
        ghostBody.velocity = inputVector * ghostSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.TryGetComponent(out IScareable scareable))
        {
            scareableEntity = scareable;
        }
        else if(other.TryGetComponent(out Interactable interactable))
        {
            if(interactable.IsAvailable())
            {
                currentInteractable = interactable;
                currentInteractable.Highlight();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(other.TryGetComponent(out IScareable scareable))
        {
            scareableEntity = null;
        }
        else if(other.TryGetComponent(out Interactable interactable))
        {
            if(currentInteractable != null)
                currentInteractable.Unhighlight();
            currentInteractable = null;
        }
    }

    private void Boo(InputAction.CallbackContext context)
    {
        if(currentBoos < maxBoos)
        {
            scareableEntity?.Scare(booFearAmount);
            booFX.Play();
            currentBoos++;
            scareCooldownText.text = $"Boos left: {maxBoos - currentBoos}";
            // StartCoroutine(ScareCooldown());
        }
    }

    private void Interact(InputAction.CallbackContext context)
    {
        if(currentInteractable != null && currentInteractable.IsAvailable())
        {
            currentInteractable.Interact();
        }
    }

    // IEnumerator ScareCooldown()
    // {
    //     booEnabled = false;
    //     float cooldown = scareCooldownTime;
    //     while(cooldown > 0)
    //     {
    //         cooldown -= 1;
    //         scareCooldownText.text = $"Scare Timeout: {cooldown}";
    //         yield return new WaitForSeconds(1);
    //     }
    //     booEnabled = true;
    // }

    public void Kill()
    {
        GhostDeathEvent?.Invoke();
    }

    public Vector2 GetPosition()
    {
        return transform.position;
    } 

    public void GameOver()
    {
        customInputActions.Ghost.Disable();
    }
}

public interface IGhost
{
    public void Kill();
    public Vector2 GetPosition();
}
