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
        float rawVolume = recorder.latestMicVolume;  // 不再重复调用 GetMicVolume

        // 映射到 0~1 范围（我们认为 0.03 是最大音量）
        float normalizedVolume = Mathf.Clamp01(rawVolume / 0.02f);

        // 平滑过渡显示
        displayVolume = Mathf.Lerp(displayVolume, normalizedVolume, Time.deltaTime * 10f);

        // 应用于 fillAmount
        image.fillAmount = displayVolume;
    }

}
