using UnityEngine;

[System.Serializable]
public struct ColorOption
{
    public int colorID;
    public Color color;
}

[CreateAssetMenu(fileName = "ColorProfile", menuName = "ScriptableObjects/ColorProfile")]
public class ColorProfile : ScriptableObject
{
    private const string ITEM_RESOURCE_FOLDER_PATH = "Data/ColorProfile";

    private static ResourceAsset<ColorProfile> asset = new(ITEM_RESOURCE_FOLDER_PATH);
    public ColorOption[] colorPalette;

    public static ColorOption[] GetColorPalette(string profileName)
    {
        ColorProfile profile = asset.Value;
        if (profile != null && profile.name == profileName)
        {
            return profile.colorPalette;
        }
        return null;
    }

    public static Color GetColor(int id)
    {
        ColorProfile profile = asset.Value;
        if (profile != null)
        {
            if (id < 0 || id >= profile.colorPalette.Length)
            {
                return Color.white;
            }
            return profile.colorPalette[id].color;
        }
        return Color.white;
    }

    public static int Count()
    {
        ColorProfile profile = asset.Value;
        return profile != null ? profile.colorPalette.Length : 0;
    }
}