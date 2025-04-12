using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System.Linq;

public class ComfySender : MonoBehaviour
{
    public RawImage displayImage;

    // 文件夹路径：用于监听 prompt txt 的出现
    public string promptFolderPath = Path.Combine(Application.dataPath, "Transcripts");
    public string workflowPath = "Assets/StreamingAssets/comfy_prompt.json";
    public string comfyURL = "http://127.0.0.1:8188/prompt";
    public string outputImagePath = "D:/AI/ComfyUI-master/output";

    private string lastProcessedFile = "";

    void Update()
    {
        // 每一帧都扫描一下 prompt 文件夹是否有新的 txt
        var txtFiles = Directory.GetFiles(promptFolderPath, "*.txt");

        if (txtFiles.Length > 0)
        {
            // 找一个最早修改的文件来处理（避免冲突）
            string txtPath = txtFiles.OrderBy(File.GetLastWriteTime).First();

            // 避免重复处理同一个文件
            if (txtPath != lastProcessedFile)
            {
                lastProcessedFile = txtPath;
                StartCoroutine(ProcessPromptFile(txtPath));
            }
        }
    }

    IEnumerator ProcessPromptFile(string txtPath)
    {
        Debug.Log("Detected new prompt file: " + txtPath);

        string prompt = File.ReadAllText(txtPath).Trim();

        // 读取 workflow 模板
        if (!File.Exists(workflowPath))
        {
            Debug.LogError("Workflow JSON not found.");
            yield break;
        }

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

            // 删除处理过的 txt 文件
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
        var files = Directory.GetFiles(outputImagePath, "*.png");
        if (files.Length == 0)
        {
            Debug.LogWarning("No images found in output folder.");
            return;
        }

        string latest = files.OrderByDescending(File.GetLastWriteTime).First();
        byte[] data = File.ReadAllBytes(latest);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(data);
        displayImage.texture = tex;

        Debug.Log("Image loaded from: " + latest);
    }
}
