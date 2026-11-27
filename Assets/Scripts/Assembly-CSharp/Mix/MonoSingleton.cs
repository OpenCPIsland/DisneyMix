using UnityEngine;

namespace Mix
{
	internal static class MonoSingletonState
	{
		public static bool ApplicationIsQuitting;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetOnLoad()
		{
			// Statics survive play-mode entry when domain reload is disabled.
			ApplicationIsQuitting = false;
			SingletonCleaner.cleaningHouse = 0;
			UnityEngine.Application.quitting -= OnQuitting;
			UnityEngine.Application.quitting += OnQuitting;
		}

		private static void OnQuitting()
		{
			ApplicationIsQuitting = true;
		}
	}

	public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		private static T _instance;

		private static object _lock = new object();

		public static T Instance
		{
			get
			{
				lock (_lock)
				{
					if (MonoSingletonState.ApplicationIsQuitting)
					{
						return (T)null;
					}
					if (_instance == null)
					{
						_instance = (T)Object.FindFirstObjectByType(typeof(T));
						if (_instance == null)
						{
							GameObject gameObject = new GameObject();
							_instance = gameObject.AddComponent<T>();
							gameObject.name = "(singleton) " + typeof(T).ToString();
							Object.DontDestroyOnLoad(gameObject);
							Singleton<SingletonCleaner>.Instance?.AddSingleton(gameObject);
						}
					}
					return _instance;
				}
			}
		}

		public void OnDestroy()
		{
			// Destroying a singleton (scene change, SingletonCleaner restart) must not
			// permanently disable it; just drop the cached reference so it can be recreated.
			if (_instance == this)
			{
				_instance = (T)null;
			}
			if (SingletonCleaner.cleaningHouse > 0)
			{
				SingletonCleaner.cleaningHouse--;
			}
		}

		public static bool IsInstanceCreated()
		{
			return _instance != null;
		}
	}
}
