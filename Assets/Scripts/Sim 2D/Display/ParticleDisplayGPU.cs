using UnityEngine;

public class ParticleDisplay2D : MonoBehaviour
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
            projectionSizeBounds.x, projectionSizeBounds.y
        ));
        }
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
		ComputeHelper.Release(argsBuffer);
	}
}
