using System;
using System.Collections.Generic;
using Fabric;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Assets
{
	public abstract class AssetCache
	{
		public long reductionMegs = 2097152L;

		public float clearPercent = 0.33f;

		public long MinBytes = 10485760L;

		private long _MaxBytes = 10485760L;

		public long currentSize;

		protected Dictionary<string, CacheObject> cache = new Dictionary<string, CacheObject>();

		protected List<CacheObject> priorityList = new List<CacheObject>();

		public long MaxBytes
		{
			get
			{
				return _MaxBytes;
			}
			set
			{
				_MaxBytes = value;
				CheckMinBytes();
			}
		}

		public AssetCache(long maxBytes = 10485760, long minBytes = 10485760)
		{
			MinBytes = minBytes;
			MaxBytes = maxBytes;
		}

		public void CheckMinBytes()
		{
			if (_MaxBytes < MinBytes)
			{
				_MaxBytes = MinBytes;
			}
		}

		public void LowMemoryWarning()
		{
			MaxBytes -= reductionMegs;
			ClearCache();
			UpdatePriorityList();
			Resources.UnloadUnusedAssets();
		}

		public void ClearCache()
		{
			List<string> list = new List<string>(cache.Keys);
			foreach (string item in list)
			{
				Remove(item);
			}
			cache.Clear();
			priorityList.Clear();
			currentSize = 0L;
		}

		public long GetSize()
		{
			return currentSize;
		}

		public long RecalculateSize()
		{
			long num = 0L;
			for (int i = 0; i < priorityList.Count; i++)
			{
				num += priorityList[i].size;
			}
			currentSize = num;
			return num;
		}

		public long EstimateSizeOfGameObject(GameObject aGameObject)
		{
			long num = 0L;
			if (aGameObject == null)
			{
				return 1024L;
			}
			UnityEngine.Component[] componentsInChildren = aGameObject.GetComponentsInChildren<UnityEngine.Component>(true);
			foreach (UnityEngine.Component component in componentsInChildren)
			{
				if (component == null)
				{
					continue;
				}
				if (component is Animator animator)
				{
					int num2 = component.gameObject.GetComponentsInChildren<Transform>(true).Length;
					if (!(animator != null) || !(animator.runtimeAnimatorController != null))
					{
						continue;
					}
					AnimationClip[] animationClips = animator.runtimeAnimatorController.animationClips;
					if (animationClips != null)
					{
						AnimationClip[] array = animationClips;
						foreach (AnimationClip animationClip in array)
						{
							num += (long)((float)num2 * animationClip.length * animationClip.frameRate * 100f);
						}
					}
				}
				else if (component is AudioComponent audioComponent)
				{
					if (audioComponent != null && audioComponent.AudioClip != null)
					{
						num += (long)(audioComponent.AudioClip.length * 6f * 1024f);
					}
				}
				else if (component is SkinnedMeshRenderer skinnedMeshRenderer)
				{
					Material[] sharedMaterials = skinnedMeshRenderer.sharedMaterials;
					if (sharedMaterials == null)
					{
						continue;
					}
					Material[] array2 = sharedMaterials;
					foreach (Material material in array2)
					{
						if (material != null && material.mainTexture != null)
						{
							num += material.mainTexture.width * material.mainTexture.height * 8;
						}
					}
				}
				else if (component is Image image)
				{
					if (image != null && image.mainTexture != null)
					{
						num = image.mainTexture.width * image.mainTexture.height * 8;
					}
				}
				else
				{
					num += 100;
				}
			}
			if (num == 0L)
			{
				num = 1024L;
			}
			return num;
		}

		public int GetIndexOfKey(string aKey)
		{
			if (cache.ContainsKey(aKey))
			{
				return cache[aKey].index;
			}
			return -1;
		}

		public void Add(string aKey, object aValue, long aSize, bool aIsRefCounting = false)
		{
			if (aSize == 0L && aValue as GameObject != null)
			{
				aSize = EstimateSizeOfGameObject((GameObject)aValue);
			}
			Remove(aKey);
			if (currentSize + aSize > MaxBytes)
			{
				if (aSize > MaxBytes)
				{
					ClearCache();
				}
				else
				{
					while (currentSize + aSize > MaxBytes)
					{
						Remove(clearPercent);
					}
				}
			}
			CacheObject cacheObject = new CacheObject(aValue, aKey, 0, aSize, aIsRefCounting);
			currentSize += aSize;
			priorityList.Insert(0, cacheObject);
			cache.Add(aKey, cacheObject);
			UpdatePriorityList();
		}

		public void Remove(float aPercent = 0.1f)
		{
			double num = Math.Ceiling((float)priorityList.Count * aPercent);
			for (double num2 = 0.0; num2 < num; num2 += 1.0)
			{
				Remove(priorityList[priorityList.Count - 1].key);
			}
		}

		public void Remove(string aKey)
		{
			if (cache.ContainsKey(aKey))
			{
				long size = cache[aKey].size;
				int index = cache[aKey].index;
				if (cache[aKey].isRefCounting && MonoSingleton<AssetManager>.Instance != null)
				{
					MonoSingleton<AssetManager>.Instance.DestroyBundleInstance(aKey);
				}
				cache.Remove(aKey);
				priorityList.RemoveAt(index);
				currentSize -= size;
				UpdatePriorityList();
			}
		}

		public void Remove(int index)
		{
			if (index < priorityList.Count)
			{
				Remove(priorityList[index].key);
			}
		}

		protected void UpdatePriorityList()
		{
			for (int i = 0; i < priorityList.Count; i++)
			{
				priorityList[i].index = i;
			}
		}

		public abstract void Get(string aKey, ref object obj);

		public abstract object Get(string aKey);
	}
}
