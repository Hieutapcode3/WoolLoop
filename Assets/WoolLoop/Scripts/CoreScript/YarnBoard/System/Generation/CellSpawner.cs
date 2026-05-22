using UnityEngine;

public sealed class CellSpawner : ICellSpawner
{
    private readonly GameObject _cellPrefab;

    public CellSpawner(GameObject cellPrefab)
    {
        _cellPrefab = cellPrefab;
    }

    public void SpawnCells(LevelData data, Transform parent)
    {
        if (data == null || parent == null)
            return;

        GameObject prefab = _cellPrefab;
        if (prefab == null)
        {
            try
            {
                prefab = PrefabProfile.CellPrefab;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"Could not load cell prefab from PrefabProfile: {exception.Message}");
            }
        }

        if (prefab == null)
        {
            Debug.LogError("Cell prefab is missing. Assign it on LevelGenerationController or PrefabProfile.");
            return;
        }

        for (int i = 0; i < data.tileData.Length; i++)
        {
            if (!data.tileData[i])
                continue;

            Vector2Int tile = GridCoordinateUtility.ToTile(i, data.size.x);
            Vector3 position = GridCoordinateUtility.TileToWorld(tile, data);
            GameObject cell = Object.Instantiate(prefab, position, Quaternion.identity, parent);
            cell.name = $"Cell_{tile.x}_{tile.y}";
        }
    }
}
