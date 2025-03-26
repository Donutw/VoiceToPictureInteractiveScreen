Shader "Custom/Particle2D_UVMapping_CombinedBlended" {
    Properties {
        _MainTex("Main Texture", 2D) = "white" {}
        _StarTex("Star Texture", 2D) = "white" {}
        scale("Scale", Float) = 1.0
        _Blend("Blend Factor", Range(0,1)) = 0.5
        _GlowIntensity("Glow Intensity", Range(1, 5)) = 1.5
    }
    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "UnityCG.cginc"

            // 从 CPU/Compute Shader 传入的数据
            StructuredBuffer<float2> Positions2D;    // 每个粒子的中心位置
            StructuredBuffer<float2> UVs;            // 每个粒子用来采样 _MainTex 的 UV 坐标
            StructuredBuffer<float2> Velocities;       // 每个粒子的速度

            float scale;
            float velocityMax;       // 传入的最大速度值，用于归一化速度
            float _Blend;            // 控制图片色与渐变色混合比例（0～1）
            float _GlowIntensity;

            sampler2D _StarTex;
            sampler2D _MainTex;
            Texture2D<float4> ColourMap;
            SamplerState linear_clamp_sampler;

            // 顶点输入：使用了四边形 Mesh 的局部顶点信息
            struct appdata {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0; // 局部UV，通常范围为[0,1]
            };

            // 顶点到片元的数据传递
            struct v2f {
                float4 pos : SV_POSITION;
                float2 localUV : TEXCOORD0;    // 用于生成粒子圆形遮罩
                float2 instanceUV : TEXCOORD1; // 用于采样 _MainTex 的全局图片
                float gradientFactor : TEXCOORD2; // 根据速度计算的用于采样 ColourMap 的因子
            };

            v2f vert (appdata v, uint instanceID : SV_InstanceID) {
                v2f o;
                // 从 Buffer 中取出每个粒子的中心位置和用于采样图片的 UV
                float2 particlePos = Positions2D[instanceID];
                float2 instUV = UVs[instanceID];

                float3 centreWorld = float3(particlePos, 0);
                // 计算当前粒子四边形顶点的世界位置：
                float3 worldVertPos = centreWorld + mul(unity_ObjectToWorld, v.vertex * scale).xyz;
                o.pos = UnityObjectToClipPos(float4(worldVertPos, 1));

                // 将四边形本身的局部 UV 传递出去，用于计算圆形遮罩
                o.localUV = v.texcoord;
                // 将 Buffer 中预先计算的全局采样 UV 传递出去
                o.instanceUV = instUV;

                // 利用每个粒子的速度计算一个归一化的因子（0～1），供 ColourMap 采样
                float speed = length(Velocities[instanceID]);
                o.gradientFactor = saturate(speed / velocityMax);

                return o;
            }

            float4 frag (v2f i) : SV_Target {
                // 计算局部遮罩：利用四边形局部 UV 生成一个圆形（圆心在0.5,0.5）的软边遮罩
                float2 centreOffset = (i.localUV - 0.5) * 2;  // 转换到 [-1,1] 范围
                float sqrDst = dot(centreOffset, centreOffset);
                float delta = fwidth(sqrt(sqrDst));
                //float mask = 1 - smoothstep(1 - delta, 1 + delta, sqrDst);

                // 从 _MainTex 中采样图片颜色，使用每个粒子的全局采样 UV
                float4 imageColor = tex2D(_MainTex, i.instanceUV);
                // 从 ColourMap 渐变纹理中采样颜色，根据粒子速度（gradientFactor）
                float4 gradColor = ColourMap.SampleLevel(linear_clamp_sampler, float2(i.gradientFactor, 0.5), 0);

                // 混合图片颜色和渐变颜色，混合比例由 _Blend 控制（0=全图片色，1=全渐变色）
                float4 finalColor = imageColor * lerp(float4(1,1,1,1), gradColor, _Blend);

                //新增粒子纹理采样
                  float4 starTexture = tex2D(_StarTex, i.localUV);
                  finalColor *= starTexture;

                  //额外增加自发光强度（中心更亮）
                  finalColor.rgb *= lerp(_GlowIntensity, 1.0, starTexture.a);
                // 将混合后的颜色的 alpha 乘以圆形遮罩，使得粒子呈现圆形
                //finalColor.a *= mask;

                return finalColor;
            }
            ENDCG
        }
    }
}
