using System.Collections.Generic;
using BoardSpline.Runtime;
using Common.Helper;
using DG.Tweening;
using UnityEngine;

public class WoolBallVisual : MonoBehaviour
{
    private const string PiecesContainerName = "Pieces";
    private const float PieceHeightOffset = .3f;
    private readonly List<Transform> pieceTransforms = new();

    public IReadOnlyList<Transform> PieceTransforms => pieceTransforms;
    public int VisiblePieceCount
    {
        get
        {
            var count = 0;
            for (var i = 0; i < pieceTransforms.Count; i++)
            {
                if (pieceTransforms[i] != null && pieceTransforms[i].gameObject.activeSelf)
                    count++;
            }

            return count;
        }
    }

    public void Render(WoolBallData data, BoardSplineDataAdapterInfo adapter)
    {
        ClearChildren();
        if (data == null)
            return;

        var container = new GameObject(PiecesContainerName);
        container.transform.SetParent(transform, false);

        var rootWorld = adapter.IndexToWorld(data.tileId);
        var color = ColorsParamSO.GetColorByPaletteIndex(data.ColorId);
        var tiles = CollectTiles(data);

        for (var i = 0; i < tiles.Count; i++)
        {
            var tile = tiles[i];
            var piece = CreatePiece(tile, color);
            piece.transform.SetParent(container.transform, false);
            piece.transform.localPosition = TileToLocalPosition(tile, adapter, rootWorld);
            piece.transform.localScale = Vector3.one * adapter.CellSize * 0.8f;
            pieceTransforms.Add(piece.transform);
        }
    }

    public Sequence CreateMoveTween(
        IReadOnlyList<Vector2Int> targetTiles,
        BoardSplineDataAdapterInfo adapter,
        Vector3 rootWorld,
        float duration
    )
    {
        var sequence = DOTween.Sequence();
        if (targetTiles == null)
            return sequence;

        var count = Mathf.Min(pieceTransforms.Count, targetTiles.Count);
        for (var i = 0; i < count; i++)
        {
            if (pieceTransforms[i] == null)
                continue;

            sequence.Join(pieceTransforms[i].DOLocalMove(
                TileToLocalPosition(targetTiles[i], adapter, rootWorld),
                duration
            ).SetEase(Ease.Linear));
        }

        return sequence;
    }

    public Sequence CreateWorldMoveTween(IReadOnlyList<Vector3> targetWorldPositions, float duration)
    {
        var sequence = DOTween.Sequence();
        if (targetWorldPositions == null)
            return sequence;

        var count = Mathf.Min(pieceTransforms.Count, targetWorldPositions.Count);
        for (var i = 0; i < count; i++)
        {
            if (pieceTransforms[i] == null)
                continue;

            sequence.Join(pieceTransforms[i]
                .DOMove(targetWorldPositions[i], duration)
                .SetEase(Ease.Linear));
        }

        return sequence;
    }

    public List<Vector3> GetPieceWorldPositions()
    {
        var positions = new List<Vector3>(pieceTransforms.Count);
        for (var i = 0; i < pieceTransforms.Count; i++)
        {
            if (pieceTransforms[i] != null)
                positions.Add(pieceTransforms[i].position);
        }

        return positions;
    }

    public bool HideNextVisiblePiece()
    {
        for (var i = pieceTransforms.Count - 1; i >= 0; i--)
        {
            if (pieceTransforms[i] == null || !pieceTransforms[i].gameObject.activeSelf)
                continue;

            pieceTransforms[i].gameObject.SetActive(false);
            return true;
        }

        return false;
    }

    public void SetAllPiecesVisible(bool visible)
    {
        for (var i = 0; i < pieceTransforms.Count; i++)
        {
            if (pieceTransforms[i] != null)
                pieceTransforms[i].gameObject.SetActive(visible);
        }
    }

    private static List<Vector2Int> CollectTiles(WoolBallData data)
    {
        var tiles = new List<Vector2Int> { data.tileId };
        if (data.childrenTileIds == null)
            return tiles;

        for (var i = 0; i < data.childrenTileIds.Count; i++)
        {
            var child = data.childrenTileIds[i];
            if (!tiles.Contains(child))
                tiles.Add(child);
        }

        return tiles;
    }

    private static GameObject CreatePiece(Vector2Int tile, Color color)
    {
        var piece = new GameObject($"WoolBallPiece_{tile.x}_{tile.y}");
        var meshFilter = piece.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = PrefabProfile.WoolBallMesh;

        var renderer = piece.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = PrefabProfile.WoolBallMaterial != null
            ? PrefabProfile.WoolBallMaterial
            : PrefabProfile.WallMaterial;
        renderer.SetBaseColor(color);

        return piece;
    }

    private static Vector3 TileToLocalPosition(Vector2Int tile, BoardSplineDataAdapterInfo adapter, Vector3 rootWorld)
    {
        return adapter.IndexToWorld(tile) - rootWorld + Vector3.up * PieceHeightOffset;
    }

    private void ClearChildren()
    {
        pieceTransforms.Clear();
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }
}
