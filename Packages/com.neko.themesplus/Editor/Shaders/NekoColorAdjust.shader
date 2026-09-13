Shader "Hidden/NekoThemesPlus/ColorAdjust"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Brightness;
            float _Saturation;
            float _Contrast;
            float4 _Tint;
            float _Opacity;

            fixed4 frag(v2f_img input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv);
                color.rgb = (color.rgb - 0.5) * _Contrast + 0.5;
                float luminance = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
                color.rgb = lerp(luminance.xxx, color.rgb, _Saturation);
                color.rgb *= _Brightness;
                color.rgb = lerp(color.rgb, color.rgb * (_Tint.rgb * 2.0), saturate(_Tint.a));
                color.a *= _Opacity;
                return color;
            }
            ENDCG
        }
    }
}
