Shader "Hidden/NekoThemesPlus/Background"
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
            float4 _SourceSize;
            float4 _TargetSize;
            float _ImageMode;
            float _ImageZoom;
            float4 _ImageAlignment;
            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 uv = input.uv;
                float sourceAspect = _SourceSize.x / max(_SourceSize.y, 1.0);
                float targetAspect = _TargetSize.x / max(_TargetSize.y, 1.0);
                float mask = 1.0;
                float zoom = clamp(_ImageZoom, 1.0, 4.0);
                float2 alignment = saturate(_ImageAlignment.xy);

                if (_ImageMode < 0.5) // Fill
                {
                    float2 visible = float2(1.0, 1.0);
                    if (sourceAspect > targetAspect)
                        visible.x = targetAspect / sourceAspect;
                    else
                        visible.y = sourceAspect / targetAspect;
                    visible /= zoom;
                    float2 origin = float2(
                        (1.0 - visible.x) * alignment.x,
                        (1.0 - visible.y) * (1.0 - alignment.y));
                    uv = origin + uv * visible;
                }
                else if (_ImageMode < 1.5) // Fit
                {
                    float2 fitted = float2(1.0, 1.0);
                    if (sourceAspect > targetAspect)
                        fitted.y = targetAspect / sourceAspect;
                    else
                        fitted.x = sourceAspect / targetAspect;
                    fitted *= zoom;
                    float2 origin = float2(
                        (1.0 - fitted.x) * alignment.x,
                        (1.0 - fitted.y) * (1.0 - alignment.y));
                    uv = (uv - origin) / fitted;
                    mask = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
                }
                else if (_ImageMode < 2.5) // Stretch
                {
                    float2 visible = 1.0 / zoom;
                    float2 origin = float2(
                        (1.0 - visible.x) * alignment.x,
                        (1.0 - visible.y) * (1.0 - alignment.y));
                    uv = origin + input.uv * visible;
                }
                else if (_ImageMode < 3.5) // Center, source pixels scaled by zoom
                {
                    float2 drawnSize = _SourceSize.xy * zoom;
                    float2 origin = float2(
                        (_TargetSize.x - drawnSize.x) * alignment.x,
                        (_TargetSize.y - drawnSize.y) * (1.0 - alignment.y));
                    uv = (input.uv * _TargetSize.xy - origin) / drawnSize;
                    mask = step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
                }
                else // Tile
                {
                    uv = frac(input.uv * _TargetSize.xy / (_SourceSize.xy * zoom));
                }

                fixed4 color = tex2D(_MainTex, saturate(uv));
                color.a *= mask;
                color.rgb *= mask;
                return color;
            }
            ENDCG
        }
    }
}
