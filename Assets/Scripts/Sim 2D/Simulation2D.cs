using UnityEngine;
using Unity.Mathematics;

public class Simulation2D : MonoBehaviour
{
    public event System.Action SimulationStepCompleted;

    [Header("Simulation Settings")]
    public float timeScale = 1;
    public bool fixedTimeStep;
    public int iterationsPerFrame;
    public float gravity;
    [Range(0, 1)] public float collisionDamping = 0.95f;
    public float smoothingRadius = 2;
    public float targetDensity;
    public float pressureMultiplier;
    public float nearPressureMultiplier;
    public float viscosityStrength;
    public Vector2 boundsSize;
    public Vector2 obstacleSize;
    public Vector2 obstacleCentre;

    [Header("Interaction Settings")]
    public float interactionRadius;
    public float interactionStrength;

    [Header("References")]
    public GestureDetectionRunner runner;
    public ComputeShader compute;
    public ComputeShader flowNoiseCompute;
    public ParticleSpawner spawner;
    public ParticleDisplay2D display;

    // Buffers
    public ComputeBuffer positionBuffer { get; private set; }
    public ComputeBuffer velocityBuffer { get; private set; }
    public ComputeBuffer densityBuffer { get; private set; }
    ComputeBuffer predictedPositionBuffer;
    ComputeBuffer spatialIndices;
    ComputeBuffer spatialOffsets;
    ComputeBuffer initialPositionBuffer;
    GPUSort gpuSort;

    // Kernel IDs
    const int externalForcesKernel = 0;
    const int spatialHashKernel = 1;
    const int densityKernel = 2;
    const int pressureKernel = 3;
    const int viscosityKernel = 4;
    const int updatePositionKernel = 5;
    int flowKernelID;

    // State
    bool isPaused;
    ParticleSpawner.ParticleSpawnData spawnData;
    bool pauseNextFrame;
    [HideInInspector] public bool enableSmoothReturn; // 控制平滑返回开关（B键控制）

    public ComputeBuffer uvBuffer; // 新增uv buffer定义

    public int numParticles { get; private set; }

    [Header("Control Setting")]
    public float Sensity = 1.5f;

    [Header("Noise Flow Settings")]
    public bool enableNoiseFlow = true;
    public float noiseScale = 0.1f;
    public float noiseStrength = 1.0f;
    public float noiseSpeed = 0.5f;

    [Header("Restore Settings")]
    public float smoothReturnSpeed = 2.0f; // 控制平滑插值速度，越大返回越快（推荐1~5）
    public float stopThreshold = 0.8f;

    void Start()
    {
        Debug.Log("Controls: Space = Play/Pause, R = Reset, LMB = Attract, RMB = Repel");

        float deltaTime = 1 / 60f;
        Time.fixedDeltaTime = deltaTime;

        spawnData = spawner.GetSpawnData();
        numParticles = spawnData.positions.Length;

        // Create buffers
        positionBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
        predictedPositionBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
        velocityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
        densityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
        spatialIndices = ComputeHelper.CreateStructuredBuffer<uint3>(numParticles);
        spatialOffsets = ComputeHelper.CreateStructuredBuffer<uint>(numParticles);
        initialPositionBuffer = ComputeHelper.CreateStructuredBuffer<float2>(spawnData.positions);

        // Set buffer data
        SetInitialBufferData(spawnData);

        flowKernelID = flowNoiseCompute.FindKernel("FlowKernel");

        flowNoiseCompute.SetBuffer(flowKernelID, "Positions", positionBuffer);
        flowNoiseCompute.SetBuffer(flowKernelID, "Velocities", velocityBuffer);
        flowNoiseCompute.SetBuffer(flowKernelID, "InitialPositions", initialPositionBuffer);

        // Init compute
        ComputeHelper.SetBuffer(compute, positionBuffer, "Positions", externalForcesKernel, updatePositionKernel);
        ComputeHelper.SetBuffer(compute, predictedPositionBuffer, "PredictedPositions", externalForcesKernel, spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);
        ComputeHelper.SetBuffer(compute, spatialIndices, "SpatialIndices", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);
        ComputeHelper.SetBuffer(compute, spatialOffsets, "SpatialOffsets", spatialHashKernel, densityKernel, pressureKernel, viscosityKernel);
        ComputeHelper.SetBuffer(compute, densityBuffer, "Densities", densityKernel, pressureKernel, viscosityKernel);
        ComputeHelper.SetBuffer(compute, velocityBuffer, "Velocities", externalForcesKernel, pressureKernel, viscosityKernel, updatePositionKernel);

        compute.SetInt("numParticles", numParticles);

        gpuSort = new();
        gpuSort.SetBuffers(spatialIndices, spatialOffsets);


        // Init display
        display.Init(this);
    }

