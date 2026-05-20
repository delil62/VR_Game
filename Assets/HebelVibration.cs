using UnityEngine;
using UnityEngine.XR; 
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic; 

public class HebelVibration : MonoBehaviour
{
    public float staerke = 0.5f; 

    private bool wirdAnvisiert = false;
    private InputDevice aktiverController;

    void Start()
    {
        var interactables = GetComponents<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        foreach(var interactable in interactables)
        {
            // Wir hören jetzt auf "Laser trifft Objekt" und "Laser verlässt Objekt"
            interactable.hoverEntered.AddListener(Anvisieren);
            interactable.hoverExited.AddListener(Wegschauen);
        }
    }

    void Anvisieren(HoverEnterEventArgs args)
    {
        wirdAnvisiert = true; // Der Laser ist drauf!

        // Hardware-Suche (welche Hand hält den Laser?)
        string handName = args.interactorObject.transform.name.ToLower();
        InputDeviceCharacteristics handEigenschaft = InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
        
        if (handName.Contains("left"))
        {
            handEigenschaft = InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
        }

        List<InputDevice> gefundeneController = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(handEigenschaft, gefundeneController);

        if (gefundeneController.Count > 0)
        {
            aktiverController = gefundeneController[0];
        }
    }

    void Wegschauen(HoverExitEventArgs args)
    {
        wirdAnvisiert = false; // Der Laser ist weg!
        
        // Den Motor sofort stoppen, sobald der Laser das Objekt verlässt
        if (aktiverController.isValid)
        {
            aktiverController.StopHaptics();
        }
    }

    void Update()
    {
        // Solange der Laser draufzeigt, feuert Unity jeden Frame einen Mini-Stromstoß
        if (wirdAnvisiert && aktiverController.isValid)
        {
            aktiverController.SendHapticImpulse(0, staerke, Time.deltaTime);
        }
    }
}