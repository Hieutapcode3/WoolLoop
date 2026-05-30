using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class YarnConveyorData
{
    public string presetId = "No Saved Preset";
    public bool loop;
    public List<Vector3> controlPoints = new List<Vector3>();
    public List<YarnConveyorExitData> exits = new List<YarnConveyorExitData>();
    public bool hasExit;
    [Range(0f, 1f)] public float exitPercent;
    [Min(0f)] public float cornerRadius = 0.25f;
    [Min(1)] public int cornerSegments = 6;
}

[Serializable]
public sealed class YarnConveyorExitData
{
    [Range(0f, 1f)] public float percent;
    [Min(0f)] public float length = 0.5f;
}
