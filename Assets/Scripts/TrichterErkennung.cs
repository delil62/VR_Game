using UnityEngine;
using UnityEngine.XR;

using System.Collections.Generic;
using TMPro;

public class TrichterErkennung : MonoBehaviour
{
    [Header("Die Flaschen (REIHENFOLGE IST WICHTIG!)")]
    [SerializeField] GameObject m_Flasche1; 
    [SerializeField] GameObject m_Flasche2; 
    [SerializeField] GameObject m_Flasche3; 

    [Header("Das Ergebnis")]
    [SerializeField] GameObject m_LeererBehaelter;
    [SerializeField] GameObject m_VollerBehaelter;
    [SerializeField] ParticleSystem m_RauchEffekt; 

    [Header("UI Anzeige")]
    public TextMeshProUGUI fortschrittsText;

    public float staerke = 1.0f;
    
    private int aktuellerSchritt = 0;
    private bool raetselGeloest = false;

    // NEU: Die beiden Schutzschilde gegen VR-Glitches!
    private List<GameObject> bereitsAbgehakteFlaschen = new List<GameObject>();
    private float letzteErkennung = 0f;
    private float cooldown = 1.0f; // 1 Sekunde Pause nach jeder Bewegung

    void Start()
    {
        if (fortschrittsText != null) fortschrittsText.text = "0/3";
    }

    void OnTriggerEnter(Collider other)
    {
        if (raetselGeloest) return;

        // SCHUTZ 1: Anti-Spam-Timer. Ist die letzte Aktion weniger als 1 Sek. her? -> Ignorieren!
        if (Time.time < letzteErkennung + cooldown) return;

        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable gegriffenesItem = other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (gegriffenesItem == null) return; 

        // Welche unserer 3 Flaschen wurde berührt?
        GameObject erkannteFlasche = null;
        if (GehoertZurFlasche(other, m_Flasche1)) erkannteFlasche = m_Flasche1;
        else if (GehoertZurFlasche(other, m_Flasche2)) erkannteFlasche = m_Flasche2;
        else if (GehoertZurFlasche(other, m_Flasche3)) erkannteFlasche = m_Flasche3;

        // SCHUTZ 2: Die VIP-Liste. Wurde diese Flasche schon erfolgreich benutzt? -> Ignorieren!
        if (erkannteFlasche != null && bereitsAbgehakteFlaschen.Contains(erkannteFlasche))
        {
            return; // Nix passiert, Flasche ist sicher verbucht!
        }

        // Ab hier gilt es! Der Timer für die nächste Sekunde Sperre startet:
        letzteErkennung = Time.time;

        bool istRichtigeFlasche = false;

        if (aktuellerSchritt == 0 && erkannteFlasche == m_Flasche1) istRichtigeFlasche = true;
        else if (aktuellerSchritt == 1 && erkannteFlasche == m_Flasche2) istRichtigeFlasche = true;
        else if (aktuellerSchritt == 2 && erkannteFlasche == m_Flasche3) istRichtigeFlasche = true;

        if (istRichtigeFlasche)
        {
            Vibriere(other);
            
            // Die Flasche auf die VIP-Liste setzen, damit sie nicht nochmal triggert
            bereitsAbgehakteFlaschen.Add(erkannteFlasche); 
            aktuellerSchritt++;
            
            if (fortschrittsText != null)
            {
                fortschrittsText.text = aktuellerSchritt + "/3";
            }

            if (aktuellerSchritt >= 3)
            {
                RaetselGeloest();
            }
        }
        else
        {
            // FALSCHES ITEM ODER FALSCHE REIHENFOLGE!
            Vibriere(other); 
            
            aktuellerSchritt = 0;
            bereitsAbgehakteFlaschen.Clear(); // VIP-Liste komplett löschen, man muss wieder bei 0 anfangen!
            
            if (fortschrittsText != null)
            {
                fortschrittsText.text = "0/3";
            }
            
            Debug.Log("Falsches Objekt! Zähler auf 0, Liste geleert.");
        }
    }

    void RaetselGeloest()
    {
        raetselGeloest = true;

        if (fortschrittsText != null)
        {
            fortschrittsText.text = "FERTIG!";
            fortschrittsText.color = new Color32(0, 120, 0, 255); 
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