using UnityEngine;

[CreateAssetMenu(fileName = "PrefabProfile", menuName = "ScriptableObjects/PrefabProfile")]
public class PrefabProfile : ScriptableObject
{
    private const string ITEM_RESOURCE_FOLDER_PATH = "Data/PrefabProfile";

    private static ResourceAsset<PrefabProfile> asset = new(ITEM_RESOURCE_FOLDER_PATH);

    [SerializeField] private GameObject cellPrefab;
    public static GameObject CellPrefab => asset.Value.cellPrefab;

    [SerializeField] private Material wallMaterial;
    public static Material WallMaterial => asset.Value.wallMaterial;
}

