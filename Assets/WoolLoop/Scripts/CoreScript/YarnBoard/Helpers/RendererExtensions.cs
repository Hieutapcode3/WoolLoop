using UnityEngine;

namespace Common.Helper
{
    public static class RendererExtensions
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionSelfGlowId = Shader.PropertyToID("_EmissionSelfGlow");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Underlay (for TextMeshPro)")]
        private static readonly int UnderlayColorId = Shader.PropertyToID("_UnderlayColor");
        private static readonly int UnderlayOffsetXId = Shader.PropertyToID("_UnderlayOffsetX");
        private static readonly int UnderlayOffsetYId = Shader.PropertyToID("_UnderlayOffsetY");
        private static readonly int UnderlayDilateId = Shader.PropertyToID("_UnderlayDilate");
        private static readonly int UnderlaySoftnessId = Shader.PropertyToID("_UnderlaySoftness");

        // Cache static 
        private static MaterialPropertyBlock _mpb;
        private static MaterialPropertyBlock Mpb
        {
            get
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                return _mpb;
            }
        }

        private static Material _tmpUnderlayInstance;

        public static void SetBaseColor(this Renderer renderer, Color color)
        {
            if (renderer == null) return;
            renderer.SetColorMPB(BaseColorId, color);
        }

        public static void SetSharedBaseColor(this Renderer renderer, Color color)
        {
            if (renderer == null) return;

            Material material = renderer.sharedMaterial;
            if (material == null) return;

            if (material.HasProperty(BaseColorId))
            {
                material.SetColor(BaseColorId, color);
            }
            else if (material.HasProperty(ColorId))
            {
                material.SetColor(ColorId, color);
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(material);
#endif
        }

        public static void SetDefaultColor(this Renderer renderer, Color color)
        {
            if (renderer == null) return;
            renderer.SetColorMPB(ColorId, color);
        }

        /// <summary>
        /// eng : Set color using MaterialPropertyBlock (MPB) to avoid creating new material instances.
        /// </summary>
        public static void SetColorMPB(this Renderer renderer, string propertyName, Color color)
        {
            SetColorMPB(renderer, Shader.PropertyToID(propertyName), color);
        }

        /// <summary>
        /// eng : Set color using MaterialPropertyBlock (MPB) to avoid creating new material instances.
        /// </summary>
        public static void SetColorMPB(this Renderer renderer, int propertyId, Color color)
        {
            if (renderer == null) return;

            renderer.GetPropertyBlock(Mpb);
            Mpb.SetColor(propertyId, color);
            renderer.SetPropertyBlock(Mpb);
        }

        /// <summary>
        /// eng : Set texture using MaterialPropertyBlock.
        /// </summary>
        public static void SetTextureMPB(this Renderer renderer, string propertyName, Texture texture)
        {
            SetTextureMPB(renderer, Shader.PropertyToID(propertyName), texture);
        }

        /// <summary>
        /// eng : Set texture using MaterialPropertyBlock.
        /// </summary>
        public static void SetTextureMPB(this Renderer renderer, int propertyId, Texture texture)
        {
            if (renderer == null) return;

            renderer.GetPropertyBlock(Mpb);
            Mpb.SetTexture(propertyId, texture);
            renderer.SetPropertyBlock(Mpb);
        }

        /// <summary>
        /// eng : Set BaseMap texture (URP/Lit default).
        /// </summary>
        public static void SetBaseMap(this Renderer renderer, Texture texture)
        {
            renderer.SetTextureMPB(BaseMapId, texture);
        }

        /// <summary>
        /// eng : Set emission color with intensity using MaterialPropertyBlock (MPB).
        /// </summary>
        public static void SetEmissionColor(this Renderer renderer, Color color, float intensity)
        {
            Color hdrColor = color * Mathf.Pow(2, intensity);
            renderer.SetColorMPB(EmissionColorId, hdrColor);
        }

        /// <summary>
        /// eng : Set float value for Emission Self Glow property.
        /// </summary>
        public static void SetEmissionSelfGlow(this Renderer renderer, float value)
        {
            if (renderer == null) return;

            renderer.GetPropertyBlock(Mpb);
            Mpb.SetFloat(EmissionSelfGlowId, value);
            renderer.SetPropertyBlock(Mpb);
        }

        /// <summary>
        /// eng : Set renderer alpha with MaterialPropertyBlock (URP: _BaseColor, Built-in: _Color).
        /// </summary>
        public static void SetAlphaMPB(this Renderer renderer, float alpha)
        {
            if (renderer == null) return;

            var material = renderer.sharedMaterial;
            if (material == null) return;
            var colorPropertyId = material.HasProperty(BaseColorId) ? BaseColorId : ColorId;
            SetAlphaMPB(renderer, alpha, colorPropertyId);
        }

        /// <summary>
        /// eng : Set renderer alpha with a custom color property id.
        /// </summary>
        public static void SetAlphaMPB(this Renderer renderer, float alpha, int colorPropertyId)
        {
            if (renderer == null) return;

            var material = renderer.sharedMaterial;
            if (material == null || !material.HasProperty(colorPropertyId)) return;

            alpha = Mathf.Clamp01(alpha);

            renderer.GetPropertyBlock(Mpb);
            var color = material.GetColor(colorPropertyId);
            color.a = alpha;
            Mpb.SetColor(colorPropertyId, color);
            renderer.SetPropertyBlock(Mpb);
        }

        /// <summary>
        /// eng : Set TextMeshPro underlay properties using MaterialPropertyBlock.
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="color"></param>
        /// <param name="dilate"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <param name="softness"></param>
        public static void SetTMPUnderlay(this Renderer renderer, Color color, float dilate = 0.35f, float offsetX = 0f, float offsetY = 0f, float softness = 0f)
        {
            if (renderer == null) return;

            if (_tmpUnderlayInstance == null)
            {
                _tmpUnderlayInstance = new Material(renderer.sharedMaterial);
                _tmpUnderlayInstance.name = "TMP_Underlay_Instance";
                _tmpUnderlayInstance.EnableKeyword("UNDERLAY_ON");
            }
            if (renderer.sharedMaterial != _tmpUnderlayInstance)
            {
                renderer.sharedMaterial = _tmpUnderlayInstance;
            }

            renderer.GetPropertyBlock(Mpb);

            Mpb.SetColor(UnderlayColorId, color);
            Mpb.SetFloat(UnderlayDilateId, dilate);
            Mpb.SetFloat(UnderlayOffsetXId, offsetX);
            Mpb.SetFloat(UnderlayOffsetYId, offsetY);
            Mpb.SetFloat(UnderlaySoftnessId, softness);

            renderer.SetPropertyBlock(Mpb);
        }
    }
}
