using UnityEngine;

namespace Mix
{
	public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		private static T _instance;

		private static object _lock = new object();

		private static bool applicationIsQuitting = false;

		public static T Instance
		{
			get
			{
				lock (_lock)
				{
					if (applicationIsQuitting)
					{
						return (T)null;
					}
					if (_instance == null)
					{
						_instance = (T)Object.FindFirstObjectByType(typeof(T));
						if (Object.FindObjectsByType(typeof(T), FindObjectsSortMode.InstanceID).Length > 1)
						{
							return _instance;
						}
						if (_instance == null)
						{
							GameObject gameObject = new GameObject();
							_instance = gameObject.AddComponent<T>();
							gameObject.name = "(singleton) " + typeof(T).ToString() + Object.FindObjectsByType(typeof(T), FindObjectsSortMode.InstanceID).Length;
							Object.DontDestroyOnLoad(gameObject);
							Singleton<SingletonCleaner>.Instance.AddSingleton(gameObject);
						}
					}
					return _instance;
				}
			}
		}

		public void OnDestroy()
		{
			if (SingletonCleaner.cleaningHouse == 0)
			{
				applicationIsQuitting = true;
			}
			else
			{
				SingletonCleaner.cleaningHouse--;
			}
		}

		public static bool IsInstanceCreated()
		{
			if (_instance == null)
			{
				return false;
			}
			return true;
		}
	}
}
