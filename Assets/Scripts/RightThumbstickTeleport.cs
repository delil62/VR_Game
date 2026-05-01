using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class RightThumbstickTeleport : MonoBehaviour
{
    [SerializeField]
    XROrigin m_XROrigin;

    [SerializeField]
    XRRayInteractor m_RightRayInteractor;

    [SerializeField]
    XRDirectInteractor m_RightDirectInteractor;

    [SerializeField]
    ControllerPointerLineVisualizer m_ControllerPointerLineVisualizer;

    [SerializeField]
    Collider[] m_TeleportSurfaces;

    [SerializeField]
    float m_ActivateThreshold = 0.7f;

    [SerializeField]
    float m_ReleaseThreshold = 0.25f;

    [SerializeField]
    float m_MaxSurfaceAngle = 35f;

    [SerializeField]
    bool m_RequireForwardStick = true;

    [SerializeField]
    float m_SnapTurnThreshold = 0.75f;

    [SerializeField]
    float m_SnapTurnReleaseThreshold = 0.35f;

    [SerializeField]
    float m_SnapTurnAmount = 45f;

    [SerializeField]
    float m_RaycastDistance = 30f;

    [SerializeField]
    GameObject m_TeleportReticlePrefab;

    [SerializeField]
    float m_InputKeepAliveInterval = 2f;

    [SerializeField]
    float m_InputRecoveryCooldown = 0.5f;

    readonly HashSet<Collider> m_SurfaceSet = new();
    static readonly List<UnityEngine.XR.InputDevice> s_InputDevices = new();
    bool m_IsAiming;
    bool m_IsSnapTurnHeld;
    float m_AimingYawOffset;
    float m_NextInputKeepAliveTime;
    float m_NextForcedInputRecoveryTime;
    bool m_WasLeftControllerTracked;
    bool m_WasRightControllerTracked;
    Coroutine m_InputWarmupRoutine;
    InputAction m_RightThumbstickAction;
    GameObject m_TeleportReticleInstance;
    Behaviour[] m_RightRayLineVisuals;
    LineRenderer m_RightRayLineRenderer;

    void Awake()
    {
        RebuildSurfaceCache();
        CreateRightThumbstickAction();
        CacheRightRayVisualComponents();
        CreateTeleportReticle();
        SetTeleportAiming(false);
    }

    void OnEnable()
    {
        InputSystem.onDeviceChange += OnInputSystemDeviceChange;
        InputTracking.trackingAcquired += OnTrackingAcquired;
        m_RightThumbstickAction?.Enable();
        RefreshQuestInput();
        RestartInputWarmup();
        SetTeleportAiming(false);
    }

    void Start()
    {
        RefreshQuestInput();
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnInputSystemDeviceChange;
        InputTracking.trackingAcquired -= OnTrackingAcquired;
        m_RightThumbstickAction?.Disable();
        StopInputWarmup();
        SetTeleportAiming(false);
    }

    void OnDestroy()
    {
        m_RightThumbstickAction?.Dispose();
        m_RightThumbstickAction = null;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            RefreshQuestInput();
            RestartInputWarmup();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            RefreshQuestInput();
            RestartInputWarmup();
        }
    }

    void Update()
    {
        UpdateControllerWakeRecovery();
        UpdateInputKeepAlive();

        var stick = ReadThumbstick();
        var stickMagnitude = stick.magnitude;
        var wantsAim = stickMagnitude >= m_ActivateThreshold;

        if (m_RequireForwardStick)
            wantsAim &= stick.y > Mathf.Abs(stick.x);

        if (!m_IsAiming)
        {
            HandleSnapTurn(stick);

            if (wantsAim)
            {
                m_AimingYawOffset = 0f;
                SetTeleportAiming(true);
            }

            return;
        }

        UpdateAimingRotation(stick);
        UpdateTeleportPreview();

        var released = stickMagnitude <= m_ReleaseThreshold;

        if (!released)
            return;

        TryTeleport();
        SetTeleportAiming(false);
    }

    void RebuildSurfaceCache()
    {
        m_SurfaceSet.Clear();
        if (m_TeleportSurfaces == null)
            return;

        foreach (var surface in m_TeleportSurfaces)
        {
            if (surface != null)
                m_SurfaceSet.Add(surface);
        }
    }

    Vector2 ReadThumbstick()
    {
        var actionStick = m_RightThumbstickAction != null && m_RightThumbstickAction.enabled
            ? m_RightThumbstickAction.ReadValue<Vector2>()
            : Vector2.zero;

        var deviceStick = ReadThumbstickFromXRDevice();
        return actionStick.sqrMagnitude >= deviceStick.sqrMagnitude ? actionStick : deviceStick;
    }

    Vector2 ReadThumbstickFromXRDevice()
    {
        var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!device.isValid)
            return Vector2.zero;

        return device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out var stick)
            ? stick
            : Vector2.zero;
    }

    void RefreshQuestInput(bool forceRebind = false)
    {
        if (forceRebind)
            ForceInputSystemDeviceRefresh();

        InputDevices.GetDevices(s_InputDevices);
        ProbeControllerInput(XRNode.LeftHand);
        ProbeControllerInput(XRNode.RightHand);

        foreach (var actionAsset in Resources.FindObjectsOfTypeAll<InputActionAsset>())
        {
            if (forceRebind)
                actionAsset?.Disable();

            actionAsset?.Enable();
        }

        if (forceRebind && m_RightThumbstickAction != null)
        {
            m_RightThumbstickAction.Disable();
            m_RightThumbstickAction.Enable();
        }
    }

    void UpdateInputKeepAlive()
    {
        if (m_InputKeepAliveInterval <= 0f || Time.unscaledTime < m_NextInputKeepAliveTime)
            return;

        m_NextInputKeepAliveTime = Time.unscaledTime + m_InputKeepAliveInterval;
        RefreshQuestInput();
    }

    static void ProbeControllerInput(XRNode node)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
            return;

        device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 _);
        device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool _);
        device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool _);
    }

    void CreateRightThumbstickAction()
    {
        m_RightThumbstickAction = new InputAction("Right Thumbstick", InputActionType.Value, expectedControlType: "Vector2");
        m_RightThumbstickAction.AddBinding("<XRController>{RightHand}/{Primary2DAxis}");
    }

    void UpdateControllerWakeRecovery()
    {
        UpdateControllerWakeRecovery(XRNode.LeftHand, ref m_WasLeftControllerTracked);
        UpdateControllerWakeRecovery(XRNode.RightHand, ref m_WasRightControllerTracked);
    }

    void UpdateControllerWakeRecovery(XRNode node, ref bool wasTracked)
    {
        var isTracked = IsControllerTracked(node);
        if (isTracked && !wasTracked)
            ForceQuestInputRecovery();

        wasTracked = isTracked;
    }

    static bool IsControllerTracked(XRNode node)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
            return false;

        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out var isTracked))
            return isTracked;

        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trackingState, out InputTrackingState trackingState))
            return (trackingState & (InputTrackingState.Position | InputTrackingState.Rotation)) != 0;

        return true;
    }

    void ForceQuestInputRecovery()
    {
        if (Time.unscaledTime < m_NextForcedInputRecoveryTime)
            return;

        m_NextForcedInputRecoveryTime = Time.unscaledTime + m_InputRecoveryCooldown;
        RefreshQuestInput(true);
        RestartInputWarmup();
    }

    static void ForceInputSystemDeviceRefresh()
    {
        foreach (var device in InputSystem.devices)
        {
            if (!IsXRInputSystemDevice(device))
                continue;

            if (!device.enabled)
                InputSystem.EnableDevice(device);

            InputSystem.QueueConfigChangeEvent(device);
            InputSystem.ResetDevice(device);
        }
    }

    static bool IsXRInputSystemDevice(UnityEngine.InputSystem.InputDevice device)
    {
        return ContainsIgnoreCase(device.layout, "XR") ||
               ContainsIgnoreCase(device.layout, "Quest") ||
               ContainsIgnoreCase(device.layout, "Oculus") ||
               ContainsIgnoreCase(device.description.interfaceName, "XR") ||
               ContainsIgnoreCase(device.description.product, "Quest") ||
               ContainsIgnoreCase(device.description.product, "Oculus") ||
               ContainsIgnoreCase(device.description.manufacturer, "Meta") ||
               ContainsIgnoreCase(device.description.manufacturer, "Oculus");
    }

    static bool ContainsIgnoreCase(string value, string search)
    {
        return !string.IsNullOrEmpty(value) &&
               value.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    void RestartInputWarmup()
    {
        StopInputWarmup();
        m_InputWarmupRoutine = StartCoroutine(InputWarmup());
    }

    void StopInputWarmup()
    {
        if (m_InputWarmupRoutine == null)
            return;

        StopCoroutine(m_InputWarmupRoutine);
        m_InputWarmupRoutine = null;
    }

    IEnumerator InputWarmup()
    {
        var endTime = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < endTime)
        {
            RefreshQuestInput();
            yield return new WaitForSecondsRealtime(0.25f);
        }

        m_InputWarmupRoutine = null;
    }

    void OnInputSystemDeviceChange(UnityEngine.InputSystem.InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added ||
            change == InputDeviceChange.Reconnected ||
            change == InputDeviceChange.Enabled ||
            change == InputDeviceChange.ConfigurationChanged ||
            change == InputDeviceChange.UsageChanged)
        {
            ForceQuestInputRecovery();
        }
    }

    void OnTrackingAcquired(XRNodeState nodeState)
    {
        if (nodeState.nodeType != XRNode.LeftHand && nodeState.nodeType != XRNode.RightHand)
            return;

        ForceQuestInputRecovery();
    }

    void HandleSnapTurn(Vector2 stick)
    {
        if (Mathf.Abs(stick.x) <= m_SnapTurnReleaseThreshold)
            m_IsSnapTurnHeld = false;

        if (m_IsSnapTurnHeld)
            return;

        if (Mathf.Abs(stick.x) < m_SnapTurnThreshold || Mathf.Abs(stick.x) <= Mathf.Abs(stick.y))
            return;

        var direction = stick.x > 0f ? 1f : -1f;
        SnapTurn(direction * m_SnapTurnAmount);
        m_IsSnapTurnHeld = true;
    }

    void SetTeleportAiming(bool enabled)
    {
        m_IsAiming = enabled;

        if (m_RightRayInteractor != null)
            m_RightRayInteractor.gameObject.SetActive(enabled);

        SetRightRayVisualsVisible(false);

        if (m_RightDirectInteractor != null)
            m_RightDirectInteractor.enabled = !enabled;

        if (m_ControllerPointerLineVisualizer != null)
        {
            m_ControllerPointerLineVisualizer.SetVisible(!enabled);
            m_ControllerPointerLineVisualizer.SetRightTeleportMode(enabled);
        }

        SetTeleportReticleVisible(false);
    }

    void CacheRightRayVisualComponents()
    {
        if (m_RightRayInteractor == null)
            return;

        m_RightRayLineRenderer = m_RightRayInteractor.GetComponent<LineRenderer>();

        var behaviours = m_RightRayInteractor.GetComponents<Behaviour>();
        var lineVisuals = new List<Behaviour>();
        foreach (var behaviour in behaviours)
        {
            if (behaviour != null && behaviour.GetType().Name.Contains("LineVisual"))
                lineVisuals.Add(behaviour);
        }

        m_RightRayLineVisuals = lineVisuals.ToArray();
    }

    void SetRightRayVisualsVisible(bool visible)
    {
        if (m_RightRayLineRenderer != null)
            m_RightRayLineRenderer.enabled = visible;

        if (m_RightRayLineVisuals == null)
            return;

        foreach (var lineVisual in m_RightRayLineVisuals)
        {
            if (lineVisual != null)
                lineVisual.enabled = visible;
        }
    }

    void TryTeleport()
    {
        if (!TryGetTeleportHit(out var hit))
            return;

        var teleportAnchor = hit.collider != null ? hit.collider.GetComponentInParent<TeleportationAnchor>() : null;
        if (teleportAnchor != null)
        {
            TeleportToAnchor(teleportAnchor);
            return;
        }

        if (!IsValidSurface(hit))
            return;

        TeleportTo(hit.point);
        RotateOriginTo(GetTargetRotation(hit, null));
    }

    bool TryGetTeleportHit(out RaycastHit hit)
    {
        hit = default;

        if (m_RightRayInteractor != null && m_RightRayInteractor.TryGetCurrent3DRaycastHit(out hit))
            return true;

        var rayTransform = m_RightRayInteractor != null ? m_RightRayInteractor.transform : null;
        if (rayTransform == null)
            return false;

        return Physics.Raycast(rayTransform.position, rayTransform.forward, out hit, m_RaycastDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
    }

    bool IsValidSurface(RaycastHit hit)
    {
        var maxAngle = Mathf.Clamp(m_MaxSurfaceAngle, 0f, 89f);
        if (Vector3.Angle(hit.normal, Vector3.up) > maxAngle)
            return false;

        if (m_SurfaceSet.Count == 0)
            return true;

        var current = hit.collider != null ? hit.collider.transform : null;
        while (current != null)
        {
            if (current.TryGetComponent<Collider>(out var currentCollider) && m_SurfaceSet.Contains(currentCollider))
                return true;

            current = current.parent;
        }

        return hit.collider != null && m_SurfaceSet.Contains(hit.collider);
    }

    void TeleportTo(Vector3 targetPosition)
    {
        if (m_XROrigin == null)
        {
            transform.position = targetPosition;
            return;
        }

        var originTransform = m_XROrigin.transform;
        var cameraTransform = m_XROrigin.Camera != null ? m_XROrigin.Camera.transform : null;
        var currentGroundPosition = cameraTransform != null
            ? new Vector3(cameraTransform.position.x, originTransform.position.y, cameraTransform.position.z)
            : originTransform.position;

        originTransform.position += targetPosition - currentGroundPosition;
    }

    void TeleportToAnchor(TeleportationAnchor anchor)
    {
        if (anchor == null)
            return;

        var targetTransform = anchor.teleportAnchorTransform != null ? anchor.teleportAnchorTransform : anchor.transform;
        TeleportTo(targetTransform.position);
        RotateOriginTo(targetTransform.rotation * Quaternion.Euler(0f, m_AimingYawOffset, 0f));
    }

    void UpdateAimingRotation(Vector2 stick)
    {
        if (stick.magnitude > m_ReleaseThreshold)
            m_AimingYawOffset = Mathf.Atan2(stick.x, stick.y) * Mathf.Rad2Deg;
    }

    void RotateOriginTo(Quaternion targetRotation)
    {
        var originTransform = m_XROrigin != null ? m_XROrigin.transform : transform;
        var cameraTransform = m_XROrigin != null && m_XROrigin.Camera != null ? m_XROrigin.Camera.transform : null;
        var pivot = cameraTransform != null ? cameraTransform.position : originTransform.position;
        var currentYaw = cameraTransform != null ? cameraTransform.eulerAngles.y : originTransform.eulerAngles.y;
        var deltaYaw = Mathf.DeltaAngle(currentYaw, targetRotation.eulerAngles.y);
        originTransform.RotateAround(pivot, Vector3.up, deltaYaw);
    }

    Quaternion GetTargetRotation(RaycastHit hit, Transform targetTransform)
    {
        if (targetTransform != null)
            return targetTransform.rotation * Quaternion.Euler(0f, m_AimingYawOffset, 0f);

        var direction = Vector3.ProjectOnPlane(m_RightRayInteractor.transform.forward, Vector3.up);
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector3.forward;

        return Quaternion.LookRotation(direction.normalized, Vector3.up) * Quaternion.Euler(0f, m_AimingYawOffset, 0f);
    }

    void CreateTeleportReticle()
    {
        if (m_TeleportReticlePrefab == null)
            return;

        m_TeleportReticleInstance = Instantiate(m_TeleportReticlePrefab);
        m_TeleportReticleInstance.name = "Teleport Reticle Preview";
        SetTeleportReticleVisible(false);
    }

    void UpdateTeleportPreview()
    {
        if (!TryGetTeleportHit(out var hit))
        {
            SetTeleportReticleVisible(false);
            return;
        }

        var teleportAnchor = hit.collider != null ? hit.collider.GetComponentInParent<TeleportationAnchor>() : null;
        if (teleportAnchor == null && !IsValidSurface(hit))
        {
            SetTeleportReticleVisible(false);
            return;
        }

        var targetTransform = teleportAnchor != null
            ? teleportAnchor.teleportAnchorTransform != null ? teleportAnchor.teleportAnchorTransform : teleportAnchor.transform
            : null;
        var targetPosition = targetTransform != null ? targetTransform.position : hit.point;
        var targetRotation = GetTargetRotation(hit, targetTransform);

        if (m_TeleportReticleInstance != null)
        {
            m_TeleportReticleInstance.transform.SetPositionAndRotation(targetPosition, targetRotation);
            SetTeleportReticleVisible(true);
        }
    }

    void SetTeleportReticleVisible(bool visible)
    {
        if (m_TeleportReticleInstance != null && m_TeleportReticleInstance.activeSelf != visible)
            m_TeleportReticleInstance.SetActive(visible);
    }

    void SnapTurn(float angle)
    {
        if (m_XROrigin == null)
        {
            transform.Rotate(0f, angle, 0f, Space.World);
            return;
        }

        var originTransform = m_XROrigin.transform;
        var cameraTransform = m_XROrigin.Camera != null ? m_XROrigin.Camera.transform : null;
        var pivot = cameraTransform != null ? cameraTransform.position : originTransform.position;
        originTransform.RotateAround(pivot, Vector3.up, angle);
    }
}
