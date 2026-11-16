using Mix;
using UnityEngine;

namespace Tests
{
	public class ThreadPoolSystemTests : MonoBehaviour
	{
		public enum TestType
		{
			TEST_FRAMEWORK = 0,
			TEST_THREAD_RAN = 1,
			TEST_CLEAN_SINGLETON = 2
		}

		public TestType test;

		private void Start()
		{
			if (test == TestType.TEST_FRAMEWORK)
			{
				baseFramework();
			}
			if (test == TestType.TEST_THREAD_RAN)
			{
				testRun();
			}
			if (test == TestType.TEST_CLEAN_SINGLETON)
			{
				cleanSingleton();
			}
		}

		private void Update()
		{
		}

		private void Success()
		{
			IntegrationTest.Pass(base.gameObject);
		}

		public void baseFramework()
		{
			verifyCleanSingleton();
			IntegrationTest.Assert(base.gameObject, ThreadPoolManager.Instance != null, "Failed to instantiate new thread pool manager singleton");
			ThreadTestImpl threadTestImpl = new ThreadTestImpl(Update);
			IntegrationTest.Assert(base.gameObject, !threadTestImpl.isComplete, "Thread impl instanced incorrectly");
			ThreadPoolManager.Instance.addToPool(threadTestImpl, null);
			IntegrationTest.Assert(base.gameObject, ThreadPoolManager.Instance.ThreadList.GetActiveThreadCount() == 1, "Adding to pool did not increment thread count");
		}

		public void testRun()
		{
			ThreadTestImpl caller = new ThreadTestImpl(Success);
			ThreadPoolManager.Instance.addToPool(caller, null);
		}

		public void cleanSingleton()
		{
			verifyCleanSingleton();
		}

		private void verifyCleanSingleton()
		{
			Object obj = Object.FindFirstObjectByType(typeof(ThreadPoolManager));
			if (obj == null)
			{
				IntegrationTest.Pass(base.gameObject);
				return;
			}
			ThreadPoolManager instance = ThreadPoolManager.Instance;
			Object obj2 = Object.FindFirstObjectByType(typeof(ThreadPoolManager));
			IntegrationTest.Assert(base.gameObject, obj != obj2, "Previous instance of Thread manager was not cleaned in between tests");
			if (!(instance != null))
			{
			}
		}
	}
}