    void FixedUpdate()
    {
        if (fixedTimeStep)
        {
            RunSimulationFrame(Time.fixedDeltaTime);
        }
    }

    void Update()
    {
        // Run simulation if not in fixed timestep mode
        // (skip running for first few frames as deltaTime can be disproportionaly large)
        if (!fixedTimeStep && Time.frameCount > 10)
        {
            RunSimulationFrame(Time.deltaTime);
        }

        if (pauseNextFrame)
        {
            isPaused = true;
            pauseNextFrame = false;
        }

        enableSmoothReturn = runner != null && runner.CurrentGesture is Fist;

        HandleInput();
    }

    void RunSimulationFrame(float frameTime)
    {
        if (!isPaused)
        {
            float timeStep = frameTime / iterationsPerFrame * timeScale;

            UpdateSettings(timeStep);

            for (int i = 0; i < iterationsPerFrame; i++)
            {
                RunSimulationStep();
                SimulationStepCompleted?.Invoke();
            }
        }
    }

    void RunSimulationStep()
    {
        ComputeHelper.Dispatch(compute, numParticles, kernelIndex: externalForcesKernel);
        flowNoiseCompute.Dispatch(0, Mathf.CeilToInt(numParticles / 256.0f), 1, 1);
        ComputeHelper.Dispatch(compute, numParticles, kernelIndex: spatialHashKernel);
        gpuSort.SortAndCalculateOffsets();
        ComputeHelper.Dispatch(compute, numParticles, kernelIndex: densityKernel);
        ComputeHelper.Dispatch(compute, numParticles, kernelIndex: pressureKernel);
        ComputeHelper.Dispatch(compute, numParticles, kernelIndex: viscosityKernel);
        ComputeHelper.Dispatch(compute, numParticles, kernelIndex: updatePositionKernel);

    }

