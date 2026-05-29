using UnityEngine;

[CreateAssetMenu(fileName = "PrefabProfile", menuName = "ScriptableObjects/PrefabProfile")]
public class PrefabProfile : ScriptableObject
{
    private const string ITEM_RESOURCE_FOLDER_PATH = "Data/PrefabProfile";

    private static ResourceAsset<PrefabProfile> asset = new(ITEM_RESOURCE_FOLDER_PATH);

    [SerializeField] private GameObject cellPrefab;
    public static GameObject CellPrefab => asset.Value.cellPrefab;

    [SerializeField] private GameObject obstaclePrefab;
    public static GameObject ObstaclePrefab => asset.Value.obstaclePrefab;

    [SerializeField] private GameObject woolBallPrefab;
    public static GameObject WoolBallPrefab => asset.Value.woolBallPrefab;

    [SerializeField] private Material woolBallMaterial;
    public static Material WoolBallMaterial => asset.Value.woolBallMaterial;

    [SerializeField] private Material wallMaterial;
    public static Material WallMaterial => asset.Value.wallMaterial;

    [SerializeField] private Mesh outerWallMesh;
    public static Mesh OuterWallMesh => asset.Value.outerWallMesh;

    [SerializeField] private Mesh woolBallMesh;
    public static Mesh WoolBallMesh => asset.Value.woolBallMesh;
}

