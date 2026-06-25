using UnityEngine;

public class FestplattenManager : MonoBehaviour
{
    [Header("Die Steckdose an der Maschine")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor festplattenSocket;

    [Header("Die richtige Festplatte")]
    public GameObject richtigeFestplatte;

    [Header("Der Erfolgs-Sound (Optional)")]
    public AudioSource erfolgsSound;

    [Header("Die Verbindung zum Timer")]
    public BombCountdownTimer meinTimer; // Hier den echten Skriptnamen eintragen!

    // NEU: Hier kommt der Steckplatz für deinen Bildschirm hin
    [Header("Der Gewinner-Bildschirm")]
    public GameObject gewinnerBildschirm;

    private bool raetselGeloest = false;

    void Update()
    {
        // Wenn das Rätsel schon gelöst ist, muss der Code nicht jeden Frame weiterlaufen
        if (raetselGeloest == true) 
        {
            return; // WICHTIG: Das sagt dem Code, er soll hier aufhören!
        }

        // 1. Prüfen: Steckt überhaupt IRGENDWAS in dieser Dose?
        if (festplattenSocket.hasSelection == true)
        {
            // 2. Genaues Objekt abfragen: Welches Objekt steckt da drin?
            GameObject drinsteckendesObjekt = festplattenSocket.firstInteractableSelected.transform.gameObject;

            // 3. Vergleichen: Ist das exakt unsere Festplatte?
            if (drinsteckendesObjekt == richtigeFestplatte)
            {
                // GEWONNEN!
                raetselGeloest = true; 
                
                if (erfolgsSound != null)
                {
                    erfolgsSound.Play(); 
                }

                // Den Timer anhalten!
                if (meinTimer != null)
                {
                    meinTimer.TimerStoppen();
                }

                // NEU: Den Winning-Screen einschalten!
                if (gewinnerBildschirm != null)
                {
                    gewinnerBildschirm.SetActive(true);
                }

                Debug.Log("Festplatte ist richtig drin! Timer gestoppt und Winning Screen aktiv.");
            }
        }
    }
}