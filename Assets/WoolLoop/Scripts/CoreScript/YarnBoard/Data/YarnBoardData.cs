using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "YarnBoardData", menuName = "ScriptableObjects/YarnBoardData")]
public class YarnBoardData : ScriptableObject
{
    #region Fields
    private const string ITEM_RESOURCE_FOLDER_PATH = "Data/YarnBoardData";

    private static ResourceAsset<YarnBoardData> asset = new(ITEM_RESOURCE_FOLDER_PATH);

    public Vector2Int size;
    public bool[] tileData;
    public List<WoolBallData> yarnBalls;
    #endregion

    #region Properties

    #endregion

    #region Lifecycle

    #endregion

    #region Private Methods

    #endregion

    #region Public Methods

    #endregion
}
