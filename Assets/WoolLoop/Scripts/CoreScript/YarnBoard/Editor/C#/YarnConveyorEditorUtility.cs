using System.Collections.Generic;
using BoardSpline.Runtime;
using UnityEditor;
using UnityEngine;

public sealed class YarnConveyorValidationResult
{
    public readonly List<string> Errors = new List<string>();
    public readonly List<string> Warnings = new List<string>();
}

[System.Serializable]
public sealed class YarnConveyorSavedPreset
{
    public string name;
    public bool loop;
    public List<Vector3> controlPoints = new List<Vector3>();
    public List<YarnConveyorExitData> exits = new List<YarnConveyorExitData>();
    public bool hasExit;
    public float exitPercent;
    public float cornerRadius = 0.25f;
    public int cornerSegments = 6;
}

[System.Serializable]
public sealed class YarnConveyorSavedPresetStore
{
    public List<YarnConveyorSavedPreset> presets = new List<YarnConveyorSavedPreset>();
}

public static class YarnConveyorEditorUtility
{
    public const string NoPresetId = "No Saved Preset";
    public const string CustomPresetId = NoPresetId;
    public const string UserPresetPrefix = "User: ";

    private const float ShortPathLength = 0.5f;
    private const string PresetPrefsKeyPrefix = "WoolLoop.YarnConveyorEditorPresets.";

    public static List<string> GetPresetChoices()
    {
        var choices = new List<string> { NoPresetId };
        var store = LoadPresetStore();
        for (var i = 0; i < store.presets.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(store.presets[i]?.name))
                continue;

            choices.Add(UserPresetPrefix + store.presets[i].name);
        }

