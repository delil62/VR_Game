using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;

/// <summary>
/// Positions the XR Origin at a configured teleport point when the scene starts.
/// </summary>
public class XRStartAtTeleportPoint : MonoBehaviour
{
    [SerializeField]
    XROrigin m_XROrigin;

    [SerializeField]
    Transform m_StartPoint;

    [SerializeField]
    bool m_AlignViewDirection = true;

    [SerializeField]
    bool m_PositionOnStart = true;

    IEnumerator Start()
    {
        if (!m_PositionOnStart)
            yield break;

        yield return null;
        MoveToStartPoint();
    }

    public void MoveToStartPoint()
    {
        if (m_StartPoint == null)
            return;

        if (m_XROrigin == null)
        {
            transform.SetPositionAndRotation(m_StartPoint.position, m_StartPoint.rotation);
            return;
        }

        if (m_AlignViewDirection)
            AlignViewDirection();

        MoveBodyGroundToStartPoint();
    }

    void AlignViewDirection()
    {
        var targetForward = Vector3.ProjectOnPlane(m_StartPoint.forward, Vector3.up);
        if (targetForward.sqrMagnitude < 0.0001f)
            return;

        m_XROrigin.MatchOriginUpCameraForward(Vector3.up, targetForward.normalized);
    }

    void MoveBodyGroundToStartPoint()
    {
        var originTransform = m_XROrigin.transform;
        var cameraTransform = m_XROrigin.Camera != null ? m_XROrigin.Camera.transform : null;

        var currentGroundPosition = cameraTransform != null
            ? new Vector3(cameraTransform.position.x, originTransform.position.y, cameraTransform.position.z)
            : originTransform.position;

        originTransform.position += m_StartPoint.position - currentGroundPosition;
    }
}
