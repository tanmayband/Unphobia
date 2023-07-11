using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using ConstantUtils;

public class InvestigationDestination : DetectiveDestination
{
    [SerializeField]
    private Light2D spotlight;

    private void Start()
    {
        spotlight.gameObject.SetActive(true);
        SetInvestigationPending();
    }

    public override void DestinationReached()
    {
        spotlight.color = Constants.ColourInvestigationOngoing;
    }

    public override void DestinationInterrupted()
    {
        SetInvestigationPending();
    }

    public override void DestinationDone()
    {
        spotlight.gameObject.SetActive(false);
    }

    private void SetInvestigationPending()
    {
        spotlight.color = Constants.ColourInvestigationPending;
    }
}
