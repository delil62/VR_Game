using UnityEngine;
using UnityEngine.XR;

using System.Collections.Generic;

public class SaeureAufloesen : MonoBehaviour
{
    [Header("Was wird zum Auflösen gebraucht?")]
    public GameObject richtigesReagenzglas;

    [Header("Was soll verschwinden?")]
    public GameObject saeure;

    [Header("Zeit & Sound")]
    public float aufloesungsDauer = 2.0f; // Die perfekten 2 Sekunden!
    public AudioClip aufloeseSound;       // Dein Sound-Feld bleibt da

    private float m_Timer = 0.0f;
    private bool m_IstImBereich = false;
    private bool m_IstAufgeloest = false;
    private Collider m_GlasCollider;

    void Update()
    {
        // Wenn das Glas im Trigger gehalten wird
        if (m_IstImBereich && !m_IstAufgeloest)
        {
            m_Timer += Time.deltaTime;

            // Die Vibration startet direkt spürbar bei 40% (0.4f) und geht hoch auf 100% (1.0f)
            float aktuelleStaerke = Mathf.Lerp(0.4f, 1.0f, m_Timer / aufloesungsDauer);

            if (m_GlasCollider != null)
            {
                Vibriere(m_GlasCollider, aktuelleStaerke);
            }

            // Erst nach Ablauf der 3 Sekunden passiert das Finale
            if (m_Timer >= aufloesungsDauer)
            {
                m_IstAufgeloest = true;
                
                // Sound abspielen
                if (aufloeseSound != null)
                {
                    AudioSource.PlayClipAtPoint(aufloeseSound, transform.position);
                }

                // Säure verschwindet
                if (saeure != null)
                {
                    saeure.SetActive(false);
                }
                
                Debug.Log("Säure nach 3 Sekunden erfolgreich aufgelöst!");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (m_IstAufgeloest) return;

        if (other.gameObject == richtigesReagenzglas || other.transform.IsChildOf(richtigesReagenzglas.transform))
        {
            m_IstImBereich = true;
            m_GlasCollider = other;
            m_Timer = 0.0f; // Startet bei 0
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Wenn man die Hand wegzieht, bricht alles ab und setzt sich zurück
        if (other.gameObject == richtigesReagenzglas || other.transform.IsChildOf(richtigesReagenzglas.transform))
        {
            m_IstImBereich = false;
            m_GlasCollider = null;
            m_Timer = 0.0f;
        }
    }

    void Vibriere(Collider other, float staerke)
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
                if (aktiverController.isValid)
                {
                    // Kurze, knackige Impulse für das flüssige Anschwellen
                    aktiverController.SendHapticImpulse(0, staerke, 0.1f);
                }
            }
        }
    }
}