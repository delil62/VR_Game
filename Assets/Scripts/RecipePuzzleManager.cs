using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public sealed class RecipePuzzleManager : MonoBehaviour
{
    [SerializeField]
    int m_TotalPieces = 6;

    [SerializeField]
    GameObject m_CompletionIndicator;

    [SerializeField]
    UnityEvent m_OnPuzzleCompleted = new UnityEvent();

    readonly HashSet<int> m_PlacedPieceIds = new HashSet<int>();

    public int PlacedPieceCount => m_PlacedPieceIds.Count;

    public bool IsComplete { get; private set; }

    public void NotifyPiecePlaced(PuzzlePieceId piece)
    {
        if (piece == null || IsComplete)
            return;

        if (!m_PlacedPieceIds.Add(piece.PieceId))
            return;

        if (m_PlacedPieceIds.Count < m_TotalPieces)
            return;

        IsComplete = true;

        if (m_CompletionIndicator != null)
            m_CompletionIndicator.SetActive(true);

        m_OnPuzzleCompleted.Invoke();
        Debug.Log("Recipe puzzle completed.", this);
    }
}
