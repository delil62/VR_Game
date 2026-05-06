using UnityEngine;

public class BlinkingLight : MonoBehaviour
{
    [Header("Einstellungen")]
    public Light alarmLight;        // Ziehe hier dein rotes Point Light rein
    public float blinkRate = 0.5f;  // Geschwindigkeit des Blinkens
    public float activateAtTime = 60f; // Ab wie viel Sekunden Restzeit soll der Alarm starten?

    [Header("Timer Status")]
    public float currentTime = 300f; // Startwert (5 Min = 300s)
    public bool isCountdown = true; 

    private float blinkTimer;

    void Start()
    {
        // Sicherstellen, dass das Licht am Anfang aus ist, falls wir noch Zeit haben
        if (alarmLight != null)
        {
            UpdateLightStatus();
        }
    }

    void Update()
    {
        if (alarmLight == null) return;
        UpdateLightStatus();
    }

    void UpdateLightStatus()
    {
        bool shouldBlink = false;

        if (isCountdown)
        {
            if (currentTime <= activateAtTime && currentTime > 0)
            {
                shouldBlink = true;
            }
        }
        else
        {
            if (currentTime >= activateAtTime)
            {
                shouldBlink = true;
            }
        }

        if (shouldBlink)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkRate)
            {
                alarmLight.enabled = !alarmLight.enabled;
                blinkTimer = 0;
            }
        }
        else
        {
            // DAS HIER schaltet das rote Leuchten aus, solange die Zeit nicht erreicht ist
            alarmLight.enabled = false;
        }
    }
}