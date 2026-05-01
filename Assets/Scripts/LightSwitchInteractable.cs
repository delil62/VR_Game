using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR;
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

    [SerializeField]
    bool m_EnableFallbackInput = true;

    [SerializeField]
    float m_FallbackActivationDistance = 2.2f;

    [SerializeField]
    float m_FallbackLookAngle = 28f;

    XRSimpleInteractable m_Interactable;
    Camera m_MainCamera;
    InputAction m_FallbackActivationAction;
    bool m_WasFallbackPressed;

    public bool isOn => m_IsOn;

    public UnityEvent onSwitchActivated => m_OnSwitchActivated;

    void Awake()
    {
        m_Interactable = GetComponent<XRSimpleInteractable>();
        m_MainCamera = Camera.main;
        CreateFallbackActivationAction();
    }

    void OnEnable()
    {
        if (m_Interactable == null)
            m_Interactable = GetComponent<XRSimpleInteractable>();

        m_Interactable.selectEntered.AddListener(OnSelectEntered);
        m_Interactable.activated.AddListener(OnActivated);
        m_FallbackActivationAction?.Enable();
    }

    void OnDisable()
    {
        if (m_Interactable == null)
            return;

        m_Interactable.selectEntered.RemoveListener(OnSelectEntered);
        m_Interactable.activated.RemoveListener(OnActivated);
        m_FallbackActivationAction?.Disable();
    }

    void OnDestroy()
    {
        m_FallbackActivationAction?.Dispose();
        m_FallbackActivationAction = null;
    }

    void Update()
    {
        if (!m_EnableFallbackInput || m_IsOn)
            return;

        var isPressed = IsFallbackActivationPressedByAction() || IsFallbackActivationPressed();
        if (!isPressed)
        {
            m_WasFallbackPressed = false;
            return;
        }

        if (m_WasFallbackPressed)
            return;

        m_WasFallbackPressed = true;

        if (IsUserAbleToFallbackActivate())
            ActivateSwitch();
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

    bool IsUserAbleToFallbackActivate()
    {
        if (m_MainCamera == null)
            m_MainCamera = Camera.main;

        if (m_MainCamera == null)
            return false;

        var cameraTransform = m_MainCamera.transform;
        var toSwitch = transform.position - cameraTransform.position;
        if (toSwitch.magnitude > m_FallbackActivationDistance)
            return false;

        return Vector3.Angle(cameraTransform.forward, toSwitch) <= m_FallbackLookAngle;
    }

    static bool IsFallbackActivationPressed()
    {
        return IsFallbackActivationPressed(XRNode.LeftHand) || IsFallbackActivationPressed(XRNode.RightHand);
    }

    static bool IsFallbackActivationPressed(XRNode node)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
            return false;

        return device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out var triggerPressed) && triggerPressed ||
               device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out var gripPressed) && gripPressed;
    }

    void CreateFallbackActivationAction()
    {
        m_FallbackActivationAction = new InputAction("Fallback Activate Switch", InputActionType.Button, expectedControlType: "Button");
        m_FallbackActivationAction.AddBinding("<XRController>{LeftHand}/{TriggerButton}");
        m_FallbackActivationAction.AddBinding("<XRController>{RightHand}/{TriggerButton}");
        m_FallbackActivationAction.AddBinding("<XRController>{LeftHand}/{GripButton}");
        m_FallbackActivationAction.AddBinding("<XRController>{RightHand}/{GripButton}");
    }

    bool IsFallbackActivationPressedByAction()
    {
        return m_FallbackActivationAction != null && m_FallbackActivationAction.enabled && m_FallbackActivationAction.IsPressed();
    }
}
