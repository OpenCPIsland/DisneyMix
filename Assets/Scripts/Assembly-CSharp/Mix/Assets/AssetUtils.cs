using UnityEngine;
using UnityEngine.UI;
using System.Collections;   // add at the top with the other usings

namespace Mix.Assets
{
	public class AssetUtils
	{
		private const float QUIET_SECONDS = 2f;
        private static float lastRequestTime;
        private static bool routineRunning;

		public static void DestroyGameObject(GameObject gameObj)
		{
			Component[] componentsInChildren = gameObj.GetComponentsInChildren<Component>(true);
			Component[] array = componentsInChildren;
			foreach (Component component in array)
			{
				DestroyComponent(component);
			}
		}

		public static void DestroyComponent(Component component)
		{
			if (component is Image)
			{
				Image image = (Image)component;
				if (image != null)
				{
					DestroySprite(image.sprite);
					image.sprite = null;
				}
			}
			if (component is SkinnedMeshRenderer)
			{
				SkinnedMeshRenderer skinnedMeshRenderer = (SkinnedMeshRenderer)component;
				if (skinnedMeshRenderer != null)
				{
					Material[] sharedMaterials = skinnedMeshRenderer.sharedMaterials;
					foreach (Material material in sharedMaterials)
					{
						if (material != null)
						{
							Texture mainTexture = material.mainTexture;
							if (mainTexture != null)
							{
								Resources.UnloadAsset(mainTexture);
							}
							material.mainTexture = null;
							Resources.UnloadAsset(material);
						}
					}
					Mesh sharedMesh = skinnedMeshRenderer.sharedMesh;
					if (sharedMesh != null)
					{
						Resources.UnloadAsset(sharedMesh);
					}
					skinnedMeshRenderer.sharedMesh = null;
				}
			}
			if (component is MeshRenderer)
			{
				MeshRenderer meshRenderer = (MeshRenderer)component;
				if (meshRenderer != null)
				{
					Material[] sharedMaterials2 = meshRenderer.sharedMaterials;
					foreach (Material material2 in sharedMaterials2)
					{
						if (material2 != null)
						{
							Texture mainTexture2 = material2.mainTexture;
							if (mainTexture2 != null)
							{
								Resources.UnloadAsset(mainTexture2);
							}
							material2.mainTexture = null;
							Resources.UnloadAsset(material2);
						}
					}
				}
			}
			if (component is MeshFilter)
			{
				MeshFilter meshFilter = (MeshFilter)component;
				if (meshFilter != null)
				{
					Mesh sharedMesh2 = meshFilter.sharedMesh;
					if (sharedMesh2 != null)
					{
						Resources.UnloadAsset(sharedMesh2);
					}
					meshFilter.sharedMesh = null;
				}
			}
			if (component is ParticleSystemRenderer)
			{
				ParticleSystemRenderer particleSystemRenderer = (ParticleSystemRenderer)component;
				if (particleSystemRenderer != null)
				{
					Material[] sharedMaterials3 = particleSystemRenderer.sharedMaterials;
					foreach (Material material3 in sharedMaterials3)
					{
						if (material3 != null)
						{
							Texture mainTexture3 = material3.mainTexture;
							if (mainTexture3 != null)
							{
								Resources.UnloadAsset(mainTexture3);
							}
							material3.mainTexture = null;
							Resources.UnloadAsset(material3);
						}
					}
				}
			}
			if (component is SpriteRenderer)
			{
				SpriteRenderer spriteRenderer = (SpriteRenderer)component;
				if (spriteRenderer != null)
				{
					DestroySprite(spriteRenderer.sprite);
					spriteRenderer.sprite = null;
				}
			}
			if (component is Animator)
			{
				Animator animator = (Animator)component;
				if (animator != null)
				{
					RuntimeAnimatorController runtimeAnimatorController = animator.runtimeAnimatorController;
					if (runtimeAnimatorController != null)
					{
						AnimationClip[] animationClips = runtimeAnimatorController.animationClips;
						foreach (AnimationClip assetToUnload in animationClips)
						{
							Resources.UnloadAsset(assetToUnload);
						}
						Resources.UnloadAsset(runtimeAnimatorController);
					}
					runtimeAnimatorController = null;
				}
			}
			if (component is AvatarComponent)
			{
				AvatarComponent avatarComponent = (AvatarComponent)component;
				if (avatarComponent != null)
				{
					AvatarComponentLayer[] layers = avatarComponent.Layers;
					foreach (AvatarComponentLayer avatarComponentLayer in layers)
					{
						Texture2D texture = avatarComponentLayer.Texture;
						if (texture != null)
						{
							Resources.UnloadAsset(texture);
							avatarComponentLayer.Texture = null;
						}
						Texture2D normalMap = avatarComponentLayer.NormalMap;
						if (normalMap != null)
						{
							Resources.UnloadAsset(normalMap);
							avatarComponentLayer.NormalMap = null;
						}
					}
				}
			}
			if (component is IAssetReferencer)
			{
				((IAssetReferencer)component).CleanupReferences();
			}
		}
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetStatics()
{
	// Needed when Domain Reload is disabled in the Editor.
	routineRunning = false;
	lastRequestTime = 0f;
}

public static void SoftUnload(Object obj)
{
	if (obj == null)
	{
		return;
	}

	lastRequestTime = Time.unscaledTime;   // every call pushes the unload back
	if (routineRunning)
	{
		return;
	}

	AssetManager am = MonoSingleton<AssetManager>.Instance;
	if (am == null)
	{
		return;
	}

	routineRunning = true;
	am.StartCoroutine(SoftUnloadRoutine());
}

private static IEnumerator SoftUnloadRoutine()
{
	// Wait for a quiet moment (no recycling for QUIET_SECONDS).
	while (Time.unscaledTime - lastRequestTime < QUIET_SECONDS)
	{
		yield return null;
	}

	// Async, and only frees assets nothing references.
	yield return Resources.UnloadUnusedAssets();
	routineRunning = false;
}

		public static void DestroySprite(Sprite sprite)
		{
			if (sprite != null)
			{
				Texture texture = sprite.texture;
				if (texture != null)
				{
					SoftUnload(texture);
				}
				SoftUnload(sprite);
			}
		}
	}
}
