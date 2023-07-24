using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableToggler : Interactable
{
    [SerializeField]
    private GameObject toggleObject;

    protected override void InitializeInteractable()
    {
        base.InitializeInteractable();
        toggleObject.SetActive(false);
    }

    public void ToggleOn(AudioClip audioClip)
    {
        PlayAudio(audioClip);
        toggleObject.SetActive(true);
    }

    public void ToggleOff(AudioClip audioClip)
    {
        PlayAudio(audioClip);
        toggleObject.SetActive(false);
    }
}
