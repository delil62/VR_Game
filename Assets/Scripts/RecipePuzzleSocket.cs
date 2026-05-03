using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRSocketInteractor))]
public sealed class RecipePuzzleSocket : MonoBehaviour, IXRHoverFilter, IXRSelectFilter
{
    [SerializeField]
    int m_ExpectedPieceId = 1;

    [SerializeField]
    RecipePuzzleManager m_Manager;

    [SerializeField]
    Transform m_LockAnchor;

    [SerializeField]
    bool m_DisableSocketAfterPlacement = true;

    XRSocketInteractor m_SocketInteractor;
    Collider m_SocketCollider;
    bool m_HasAcceptedPiece;

    public bool canProcess => isActiveAndEnabled && !m_HasAcceptedPiece;

    void Awake()
    {
        m_SocketInteractor = GetComponent<XRSocketInteractor>();
        m_SocketCollider = GetComponent<Collider>();

        if (m_LockAnchor == null && m_SocketInteractor != null)
            m_LockAnchor = m_SocketInteractor.attachTransform;
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        if (m_SocketInteractor == null)
            m_SocketInteractor = GetComponent<XRSocketInteractor>();

        m_SocketInteractor.hoverFilters.Add(this);
        m_SocketInteractor.selectFilters.Add(this);
        m_SocketInteractor.selectEntered.AddListener(OnSocketSelectEntered);
    }

    void OnDisable()
    {
        if (!Application.isPlaying || m_SocketInteractor == null)
            return;

        m_SocketInteractor.hoverFilters.Remove(this);
        m_SocketInteractor.selectFilters.Remove(this);
        m_SocketInteractor.selectEntered.RemoveListener(OnSocketSelectEntered);
    }

    public bool Process(IXRHoverInteractor interactor, UnityEngine.XR.Interaction.Toolkit.Interactables.IXRHoverInteractable interactable)
    {
        return CanAccept(interactable.transform);
    }

    public bool Process(IXRSelectInteractor interactor, UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable interactable)
    {
        return CanAccept(interactable.transform);
    }

    bool CanAccept(Transform interactableTransform)
    {
        return TryGetPiece(interactableTransform, out var piece) &&
            !piece.IsLocked &&
            piece.PieceId == m_ExpectedPieceId;
    }

    void OnSocketSelectEntered(SelectEnterEventArgs args)
    {
        if (m_HasAcceptedPiece || !TryGetPiece(args.interactableObject.transform, out var piece))
            return;

        StartCoroutine(FinalizePlacement(piece));
    }

    IEnumerator FinalizePlacement(PuzzlePieceId piece)
    {
        yield return null;

        if (piece == null || piece.IsLocked)
            yield break;

        m_HasAcceptedPiece = true;
        piece.LockTo(m_LockAnchor != null ? m_LockAnchor : transform);
        m_Manager?.NotifyPiecePlaced(piece);

        if (!m_DisableSocketAfterPlacement)
            yield break;

        if (m_SocketInteractor != null)
            m_SocketInteractor.socketActive = false;

        if (m_SocketCollider != null)
            m_SocketCollider.enabled = false;
    }

    static bool TryGetPiece(Transform interactableTransform, out PuzzlePieceId piece)
    {
        piece = interactableTransform != null ? interactableTransform.GetComponentInParent<PuzzlePieceId>() : null;
        return piece != null;
    }
}
