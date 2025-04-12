using UnityEngine;
using UnityEngine.UI;

public class VolumeMeter : MonoBehaviour
{
    public AutoVoiceRecorder recorder;
    private Image image;
    private float displayVolume;

    void Start()
    {
        image = GetComponent<Image>();
    }

    void Update()
    {
        float rawVolume = recorder.latestMicVolume;  // �����ظ����� GetMicVolume

        // ӳ�䵽 0~1 ��Χ��������Ϊ 0.03 �����������
        float normalizedVolume = Mathf.Clamp01(rawVolume / 0.02f);

        // ƽ��������ʾ
        displayVolume = Mathf.Lerp(displayVolume, normalizedVolume, Time.deltaTime * 10f);

        // Ӧ���� fillAmount
        image.fillAmount = displayVolume;
    }

}
