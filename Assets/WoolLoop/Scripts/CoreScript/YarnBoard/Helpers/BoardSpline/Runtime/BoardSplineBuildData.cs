using System.Collections.Generic;
using UnityEngine;

namespace BoardSpline.Runtime
{
    public sealed class BoardSplineBuildData
    {
        public readonly List<List<BoardSplineBorderPoint>> BorderPointRegions;
        public readonly List<Vector3> InnerEmptyPositions;

        public BoardSplineBuildData(
            List<List<BoardSplineBorderPoint>> borderPointRegions,
            List<Vector3> innerEmptyPositions
        )
        {
            BorderPointRegions = borderPointRegions ?? new List<List<BoardSplineBorderPoint>>();
            InnerEmptyPositions = innerEmptyPositions ?? new List<Vector3>();
        }
    }
}
