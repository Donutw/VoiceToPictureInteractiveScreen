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
        // 控制发光强度和速度
        float intensity = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f); // 呼吸感
        glowMaterial.SetFloat("_GlowIntensity", intensity);

        // 切换光环颜色
        glowMaterial.SetColor("_Color", mode == 0 ? recordingColor : loadingColor);

        // 按 Q 键切换状态
        if (Input.GetKeyDown(KeyCode.Q))
        {
            mode = 1 - mode;
        }
    }
}
