using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityTest;

namespace Mix.Test
{
	public class TestingUtils
	{
		public static void SetClientVersion(string aVersion)
		{
			string path = Application.DataPath + "/DynamicAssets/Common/Resources/version.txt";
			File.SetAttributes(path, FileAttributes.Normal);
			File.WriteAllText(path, aVersion);
		}

		public static void RemoveAllGameObjects()
		{
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				GameObject[] rootGameObjects = SceneManager.GetSceneAt(i).GetRootGameObjects();
				foreach (GameObject gameObject in rootGameObjects)
				{
					if (gameObject != null && gameObject.GetComponent<TestComponent>() == null && gameObject.GetComponent<UnityTest.TestRunner>() == null)
					{
						Object.DestroyImmediate(gameObject);
					}
				}
			}
		}

		public static void ClearAllCache()
		{
			Caching.ClearCache();
			PlayerPrefs.DeleteAll();
			DeleteDirectory(Application.PersistentDataPath);
		}

		public static void DeleteDirectory(string path)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			if (directoryInfo.Exists)
			{
				UpdateAllFilesWritePermissions(path);
				Directory.Delete(path, true);
			}
			if (File.Exists(path + ".meta"))
			{
				File.SetAttributes(path + ".meta", FileAttributes.Normal);
				File.Delete(path + ".meta");
			}
		}

		private static void UpdateAllFilesWritePermissions(string sourceDirName)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirName);
			DirectoryInfo[] directories = directoryInfo.GetDirectories();
			if (!directoryInfo.Exists)
			{
				throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
			}
			FileInfo[] files = directoryInfo.GetFiles();
			FileInfo[] array = files;
			foreach (FileInfo fileInfo in array)
			{
				string path = Path.Combine(sourceDirName, fileInfo.Name);
				if (File.Exists(path))
				{
					File.SetAttributes(path, FileAttributes.Normal);
				}
			}
			DirectoryInfo[] array2 = directories;
			foreach (DirectoryInfo directoryInfo2 in array2)
			{
				UpdateAllFilesWritePermissions(directoryInfo2.FullName);
			}
		}
	}
}