        return choices;
    }

    public static string CreateUniquePresetName(string baseName = "Preset")
    {
        baseName = SanitizePresetName(baseName);
        if (string.IsNullOrEmpty(baseName))
            baseName = "Preset";
        if (baseName.StartsWith(UserPresetPrefix))
            baseName = baseName.Substring(UserPresetPrefix.Length).Trim();

        var store = LoadPresetStore();
        var index = 1;
        string candidate;
        do
        {
            candidate = $"{baseName}_{index:000}";
            index++;
        }
        while (store.presets.Exists(preset =>
            preset != null && string.Equals(preset.name, candidate, System.StringComparison.OrdinalIgnoreCase)
        ));

        return candidate;
    }

    public static void Normalize(YarnConveyorData data)
    {
        if (data == null) return;

        if (string.IsNullOrEmpty(data.presetId))
            data.presetId = NoPresetId;
        if (IsRemovedBuiltInPreset(data.presetId))
            data.presetId = NoPresetId;
        if (data.controlPoints == null)
            data.controlPoints = new List<Vector3>();
        if (data.exits == null)
            data.exits = new List<YarnConveyorExitData>();
        if (data.hasExit && data.exits.Count == 0)
            data.exits.Add(new YarnConveyorExitData { percent = data.exitPercent, length = 0.5f });

        for (var i = data.exits.Count - 1; i >= 0; i--)
        {
            if (data.exits[i] == null)
            {
                data.exits.RemoveAt(i);
                continue;
            }

            data.exits[i].percent = Mathf.Clamp01(data.exits[i].percent);
        }

        SyncLegacyExitFields(data);
    }

    public static void ApplyPreset(YarnConveyorData data, string presetId)
    {
        if (data == null) return;

        Normalize(data);
        data.presetId = string.IsNullOrEmpty(presetId) ? NoPresetId : presetId;

        if (TryApplySavedPreset(data, data.presetId))
            return;

        data.presetId = NoPresetId;
    }

    public static YarnConveyorValidationResult Validate(YarnConveyorData data, CustomFrameBuilder selectedBuilder)
    {
        var result = new YarnConveyorValidationResult();
        if (data == null)
        {
            result.Errors.Add("Yarn conveyor data is missing.");
            return result;
        }

        if (data.controlPoints == null)
        {
            result.Errors.Add("Yarn conveyor control points are missing.");
            return result;
        }

        if (data.cornerRadius < 0f)
            result.Errors.Add("Yarn conveyor corner radius cannot be negative.");
        if (data.cornerSegments < 1)
            result.Errors.Add("Yarn conveyor corner segments must be at least 1.");

        var count = data.controlPoints.Count;
        if (!data.loop && count > 0 && count < 2)
            result.Errors.Add("Open yarn conveyor paths need at least 2 control points.");
        if (data.loop && count > 0 && count < 3)
            result.Errors.Add("Loop yarn conveyor paths need at least 3 control points.");
        int exitCount = data.exits != null && data.exits.Count > 0 ? data.exits.Count : (data.hasExit ? 1 : 0);
        if (exitCount > 0 && count == 0)
            result.Errors.Add("Yarn conveyor exit needs a path before it can be sampled.");
        if (data.exits != null)
        {
            for (var i = 0; i < data.exits.Count; i++)
            {
                if (data.exits[i] != null && data.exits[i].length < 0f)
                    result.Errors.Add($"Yarn conveyor exit {i + 1} length cannot be negative.");
            }
        }

        if (count == 0)
            result.Warnings.Add("Yarn conveyor path is empty.");
        else if (CustomFrameBuilder.CalculateLength(data.controlPoints, data.loop) < ShortPathLength)
            result.Warnings.Add("Yarn conveyor path is very short.");

        if (exitCount == 0)
            result.Warnings.Add("Yarn conveyor exit is not set.");
        if (selectedBuilder == null)
            result.Warnings.Add("No CustomFrameBuilder is selected for preview/apply.");

        return result;
    }

    public static bool ApplyToBuilder(YarnConveyorData data, CustomFrameBuilder builder)
    {
        if (data == null || builder == null)
            return false;

        Normalize(data);
        builder.CornerRadius = data.cornerRadius;
        builder.CornerSegments = data.cornerSegments;
        builder.SetPath(data.controlPoints.ToArray(), data.loop);
        return builder.Build();
    }

    public static bool SaveCurrentAsPreset(string presetName, YarnConveyorData data)
    {
        if (data == null || data.controlPoints == null || data.controlPoints.Count == 0)
            return false;

        Normalize(data);
        presetName = SanitizePresetName(presetName);
        if (string.IsNullOrEmpty(presetName))
            return false;

        var store = LoadPresetStore();
        var saved = new YarnConveyorSavedPreset
        {
            name = presetName,
            loop = data.loop,
            controlPoints = new List<Vector3>(data.controlPoints),
            exits = CopyExits(data.exits),
            hasExit = data.hasExit,
            exitPercent = data.exitPercent,
            cornerRadius = data.cornerRadius,
            cornerSegments = data.cornerSegments
        };

        var existingIndex = store.presets.FindIndex(preset =>
            preset != null && string.Equals(preset.name, presetName, System.StringComparison.OrdinalIgnoreCase)
        );
        if (existingIndex >= 0)
            store.presets[existingIndex] = saved;
        else
            store.presets.Add(saved);

        SavePresetStore(store);
        return true;
    }

    public static bool SaveCurrentAsSelectedPreset(string presetId, YarnConveyorData data)
    {
        string presetName = GetUserPresetName(presetId);
        if (string.IsNullOrEmpty(presetName))
            return false;

        return SaveCurrentAsPreset(presetName, data);
    }

    public static bool DuplicateSavedPreset(string sourcePresetId, string newPresetName)
    {
        string sourceName = GetUserPresetName(sourcePresetId);
        newPresetName = SanitizePresetName(newPresetName);
        if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(newPresetName))
            return false;

        var store = LoadPresetStore();
        var source = store.presets.Find(preset =>
            preset != null && string.Equals(preset.name, sourceName, System.StringComparison.OrdinalIgnoreCase)
        );
        if (source == null)
            return false;

        var duplicate = new YarnConveyorSavedPreset
        {
            name = newPresetName,
            loop = source.loop,
            controlPoints = source.controlPoints != null
                ? new List<Vector3>(source.controlPoints)
                : new List<Vector3>(),
            exits = CopyExits(source.exits),
            hasExit = source.hasExit,
            exitPercent = source.exitPercent,
            cornerRadius = source.cornerRadius,
            cornerSegments = source.cornerSegments
        };

        var existingIndex = store.presets.FindIndex(preset =>
            preset != null && string.Equals(preset.name, newPresetName, System.StringComparison.OrdinalIgnoreCase)
        );
        if (existingIndex >= 0)
            store.presets[existingIndex] = duplicate;
        else
            store.presets.Add(duplicate);

        SavePresetStore(store);
        return true;
    }

    public static bool DeleteSavedPreset(string presetId)
    {
        string presetName = GetUserPresetName(presetId);
        if (string.IsNullOrEmpty(presetName))
            return false;

        var store = LoadPresetStore();
        var removed = store.presets.RemoveAll(preset =>
            preset != null && string.Equals(preset.name, presetName, System.StringComparison.OrdinalIgnoreCase)
        );
        if (removed <= 0)
            return false;

        SavePresetStore(store);
        return true;
    }

    public static Vector3 SampleExitPosition(YarnConveyorData data)
    {
        if (data?.exits != null && data.exits.Count > 0)
            return SampleExitPosition(data, data.exits[0]);

        if (data == null || data.controlPoints == null || data.controlPoints.Count == 0)
            return Vector3.zero;

        var legacyExit = new YarnConveyorExitData { percent = data.exitPercent };
        return SampleExitPosition(data, legacyExit);
    }

    public static Vector3 SampleExitPosition(YarnConveyorData data, YarnConveyorExitData exit)
    {
        if (data == null || data.controlPoints == null || data.controlPoints.Count == 0)
            return Vector3.zero;

        var roundedPath = CustomFrameBuilder.CreateRoundedPath(
            data.controlPoints,
            data.cornerRadius,
            data.cornerSegments,
            data.loop
        );
        return CustomFrameBuilder.SamplePathAtPercent(roundedPath, exit != null ? exit.percent : 0f, data.loop);
    }

    public static Vector3 SampleExitEndPosition(YarnConveyorData data, YarnConveyorExitData exit)
    {
        if (data == null || data.controlPoints == null || data.controlPoints.Count == 0 || exit == null)
            return Vector3.zero;

        var roundedPath = CustomFrameBuilder.CreateRoundedPath(
            data.controlPoints,
            data.cornerRadius,
            data.cornerSegments,
            data.loop
        );
        float pathLength = CustomFrameBuilder.CalculateLength(roundedPath, data.loop);
        if (pathLength <= 0f)
            return CustomFrameBuilder.SamplePathAtPercent(roundedPath, exit.percent, data.loop);

        float startDistance = Mathf.Clamp01(exit.percent) * pathLength;
        float endDistance = startDistance + Mathf.Max(0f, exit.length);
        float endPercent = data.loop
            ? Mathf.Repeat(endDistance, pathLength) / pathLength
            : Mathf.Clamp01(endDistance / pathLength);

        return CustomFrameBuilder.SamplePathAtPercent(roundedPath, endPercent, data.loop);
    }

    public static bool IsUserPreset(string presetId)
    {
        return !string.IsNullOrEmpty(GetUserPresetName(presetId));
    }

    public static string GetDisplayUserPresetName(string presetId)
    {
        return GetUserPresetName(presetId);
    }

    private static bool TryApplySavedPreset(YarnConveyorData data, string presetId)
    {
        string presetName = GetUserPresetName(presetId);
        if (string.IsNullOrEmpty(presetName))
            return false;

        var store = LoadPresetStore();
        var preset = store.presets.Find(item =>
            item != null && string.Equals(item.name, presetName, System.StringComparison.OrdinalIgnoreCase)
        );
        if (preset == null)
            return false;

        data.presetId = UserPresetPrefix + preset.name;
        data.loop = preset.loop;
        data.controlPoints = preset.controlPoints != null
            ? new List<Vector3>(preset.controlPoints)
            : new List<Vector3>();
        data.exits = CopyExits(preset.exits);
        data.hasExit = preset.hasExit;
        data.exitPercent = Mathf.Clamp01(preset.exitPercent);
        if (data.hasExit && data.exits.Count == 0)
            data.exits.Add(new YarnConveyorExitData { percent = data.exitPercent, length = 0.5f });
        data.cornerRadius = preset.cornerRadius;
        data.cornerSegments = preset.cornerSegments;
        SyncLegacyExitFields(data);
        return true;
    }

    private static List<YarnConveyorExitData> CopyExits(List<YarnConveyorExitData> exits)
    {
        var copy = new List<YarnConveyorExitData>();
        if (exits == null)
            return copy;

        foreach (YarnConveyorExitData exit in exits)
        {
            if (exit == null)
                continue;

            copy.Add(new YarnConveyorExitData
            {
                percent = Mathf.Clamp01(exit.percent),
                length = Mathf.Max(0f, exit.length)
            });
        }

        return copy;
    }

    private static void SyncLegacyExitFields(YarnConveyorData data)
    {
        if (data == null)
            return;

        data.hasExit = data.exits != null && data.exits.Count > 0;
        data.exitPercent = data.hasExit ? Mathf.Clamp01(data.exits[0].percent) : Mathf.Clamp01(data.exitPercent);
    }

    private static YarnConveyorSavedPresetStore LoadPresetStore()
    {
        var json = EditorPrefs.GetString(GetPresetPrefsKey(), string.Empty);
        if (string.IsNullOrEmpty(json))
            return new YarnConveyorSavedPresetStore();

        var store = JsonUtility.FromJson<YarnConveyorSavedPresetStore>(json);
        if (store == null)
            return new YarnConveyorSavedPresetStore();
        if (store.presets == null)
            store.presets = new List<YarnConveyorSavedPreset>();

        return store;
    }

    private static void SavePresetStore(YarnConveyorSavedPresetStore store)
    {
        if (store == null)
            store = new YarnConveyorSavedPresetStore();
        if (store.presets == null)
            store.presets = new List<YarnConveyorSavedPreset>();

        EditorPrefs.SetString(GetPresetPrefsKey(), JsonUtility.ToJson(store));
    }

    private static string GetPresetPrefsKey()
    {
        return PresetPrefsKeyPrefix + Application.dataPath.Replace('\\', '/');
    }

    private static string GetUserPresetName(string presetId)
    {
        if (string.IsNullOrEmpty(presetId) || !presetId.StartsWith(UserPresetPrefix))
            return string.Empty;

        return SanitizePresetName(presetId.Substring(UserPresetPrefix.Length));
    }

    private static string SanitizePresetName(string presetName)
    {
        return string.IsNullOrWhiteSpace(presetName) ? string.Empty : presetName.Trim();
    }

    private static bool IsRemovedBuiltInPreset(string presetId)
    {
        return presetId == "Custom" ||
               presetId == "Circle Loop" ||
               presetId == "Rounded Rectangle" ||
               presetId == "S Curve" ||
               presetId == "U Shape";
    }
}
