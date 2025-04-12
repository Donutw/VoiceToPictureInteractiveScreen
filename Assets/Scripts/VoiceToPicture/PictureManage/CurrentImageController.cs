using UnityEngine;

public class CurrentImageController : MonoBehaviour
{
    public Texture2D fallbackImage; // Ĭ��ͼ��Editor �����룩
    private Texture2D currentImage;

    public static CurrentImageController Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        currentImage = fallbackImage;
    }

    public Texture2D GetCurrentImage()
    {
        return currentImage ?? fallbackImage;
    }

    public void UpdateImage(Texture2D newImage)
    {
        currentImage = newImage;
    }
}
