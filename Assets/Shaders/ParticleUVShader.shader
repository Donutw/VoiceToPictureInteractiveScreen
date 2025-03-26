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

            // �� CPU/Compute Shader ���������
            StructuredBuffer<float2> Positions2D;    // ÿ�����ӵ�����λ��
            StructuredBuffer<float2> UVs;            // ÿ�������������� _MainTex �� UV ����
            StructuredBuffer<float2> Velocities;       // ÿ�����ӵ��ٶ�

            float scale;
            float velocityMax;       // ���������ٶ�ֵ�����ڹ�һ���ٶ�
            float _Blend;            // ����ͼƬɫ�뽥��ɫ��ϱ�����0��1��
            float _GlowIntensity;

            sampler2D _StarTex;
            sampler2D _MainTex;
            Texture2D<float4> ColourMap;
            SamplerState linear_clamp_sampler;

            // �������룺ʹ�����ı��� Mesh �ľֲ�������Ϣ
            struct appdata {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0; // �ֲ�UV��ͨ����ΧΪ[0,1]
            };

            // ���㵽ƬԪ�����ݴ���
            struct v2f {
                float4 pos : SV_POSITION;
                float2 localUV : TEXCOORD0;    // ������������Բ������
                float2 instanceUV : TEXCOORD1; // ���ڲ��� _MainTex ��ȫ��ͼƬ
                float gradientFactor : TEXCOORD2; // �����ٶȼ�������ڲ��� ColourMap ������
            };

            v2f vert (appdata v, uint instanceID : SV_InstanceID) {
                v2f o;
                // �� Buffer ��ȡ��ÿ�����ӵ�����λ�ú����ڲ���ͼƬ�� UV
                float2 particlePos = Positions2D[instanceID];
                float2 instUV = UVs[instanceID];

                float3 centreWorld = float3(particlePos, 0);
                // ���㵱ǰ�����ı��ζ��������λ�ã�
                float3 worldVertPos = centreWorld + mul(unity_ObjectToWorld, v.vertex * scale).xyz;
                o.pos = UnityObjectToClipPos(float4(worldVertPos, 1));

                // ���ı��α���ľֲ� UV ���ݳ�ȥ�����ڼ���Բ������
                o.localUV = v.texcoord;
                // �� Buffer ��Ԥ�ȼ����ȫ�ֲ��� UV ���ݳ�ȥ
                o.instanceUV = instUV;

                // ����ÿ�����ӵ��ٶȼ���һ����һ�������ӣ�0��1������ ColourMap ����
                float speed = length(Velocities[instanceID]);
                o.gradientFactor = saturate(speed / velocityMax);

                return o;
            }

            float4 frag (v2f i) : SV_Target {
                // ����ֲ����֣������ı��ξֲ� UV ����һ��Բ�Σ�Բ����0.5,0.5�����������
                float2 centreOffset = (i.localUV - 0.5) * 2;  // ת���� [-1,1] ��Χ
                float sqrDst = dot(centreOffset, centreOffset);
                float delta = fwidth(sqrt(sqrDst));
                //float mask = 1 - smoothstep(1 - delta, 1 + delta, sqrDst);

                // �� _MainTex �в���ͼƬ��ɫ��ʹ��ÿ�����ӵ�ȫ�ֲ��� UV
                float4 imageColor = tex2D(_MainTex, i.instanceUV);
                // �� ColourMap ���������в�����ɫ�����������ٶȣ�gradientFactor��
                float4 gradColor = ColourMap.SampleLevel(linear_clamp_sampler, float2(i.gradientFactor, 0.5), 0);

                // ���ͼƬ��ɫ�ͽ�����ɫ����ϱ����� _Blend ���ƣ�0=ȫͼƬɫ��1=ȫ����ɫ��
                float4 finalColor = imageColor * lerp(float4(1,1,1,1), gradColor, _Blend);

                //���������������
                  float4 starTexture = tex2D(_StarTex, i.localUV);
                  finalColor *= starTexture;

                  //���������Է���ǿ�ȣ����ĸ�����
                  finalColor.rgb *= lerp(_GlowIntensity, 1.0, starTexture.a);
                // ����Ϻ����ɫ�� alpha ����Բ�����֣�ʹ�����ӳ���Բ��
                //finalColor.a *= mask;

                return finalColor;
            }
            ENDCG
        }
    }
}
