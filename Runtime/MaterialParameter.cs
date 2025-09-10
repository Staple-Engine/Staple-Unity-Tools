using UnityEngine;

namespace Staple
{
    public class MaterialParameter
    {
        public MaterialParameterType type;

        public Vector2Holder vec2Value;

        public Vector3Holder vec3Value;

        public Vector4Holder vec4Value;

        public string textureValue;

        public Color32 colorValue;

        public float floatValue;

        public TextureWrap textureWrapValue;

        public MaterialParameterSource source;

        public int intValue;

        public bool ShouldSerializevec2Value() => vec2Value != null && type == MaterialParameterType.Vector2;

        public bool ShouldSerializevec3Value() => vec3Value != null && type == MaterialParameterType.Vector3;

        public bool ShouldSerializevec4Value() => vec4Value != null && type == MaterialParameterType.Vector4;

        public bool ShouldSerializefloatValue() => type == MaterialParameterType.Float;

        public bool ShouldSerializeintValue() => type == MaterialParameterType.Int;

        public bool ShouldSerializetextureValue() => type == MaterialParameterType.Texture;

        public bool ShouldSerializecolorValue() => type == MaterialParameterType.Color;

        public bool ShouldSerializetextureWrapValue() => type == MaterialParameterType.TextureWrap;
    }
}
