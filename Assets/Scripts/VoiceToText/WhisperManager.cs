using UnityEngine;
using System.Diagnostics;

public class WhisperManager : MonoBehaviour
{
    private Process whisperProcess;

    void Start()
    {
        string pyPath = Application.dataPath + "/../whisper_transcribe.py";

        whisperProcess = new Process();
        whisperProcess.StartInfo.FileName = "C:\\Users\\Newuser\\AppData\\Local\\Programs\\Python\\Python313\\python.exe";
        whisperProcess.StartInfo.Arguments = $"\"{pyPath}\"";
        whisperProcess.StartInfo.UseShellExecute = false;
        whisperProcess.StartInfo.CreateNoWindow = true;
        // 👇👇👇 加这句才是真正关键！
        whisperProcess.StartInfo.WorkingDirectory = Application.dataPath + "/..";

        try
        {
            whisperProcess.Start();
            UnityEngine.Debug.Log("✅ Whisper 后台监听已启动");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError("❌ 启动 Python 失败: " + ex.Message);
        }
    }

    void OnApplicationQuit()
    {
        if (whisperProcess != null && !whisperProcess.HasExited)
        {
            whisperProcess.Kill();
            whisperProcess.Dispose();
            UnityEngine.Debug.Log("🛑 Whisper 后台监听已关闭");
        }
    }
}
