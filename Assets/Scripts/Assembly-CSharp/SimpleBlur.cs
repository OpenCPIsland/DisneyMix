using UnityEngine;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class SimpleBlur : MonoBehaviour
{
	public Shader shader;

	private Material m_Material;

	[Range(0f, 2f)]
	public int downsample;

	[Range(0f, 0.1f)]
	public float blurStrength = 0.01f;

	protected Material material
	{
		get
		{
			if (m_Material == null)
			{
				m_Material = new Material(shader);
				m_Material.hideFlags = HideFlags.HideAndDontSave;
			}
			return m_Material;
		}
	}

	private void Start()
	{
		if (!shader || !shader.isSupported)
		{
			base.enabled = false;
		}
	}

	protected virtual void OnDisable()
	{
		if ((bool)m_Material)
		{
			Object.DestroyImmediate(m_Material);
			m_Material = null;
		}
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		material.SetFloat("_BlurStrength", blurStrength);
		int width = ((downsample <= 0) ? source.width : (source.width >> downsample));
		int height = ((downsample <= 0) ? source.height : (source.height >> downsample));
		source.filterMode = FilterMode.Bilinear;
		RenderTexture temporary = RenderTexture.GetTemporary(width, height);
		Graphics.Blit(source, temporary);
		RenderTexture temporary2 = RenderTexture.GetTemporary(width, height);
		temporary.filterMode = FilterMode.Bilinear;
		Graphics.Blit(temporary, temporary2, material, 0);
		temporary.DiscardContents();
		temporary2.filterMode = FilterMode.Bilinear;
		Graphics.Blit(temporary2, temporary, material, 1);
		Graphics.Blit(temporary, destination);
		RenderTexture.ReleaseTemporary(temporary);
		RenderTexture.ReleaseTemporary(temporary2);
	}
}
