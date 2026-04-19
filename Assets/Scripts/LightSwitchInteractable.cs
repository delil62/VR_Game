using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Minimal hook for a light switch that can later start escape-room systems.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class LightSwitchInteractable : MonoBehaviour
{
    [SerializeField]
    bool m_IsOn;

    [SerializeField]
    Light[] m_LightsToEnable = new Light[0];

    [SerializeField]
    UnityEvent m_OnSwitchActivated = new UnityEvent();

    XRSimpleInteractable m_Interactable;

    public bool isOn => m_IsOn;

    public UnityEvent onSwitchActivated => m_OnSwitchActivated;

    void Awake()
    {
        m_Interactable = GetComponent<XRSimpleInteractable>();
    }

    void OnEnable()
    {
        if (m_Interactable == null)
            m_Interactable = GetComponent<XRSimpleInteractable>();

        m_Interactable.selectEntered.AddListener(OnSelectEntered);
        m_Interactable.activated.AddListener(OnActivated);
    }

    void OnDisable()
    {
        if (m_Interactable == null)
            return;

        m_Interactable.selectEntered.RemoveListener(OnSelectEntered);
        m_Interactable.activated.RemoveListener(OnActivated);
    }

    public void ActivateSwitch()
    {
        if (m_IsOn)
            return;

        m_IsOn = true;
        SetLightsEnabled(true);
        m_OnSwitchActivated.Invoke();
    }

    public void ResetSwitch()
    {
        m_IsOn = false;
        SetLightsEnabled(false);
    }

    void SetLightsEnabled(bool isEnabled)
    {
        foreach (var lightToEnable in m_LightsToEnable)
        {
            if (lightToEnable == null)
                continue;

            lightToEnable.enabled = isEnabled;
        }
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        ActivateSwitch();
    }

    void OnActivated(ActivateEventArgs args)
    {
        ActivateSwitch();
    }
}
