using System.IO;
using UnityEngine;

public sealed class LevelDataLoader : ILevelDataLoader
{
    public LevelData Load(string jsonPath)
    {
        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            Debug.LogError("Level json path is empty.");
            return null;
        }

        string json = null;
        if (File.Exists(jsonPath))
        {
            json = File.ReadAllText(jsonPath);
        }
        else
        {
            TextAsset asset = Resources.Load<TextAsset>(Path.ChangeExtension(jsonPath, null));
            if (asset != null)
                json = asset.text;
        }

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"Level json not found: {jsonPath}");
            return null;
        }

        return LoadFromJson(json);
    }

    public LevelData LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("Level json content is empty.");
            return null;
        }

        LevelData data = JsonUtility.FromJson<LevelData>(json);
        if (!Validate(data))
            return null;

        if (data.boardSetting == null)
            data.boardSetting = new GlobalYarnBoardSetting { cellSize = 1f, cellSpacing = 1f };

        if (data.yarnBalls == null && data.blockData != null)
            data.yarnBalls = data.blockData;
        if (data.blockData == null && data.yarnBalls != null)
            data.blockData = data.yarnBalls;

        return data;
    }

    private static bool Validate(LevelData data)
    {
        if (data == null)
        {
            Debug.LogError("Level json could not be parsed.");
            return false;
        }

        if (data.size.x <= 0 || data.size.y <= 0)
        {
            Debug.LogError($"Invalid level size: {data.size.x}x{data.size.y}");
            return false;
        }

        int expectedTileCount = data.size.x * data.size.y;
        if (data.tileData == null || data.tileData.Length != expectedTileCount)
        {
            Debug.LogError($"Invalid tile data length. Expected {expectedTileCount}, got {(data.tileData == null ? 0 : data.tileData.Length)}.");
            return false;
        }

        return true;
    }
}
