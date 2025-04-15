using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Linq;

public class AutoVoiceRecorder : MonoBehaviour
{
    [Header("麦克风设置")]
    public string selectedMic = null; // 默认使用系统默认麦克风
    public int sampleRate = 44100;

    [Header("灵敏度设置")]
    [Range(0.001f, 0.03f)] public float silenceThreshold = 0.01f;     // 小于这个值视为安静
    public float loudCheckPeriod = 0.3f; // 在n秒内检查累计说话时长
    [Range(0f, 1f)] public float requiredLoudRatio = 0.6f; // 比例阈值：至少 60% 的非零帧音量高于 silenceThreshold
    public float rollingSilenceDuration = 1.2f;
    [Range(0f, 1f)] public float requiredSilenceRatio = 0.8f; // 比如过去1.2秒中80%是静音就结束录音
    public float sampleDuration = 10f; // 安静检测时间

    private bool isRecording = false;
    private bool isCalibrating = false;

    private AudioClip recordingClip;
    private float totalRecordTime = 0f;

    private float loudCheckTimer = 0f;
    private List<float> volumeBuffer = new List<float>();
    private List<float> rollingSilenceBuffer = new List<float>();

    private int micPosition = 0;
    private const int maxRecordSeconds = 10;

    float cooldownTimer = 0f;
    public float cooldownDuration = 1.0f;

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
        StartCoroutine(CalibrateSilenceThreshold(sampleDuration));
    }

    void StartMic()
    {
        recordingClip = Microphone.Start(selectedMic, true, maxRecordSeconds, sampleRate);
        micPosition = 0;
        UnityEngine.Debug.Log("🎧 开始新一轮监听...");
    }


    void Update()
    {
        if (isCalibrating) return; // 正在校准时不做任何录音判断
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }
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


                if (totalRecordTime >= maxRecordSeconds - 0.1f)
                {
                    UnityEngine.Debug.Log("⏰ 自动触发最大录音时间限制，保存");
                    StopAndSaveRecording();
                    return;
                }

                if (volume > 0f)
                    rollingSilenceBuffer.Add(volume);

                // 控制 buffer 长度不超过 1.2 秒对应的帧数
                int maxCount = Mathf.RoundToInt(rollingSilenceDuration / Time.deltaTime);
                if (rollingSilenceBuffer.Count > maxCount)
                    rollingSilenceBuffer.RemoveAt(0);

                // 每帧判断是否满足静音
                if (rollingSilenceBuffer.Count > 10)
                {
                    int silentCount = rollingSilenceBuffer.FindAll(v => v < silenceThreshold).Count;
                    float ratio = (float)silentCount / rollingSilenceBuffer.Count;

                    // 可选调试打印
                    // Debug.Log($"🧪 静音比例: {ratio:P0}");

                    if (ratio >= requiredSilenceRatio && totalRecordTime > 1.0f)
                    {
                        UnityEngine.Debug.Log("🛑 最近 1.2 秒大部分是静音，保存录音");
                        StopAndSaveRecording();
                    }

                }
            }
        }
        CheckTranscriptForTrigger();
    }
    void CheckTranscriptForTrigger()
    {
        string folder = Application.dataPath + "/../Transcripts";
        if (!Directory.Exists(folder)) return;
        string[] keywords = { "开始检测", "重新检测", "校准灵敏度", "检测" };

        string[] files = Directory.GetFiles(folder, "*.txt");
        foreach (string file in files)
        {
            string text = File.ReadAllText(file, System.Text.Encoding.UTF8);
            if (keywords.Any(kw => text.Contains(kw)))
            {
                Debug.Log($"🗣️ 触发重设灵敏度指令，来源文件: {Path.GetFileName(file)}");
                StartCoroutine(CalibrateSilenceThreshold());

                // 防止重复触发：重命名或删除
                File.Delete(file); // 或者 File.Move(file, file + ".processed");
                break; // 一次只触发一个
            }
        }
    }

    IEnumerator CalibrateSilenceThreshold(float _sampleDuration = 10f)
    {
        // 🛑 如果正在录音，先结束
        if (isRecording)
        {
            Debug.Log("⚠️ 检测过程中录音未结束，自动终止录音");
            StopRecordingWithoutSave(); //如果你不想保存
        }
        isCalibrating = true;
        List<float> samples = new List<float>();
        float timer = 0f;

        UnityEngine.Debug.Log("📢 开始静音校准，请保持安静...");

        while (timer < _sampleDuration)
        {
            float volume = GetMicVolumeSimple();
            if (volume > 0f) samples.Add(volume); // 只记录非零音量
            timer += Time.deltaTime;
            yield return null;
        }

        if (samples.Count == 0)
        {
            UnityEngine.Debug.LogWarning("⚠️ 校准失败：未检测到有效音量数据");
            yield break;
        }

        float average = 0f;
        foreach (float v in samples) average += v;
        average /= samples.Count;

        silenceThreshold = average * 1.8f; // 可调节倍数
        UnityEngine.Debug.Log($"✅ 校准完成！环境音量平均值: {average:F5}，设置的 silenceThreshold: {silenceThreshold:F5}");
        isCalibrating = false;
    }
    public float GetMicVolumeSimple()
    {
        if (recordingClip == null) return 0f;

        int sampleCount = 1024;
        float[] samples = new float[sampleCount];
        int micPos = Microphone.GetPosition(selectedMic);
        if (micPos < sampleCount) return 0f;

        recordingClip.GetData(samples, micPos - sampleCount);

        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
            sum += samples[i] * samples[i];

        return Mathf.Sqrt(sum / sampleCount);
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
        loudCheckTimer = 0f;
        totalRecordTime = 0f;
        rollingSilenceBuffer.Clear();

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
        cooldownTimer = cooldownDuration;  // 防止立刻又触发新录音

        StartMic(); // 保存后进入下一轮监听
    }
    void StopRecordingWithoutSave()
    {
        Debug.Log("🛑 录音强制中断，未保存");
        isRecording = false;
        Microphone.End(selectedMic);
        StartMic(); // 回到监听状态
    }
}


