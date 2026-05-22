using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WoolBallData
{
    #region Fields
    public int BallId;
    public int ColorId;
    public Vector2Int tileId;
    public List<Vector2Int> childrenTileIds;
    #endregion
}