Shader "Hidden/NekoThemesPlus/Blur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex;
        float4 _MainTex_TexelSize;
        float _Offset;

        fixed4 fragDown(v2f_img input) : SV_Target
        {
            float2 offset = _MainTex_TexelSize.xy * _Offset;
            fixed4 color = tex2D(_MainTex, input.uv) * 4.0;
            color += tex2D(_MainTex, input.uv + float2(-offset.x, -offset.y));
            color += tex2D(_MainTex, input.uv + float2(offset.x, -offset.y));
            color += tex2D(_MainTex, input.uv + float2(-offset.x, offset.y));
            color += tex2D(_MainTex, input.uv + float2(offset.x, offset.y));
            return color / 8.0;
        }

        fixed4 fragUp(v2f_img input) : SV_Target
        {
            float2 offset = _MainTex_TexelSize.xy * _Offset;
            fixed4 color = 0;
            color += tex2D(_MainTex, input.uv + float2(-offset.x, 0.0));
            color += tex2D(_MainTex, input.uv + float2(offset.x, 0.0));
            color += tex2D(_MainTex, input.uv + float2(0.0, -offset.y));
            color += tex2D(_MainTex, input.uv + float2(0.0, offset.y));
            color += tex2D(_MainTex, input.uv + float2(-offset.x, -offset.y)) * 0.5;
            color += tex2D(_MainTex, input.uv + float2(offset.x, -offset.y)) * 0.5;
            color += tex2D(_MainTex, input.uv + float2(-offset.x, offset.y)) * 0.5;
            color += tex2D(_MainTex, input.uv + float2(offset.x, offset.y)) * 0.5;
            return color / 6.0;
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragDown
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragUp
            ENDCG
        }
    }
}
