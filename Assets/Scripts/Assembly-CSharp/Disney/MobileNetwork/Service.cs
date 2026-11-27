using System;
using System.Collections.Generic;

namespace Disney.MobileNetwork
{
	public static class Service
	{
		private static List<IServiceWrapper> serviceWrapperList;

		[UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetOnLoad()
		{
			// Static services survive play-mode entry when domain reload is disabled.
			ResetAll();
		}

		private static bool IsAlive(object instance)
		{
			if (instance == null)
			{
				return false;
			}
			UnityEngine.Object unityObject = instance as UnityEngine.Object;
			return (object)unityObject == null || unityObject != null;
		}

		public static void Set<T>(T instance)
		{
			if (IsAlive(ServiceWrapper<T>.instance))
			{
				throw new Exception("An instance of this service class has already been set!");
			}
			ServiceWrapper<T>.instance = instance;
			if (serviceWrapperList == null)
			{
				serviceWrapperList = new List<IServiceWrapper>();
			}
			serviceWrapperList.Add(new ServiceWrapper<T>());
		}

		public static T Get<T>()
		{
			T instance = ServiceWrapper<T>.instance;
			return IsAlive(instance) ? instance : default(T);
		}

		public static bool IsSet<T>()
		{
			return IsAlive(ServiceWrapper<T>.instance);
		}

		public static void ResetAll()
		{
			if (serviceWrapperList != null)
			{
				for (int num = serviceWrapperList.Count - 1; num >= 0; num--)
				{
					serviceWrapperList[num].Unset();
				}
				serviceWrapperList.Clear();
			}
		}
	}
}
