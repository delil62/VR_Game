using UnityEngine;
using UnityEngine.XR;

using System.Collections.Generic;

public class TrichterErkennung : MonoBehaviour
{
    [Header("Die Flaschen")]
    [SerializeField] GameObject m_Flasche1;
    [SerializeField] GameObject m_Flasche2;
    [SerializeField] GameObject m_Flasche3;

    [Header("Das Ergebnis")]
    [SerializeField] GameObject m_LeererBehaelter;
    [SerializeField] GameObject m_VollerBehaelter;
    [SerializeField] ParticleSystem m_RauchEffekt; // Kann erstmal leer bleiben, falls du noch keinen hast

    public float staerke = 1.0f;

    // Unser Gedächtnis: Hier wandern die erkannten Flaschen rein
    private List<GameObject> m_EingefuellteFlaschen = new List<GameObject>();

    void OnTriggerEnter(Collider other)
    {
        GameObject erkannteFlasche = null;

        // Checken, welche der 3 Flaschen gerade den Trichter berührt
        if (GehoertZurFlasche(other, m_Flasche1)) erkannteFlasche = m_Flasche1;
        else if (GehoertZurFlasche(other, m_Flasche2)) erkannteFlasche = m_Flasche2;
        else if (GehoertZurFlasche(other, m_Flasche3)) erkannteFlasche = m_Flasche3;

        // Wenn es wirklich eine unserer Lösungs-Flaschen ist...
        if (erkannteFlasche != null)
        {
            // 1. Controller vibrieren lassen (wie vorher)
            Vibriere(other);

            // 2. Prüfen, ob wir genau DIESE Flasche schon eingefüllt haben
            if (!m_EingefuellteFlaschen.Contains(erkannteFlasche))
            {
                // Flasche ins Gedächtnis aufnehmen
                m_EingefuellteFlaschen.Add(erkannteFlasche);
                Debug.Log("Flasche hinzugefügt! Bisher drin: " + m_EingefuellteFlaschen.Count);

                // 3. Haben wir alle 3 zusammen?
                if (m_EingefuellteFlaschen.Count >= 3)
                {
                    RaetselGeloest();
                }
            }
        }
    }

    void RaetselGeloest()
    {
        Debug.Log("BINGO! ALLE 3 FLASCHEN SIND DRIN!");

        // Leeren Behälter ausschalten, vollen Behälter einschalten
        if (m_LeererBehaelter != null) m_LeererBehaelter.SetActive(false);
        if (m_VollerBehaelter != null) m_VollerBehaelter.SetActive(true);

        // Falls ein Rauch-Effekt zugewiesen ist, abspielen!
        if (m_RauchEffekt != null) m_RauchEffekt.Play();
    }

    // --- Deine funktionierenden Hilfsfunktionen von vorhin ---

    void Vibriere(Collider other)
    {
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabScript = other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabScript != null && grabScript.isSelected)
        {
            UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor = grabScript.firstInteractorSelecting;
            string handName = interactor.transform.name.ToLower();
            InputDeviceCharacteristics handEigenschaft = InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;
            if (handName.Contains("left")) handEigenschaft = InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;

            List<InputDevice> gefundeneController = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(handEigenschaft, gefundeneController);

            if (gefundeneController.Count > 0)
            {
                InputDevice aktiverController = gefundeneController[0];
                if (aktiverController.isValid) aktiverController.SendHapticImpulse(0, staerke, 0.5f);
            }
        }
    }

    bool GehoertZurFlasche(Collider beruehrtesTeil, GameObject richtigeFlasche)
    {
        if (richtigeFlasche == null) return false;
        return beruehrtesTeil.gameObject == richtigeFlasche || beruehrtesTeil.transform.IsChildOf(richtigeFlasche.transform);
    }
}