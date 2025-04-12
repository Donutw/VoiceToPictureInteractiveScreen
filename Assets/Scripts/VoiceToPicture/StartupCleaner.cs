using UnityEngine;
using System.IO;

public class StartupCleaner : MonoBehaviour
{
    public string rootPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    public string comfyPath = "D:/AI/ComfyUI-master/output";

    void Start()
    {
        DeleteAllFilesIn(Path.Combine(rootPath, "AudioInput"));
        DeleteAllFilesIn(Path.Combine(rootPath, "Transcripts"));
        DeleteAllFilesIn(comfyPath);
    }

    void DeleteAllFilesIn(string folder)
    {
        if (!Directory.Exists(folder)) return;

        foreach (string file in Directory.GetFiles(folder))
        {
            try
            {
                File.Delete(file);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"❗ 无法删除文件: {file} → {e.Message}");
            }
        }

        Debug.Log($"🧹 清空文件夹: {folder}");
    }
}
