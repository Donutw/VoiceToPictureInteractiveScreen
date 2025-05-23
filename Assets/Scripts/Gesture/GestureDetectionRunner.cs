using System.Collections;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using UnityEngine;
using UnityEngine.Rendering;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using Mediapipe.Unity.Experimental;
using Mediapipe;
using Tasks = Mediapipe.Tasks;

public class GestureDetectionRunner : VisionTaskApiRunner<HandLandmarker>
{
    public readonly HandLandmarkDetectionConfig config = new HandLandmarkDetectionConfig();
    private TextureFramePool _textureFramePool;
    private Gesture _currentGesture;

    // ---- Position Update 相关 ---- 
    public Gesture CurrentGesture => _positionGesture;
    public Vector3 CurrentGesturePos => _currentPos;

    [Space(10)]
    [Header("Position Update")]
    [Range(1f, 3f)]
    public float Sensity = 1.5f;
    public float Speed = 100;
    public float ResetTime = 1;
    private Gesture _positionGesture;
    private bool _isUpdatingPosition = false;
    private Vector3 _currentPos;
    private Vector3 _targetPos;
    private float _currentWaitingTime = 0;

    public override void Play()
    {
        base.Play();
        StartCoroutine(nameof(UpdatePosition));
    }

    public override void Pause()
    {
        base.Pause();
        StopCoroutine(nameof(UpdatePosition));
        _isUpdatingPosition = false;
    }

    public override void Resume()
    {
        base.Resume();
        StartCoroutine(nameof(UpdatePosition));
    }

    public override void Stop()
    {
        base.Stop();
        StopCoroutine(nameof(UpdatePosition));
        _isUpdatingPosition = false;
    }

    protected override IEnumerator Run()
    {
        yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

        if (config.RunningMode != Tasks.Vision.Core.RunningMode.LIVE_STREAM)
        {
            Debug.LogWarning("Please ensure the running mode in config is LIVE STREAM. Other method is not supported. This will be automatically corrected, but you should check your code for protential issues.");
            config.RunningMode = Tasks.Vision.Core.RunningMode.LIVE_STREAM;
        }

        var options = config.GetHandLandmarkerOptions(config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnHandLandmarkDetectionOutput : null);
        taskApi = HandLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
        var imageSource = ImageSourceProvider.ImageSource;

        yield return imageSource.Play();

        if (!imageSource.isPrepared)
        {
            Debug.LogError("Failed to start ImageSource, exiting...");
            yield break;
        }

        _textureFramePool = new TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);

        var transformationOptions = imageSource.GetTransformationOptions();
        var flipHorizontally = transformationOptions.flipHorizontally;
        var flipVertically = transformationOptions.flipVertically;
        var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: (int)transformationOptions.rotationAngle);

        AsyncGPUReadbackRequest req = default;
        var waitUntilReqDone = new WaitUntil(() => req.done);
        var waitForEndOfFrame = new WaitForEndOfFrame();
        var result = HandLandmarkerResult.Alloc(options.numHands);

        var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
        using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

        while (true)
        {
            if (isPaused)
            {
                yield return new WaitWhile(() => isPaused);
            }

            if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
            {
                yield return new WaitForEndOfFrame();
                continue;
            }

            // Build the input Image
            Image image;
            switch (config.ImageReadMode)
            {
                case ImageReadMode.GPU:
                    if (!canUseGpuImage)
                    {
                        throw new System.Exception("ImageReadMode.GPU is not supported");
                    }
                    textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                    image = textureFrame.BuildGPUImage(glContext);
                    // TODO: Currently we wait here for one frame to make sure the texture is fully copied to the TextureFrame before sending it to MediaPipe.
                    // This usually works but is not guaranteed. Find a proper way to do this. See: https://github.com/homuler/MediaPipeUnityPlugin/pull/1311
                    yield return waitForEndOfFrame;
                    break;
                case ImageReadMode.CPU:
                    yield return waitForEndOfFrame;
                    textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                    image = textureFrame.BuildCPUImage();
                    textureFrame.Release();
                    break;
                case ImageReadMode.CPUAsync:
                default:
                    req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                    yield return waitUntilReqDone;

                    if (req.hasError)
                    {
                        Debug.LogWarning($"Failed to read texture from the image source");
                        continue;
                    }
                    image = textureFrame.BuildCPUImage();
                    textureFrame.Release();
                    break;
            }

            switch (taskApi.runningMode)
            {
                case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
                    taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
                    break;
                default:
                    break;
            }
        }
    }

    protected IEnumerator UpdatePosition()
    {
        if (_isUpdatingPosition) yield break;

        _isUpdatingPosition = true;

        while (true)
        {
            // 如果当前有检测到手势
            if (_currentGesture != null)
            {
                // 就进行更新
                var newPos = _currentGesture.GetScreenPos(UnityEngine.Screen.width, UnityEngine.Screen.height, Sensity);
                // 如果当前没有缓存的手势（意味着在空状态）
                if (_positionGesture == null)
                {
                    // 直接置于新位置
                    _currentPos = newPos;
                }
                _positionGesture = _currentGesture;
                _currentWaitingTime = 0;
                // 设置目标位置为新位置
                _targetPos = newPos;
            }
            // 如果没检测到手势
            else
            {
                // 手势缓存的时间++
                _currentWaitingTime += Time.deltaTime;
            }
            // 如果当前有缓存的手势
            if (_positionGesture != null)
            {
                // 且如果当前缓存的时间已经超过了最大缓存时间
                if (_currentWaitingTime > ResetTime)
                {
                    // 重置
                    _positionGesture = null;
                    _currentWaitingTime = 0;
                    _currentPos = Vector3.zero;
                    _targetPos = Vector3.zero;
                }
                else
                {
                    // 否则移动位置
                    _currentPos += (_targetPos - _currentPos) * Speed * Time.deltaTime;
                }
            }

            yield return null;
        }
    }

    private void OnHandLandmarkDetectionOutput(HandLandmarkerResult result, Image image, long timestamp)
    {
        if (result.handLandmarks != null)
        {
            var lms = result.handLandmarks[0].landmarks;
            if (Gesture.Get(lms, out var gesture))
            {
                _currentGesture = gesture;
            }
            else
            {
                _currentGesture = null;
            }
        }
    }
}

