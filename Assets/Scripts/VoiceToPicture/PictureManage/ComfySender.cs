using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System.Linq;

public class ComfySender : MonoBehaviour
{
    public RawImage displayImage;

    public ParticleDisplay2D particleDisplay;
    public string promptFolderPath = "C:/Users/Newuser/Desktop/Fluid-Sim/Transcripts";
    public string workflowPath = "Assets/StreamingAssets/comfy_prompt.json";
    public string comfyURL = "http://127.0.0.1:8188/prompt";
    public string outputImagePath = "D:/AI/ComfyUI-master/output";

    private string lastProcessedFile = "";

    private System.Diagnostics.Process comfyProcess;

    //void Start()
    //{
    //    StartComfyUI();
    //}
    //void StartComfyUI()
    //{
    //    try
    //    {
    //        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
    //        startInfo.FileName = "C:\\Users\\Newuser\\AppData\\Local\\Programs\\Python\\Python313\\python.exe";
    //        startInfo.Arguments = "main.py";
    //        startInfo.WorkingDirectory = "D:\\AI\\ComfyUI-master"; // 替换成你实际路径
    //        startInfo.UseShellExecute = false;
    //        startInfo.CreateNoWindow = true;
    //        startInfo.RedirectStandardOutput = true;
    //        startInfo.RedirectStandardError = true;
    //        startInfo.EnvironmentVariables["VRAM_MODE"] = "LOW";

    //        comfyProcess = new System.Diagnostics.Process();
    //        comfyProcess.StartInfo = startInfo;
    //        comfyProcess.OutputDataReceived += (s, e) =>
    //        {
    //            if (!string.IsNullOrEmpty(e.Data))
    //            {
    //                if (e.Data.Contains("loaded completely") || e.Data.Contains("Prompt executed"))
    //                    Debug.Log("✅ ComfyUI: " + e.Data);
    //                else
    //                    Debug.Log("ComfyUI: " + e.Data); // 不是错误！
    //            }
    //        };

    //        comfyProcess.ErrorDataReceived += (s, e) =>
    //        {
    //            if (!string.IsNullOrEmpty(e.Data))
    //            {
    //                if (e.Data.Contains("cudaMalloc"))
    //                    Debug.LogError("❗️ComfyUI CUDA 错误: " + e.Data);
    //                else
    //                    Debug.LogWarning("ComfyUI 输出: " + e.Data); // 不是真 error，只是 stderr
    //            }
    //        };


    //        comfyProcess.Start();
    //        comfyProcess.BeginOutputReadLine();
    //        comfyProcess.BeginErrorReadLine();

    //        Debug.Log("🚀 启动 ComfyUI 成功！");
    //    }
    //    catch (System.Exception e)
    //    {
    //        Debug.LogError("❌ 启动 ComfyUI 失败：" + e.Message);
    //    }
    //}

    void Update()
    {
        var txtFiles = Directory.GetFiles(promptFolderPath, "*.txt");

        if (txtFiles.Length > 0)
        {
            string txtPath = txtFiles.OrderBy(File.GetLastWriteTime).First();
            string id = txtPath + File.GetLastWriteTime(txtPath).ToString();

            if (id != lastProcessedFile)
            {
                lastProcessedFile = id;
                StartCoroutine(ProcessPromptFile(txtPath));
            }
        }
    }
    

    IEnumerator ProcessPromptFile(string txtPath)
    {
        Debug.Log("Detected new prompt file: " + txtPath);

        string prompt = File.ReadAllText(txtPath).Trim();

        if (!File.Exists(workflowPath))
        {
            Debug.LogError("Workflow JSON not found.");
            yield break;
        }

        // ✅ 清除上一张图片
        ClearComfyOutputImages();

        string json = File.ReadAllText(workflowPath);
        json = json.Replace("$PROMPT$", prompt);

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(comfyURL, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Prompt sent successfully: " + prompt);
            yield return new WaitForSeconds(3f);
            LoadGeneratedImage();

            File.Delete(txtPath);
            Debug.Log("Deleted prompt file: " + txtPath);
        }
        else
        {
            Debug.LogError("Failed to send prompt: " + request.error);
        }
    }

    void LoadGeneratedImage()
    {
        if (string.IsNullOrEmpty(outputImagePath) || !Directory.Exists(outputImagePath))
            return;

        var files = Directory.GetFiles(outputImagePath, "*.png", SearchOption.AllDirectories);
        if (files.Length == 0) return;

        string latest = files.OrderByDescending(File.GetLastWriteTime).First();
        byte[] data = File.ReadAllBytes(latest);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(data);
        tex.name = "DynamicGenerated";

        CurrentImageController.Instance.UpdateImage(tex);

        if (particleDisplay != null)
            particleDisplay.SetImage(tex);
    }

    // ✅ 新增：清空 ComfyUI 输出文件夹的图片
    void ClearComfyOutputImages()
    {
        if (Directory.Exists(outputImagePath))
        {
            var images = Directory.GetFiles(outputImagePath, "*.png");
            foreach (var file in images)
            {
                try
                {
                    File.Delete(file);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"❗️ 删除 ComfyUI 图像失败: {file} → {e.Message}");
                }
            }

            Debug.Log($"🧹 清空 ComfyUI 输出图像，共删除 {images.Length} 张");
        }
    }
    //void OnApplicationQuit()
    //{
    //    if (comfyProcess != null && !comfyProcess.HasExited)
    //    {
    //        comfyProcess.Kill(); // 强制终止
    //        comfyProcess.Dispose();
    //        Debug.Log("🛑 已关闭 ComfyUI");
    //    }
    //}

}
