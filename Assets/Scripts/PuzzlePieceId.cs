using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public sealed class PuzzlePieceId : MonoBehaviour
{
    [SerializeField]
    int m_PieceId = 1;

    [SerializeField]
    bool m_DisableCollidersWhenLocked = true;

    XRGrabInteractable m_GrabInteractable;
    Rigidbody m_Rigidbody;
    Collider[] m_Colliders;

    public int PieceId => m_PieceId;

    public bool IsLocked { get; private set; }

    void Awake()
    {
        m_GrabInteractable = GetComponent<XRGrabInteractable>();
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Colliders = GetComponentsInChildren<Collider>(true);
    }

    public void LockTo(Transform anchor)
    {
        if (IsLocked)
            return;

        IsLocked = true;

        if (anchor != null)
        {
            transform.SetParent(anchor, true);
            transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        }

        if (m_Rigidbody != null)
        {
            m_Rigidbody.isKinematic = true;
            m_Rigidbody.useGravity = false;
        }

        if (m_GrabInteractable != null)
            m_GrabInteractable.enabled = false;

        if (!m_DisableCollidersWhenLocked || m_Colliders == null)
            return;

        foreach (var col in m_Colliders)
        {
            if (col != null)
                col.enabled = false;
        }
    }
}
