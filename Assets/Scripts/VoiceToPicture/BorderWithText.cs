using UnityEngine;
using UnityEngine.UI;

public class BorderWithText : MonoBehaviour
{
    public Material glowMaterial;
    public Color recordingColor = Color.red;
    public Color loadingColor = Color.cyan;
    private int mode = 0;

    void Update()
    {
        // ���Ʒ���ǿ�Ⱥ��ٶ�
        float intensity = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f); // ������
        glowMaterial.SetFloat("_GlowIntensity", intensity);

        // �л��⻷��ɫ
        glowMaterial.SetColor("_Color", mode == 0 ? recordingColor : loadingColor);

        // �� Q ���л�״̬
        if (Input.GetKeyDown(KeyCode.Q))
        {
            mode = 1 - mode;
        }
    }
}
