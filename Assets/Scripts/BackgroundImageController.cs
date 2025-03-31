using UnityEngine;
using UnityEngine.UI;

public class BackgroundImageController : MonoBehaviour
{
    // ���� Simulation2D �ű������ڻ�ȡ enableSmoothReturn ״̬
    public Simulation2D simulation;
    // ���ñ��� Image ���
    public RawImage backgroundImage;

    // ���� B �����ӳټ��뿪ʼ���뱳��ͼƬ
    public float fadeInDelay = 3f;
    // ����������ٶȣ�ֵԽ��Խ�죩
    public float fadeInSpeed = 2f;
    // �����������ٶȣ�ֵԽ��Խ�죩
    public float fadeOutSpeed = 5f;

    // �ڲ���ʱ�������ڼ��� B �����µ�ʱ��
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
        // ��� Simulation2D ��������ƽ���ع飨���� B ����
        if (simulation.enableSmoothReturn)
        {
            returnTimer += Time.deltaTime;
            // ����ʱ�����������ӳٺ󣬿�ʼ����������ͼƬ��͸���ȵ��� 1����ȫ��ʾ��
            if (returnTimer >= fadeInDelay)
            {
                Color col = backgroundImage.color;
                col.a = Mathf.Lerp(col.a, 1f, Time.deltaTime * fadeInSpeed);
                backgroundImage.color = col;
            }
        }
        else
        {
            // �ɿ� B ��ʱ�����ü�ʱ�������ٽ�����ͼƬ������͸�������� 0��
            returnTimer = 0f;
            Color col = backgroundImage.color;
            col.a = Mathf.Lerp(col.a, 0f, Time.deltaTime * fadeOutSpeed);
            backgroundImage.color = col;
        }
    }
}
