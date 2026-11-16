using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		private static T _instance;

		private static object _lock = new object();

		private static bool applicationIsQuitting = false;

		public static T Instance
		{
			get
			{
				if (applicationIsQuitting)
				{
					Debug.LogWarning(string.Concat("[Singleton] Instance '", typeof(T), "' already destroyed on application quit. Won't create again - returning null."));
					return (T)null;
				}
				lock (_lock)
				{
					if (_instance == null)
					{
						_instance = (T)Object.FindFirstObjectByType(typeof(T));
						if (Object.FindObjectsByType(typeof(T), FindObjectsSortMode.InstanceID).Length > 1)
						{
							Debug.LogError("[Singleton] Something went really wrong  - there should never be more than 1 singleton! Reopenning the scene might fix it.");
							return _instance;
						}
						if (_instance == null)
						{
							GameObject gameObject = new GameObject();
							_instance = gameObject.AddComponent<T>();
							gameObject.name = "(singleton) " + typeof(T).ToString();
							Object.DontDestroyOnLoad(gameObject);
						}
					}
					return _instance;
				}
			}
		}

		public static void DestroyInstance()
		{
			_instance = (T)null;
		}

		public void OnDestroy()
		{
			applicationIsQuitting = true;
		}
	}
}
