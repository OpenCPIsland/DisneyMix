using TestRunnerType = UnityTest.TestRunner;
using UnityTest;

namespace Mix
{
	public class TestRunnerGameObject : IUnityTestRunnerObject
	{
		public TestRunnerType testRunnerObj;

		public TestRunnerGameObject(TestRunnerType _gameObj)
		{
			testRunnerObj = _gameObj;
		}

		public string GetTestName()
		{
			if (testRunnerObj == null)
			{
				return string.Empty;
			}
			if (testRunnerObj.currentTest == null)
			{
				return string.Empty;
			}
			return testRunnerObj.currentTest.name;
		}
	}
}
