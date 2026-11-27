using Mix.Games.Session;
using UnityEngine;

namespace Mix.Games.Tray
{
	public abstract class DebugSceneIndicator : MonoBehaviour
	{
		public delegate IGameSession GameManagerRetriever();

		private static bool mIsDebugScene;

		public static bool IsDebugScene
		{
			get
			{
				return UnityEngine.Application.isEditor && mIsDebugScene;
			}
		}

		public static bool IsMainScene
		{
			get
			{
				return !IsDebugScene;
			}
		}

		public static GameManagerRetriever GetGameManager { get; set; }

		// CoreCLR / no domain reload: mIsDebugScene is only ever set to true, so after one run of a
		// standalone game debug scene every later play session would be treated as a debug scene.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticStateForPlaySession()
		{
			mIsDebugScene = false;
			GetGameManager = null;
		}

		private void Awake()
		{
			mIsDebugScene = true;
		}
	}
}
