using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BetterCollider2D : MonoBehaviour
{
    private Collider2D Collider;
    public event Action<Collider2D> OnTriggerEnterEvent;
    public event Action<Collider2D> OnTriggerExitEvent;

    void Awake()
    {
        Collider = GetComponent<Collider2D>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        OnTriggerEnterEvent?.Invoke(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        OnTriggerExitEvent?.Invoke(other);
    }

    public void ClearEventHandlers()
    {
        OnTriggerEnterEvent = null;
        OnTriggerExitEvent = null;
    }
}
