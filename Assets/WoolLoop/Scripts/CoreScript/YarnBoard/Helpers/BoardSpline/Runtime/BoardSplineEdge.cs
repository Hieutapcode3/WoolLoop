namespace BoardSpline.Runtime
{
    internal readonly struct BoardSplineEdge
    {
        public readonly BoardSplineBorderPoint Start;
        public readonly BoardSplineBorderPoint End;

        public BoardSplineEdge(BoardSplineBorderPoint start, BoardSplineBorderPoint end)
        {
            Start = start;
            End = end;
        }
    }
}
