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

    private void Awake()
    {
        customInputActions = new CustomInput();
        customInputActions.Ghost.Enable();

        customInputActions.Ghost.Movement.performed += Move;
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

    public void Move(InputAction.CallbackContext context)
    {

    }
}
