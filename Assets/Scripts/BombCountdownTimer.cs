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
