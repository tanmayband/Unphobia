using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GhostController : MonoBehaviour, IGhost
{
    [SerializeField]
    private Rigidbody2D ghostBody;
    [SerializeField]
    private float ghostSpeed = 5f;
    [SerializeField]
    private float scareCooldownTime = 5f; 

    private CustomInput customInputActions;
    private IScareable scareableEntity;
    private bool scareEnabled = true;
    

    private void Awake()
    {
        customInputActions = new CustomInput();
        customInputActions.Ghost.Enable();

        customInputActions.Ghost.Scare.performed += CauseAScare;
    }

    // Start is called before the first frame update
    void Start()
    {
        
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
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(other.TryGetComponent(out IScareable scareable))
        {
            if(scareableEntity == scareable)
                scareableEntity = null;
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
            yield return new WaitForSeconds(1);
        }
        scareEnabled = true;
    }

    public void Kill()
    {
        Debug.Log("ded GAME OVER");
    }

    public Vector2 GetPosition()
    {
        return transform.position;
    } 
}

public interface IGhost
{
    public void Kill();
    public Vector2 GetPosition();
}
