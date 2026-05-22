using Dreamteck.Splines;
using UnityEngine;

public interface IFrameSplineBuilder
{
    SplineComputer BuildFrame(LevelData data, Transform parent);
}
