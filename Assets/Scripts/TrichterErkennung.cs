using UnityEngine;
using UnityEngine.XR;

using System.Collections.Generic;
using TMPro; // WICHTIG: Das erlaubt dem Skript, mit deinem TextMeshPro zu reden!

public class TrichterErkennung : MonoBehaviour
{
    [Header("Die Flaschen")]
    [SerializeField] GameObject m_Flasche1;
    [SerializeField] GameObject m_Flasche2;
    [SerializeField] GameObject m_Flasche3;

    [Header("Das Ergebnis")]
    [SerializeField] GameObject m_LeererBehaelter;
    [SerializeField] GameObject m_VollerBehaelter;
    [SerializeField] ParticleSystem m_RauchEffekt; 

    [Header("UI Anzeige")]
    public TextMeshProUGUI fortschrittsText; // Das ist der Platzhalter für dein Hologramm!

    public float staerke = 1.0f;
    private List<GameObject> m_EingefuellteFlaschen = new List<GameObject>();

    void Start()
    {
        // Setzt den Text am Anfang sicherheitshalber auf 0/3
        if (fortschrittsText != null) fortschrittsText.text = "0/3";
    }

    void OnTriggerEnter(Collider other)
    {
        GameObject erkannteFlasche = null;

        if (GehoertZurFlasche(other, m_Flasche1)) erkannteFlasche = m_Flasche1;
        else if (GehoertZurFlasche(other, m_Flasche2)) erkannteFlasche = m_Flasche2;
        else if (GehoertZurFlasche(other, m_Flasche3)) erkannteFlasche = m_Flasche3;

        if (erkannteFlasche != null)
        {
            Vibriere(other);

            if (!m_EingefuellteFlaschen.Contains(erkannteFlasche))
            {
                m_EingefuellteFlaschen.Add(erkannteFlasche);
                
                // HIER PASSIERT DIE MAGIE: Der Text wird live aktualisiert!
                if (fortschrittsText != null)
                {
                    fortschrittsText.text = m_EingefuellteFlaschen.Count + "/3";
                }

                if (m_EingefuellteFlaschen.Count >= 3)
                {
                    RaetselGeloest();
                }
            }
        }
    }

    void RaetselGeloest()
    {
        if (fortschrittsText != null)
        {
            fortschrittsText.text = "FERTIG!";
            fortschrittsText.color = new Color32(0, 110, 0, 255); // Wird beim Sieg grün
        }

        if (m_LeererBehaelter != null) m_LeererBehaelter.SetActive(false);
        if (m_VollerBehaelter != null) m_VollerBehaelter.SetActive(true);
        if (m_RauchEffekt != null) m_RauchEffekt.Play();
    }

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