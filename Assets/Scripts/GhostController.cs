using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GhostController : MonoBehaviour
{
    [SerializeField]
    private Rigidbody2D ghostBody;
    [SerializeField]
    private float ghostSpeed = 5f;

    private CustomInput customInputActions;
    private IScareable scareableEntity;

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
        scareableEntity?.Scare(10);
    }
}
