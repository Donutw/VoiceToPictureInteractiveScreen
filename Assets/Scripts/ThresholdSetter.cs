using UnityEngine;
using UnityEngine.UI;

public class ThresholdSetter : MonoBehaviour
{
    public AutoVoiceRecorder recorder;
    private Slider slider;

    void Start()
    {
        slider = GetComponent<Slider>();
        slider.minValue = 0.001f;
        slider.maxValue = 0.03f;
        slider.value = recorder.silenceThreshold; // 初始值同步
    }

    void Update()
    {
        recorder.silenceThreshold = slider.value;
    }
}
