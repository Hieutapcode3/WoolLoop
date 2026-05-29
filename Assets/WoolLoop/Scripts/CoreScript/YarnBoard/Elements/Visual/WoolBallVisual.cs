using System.Collections.Generic;
using BoardSpline.Runtime;
using Common.Helper;
using UnityEngine;

public class WoolBallVisual : MonoBehaviour
{
    private const string PiecesContainerName = "Pieces";

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
            piece.transform.localPosition = adapter.IndexToWorld(tile) - rootWorld;
            piece.transform.localScale = Vector3.one * adapter.CellSize;
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

    private void ClearChildren()
    {
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
