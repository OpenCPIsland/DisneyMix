using UnityEngine;
using UnityTest;

namespace Mix
{
	public class UnityGameSceneWrapper : IUnitySceneInterface
	{
		private static ServiceLocator serviceLocator;

		public IUnityTestRunnerObject GetTestObj()
		{
			UnityTest.TestRunner testRunner = (UnityTest.TestRunner)Object.FindObjectOfType(typeof(UnityTest.TestRunner));
			if (testRunner != null)
			{
				return new TestRunnerGameObject(testRunner);
			}
			return null;
		}

		public IServiceLocator GetServiceLocator()
		{
			if (serviceLocator == null)
			{
				serviceLocator = new ServiceLocator();
			}
			return serviceLocator;
		}
	}
}
