using UnityEngine;

namespace BoardSpline.Runtime
{
    [System.Serializable]
    public struct BoardSplineSettings
    {
        public float borderWidth;
        public float borderCornerRadius;
        public float borderPadding;
        public int borderSegmentCount;
        public Vector3 splineNormal;
        public bool removeInnerFaces;
        public bool renderInnerWalls;
        public bool clearExistingInnerWalls;

        public static BoardSplineSettings Default => new BoardSplineSettings
        {
            borderWidth = 0.5f,
            borderCornerRadius = 0.25f,
            borderPadding = 0.23f,
            borderSegmentCount = 6,
            splineNormal = Vector3.up,
            removeInnerFaces = true,
            renderInnerWalls = true,
            clearExistingInnerWalls = true,
        };
    }
}
