using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    [SerializeField]
    private float cooldownTime = 10f;
    [SerializeField]
    private ParticleSystem highlightFX;
    private bool onCooldown = false;

    private void Start()
    {
        Unhighlight();
    }

    public void Interact()
    {
        if(!onCooldown)
        {
            // play interaction

            // start cooldown
            StartCoroutine(InteractCooldown());
        }
    }

    public void Highlight()
    {
        if(!onCooldown)
        {
            highlightFX.gameObject.SetActive(true);
        }
    }

    public void Unhighlight()
    {
        highlightFX.gameObject.SetActive(false);
    }

    public bool OnCooldown()
    {
        return onCooldown;
    }

    IEnumerator InteractCooldown()
    {
        onCooldown = true;
        float timeLeft = cooldownTime;
        while(timeLeft > 0)
        {
            yield return new WaitForSeconds(1);
            timeLeft -= 1;
        }
        onCooldown = false;
    }
}
