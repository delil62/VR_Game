using UnityEngine;

/// <summary>
/// Minimal countdown display that starts only when triggered by game logic.
/// </summary>
public class BombCountdownTimer : MonoBehaviour
{
    [SerializeField]
    float m_DurationSeconds = 300f;

    [SerializeField]
    TextMesh m_TimerText;

    [SerializeField]
    GameObject m_TutorialCanvas;

    // --- NUR DIESE ZWEI REALEN ERWEITERUNGEN FÜR DEN SOUND ---
    [Header("Audio Einstellungen")]
    [SerializeField] 
    AudioSource m_AudioSource; // Der Lautsprecher der Bombe

    [SerializeField] 
    AudioClip m_Voice1Min;     // Deine Sounddatei für "noch 1 Minute"

    bool m_Played60sWarning;   // Merkt sich, ob der Sound in dieser Runde schon lief
    // --------------------------------------------------------

    // NEU: HIER IST DAS FELD FÜR DEIN BILD
    [Header("Das Start-Bild")]
    [SerializeField] 
    GameObject m_StartBild; 
    // --------------------------------------------------------

    // NEU: HIER IST DAS FELD FÜR DEINEN GAME-OVER-SCREEN
    [Header("Der Verlierer-Bildschirm")]
    [SerializeField] 
    GameObject m_VerliererBildschirm; 
    // --------------------------------------------------------

    float m_RemainingSeconds;
    bool m_IsRunning;
    bool m_HasStarted;

    void Awake()
    {
        ResetTimer();
    }

    void Update()
    {
        if (!m_IsRunning)
            return;

        m_RemainingSeconds = Mathf.Max(0f, m_RemainingSeconds - Time.deltaTime);
        UpdateTimerText();

        // --- NUR DIESE LOGIK IST NEU: Spielt den Sound exakt bei 60 Sekunden ---
        int totalSeconds = Mathf.CeilToInt(m_RemainingSeconds);
        if (totalSeconds == 60 && !m_Played60sWarning)
        {
            m_Played60sWarning = true; // Verhindert, dass der Sound mehrfach abspielt
            if (m_AudioSource != null && m_Voice1Min != null)
            {
                m_AudioSource.PlayOneShot(m_Voice1Min);
            }
        }
        // ----------------------------------------------------------------------

        // HIER WURDE DIE LOGIK FÜR DEN VERLIERER-BILDSCHIRM ERWEITERT
        if (m_RemainingSeconds <= 0f)
        {
            m_IsRunning = false;

            if (m_VerliererBildschirm != null)
            {
                m_VerliererBildschirm.SetActive(true);
            }
        }
    }

    public void StartTimer()
    {
        if (m_HasStarted)
            return;

        m_HasStarted = true;
        m_IsRunning = true;

        if (m_TutorialCanvas != null)
        {
            m_TutorialCanvas.SetActive(false);
        }

        // NEU: HIER WIRD DAS BILD AUSGEBLENDET, SOBALD DER TIMER STARTET
        if (m_StartBild != null)
        {
            m_StartBild.SetActive(false);
        }
    }

    public void ResetTimer()
    {
        m_HasStarted = false;
        m_IsRunning = false;
        m_RemainingSeconds = Mathf.Max(0f, m_DurationSeconds);
        
        // Vergisst das Abspielen beim Reset, damit es in der nächsten Runde wieder geht
        m_Played60sWarning = false; 
        
        UpdateTimerText();
    }

    void UpdateTimerText()
    {
        if (m_TimerText == null)
            return;

        var totalSeconds = Mathf.CeilToInt(m_RemainingSeconds);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        m_TimerText.text = $"{minutes}:{seconds:00}";

        // Der Panik-Modus: Nur Rot ab 30 Sekunden
        if (totalSeconds <= 30)
        {
            m_TimerText.color = new Color(3f, 0f, 0f); 
        }
        else
        {
            m_TimerText.color = new Color(1f, 1f, 1f); 
        }
    }

    void OnValidate()
    {
        m_DurationSeconds = Mathf.Max(0f, m_DurationSeconds);

        if (!Application.isPlaying)
        {
            m_RemainingSeconds = m_DurationSeconds;
            UpdateTimerText();
        }
    }
    
    // Diesen Block unten ins Timer-Skript einfügen:
    public void TimerStoppen()
    {
        m_IsRunning = false; // BÄM! Das zieht den Stecker der Uhr.
        
        Debug.Log("Der Timer wurde erfolgreich gestoppt!");
    }
}