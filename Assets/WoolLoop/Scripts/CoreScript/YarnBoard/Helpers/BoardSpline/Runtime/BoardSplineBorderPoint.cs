using System;
using UnityEngine;

namespace BoardSpline.Runtime
{
    [Serializable]
    public struct BoardSplineBorderPoint : IEquatable<BoardSplineBorderPoint>
    {
        public Vector3 Position;
        public Vector3 EmptyDirection;

        public BoardSplineBorderPoint(Vector3 position, Vector3 emptyDirection)
        {
            Position = position;
            EmptyDirection = emptyDirection;
        }

        public bool Equals(BoardSplineBorderPoint other) =>
            Position.Equals(other.Position) && EmptyDirection.Equals(other.EmptyDirection);

        public override bool Equals(object obj) =>
            obj is BoardSplineBorderPoint other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Position, EmptyDirection);
    }
}
