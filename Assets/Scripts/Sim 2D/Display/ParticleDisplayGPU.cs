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

    // �������ԣ�����ͶӰģʽ
    public enum ProjectionMode { ParticleUV = 0, WorldProjection = 1 };
    public ProjectionMode projectionMode;
    // ��������߽� (����������ӳ�ʼ�ֲ�������д�����)
    public Vector2 projectionMinBounds = new Vector2(-10, -10);
    public Vector2 projectionSizeBounds = new Vector2(20, 20);

    public List<Texture2D> imageList;         // Inspector �п���ָ������ͼƬ
    public float transformationSpeed = 1.0f;    // �����ٶȣ����Ը�����Ҫ����

    private int currentImageIndex = 0;         // ��ǰͼƬ������
    private bool isTransitioning = false;      // ����Ƿ������л�
    private float transitionProgress = 0f;       // ���ɽ��ȣ���Χ0��1

    public event System.Action<Texture2D> OnCurrentTextureChanged;

    public void Init(Simulation2D sim)
	{
        material = new Material(shader);
        material.SetBuffer("Positions2D", sim.positionBuffer);
		material.SetBuffer("Velocities", sim.velocityBuffer);
		material.SetBuffer("DensityData", sim.densityBuffer);

        // ������UV�������ݴ���
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
            // �������л�ͶӰģʽ
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
        // ������һ��ͼƬ������ѭ���б�
        int nextImageIndex = (currentImageIndex + 1) % imageList.Count;

        // ��Ŀ����������Ϊ��һ��ͼƬ
        material.SetTexture("_TargetTex", imageList[nextImageIndex]);

        transitionProgress = 0f;
        while (transitionProgress < 1f)
        {
            transitionProgress += Time.deltaTime * transformationSpeed;
            transitionProgress = Mathf.Clamp01(transitionProgress);

            // �����ɽ��ȴ��ݸ� shader���½�һ������������ _TransitionProgress��
            material.SetFloat("_TransitionProgress", transitionProgress);

            yield return null;
        }
        // ������ɺ󣬸��µ�ǰͼƬΪ��ͼƬ�������ù��ɽ���
        currentImageIndex = nextImageIndex;
        material.SetTexture("_CurrentTex", imageList[currentImageIndex]);
        material.SetFloat("_TransitionProgress", 0f);

        isTransitioning = false;
        // ֪ͨ������
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
