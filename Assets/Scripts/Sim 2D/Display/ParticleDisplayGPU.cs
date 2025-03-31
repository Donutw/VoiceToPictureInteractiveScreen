using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ParticleDisplay2D : MonoBehaviour, IDisposable
{
	public Mesh mesh;
	public Shader shader;
	public float scale;
	public float blend;
	public Gradient colourMap;
	public int gradientResolution;
	public float velocityDisplayMax;

	Material material;
	ComputeBuffer argsBuffer;
	Bounds bounds;
	Texture2D gradientTexture;
	bool needsUpdate;

    // 新增属性，决定投影模式
    public enum ProjectionMode { ParticleUV = 0, WorldProjection = 1 };
    public ProjectionMode projectionMode;
    // 粒子区域边界 (根据你的粒子初始分布区域填写或计算)
    public Vector2 projectionMinBounds = new Vector2(-10, -10);
    public Vector2 projectionSizeBounds = new Vector2(20, 20);

    public List<Texture2D> imageList;         // Inspector 中可以指定多张图片
    public float transformationSpeed = 1.0f;    // 过渡速度，可以根据需要调试

    private int currentImageIndex = 0;         // 当前图片的索引
    private bool isTransitioning = false;      // 标记是否正在切换
    private float transitionProgress = 0f;       // 过渡进度，范围0～1

    public event System.Action<Texture2D> OnCurrentTextureChanged;

    public void Init(Simulation2D sim)
	{
        material = new Material(shader);
        material.SetBuffer("Positions2D", sim.positionBuffer);
		material.SetBuffer("Velocities", sim.velocityBuffer);
		material.SetBuffer("DensityData", sim.densityBuffer);

        // 新增的UV坐标数据传递
        material.SetBuffer("UVs", sim.uvBuffer);

        argsBuffer = ComputeHelper.CreateArgsBuffer(mesh, sim.positionBuffer.count);
		bounds = new Bounds(Vector3.zero, Vector3.one * 10000);

        if (imageList != null && imageList.Count > 0)
        {
            material.SetTexture("_CurrentTex", imageList[currentImageIndex]);
        }

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N) && !isTransitioning)
        {
            StartCoroutine(TransitionToNextImage());
        }
    }

    void LateUpdate()
	{
		if (shader != null)
		{
			UpdateSettings();
			Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
		}
	}

	void UpdateSettings()
	{
		if (needsUpdate)
		{
			needsUpdate = false;
			TextureFromGradient(ref gradientTexture, gradientResolution, colourMap);
			material.SetTexture("ColourMap", gradientTexture);

			material.SetFloat("scale", scale);
			material.SetFloat("velocityMax", velocityDisplayMax);
            material.SetFloat("_Blend", blend);
            // 新增：切换投影模式
            material.SetInt("_ProjectionMode", (int)projectionMode);
            material.SetVector("_ProjectionBounds", new Vector4(
            projectionMinBounds.x, projectionMinBounds.y,
            projectionSizeBounds.x, projectionSizeBounds.y));

            material.SetTexture("_CurrentTex", imageList[currentImageIndex]);
        }
    }

    IEnumerator TransitionToNextImage()
    {
        isTransitioning = true;
        // 计算下一个图片索引（循环列表）
        int nextImageIndex = (currentImageIndex + 1) % imageList.Count;

        // 将目标纹理设置为下一个图片
        material.SetTexture("_TargetTex", imageList[nextImageIndex]);

        transitionProgress = 0f;
        while (transitionProgress < 1f)
        {
            transitionProgress += Time.deltaTime * transformationSpeed;
            transitionProgress = Mathf.Clamp01(transitionProgress);

            // 将过渡进度传递给 shader（新建一个变量，比如 _TransitionProgress）
            material.SetFloat("_TransitionProgress", transitionProgress);

            yield return null;
        }
        // 过渡完成后，更新当前图片为新图片，并重置过渡进度
        currentImageIndex = nextImageIndex;
        material.SetTexture("_CurrentTex", imageList[currentImageIndex]);
        material.SetFloat("_TransitionProgress", 0f);

        isTransitioning = false;
        // 通知监听者
        if (OnCurrentTextureChanged != null)
            OnCurrentTextureChanged(imageList[currentImageIndex]);
    }

    public static void TextureFromGradient(ref Texture2D texture, int width, Gradient gradient, FilterMode filterMode = FilterMode.Bilinear)
	{
		if (texture == null)
		{
			texture = new Texture2D(width, 1);
		}
		else if (texture.width != width)
		{
			texture.Reinitialize(width, 1);
		}
		if (gradient == null)
		{
			gradient = new Gradient();
			gradient.SetKeys(
				new GradientColorKey[] { new GradientColorKey(Color.black, 0), new GradientColorKey(Color.black, 1) },
				new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
			);
		}
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = filterMode;

		Color[] cols = new Color[width];
		for (int i = 0; i < cols.Length; i++)
		{
			float t = i / (cols.Length - 1f);
			cols[i] = gradient.Evaluate(t);
		}
		texture.SetPixels(cols);
		texture.Apply();
	}

    void OnValidate()
	{
		needsUpdate = true;
	}

	void OnDestroy()
	{
		Dispose();
	}

    public void Dispose()
    {
        ComputeHelper.Release(argsBuffer);
    }
}
