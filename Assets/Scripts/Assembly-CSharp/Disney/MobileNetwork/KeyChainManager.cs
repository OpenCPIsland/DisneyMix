using System.Collections.Generic;
using UnityEngine;

namespace Disney.MobileNetwork
{
	public class KeyChainManager : MonoBehaviour
	{
		protected Dictionary<string, string> appData = new Dictionary<string, string>();

		// Returns the live keychain manager, creating one if it is missing or was destroyed.
		public static KeyChainManager Instance
		{
			get
			{
				KeyChainManager existing = Service.Get<KeyChainManager>();
				if (existing != null)
				{
					return existing;
				}
				GameObject gameObject = new GameObject(typeof(KeyChainManager).Name);
				return gameObject.AddComponent<KeyChainWindowsManager>();
			}
		}

		public void Awake()
		{
			KeyChainManager existing = Service.Get<KeyChainManager>();
			if (existing != null && existing != this)
			{
				Object.Destroy(base.gameObject);
				return;
			}
			if (existing == null)
			{
				Service.Set<KeyChainManager>(this);
			}
			Object.DontDestroyOnLoad(base.gameObject);
			Init();
		}

		protected virtual void Init()
		{
		}

		public virtual void PutString(string key, string value)
		{
		}

		public virtual string GetString(string key)
		{
			return string.Empty;
		}

		public virtual void RemoveString(string key)
		{
		}
	}
}
