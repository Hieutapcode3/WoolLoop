using UnityEngine;

namespace VTLTools
{
    public static class ColorExtensions
    {
        /// <summary>
        /// Thay đổi độ trong suốt (Alpha) của màu hiện tại và trả về màu mới.
        /// Cách dùng: myColor.SetAlpha(0.5f);
        /// </summary>
        public static Color SetAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        /// <summary>
        /// Chuyển đổi mã Hex (VD: "#FF0000" hoặc "FF0000") sang Color.
        /// Cách dùng: "#FFFFFF".ToColor();
        /// </summary>
        public static Color ToColor(this string hexString)
        {
            // Tự động thêm dấu # nếu thiếu để tránh lỗi
            if (!hexString.StartsWith("#"))
                hexString = "#" + hexString;

            if (ColorUtility.TryParseHtmlString(hexString, out Color color))
            {
                return color;
            }

            Debug.LogWarning($"[ColorExtensions] Không thể parse mã màu: {hexString}. Trả về màu trắng.");
            return Color.white;
        }

        /// <summary>
        /// Tạo màu sáng hơn (Tint).
        /// </summary>
        public static Color Lighten(this Color color, float amount = 0.1f)
        {
            return Color.Lerp(color, Color.white, amount);
        }

        /// <summary>
        /// Tạo màu tối hơn (Shade).
        /// </summary>
        public static Color Darken(this Color color, float amount = 0.1f)
        {
            return Color.Lerp(color, Color.black, amount);
        }

        /// <summary>
        /// Đổi màu startColor của ParticleSystem.
        /// </summary>
        public static void SetStartColor(this ParticleSystem particleSystem, Color color)
        {
            if (particleSystem == null)
                return;

            var main = particleSystem.main;
            main.startColor = color;
        }

        /// <summary>
        /// Đổi màu startColor của ParticleSystem bằng Color32.
        /// </summary>
        public static void SetStartColor(this ParticleSystem particleSystem, Color32 color)
        {
            particleSystem.SetStartColor((Color)color);
        }

        /// <summary>
        /// Đổi startColor cho ParticleSystem hiện tại và toàn bộ ParticleSystem con.
        /// </summary>
        public static void SetStartColorIncludingChildren(this ParticleSystem particleSystem, Color color)
        {
            if (particleSystem == null)
                return;

            ParticleSystem[] systems = particleSystem.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].SetStartColor(color);
            }
        }
    }
}