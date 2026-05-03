using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Pulls a visual lever downward once and forwards the trigger to the existing light switch logic.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class LeverSwitchInteractable : MonoBehaviour
{
    [SerializeField]
    LightSwitchInteractable m_TargetSwitch;

    [SerializeField]
    Vector3 m_PulledLocalPositionOffset = new(-0.018f, -0.065f, 0f);

    [SerializeField]
    Vector3 m_PulledLocalEulerAngles = new(0f, 0f, 58f);

    [SerializeField]
    float m_PullDuration = 0.18f;

    XRSimpleInteractable m_Interactable;
    Vector3 m_InitialLocalPosition;
    Quaternion m_InitialLocalRotation;
    Coroutine m_AnimateRoutine;
    bool m_HasPulled;

    void Awake()
    {
        m_Interactable = GetComponent<XRSimpleInteractable>();
        m_InitialLocalPosition = transform.localPosition;
        m_InitialLocalRotation = transform.localRotation;
    }

    void OnEnable()
    {
        if (m_Interactable == null)
            m_Interactable = GetComponent<XRSimpleInteractable>();

        m_Interactable.selectEntered.AddListener(OnSelectEntered);
        m_Interactable.activated.AddListener(OnActivated);
        SyncStateFromTargetSwitch();
    }

    void OnDisable()
    {
        if (m_Interactable == null)
            return;

        m_Interactable.selectEntered.RemoveListener(OnSelectEntered);
        m_Interactable.activated.RemoveListener(OnActivated);
    }

    public void ResetLever()
    {
        m_HasPulled = false;
        SetPose(pulled: false);

        if (m_Interactable != null)
            m_Interactable.enabled = true;
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        TryPullLever();
    }

    void OnActivated(ActivateEventArgs args)
    {
        TryPullLever();
    }

    void TryPullLever()
    {
        if (m_HasPulled)
            return;

        m_HasPulled = true;

        if (m_AnimateRoutine != null)
            StopCoroutine(m_AnimateRoutine);

        m_AnimateRoutine = StartCoroutine(PullLeverDown());
    }

    IEnumerator PullLeverDown()
    {
        var startPosition = transform.localPosition;
        var startRotation = transform.localRotation;
        var endPosition = GetPulledLocalPosition();
        var endRotation = GetPulledLocalRotation();
        var duration = Mathf.Max(0.01f, m_PullDuration);
        var elapsed = 0f;

        // Keep the lever motion deterministic so the switch always finishes in the down pose.
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var easedT = Mathf.SmoothStep(0f, 1f, t);

            transform.localPosition = Vector3.Lerp(startPosition, endPosition, easedT);
            transform.localRotation = Quaternion.Slerp(startRotation, endRotation, easedT);
            yield return null;
        }

        SetPose(pulled: true);
        m_TargetSwitch?.ActivateSwitch();

        if (m_Interactable != null)
            m_Interactable.enabled = false;

        m_AnimateRoutine = null;
    }

    void SyncStateFromTargetSwitch()
    {
        if (m_TargetSwitch == null || !m_TargetSwitch.isOn)
            return;

        m_HasPulled = true;
        SetPose(pulled: true);

        if (m_Interactable != null)
            m_Interactable.enabled = false;
    }

    void SetPose(bool pulled)
    {
        transform.localPosition = pulled ? GetPulledLocalPosition() : m_InitialLocalPosition;
        transform.localRotation = pulled ? GetPulledLocalRotation() : m_InitialLocalRotation;
    }

    Vector3 GetPulledLocalPosition()
    {
        return m_InitialLocalPosition + m_PulledLocalPositionOffset;
    }

    Quaternion GetPulledLocalRotation()
    {
        return m_InitialLocalRotation * Quaternion.Euler(m_PulledLocalEulerAngles);
    }

    void OnValidate()
    {
        m_PullDuration = Mathf.Max(0.01f, m_PullDuration);
    }
}
