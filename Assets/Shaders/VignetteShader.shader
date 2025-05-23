Shader "Custom/VignetteShader"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 1)  // ��Ե��ɫ����ɫ
        _Intensity ("Intensity", Range(0, 1)) = 0.5  // ����ǿ��
        _Radius ("Radius", Range(0, 1)) = 0.5  // ���Ƿ�Χ
        _WaveSpeed ("Wave Speed", Range(0, 2)) = 1.0  // �����ٶ�
        _MainTex ("Base (RGB)", 2D) = "white" { }  // Ĭ������
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" } // ����Ϊ͸����Ⱦ
        Pass
        {
            // ���û��ģʽΪ͸��
            Blend SrcAlpha OneMinusSrcAlpha
            // �ر����д�룬��ֹ�ڵ�����
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

            // Shader�б�¶�ı���
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
                float2 center = float2(0.5, 0.5);  // ��Ļ����

                // ��������ĵ�ÿ�����صľ��룬������Բ��
                float distX = abs(uv.x - center.x);  // ˮƽ����ľ���
                float distY = abs(uv.y - center.y);  // ��ֱ����ľ���

                // ʹ�ò����ٶ�������ǿ��
                float dynamicIntensity = _Intensity + sin(_Time.y * _WaveSpeed) * 0.2;  // ǿ�Ȳ����������ٶ�ͨ�� _WaveSpeed ����
                float dynamicRadius = _Radius + sin(_Time.y * _WaveSpeed * 0.5) * 0.1;  // �뾶����

                // ������Բ��״�İ���Ч��
                float vignetteX = smoothstep(dynamicRadius, dynamicRadius + 0.1, distX);  // ˮƽ���򽥱�
                float vignetteY = smoothstep(dynamicRadius, dynamicRadius + 0.1, distY);  // ��ֱ���򽥱�

                // ������ԲЧ��
                float vignette = smoothstep(dynamicRadius, dynamicRadius + 0.1, distX * distY);

                // ʹ�ö�̬ǿ�Ⱥͷ�Χ�������յ���ɫ����������͸������Ե����Ϊ��ɫ
                float4 texColor = tex2D(_MainTex, uv);  // ��ȡ������ɫ
                texColor.rgb = lerp(texColor.rgb, _Color.rgb, vignette * dynamicIntensity);  // ��������ɫ

                // ����͸���ȣ����ǲ�����ʾ������������ȫ͸��
                texColor.a = vignette;  // ʹ��Ե���ֲ�͸��������������ȫ͸��

                return texColor;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}
