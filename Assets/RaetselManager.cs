using UnityEngine;


public class RaetselManager : MonoBehaviour
{
    [Header("Die 6 Steckdosen an der Wand")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor[] steckdosen;

    [Header("Die 6 passenden Papierschnipsel")]
    public GameObject[] richtigeSchnipsel;

    [Header("Der Erfolgs-Sound")]
    public AudioSource erfolgsSound;

    private bool raetselGeloest = false;

    void Update()
    {
        if (raetselGeloest == true) 
        {
            return;
        }

        int richtigeTreffer = 0;

        for (int i = 0; i < 6; i++)
        {
            // 1. Prüfen: Steckt überhaupt IRGENDWAS in dieser Dose?
            if (steckdosen[i].hasSelection == true)
            {
                // 2. Genaues Objekt abfragen: Welches Objekt steckt da drin?
                GameObject drinsteckendesObjekt = steckdosen[i].firstInteractableSelected.transform.gameObject;

                // 3. Vergleichen: Ist das exakt der Papierschnipsel, der in diese Dose gehört?
                if (drinsteckendesObjekt == richtigeSchnipsel[i])
                {
                    richtigeTreffer++;
                }
            }
        }

        // Wenn alle 6 perfekten Treffer sitzen -> GEWONNEN!
        if (richtigeTreffer == 6)
        {
            raetselGeloest = true; 
            
            if (erfolgsSound != null)
            {
                erfolgsSound.Play(); 
            }
        }
    }
}