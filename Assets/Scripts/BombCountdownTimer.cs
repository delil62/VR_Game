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

    // --- NEU: PLATZ FÜR DEIN TUTORIAL-CANVAS ---
    [SerializeField]
    GameObject m_TutorialCanvas;
    // -------------------------------------------

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

        if (m_RemainingSeconds <= 0f)
            m_IsRunning = false;
    }

    public void StartTimer()
    {
        if (m_HasStarted)
            return;

        m_HasStarted = true;
        m_IsRunning = true;

        // --- NEU: TUTORIAL AUTOMATISCH BEIM START AUSBLENDEN ---
        if (m_TutorialCanvas != null)
        {
            m_TutorialCanvas.SetActive(false);
        }
        // ------------------------------------------------------
    }

    public void ResetTimer()
    {
        m_HasStarted = false;
        m_IsRunning = false;
        m_RemainingSeconds = Mathf.Max(0f, m_DurationSeconds);
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

        // --- DER PANIK-MODUS (Nur Rot + Glow) ---
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
}