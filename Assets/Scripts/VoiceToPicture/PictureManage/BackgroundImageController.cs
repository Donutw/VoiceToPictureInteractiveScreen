using UnityEngine;
using UnityEngine.UI;

public class BackgroundImageController : MonoBehaviour
{
    public Simulation2D simulation;
    public RawImage backgroundImage;

    public float fadeInDelay = 3f;
    public float fadeInSpeed = 2f;
    public float fadeOutSpeed = 5f;

    private float returnTimer = 0f;

    private Texture2D lastTexture = null;

    void Update()
    {
        // 检查贴图是否更新（来自 CurrentImageController）
        var currentTex = CurrentImageController.Instance?.GetCurrentImage();
        if (currentTex != null && currentTex != lastTexture)
        {
            backgroundImage.texture = currentTex;
            lastTexture = currentTex;
        }

        if (simulation.enableSmoothReturn)
        {
            returnTimer += Time.deltaTime;

            if (returnTimer >= fadeInDelay)
            {
                Color col = backgroundImage.color;
                col.a = Mathf.Lerp(col.a, 1f, Time.deltaTime * fadeInSpeed);
                backgroundImage.color = col;
            }
        }
        else
        {
            returnTimer = 0f;
            Color col = backgroundImage.color;
            col.a = Mathf.Lerp(col.a, 0f, Time.deltaTime * fadeOutSpeed);
            backgroundImage.color = col;
        }
    }
}
