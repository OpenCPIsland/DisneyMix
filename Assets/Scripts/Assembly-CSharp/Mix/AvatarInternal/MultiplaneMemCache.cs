using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mix.AvatarInternal
{
	public class MultiplaneMemCache
	{
		public class MultiplaneCacheObject
		{
			public int references;

			public Texture2D texture;

			public Action cleanup;

			public MultiplaneCacheObject(int references, Texture2D texture, Action cleanup)
			{
				this.references = references;
				this.texture = texture;
				this.cleanup = cleanup;
			}
		}

		private static Dictionary<string, MultiplaneCacheObject> mpCache = new Dictionary<string, MultiplaneCacheObject>();

		public static Texture2D GetTexture(string key)
		{
			if (mpCache.ContainsKey(key))
			{
				mpCache[key].references++;
				return mpCache[key].texture;
			}
			return null;
		}

		public static void AddTexture(string key, Texture2D texture, Action cleanup)
		{
			if (mpCache.ContainsKey(key))
			{
				mpCache[key].references++;
				return;
			}
			mpCache.Add(key, new MultiplaneCacheObject(1, texture, delegate
			{
				mpCache[key].texture = null;
				mpCache.Remove(key);
				cleanup();
			}));
		}
public static void AddReference(string key)
{
	if (mpCache.ContainsKey(key))
	{
		mpCache[key].references++;
	}
}
		public static void RemoveTexture(string key)
		{
			if (mpCache.ContainsKey(key))
			{
				mpCache[key].references--;
				if (mpCache[key].references == 0)
				{
					mpCache[key].cleanup();
				}
			}
		}
	}
}