    void UpdateSettings(float deltaTime)
    {
        compute.SetFloat("deltaTime", deltaTime);
        compute.SetFloat("gravity", gravity);
        compute.SetFloat("collisionDamping", collisionDamping);
        compute.SetFloat("smoothingRadius", smoothingRadius);
        compute.SetFloat("targetDensity", targetDensity);
        compute.SetFloat("pressureMultiplier", pressureMultiplier);
        compute.SetFloat("nearPressureMultiplier", nearPressureMultiplier);
        compute.SetFloat("viscosityStrength", viscosityStrength);
        compute.SetVector("boundsSize", boundsSize);
        compute.SetVector("obstacleSize", obstacleSize);
        compute.SetVector("obstacleCentre", obstacleCentre);

        compute.SetFloat("Poly6ScalingFactor", 4 / (Mathf.PI * Mathf.Pow(smoothingRadius, 8)));
        compute.SetFloat("SpikyPow3ScalingFactor", 10 / (Mathf.PI * Mathf.Pow(smoothingRadius, 5)));
        compute.SetFloat("SpikyPow2ScalingFactor", 6 / (Mathf.PI * Mathf.Pow(smoothingRadius, 4)));
        compute.SetFloat("SpikyPow3DerivativeScalingFactor", 30 / (Mathf.Pow(smoothingRadius, 5) * Mathf.PI));
        compute.SetFloat("SpikyPow2DerivativeScalingFactor", 12 / (Mathf.Pow(smoothingRadius, 4) * Mathf.PI));

        // Interaction settings:
        float currInteractStrength = 0;
        Vector2 worldPos = Vector2.zero;
        Gesture gesture;
        if (runner != null && (gesture = runner.CurrentGesture) != null)
        {
            var pixelWidth = Screen.width;
            var pixelHeight = Screen.height;
            var screenPos = runner.CurrentGesturePos;
            worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            bool isPullInteraction = gesture is Pinch;
            bool isPushInteraction = gesture is IndexOpen;
            if (isPushInteraction || isPullInteraction)
            {
                currInteractStrength = isPushInteraction ? -interactionStrength : interactionStrength;
            }
        }
        compute.SetVector("interactionInputPoint", worldPos);
        compute.SetFloat("interactionInputStrength", currInteractStrength);
        compute.SetFloat("interactionInputRadius", interactionRadius);

        // 更新 FlowNoise.compute 参数
        flowNoiseCompute.SetBool("enableNoiseFlow", enableNoiseFlow);
        flowNoiseCompute.SetFloat("noiseScale", noiseScale);
        flowNoiseCompute.SetFloat("noiseStrength", noiseStrength);
        flowNoiseCompute.SetFloat("noiseSpeed", noiseSpeed);
        flowNoiseCompute.SetFloat("time", Time.time);
        flowNoiseCompute.SetFloat("deltaTime", deltaTime);
        flowNoiseCompute.SetInt("numParticles", numParticles);
        flowNoiseCompute.SetBool("enableSmoothReturn", enableSmoothReturn);
        flowNoiseCompute.SetFloat("smoothReturnSpeed", smoothReturnSpeed);
        flowNoiseCompute.SetFloat("stopThreshold", stopThreshold);
    }

    void SetInitialBufferData(ParticleSpawner.ParticleSpawnData spawnData)
    {
        float2[] allPoints = new float2[spawnData.positions.Length];
        System.Array.Copy(spawnData.positions, allPoints, spawnData.positions.Length);

        positionBuffer.SetData(allPoints);
        predictedPositionBuffer.SetData(allPoints);
        velocityBuffer.SetData(spawnData.velocities);

        // 新增的UV坐标buffer传递
        uvBuffer = ComputeHelper.CreateStructuredBuffer<float2>(spawnData.uvs.Length);
        uvBuffer.SetData(spawnData.uvs);
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPaused = !isPaused;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            isPaused = false;
            pauseNextFrame = true;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            isPaused = true;
            // Reset positions, the run single frame to get density etc (for debug purposes) and then reset positions again
            SetInitialBufferData(spawnData);
            RunSimulationStep();
            SetInitialBufferData(spawnData);
        }
    }


    void OnDestroy()
    {
        ComputeHelper.Release(positionBuffer, predictedPositionBuffer, velocityBuffer, densityBuffer, spatialIndices, spatialOffsets);
    }


    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawWireCube(Vector2.zero, boundsSize);
        Gizmos.DrawWireCube(obstacleCentre, obstacleSize);

        if (Application.isPlaying)
        {
            float currInteractStrength = 0;
            Vector2 mousePos = Vector2.zero;
            Gesture gesture;
            bool isPullInteraction = false;
            bool isPushInteraction = false;

            if (runner != null && (gesture = runner.CurrentGesture) != null)
            {
                var pixelWidth = Screen.width;
                var pixelHeight = Screen.height;
                var screenPos = runner.CurrentGesturePos;
                mousePos = Camera.main.ScreenToWorldPoint(screenPos);
                isPullInteraction = gesture is Pinch;
                isPushInteraction = gesture is IndexOpen;
                if (isPushInteraction || isPullInteraction)
                {
                    currInteractStrength = isPushInteraction ? -interactionStrength : interactionStrength;
                }
            }

            var isInteracting = isPullInteraction || isPushInteraction;
            Gizmos.color = Color.white;
            if (isInteracting)
            {
                Gizmos.color = isPullInteraction ? Color.green : Color.red;
            }
            Gizmos.DrawWireSphere(mousePos, interactionRadius);
        }

    }
}
