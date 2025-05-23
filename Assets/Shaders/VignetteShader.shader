Shader "Custom/VignetteShader"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 1)  // 边缘颜色，黑色
        _Intensity ("Intensity", Range(0, 1)) = 0.5  // 暗角强度
        _Radius ("Radius", Range(0, 1)) = 0.5  // 暗角范围
        _WaveSpeed ("Wave Speed", Range(0, 2)) = 1.0  // 波动速度
        _MainTex ("Base (RGB)", 2D) = "white" { }  // 默认纹理
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" } // 设置为透明渲染
        Pass
        {
            // 设置混合模式为透明
            Blend SrcAlpha OneMinusSrcAlpha
            // 关闭深度写入，防止遮挡错误
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Shader中暴露的变量
            sampler2D _MainTex;
            float4 _Color;
            float _Intensity;
            float _Radius;
            float _WaveSpeed;

            float timeElapsed;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 center = float2(0.5, 0.5);  // 屏幕中心

                // 计算从中心到每个像素的距离，基于椭圆形
                float distX = abs(uv.x - center.x);  // 水平方向的距离
                float distY = abs(uv.y - center.y);  // 垂直方向的距离

                // 使用波动速度来控制强度
                float dynamicIntensity = _Intensity + sin(_Time.y * _WaveSpeed) * 0.2;  // 强度波动，波动速度通过 _WaveSpeed 控制
                float dynamicRadius = _Radius + sin(_Time.y * _WaveSpeed * 0.5) * 0.1;  // 半径波动

                // 计算椭圆形状的暗角效果
                float vignetteX = smoothstep(dynamicRadius, dynamicRadius + 0.1, distX);  // 水平方向渐变
                float vignetteY = smoothstep(dynamicRadius, dynamicRadius + 0.1, distY);  // 垂直方向渐变

                // 计算椭圆效果
                float vignette = smoothstep(dynamicRadius, dynamicRadius + 0.1, distX * distY);

                // 使用动态强度和范围调整最终的颜色，保持中心透明，边缘渐变为黑色
                float4 texColor = tex2D(_MainTex, uv);  // 获取纹理颜色
                texColor.rgb = lerp(texColor.rgb, _Color.rgb, vignette * dynamicIntensity);  // 渐变至黑色

                // 设置透明度，暗角部分显示，其他部分完全透明
                texColor.a = vignette;  // 使边缘部分不透明，其他部分完全透明

                return texColor;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}
