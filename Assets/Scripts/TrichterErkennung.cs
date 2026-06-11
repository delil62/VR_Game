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

    [Header("Zwischen-Belohnungen (Spawns)")]
    [SerializeField] GameObject m_SpawnNachFlasche1; 
    [SerializeField] GameObject m_SpawnNachFlasche2; 

    [Header("UI Anzeige")]
    public TextMeshProUGUI fortschrittsText;

    [Header("Fehler-Strafe")]
    [SerializeField] GameObject m_FalscheSpawFlasche; 

    public float staerke = 1.0f;
    
    private int aktuellerSchritt = 0;
    private bool raetselGeloest = false;

    private List<GameObject> bereitsAbgehakteFlaschen = new List<GameObject>();
    private float letzteErkennung = 0f;
    private float cooldown = 1.0f; 

    void Start()
    {
        if (fortschrittsText != null) fortschrittsText.text = "0/3";
        
        if (m_FalscheSpawFlasche != null) m_FalscheSpawFlasche.SetActive(false); 
        if (m_SpawnNachFlasche1 != null) m_SpawnNachFlasche1.SetActive(false);
        if (m_SpawnNachFlasche2 != null) m_SpawnNachFlasche2.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (raetselGeloest) return;

        if (Time.time < letzteErkennung + cooldown) return;

        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable gegriffenesItem = other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (gegriffenesItem == null) return; 

        GameObject erkannteFlasche = null;
        if (GehoertZurFlasche(other, m_Flasche1)) erkannteFlasche = m_Flasche1;
        else if (GehoertZurFlasche(other, m_Flasche2)) erkannteFlasche = m_Flasche2;
        else if (GehoertZurFlasche(other, m_Flasche3)) erkannteFlasche = m_Flasche3;

        if (erkannteFlasche != null && bereitsAbgehakteFlaschen.Contains(erkannteFlasche))
        {
            return; 
        }

        letzteErkennung = Time.time;

        bool istRichtigeFlasche = false;

        if (aktuellerSchritt == 0 && erkannteFlasche == m_Flasche1) istRichtigeFlasche = true;
        else if (aktuellerSchritt == 1 && erkannteFlasche == m_Flasche2) istRichtigeFlasche = true;
        else if (aktuellerSchritt == 2 && erkannteFlasche == m_Flasche3) istRichtigeFlasche = true;

        if (istRichtigeFlasche)
        {
            Vibriere(other);
            bereitsAbgehakteFlaschen.Add(erkannteFlasche); 
            aktuellerSchritt++;
            
            if (fortschrittsText != null)
            {
                fortschrittsText.text = aktuellerSchritt + "/3";
            }

            // --- HIER IST DIE NEUE MAGIE ---
            // Sobald du EINE Sache richtig machst, räumen wir alten Müll weg!
            if (m_FalscheSpawFlasche != null)
            {
                m_FalscheSpawFlasche.SetActive(false); 
            }
            // ---------------------------------

            if (aktuellerSchritt == 1)
            {
                if (m_SpawnNachFlasche1 != null) m_SpawnNachFlasche1.SetActive(true);
            }
            else if (aktuellerSchritt == 2)
            {
                if (m_SpawnNachFlasche1 != null) m_SpawnNachFlasche1.SetActive(false); 
                if (m_SpawnNachFlasche2 != null) m_SpawnNachFlasche2.SetActive(true);
            }

            if (aktuellerSchritt >= 3)
            {
                RaetselGeloest();
            }
        }
        else
        {
            Vibriere(other); 
            
            aktuellerSchritt = 0;
            bereitsAbgehakteFlaschen.Clear();
            
            if (fortschrittsText != null)
            {
                fortschrittsText.text = "0/3";
            }
            
            if (m_FalscheSpawFlasche != null)
            {
                m_FalscheSpawFlasche.SetActive(true);
            }

            if (m_SpawnNachFlasche1 != null) m_SpawnNachFlasche1.SetActive(false);
            if (m_SpawnNachFlasche2 != null) m_SpawnNachFlasche2.SetActive(false);
            
            Debug.Log("Falsches Objekt! Zähler auf 0, Straf-Flasche an, Belohnungen weg.");
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

        if (m_FalscheSpawFlasche != null) m_FalscheSpawFlasche.SetActive(false);
        if (m_SpawnNachFlasche1 != null) m_SpawnNachFlasche1.SetActive(false);
        if (m_SpawnNachFlasche2 != null) m_SpawnNachFlasche2.SetActive(false);
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