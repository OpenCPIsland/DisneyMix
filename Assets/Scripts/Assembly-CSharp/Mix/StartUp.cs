using System;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mix
{
	public class StartUp : MonoBehaviour
	{
		private void Awake()
		{
		//	ClearCacheFolders();
		}

		private void ClearCacheFolders()
		{
			try
			{
				string cachePath = Path.Combine(Application.PersistentDataPath, "cache");

				string keyValDbPath = Path.Combine(cachePath, "KeyValDB");
				string mixSdkPath = Path.Combine(cachePath, "MixSDK");

				if (Directory.Exists(keyValDbPath))
				{
					Directory.Delete(keyValDbPath, true);
					Debug.Log("Deleted KeyValDB cache folder.");
				}

				if (Directory.Exists(mixSdkPath))
				{
					Directory.Delete(mixSdkPath, true);
					Debug.Log("Deleted MixSDK cache folder.");
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error clearing cache folders: {ex.Message}");
			}
		}

		private void Start()
		{
			Screen.fullScreen = false;
			SetCanvasSize();

			EventSystem eventSystem = EnsureEventSystem();
			ConfigureEventSystem(eventSystem);

			TouchScreenKeyboard.hideInput = true;
			Invoke("SetCanvasSize", 1f);
		}

		private EventSystem EnsureEventSystem()
		{
			EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
			if (eventSystem == null)
			{
				GameObject eventSystemObject = new GameObject("EventSystem");
				eventSystem = eventSystemObject.AddComponent<EventSystem>();
				eventSystemObject.AddComponent<StandaloneInputModule>();
				Debug.Log("[StartUp] EventSystem missing. Created fallback EventSystem with StandaloneInputModule.");
				return eventSystem;
			}

			if (eventSystem.GetComponent<BaseInputModule>() == null)
			{
				eventSystem.gameObject.AddComponent<StandaloneInputModule>();
				Debug.Log("[StartUp] EventSystem had no input module. Added StandaloneInputModule.");
			}

			return eventSystem;
		}

		private void ConfigureEventSystem(EventSystem eventSystem)
		{
			int threshold = (Screen.width <= Screen.height) ? (Screen.width / 20) : (Screen.height / 20);
			eventSystem.pixelDragThreshold = (threshold >= 10) ? threshold : 10;
		}

		public void SetCanvasSize()
		{
			if (this.IsNullOrDisposed())
			{
				return;
			}

			GameObject scrimLayer = GameObject.Find("UI_Scrim_Layer");
			if (scrimLayer == null)
			{
				Debug.LogWarning("[StartUp] UI_Scrim_Layer not found. Canvas size not set.");
				return;
			}

			RectTransform component = scrimLayer.GetComponent<RectTransform>();
			if (component == null)
			{
				Debug.LogWarning("[StartUp] UI_Scrim_Layer has no RectTransform. Canvas size not set.");
				return;
			}

			MixConstants.CANVAS_HEIGHT = component.sizeDelta.y;
			MixConstants.CANVAS_WIDTH = component.sizeDelta.x;
		}

		public static void BeginStartSequence()
		{
			MonoSingleton<StartUpSequence>.Instance.Init();
		}

		private void SetLauncherParams()
		{
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			string[] array = commandLineArgs;
			foreach (string text in array)
			{
			}
		}
	}
}
