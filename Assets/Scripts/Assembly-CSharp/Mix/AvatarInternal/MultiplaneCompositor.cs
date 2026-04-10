using System;
using System.Collections;
using System.Collections.Generic;
using Avatar;
using Avatar.DataTypes;
using Disney.Mix.SDK;
using UnityEngine;

namespace Mix.AvatarInternal
{
	public class MultiplaneCompositor
	{
		public class LayerPropertyInfo
		{
			public string name;

			public IAvatarProperty prop;

			public AvatarElement entitlementDefinition;

			public LayerPropertyInfo(string aName, IAvatarProperty aProp, AvatarElement aEntitlementDefinition)
			{
				name = aName;
				prop = aProp;
				entitlementDefinition = aEntitlementDefinition;
			}
		}

		public List<LayerPropertyInfo> layers;

		public Dictionary<string, AvatarComponent> components;

		private List<Action> cleanupBundles = new List<Action>();

		private int size;

		private string shaderName;

		private TextureFormat format;

		private MonoBehaviour monoEngine;

		private readonly Material workerMaterial;

		public MultiplaneCompositor(MonoBehaviour aMonoEngine, int aSize, TextureFormat aFormat, string aShaderName)
		{
			layers = new List<LayerPropertyInfo>();
			components = new Dictionary<string, AvatarComponent>();
			monoEngine = aMonoEngine;
			format = aFormat;
			size = aSize;
			shaderName = aShaderName;
			Shader shader = Shader.Find(shaderName);
			workerMaterial = new Material(shader);
		}

		public void CleanLayers()
		{
			foreach (Action cleanupBundle in cleanupBundles)
			{
				cleanupBundle();
			}
			UnityEngine.Object.Destroy(workerMaterial);
			cleanupBundles.Clear();
			layers.Clear();
			components.Clear();
		}

		public void AddBundleCleanupCall(Action bundleCleanup)
		{
			cleanupBundles.Add(bundleCleanup);
		}

		public void AddLayerInfo(string name, IAvatarProperty prop, AvatarElement elemDef)
		{
			layers.Add(new LayerPropertyInfo(name, prop, elemDef));
		}

		public void AddComponent(string key, AvatarComponent comp)
		{
			components.Add(key, comp);
		}

		public bool IsComponentIgnored(string selectionKey)
		{
			bool flag = true;
			if (components.ContainsKey(selectionKey))
			{
				AvatarComponent avatarComponent = components[selectionKey];
				AvatarComponentLayer[] array = avatarComponent.Layers;
				foreach (AvatarComponentLayer avatarComponentLayer in array)
				{
					flag = flag && avatarComponentLayer.BlendMode == AvatarComponentLayer.LayerType.IGNORE;
				}
			}
			return flag;
		}

		public void Composite(AvatarFlags flags, Action<Texture2D> callback)
		{
			foreach (LayerPropertyInfo layer in layers)
			{
				if (components.ContainsKey(layer.prop.SelectionKey))
				{
					SetupLayerProperties(flags, layer);
				}
			}
			monoEngine.StartCoroutine(RunShaderAndGetTexture(workerMaterial, size, callback));
		}

		private IEnumerator RunShaderAndGetTexture(Material mat, int aSize, Action<Texture2D> output)
		{
			   Debug.Log($"[MultiplaneCompositor] Shader: {mat.shader?.name}");
			   Debug.Log($"[MultiplaneCompositor] Material: {mat}");
			   Debug.Log($"[MultiplaneCompositor] mainTexture: {mat.mainTexture}");
			   yield return new WaitForEndOfFrame();
			   RenderTexture.active = null;
			   RenderTexture texOut = RenderTexture.GetTemporary(aSize, aSize);
			   // Try blitting with null as source
			   Debug.Log("[MultiplaneCompositor] Blitting with null as source texture");
			   Graphics.Blit(null, texOut, mat);
			   RenderTexture.active = texOut;
			   Texture2D tex = new Texture2D(aSize, aSize, format, true);
			   tex.ReadPixels(new Rect(0f, 0f, aSize, aSize), 0, 0, true);
			   RenderTexture.active = null;
			   RenderTexture.ReleaseTemporary(texOut);
			   yield return new WaitForEndOfFrame();
			   tex.Apply(false);
			   Debug.Log($"[MultiplaneCompositor] Output texture created: {tex.width}x{tex.height}");
			   output(tex);
		}

