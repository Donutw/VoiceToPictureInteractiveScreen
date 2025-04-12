using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.IO;

public class ComfySender : MonoBehaviour
{
    public InputField promptInput;
    public RawImage displayImage;
    public string comfyURL = "http://127.0.0.1:8188/prompt";
    public string outputImagePath = "D:/ComfyUI/output/"; // 改成你的路径

    public void OnSendPrompt()
    {
        string prompt = promptInput.text;
        StartCoroutine(SendPromptToComfy(prompt));
    }

    IEnumerator SendPromptToComfy(string prompt)
    {
        // 👇 准备好请求内容（这里需要你自己填好 prompt 的 workflow json）
        string json = File.ReadAllText(Application.streamingAssetsPath + "/comfy_prompt.json");
        json = json.Replace("$PROMPT$", prompt); // 用用户输入替换 prompt 占位符

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(comfyURL, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Prompt sent successfully!");
            // 等个几秒，再去读取 output 文件夹的图片
            yield return new WaitForSeconds(3f);
            LoadGeneratedImage();
        }
        else
        {
            Debug.LogError("Failed to send prompt: " + request.error);
        }
    }

    void LoadGeneratedImage()
    {
        // 加载 output 文件夹中最新的一张图片
        var files = Directory.GetFiles(outputImagePath, "*.png");
        if (files.Length == 0) return;

        string latest = files[files.Length - 1]; // 最后一张图
        byte[] data = File.ReadAllBytes(latest);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(data);
        displayImage.texture = tex;
    }
}
