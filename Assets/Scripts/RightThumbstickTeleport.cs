using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
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

    readonly HashSet<Collider> m_SurfaceSet = new();
    bool m_IsAiming;
    bool m_IsSnapTurnHeld;
    float m_AimingYawOffset;

    void Awake()
    {
        RebuildSurfaceCache();
        SetTeleportAiming(false);
    }

    void OnEnable()
    {
        SetTeleportAiming(false);
    }

    void OnDisable()
    {
        SetTeleportAiming(false);
    }

    void Update()
    {
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
        var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!device.isValid)
            return Vector2.zero;

        return device.TryGetFeatureValue(CommonUsages.primary2DAxis, out var stick)
            ? stick
            : Vector2.zero;
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

        if (m_RightDirectInteractor != null)
            m_RightDirectInteractor.enabled = !enabled;

        if (m_ControllerPointerLineVisualizer != null)
            m_ControllerPointerLineVisualizer.SetVisible(!enabled);
    }

    void TryTeleport()
    {
        if (m_RightRayInteractor == null)
            return;

        if (!m_RightRayInteractor.TryGetCurrent3DRaycastHit(out var hit))
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