		private AvatarCategoryInfo GetCategoryInfo(string name)
		{
			for (int i = 0; i < AvatarCategoryInfoConstants.MultiplaneCategories.Length; i++)
			{
				if (AvatarCategoryInfoConstants.MultiplaneCategories[i].name == name)
				{
					return AvatarCategoryInfoConstants.MultiplaneCategories[i];
				}
			}
			return null;
		}

		public void SetupLayerProperties(AvatarFlags flags, LayerPropertyInfo layer)
		{
			AvatarComponent avatarComponent = components[layer.prop.SelectionKey];
			AvatarCategoryInfo categoryInfo = GetCategoryInfo(layer.name);
			Color[] colorsByCategoryName = AvatarColorTints.GetColorsByCategoryName(layer.name);
			if (layer.name == "Mouth")
			{
				colorsByCategoryName = AvatarColorTints.GetColorsByCategoryName("Skin");
			}
			else if (layer.name == "Brow")
			{
				colorsByCategoryName = AvatarColorTints.GetColorsByCategoryName("Hair");
			}
			if (colorsByCategoryName != null)
			{
				int num = layer.prop.TintIndex;
				if (layer.entitlementDefinition.DefaultTint != -1)
				{
					num = layer.entitlementDefinition.DefaultTint;
				}
				workerMaterial.SetColor(layer.name + "Color", colorsByCategoryName[num]);
			}
			if (layer.name == "Eyes" || layer.name == "Brow" || layer.name == "Mouth")
			{
				workerMaterial.SetFloat(layer.name + "XOffset", (float)avatarComponent.xOffset + categoryInfo.xOffset);
				workerMaterial.SetFloat(layer.name + "YOffset", (float)avatarComponent.yOffset + categoryInfo.yOffset);
			}
			else
			{
				workerMaterial.SetFloat(layer.name + "XOffset", (float)avatarComponent.xOffset + categoryInfo.xOffset + (float)layer.prop.XOffset);
				workerMaterial.SetFloat(layer.name + "YOffset", (float)avatarComponent.yOffset + categoryInfo.yOffset + (float)layer.prop.YOffset);
			}
			if (categoryInfo.crop != 1f)
			{
				workerMaterial.SetFloat(categoryInfo.name + "Crop", categoryInfo.crop);
			}
			for (int i = 0; i < categoryInfo.layerCount; i++)
			{
				if (i >= avatarComponent.Layers.Length)
				{
					workerMaterial.SetInt(categoryInfo.name + "BlendMode" + i.ToString("D2"), 5);
					continue;
				}
				AvatarComponentLayer avatarComponentLayer = avatarComponent.Layers[i];
				if (avatarComponentLayer.Texture == null)
				{
					workerMaterial.SetInt(categoryInfo.name + "BlendMode" + i.ToString("D2"), 5);
					continue;
				}
				workerMaterial.SetInt(categoryInfo.name + "BlendMode" + i.ToString("D2"), (int)avatarComponentLayer.BlendMode);
				avatarComponentLayer.Texture.wrapMode = TextureWrapMode.Clamp;
				if (avatarComponentLayer.BlendMode != AvatarComponentLayer.LayerType.MASK && avatarComponentLayer.BlendMode != AvatarComponentLayer.LayerType.COLOR)
				{
					workerMaterial.SetTexture(categoryInfo.name + "Tex" + i.ToString("D2"), avatarComponentLayer.Texture);
				}
				else
				{
					workerMaterial.SetTexture(categoryInfo.name + "Mask", avatarComponentLayer.Texture);
				}
			}
		}
	}
}
