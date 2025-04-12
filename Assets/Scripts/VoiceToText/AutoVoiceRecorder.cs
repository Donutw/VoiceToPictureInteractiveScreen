using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;

public class AutoVoiceRecorder : MonoBehaviour
{
    [Header("麦克风设置")]
    public string selectedMic = null; // 默认使用系统默认麦克风
    public int sampleRate = 44100;

    [Header("灵敏度设置")]
    [Range(0.001f, 0.02f)] public float silenceThreshold = 0.01f;     // 小于这个值视为安静
    public float loudCheckPeriod = 0.3f; // 在n秒内检查累计说话时长
    [Range(0f, 1f)] public float requiredLoudRatio = 0.6f; // 比例阈值：至少 60% 的非零帧音量高于 silenceThreshold
    public float silenceCheckPeriod = 0.3f;
    [Range(0f, 1f)] public float requiredSilenceRatio = 0.8f; // 比如过去1.2秒中80%是静音就结束录音

    private bool isRecording = false;

    private AudioClip recordingClip;
    private float totalRecordTime = 0f;

    private float loudCheckTimer = 0f;
    private List<float> volumeBuffer = new List<float>();
    private float silenceCheckTimer = 0f;
    private List<float> silenceVolumeBuffer = new List<float>();
    
    private int micPosition = 0;
    private const int maxRecordSeconds = 10;

    [HideInInspector]
    public float latestMicVolume = 0f;//传给可视化使用的

    void Start()
    {
        // 自动使用系统默认麦克风，或可替换为指定设备名
        if (Microphone.devices.Length > 0)
        {
            if (string.IsNullOrEmpty(selectedMic))
            {
                selectedMic = Microphone.devices[0];
                UnityEngine.Debug.Log("使用默认麦克风: " + selectedMic);
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("没有检测到麦克风设备！");
            return;
        }

        StartMic(); // 开始监听
    }

    void StartMic()
    {
        recordingClip = Microphone.Start(selectedMic, true, maxRecordSeconds, sampleRate);
        micPosition = 0;
        //UnityEngine.Debug.Log("🎧 开始新一轮监听...");
    }


    void Update()
    {
        if (Microphone.IsRecording(selectedMic))
        {
            latestMicVolume = GetMicVolume();
            float volume = latestMicVolume;
           
            if (!isRecording)
            {
                loudCheckTimer += Time.deltaTime;

                if (volume > 0f) volumeBuffer.Add(volume);

                if (loudCheckTimer >= loudCheckPeriod)
                {
                    // 去除 volume == 0 的帧（无效）
                    List<float> validVolumes = volumeBuffer.FindAll(v => v > 0f);

                    if (validVolumes.Count > 0)
                    {
                        int loudCount = validVolumes.FindAll(v => v > silenceThreshold).Count;
                        float ratio = (float)loudCount / validVolumes.Count;

                        //UnityEngine.Debug.Log($"🧪 有效帧数: {validVolumes.Count}, 高于阈值帧数: {loudCount}, 占比: {ratio:P0}");

                        if (ratio >= requiredLoudRatio)
                        {
                            UnityEngine.Debug.Log("✅ 检测到足够的说话帧，开始录音！");
                            StartActualRecording();
                        }
                        else
                        {
                            //UnityEngine.Debug.Log("🟡 没有达到说话比例要求，继续监听");
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.Log("🔇 没有有效音量帧，跳过判断");
                    }

                    // 重置检测
                    volumeBuffer.Clear();
                    loudCheckTimer = 0f;
                }
            }
            else
            {
                totalRecordTime += Time.deltaTime;
                silenceCheckTimer += Time.deltaTime;

                if (totalRecordTime >= maxRecordSeconds - 0.1f)
                {
                    UnityEngine.Debug.Log("⏰ 自动触发最大录音时间限制，保存");
                    StopAndSaveRecording();
                    return;
                }

                if (volume > 0f) silenceVolumeBuffer.Add(volume);

                if (silenceCheckTimer >= silenceCheckPeriod)
                {
                    List<float> validVolumes = silenceVolumeBuffer.FindAll(v => v > 0f);

                    if (validVolumes.Count > 0)
                    {
                        int silentCount = validVolumes.FindAll(v => v < silenceThreshold).Count;
                        float ratio = (float)silentCount / validVolumes.Count;

                        //UnityEngine.Debug.Log($"🧪 录音中检测：静音比例 {ratio:P0}");

                        if (ratio >= requiredSilenceRatio && totalRecordTime > 1.0f)
                        {
                            UnityEngine.Debug.Log("🛑 检测到说话停止，保存录音");
                            StopAndSaveRecording();
                        }
                    }

                    // 重置判断
                    silenceCheckTimer = 0f;
                    silenceVolumeBuffer.Clear();
                }
            }
        }
    }

    public float GetMicVolume()
    {
        if (recordingClip == null)
        {
            UnityEngine.Debug.LogWarning("❗ AudioClip 为 null，无法读取音频数据");
            return 0f;
        }

        int currentPosition = Microphone.GetPosition(selectedMic);
        int length = currentPosition - micPosition;
        if (length < 0) length += recordingClip.samples;

        if (length <= 0)
        {
            // 没有新采样数据
            return 0f;
        }

        // 使用动态长度的临时 buffer 读取数据
        float[] tempData = new float[length];
        recordingClip.GetData(tempData, micPosition);
        micPosition = currentPosition;

        // 计算 RMS 音量值
        float sum = 0f;
        for (int i = 0; i < tempData.Length; i++)
        {
            sum += tempData[i] * tempData[i];
        }

        float rms = Mathf.Sqrt(sum / tempData.Length);

        // 可选：调试打印（建议测试时开）
        //UnityEngine.Debug.Log($"📈 音量: {rms:F5}（采样数: {length}）");

        return rms;
    }



    void StartActualRecording()
    {
        UnityEngine.Debug.Log("🎙️ 开始录音...");
        isRecording = true;
        silenceCheckTimer = 0f;
        loudCheckTimer = 0f;
        totalRecordTime = 0f;

        // 停掉当前循环监听录音，开始实际录制（非循环）
        Microphone.End(selectedMic);
        recordingClip = Microphone.Start(selectedMic, false, maxRecordSeconds, sampleRate);
    }

    void StopAndSaveRecording()
    {
        UnityEngine.Debug.Log("🛑 录音结束，保存音频...");
        isRecording = false;

        int samplesRecorded = Microphone.GetPosition(selectedMic);
        Microphone.End(selectedMic);

        if (samplesRecorded <= 0)
        {
            UnityEngine.Debug.LogWarning("录音数据无效，未保存");
            StartMic(); // 重新进入监听
            return;
        }

        AudioClip trimmedClip = AudioClip.Create("recorded", samplesRecorded, 1, sampleRate, false);
        float[] samples = new float[samplesRecorded];
        recordingClip.GetData(samples, 0);
        trimmedClip.SetData(samples, 0);

        // 加上时间戳保存
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string folder = Application.dataPath + "/../AudioInput";
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, $"recording_{timestamp}.wav");

        WavUtility.Save(trimmedClip, path);
        UnityEngine.Debug.Log("✅ 音频已保存：" + path);

        StartMic(); // 保存后进入下一轮监听
    }
}


