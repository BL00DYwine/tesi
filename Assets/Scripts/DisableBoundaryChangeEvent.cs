using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class DisableBoundaryChangeEvent : MonoBehaviour
{
    private XRInputSubsystem xrInputSubsystem;

    void Start()
    {
        // Trova l'XRInputSubsystem attivo
        var subsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetInstances(subsystems);

        if (subsystems.Count > 0)
        {
            xrInputSubsystem = subsystems[0];
            //xrInputSubsystem.boundaryChanged -= ;
        }
    }

    /*
    void DisableBoundaryEvent()
    {
        if (xrInputSubsystem != null)
        {
            // Rimuovi tutti i gestori dell'evento boundaryChanged
            xrInputSubsystem.boundaryChanged -= ;
        }
    }
    */
}
