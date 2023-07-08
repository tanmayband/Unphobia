using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectiveVisibility : MonoBehaviour
{
    public delegate void DestinationFoundDelegate(DetectiveDestination destination);
    public event DestinationFoundDelegate DestinationFound; 
    public delegate void KillableFoundDelegate(IGhost killable);
    public event KillableFoundDelegate KillableFound; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.TryGetComponent(out DetectiveDestination destination))
        {
            DestinationFound?.Invoke(destination);
        }
        else if(other.TryGetComponent(out IGhost killable))
        {
            KillableFound?.Invoke(killable);
        }
    }

    // private void OnTriggerExit2D(Collider2D other)
    // {
    //     if(other.TryGetComponent(out DetectiveDestination destination))
    //     {
    //         DestinationFound?.Invoke(destination);
    //     }
    // }
}
