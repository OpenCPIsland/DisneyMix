using UnityEngine;

namespace Mix.Games.Tray.Drop.PatternEditor
{
	[RequireComponent(typeof(Platform))]
	[ExecuteInEditMode]
	public class LevelCreatorPlatform : MonoBehaviour
	{
		public PlatformInfo PlatformConfiguration;

		[Space(10f)]
		public bool IsDecoy;

		private Platform platform;

		private DropGame dropGame;

		private void Awake()
		{
			platform = GetComponent<Platform>();
			dropGame = DropGame.Instance;
		}

		private void Update()
		{
			PlatformConfiguration.GridCoordinates.x = Mathf.RoundToInt(base.transform.localPosition.x / dropGame.GridSpacing.x);
			PlatformConfiguration.GridCoordinates.y = Mathf.RoundToInt(base.transform.localPosition.z / dropGame.GridSpacing.y);
			UpdateAll();
			base.name = PlatformConfiguration.PathOrder + ": " + PlatformConfiguration.Type.ToString();
			if (IsDecoy)
			{
				base.name += " (Decoy)";
			}
		}

		public void UpdateAll()
		{
			platform.SetDropGame(dropGame);
			platform.CopyConfiguration(PlatformConfiguration);
			UpdatePlatformTime();
			platform.ApplyPlatformConfiguration();
		}

		public void UpdatePlatformTime()
		{
			platform.Configuration.EnterTime = PlatformConfiguration.EnterTime;
			platform.Configuration.ExitTime = PlatformConfiguration.ExitTime;
			float currentTime = 0f;
			LevelCreator levelCreator = Object.FindFirstObjectByType<LevelCreator>();
			if (levelCreator != null && levelCreator.CurrentPatternTemplate != null)
			{
				currentTime = levelCreator.CurrentPatternTemplate.Overlap;
			}
			platform.CurrentTime = currentTime;
		}

		private void OnDrawGizmos()
		{
			if (IsDecoy)
			{
				Gizmos.DrawIcon(base.transform.position + Vector3.up * 1.5f, "Games/Tray/Drop/DecoyIcon.png", true);
			}
		}
	}
}
