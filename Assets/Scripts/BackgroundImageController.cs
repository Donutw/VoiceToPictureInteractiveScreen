using UnityEngine;
using UnityEngine.UI;

public class BackgroundImageController : MonoBehaviour
{
    // 引用 Simulation2D 脚本，用于获取 enableSmoothReturn 状态
    public Simulation2D simulation;
    // 引用背景 Image 组件
    public RawImage backgroundImage;

    // 按下 B 键后延迟几秒开始淡入背景图片
    public float fadeInDelay = 3f;
    // 背景淡入的速度（值越大越快）
    public float fadeInSpeed = 2f;
    // 背景淡出的速度（值越大越快）
    public float fadeOutSpeed = 5f;

    // 内部计时器，用于计算 B 键按下的时间
    private float returnTimer = 0f;

    public ParticleDisplay2D particleDisplay;

    void OnEnable()
    {
        if (particleDisplay != null)
        {
            particleDisplay.OnCurrentTextureChanged += UpdateBackgroundTexture;
        }
    }

    void OnDisable()
    {
        if (particleDisplay != null)
        {
            particleDisplay.OnCurrentTextureChanged -= UpdateBackgroundTexture;
        }
    }

    void UpdateBackgroundTexture(Texture2D newTexture)
    {
        backgroundImage.texture = newTexture;
    }

    void Update()
    {
        // 如果 Simulation2D 中启用了平滑回归（按下 B 键）
        if (simulation.enableSmoothReturn)
        {
            returnTimer += Time.deltaTime;
            // 当计时器超过淡入延迟后，开始渐渐将背景图片的透明度调到 1（完全显示）
            if (returnTimer >= fadeInDelay)
            {
                Color col = backgroundImage.color;
                col.a = Mathf.Lerp(col.a, 1f, Time.deltaTime * fadeInSpeed);
                backgroundImage.color = col;
            }
        }
        else
        {
            // 松开 B 键时，重置计时器并快速将背景图片淡出（透明度趋向 0）
            returnTimer = 0f;
            Color col = backgroundImage.color;
            col.a = Mathf.Lerp(col.a, 0f, Time.deltaTime * fadeOutSpeed);
            backgroundImage.color = col;
        }
    }
}
