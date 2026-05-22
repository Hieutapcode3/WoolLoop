using System;
using UnityEngine;

[CreateAssetMenu(menuName = "WoolLoop/Color Param", fileName = "WoolColorsParam")]
public class ColorsParamSO : ScriptableObject
{
    [Serializable]
    public struct ColorEntry
    {
        public WoolColorType woolColor;
        public Color displayColor;
    }

    [SerializeField]
    private ColorEntry[] setup =
    {
        new() { woolColor = WoolColorType.Red, displayColor = new Color(0.88f, 0.08f, 0.07f) },
        new() { woolColor = WoolColorType.Green, displayColor = new Color(0.16f, 0.64f, 0.25f) },
        new() { woolColor = WoolColorType.Blue, displayColor = new Color(0.21f, 0.24f, 0.88f) },
        new() { woolColor = WoolColorType.Yellow, displayColor = new Color(1.00f, 0.86f, 0.16f) },
        new() { woolColor = WoolColorType.Purple, displayColor = new Color(0.53f, 0.19f, 0.79f) },
        new() { woolColor = WoolColorType.Orange, displayColor = new Color(0.92f, 0.26f, 0.00f) },
        new() { woolColor = WoolColorType.Cyan, displayColor = new Color(0.31f, 1.00f, 0.81f) },
        new() { woolColor = WoolColorType.Magenta, displayColor = new Color(0.79f, 0.07f, 0.49f) },
        new() { woolColor = WoolColorType.White, displayColor = new Color(1.00f, 1.00f, 1.00f) },
        new() { woolColor = WoolColorType.Black, displayColor = new Color(0.06f, 0.07f, 0.07f) }
    };

    private Color[] _cachedPalette;
    public int ColorCount => setup != null ? setup.Length : 0;

    public Color[] GetPalette()
    {
        if (_cachedPalette != null && _cachedPalette.Length == ColorCount)
            return _cachedPalette;

        int count = Mathf.Max(0, ColorCount);
        var pal = new Color[count];

        for (int i = 0; i < count; i++)
        {
            pal[i] = setup[i].displayColor;
        }

        _cachedPalette = pal;
        return pal;
    }

    public bool TryGetColorByPaletteIndex(int index, out WoolColorType color, out Color displayColor)
    {
        color = WoolColorType.White;
        displayColor = Color.white;

        if (setup == null || index < 0 || index >= setup.Length)
            return false;

        color = setup[index].woolColor;
        displayColor = setup[index].displayColor;
        return true;
    }

    public Color GetColor(WoolColorType t)
    {
        if (setup == null)
            return Color.white;

        for (int i = 0; i < setup.Length; i++)
        {
            if (setup[i].woolColor == t)
                return setup[i].displayColor;
        }

        return Color.white;
    }

    private void OnValidate()
    {
        _cachedPalette = null;
    }
}
