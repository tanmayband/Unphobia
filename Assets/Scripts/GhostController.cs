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
    private float scareCooldownTime = 20f;
    [SerializeField]
    private TextMeshPro scareCooldownText; 

    public delegate void GhostDeathDelegate();
    public event GhostDeathDelegate GhostDeathEvent;

    private CustomInput customInputActions;
    private IScareable scareableEntity;
    private bool scareEnabled = true;
    private Interactable currentInteractable;

    private void Awake()
    {
        customInputActions = new CustomInput();
        customInputActions.Ghost.Enable();

        customInputActions.Ghost.Scare.performed += CauseAScare;
    }

    // Start is called before the first frame update
    void Start()
    {
        scareCooldownText.text = $"Scare Timeout: {scareCooldownTime}";
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
            if(!interactable.OnCooldown())
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
            if(scareableEntity == scareable)
                scareableEntity = null;
        }
        else if(other.TryGetComponent(out Interactable interactable))
        {
            if(currentInteractable != null)
                currentInteractable.Unhighlight();
            currentInteractable = null;
        }
    }

    private void CauseAScare(InputAction.CallbackContext context)
    {
        if(scareEnabled)
        {
            scareableEntity?.Scare(10);
            StartCoroutine(ScareCooldown());
        }
    }

    IEnumerator ScareCooldown()
    {
        scareEnabled = false;
        float cooldown = scareCooldownTime;
        while(cooldown > 0)
        {
            cooldown -= 1;
            scareCooldownText.text = $"Scare Timeout: {cooldown}";
            yield return new WaitForSeconds(1);
        }
        scareEnabled = true;
    }

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
