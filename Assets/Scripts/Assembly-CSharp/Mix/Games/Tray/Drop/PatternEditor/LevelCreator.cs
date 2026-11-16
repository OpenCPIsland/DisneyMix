using System.Collections.Generic;
using Mix.Games.Tray.Common;
using UnityEngine;

namespace Mix.Games.Tray.Drop.PatternEditor
{
	[ExecuteInEditMode]
	public class LevelCreator : MonoBehaviour
	{
		public int GridSize = 9;

		public bool TestCurrentPattern;

		private DropGame dropGame;

		public PatternTemplate CurrentPatternTemplate;

		public float VisualizerTimeOffset
		{
			get
			{
				return 2f;
			}
		}

		private void Update()
		{
			if (CurrentPatternTemplate != null)
			{
				base.name = "Pattern Creator - " + CurrentPatternTemplate.Name;
			}
			if (UnityEngine.Application.isPlaying)
			{
				base.gameObject.SetActive(false);
			}
		}

		public List<LevelCreatorPlatform> GetPathPlatforms()
		{
			List<LevelCreatorPlatform> list = new List<LevelCreatorPlatform>(GetComponentsInChildren<LevelCreatorPlatform>());
			List<LevelCreatorPlatform> list2 = new List<LevelCreatorPlatform>();
			for (int i = 0; i < list.Count; i++)
			{
				if (!list[i].IsDecoy)
				{
					list2.Add(list[i]);
				}
			}
			list2.Sort(new LevelCreatorPlatformOrderSorter());
			return list2;
		}

		private List<LevelCreatorPlatform> GetDecoyPlatforms()
		{
			List<LevelCreatorPlatform> list = new List<LevelCreatorPlatform>(GetComponentsInChildren<LevelCreatorPlatform>());
			List<LevelCreatorPlatform> list2 = new List<LevelCreatorPlatform>();
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].IsDecoy)
				{
					list2.Add(list[i]);
				}
			}
			list2.Sort(new LevelCreatorPlatformOrderSorter());
			return list2;
		}

		private void OnDrawGizmos()
		{
			if (dropGame == null)
			{
				dropGame = Object.FindFirstObjectByType<DropGame>();
			}
			Gizmos.matrix = base.transform.localToWorldMatrix;
			Gizmos.color = new Color(0f, 0f, 0f, 0.5f);
			Gizmos.DrawCube(Vector3.zero, new Vector3(dropGame.GridSpacing.x, 0.1f, dropGame.GridSpacing.y));
			Gizmos.color = new Color(0f, 0f, 0f, 0.8f);
			Gizmos.DrawWireCube(Vector3.zero, new Vector3(dropGame.GridSpacing.x, 0.1f, dropGame.GridSpacing.y));
			DrawPathOrder();
			DrawGizmoGrid(GridSize * 2 + 1, GridSize * 2 + 1);
			float num = 5f;
			float visualizerTimeOffset = VisualizerTimeOffset;
			Gizmos.color = new Color(0f, 0f, 0f, 0.2f);
			Gizmos.DrawCube(Vector3.up * (visualizerTimeOffset / 2f) * num, new Vector3(1.8f, visualizerTimeOffset * num, 1.8f));
			Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
			Gizmos.DrawWireCube(Vector3.up * (visualizerTimeOffset / 2f) * num, new Vector3(1.8f, visualizerTimeOffset * num, 1.8f));
			if (CurrentPatternTemplate != null && CurrentPatternTemplate.PathPlatforms.Count > 0)
			{
				float num2 = 1f;
				float num3 = VisualizerTimeOffset - CurrentPatternTemplate.Overlap;
				float num4 = float.MinValue;
				Gizmos.color = new Color(1f, 1f, 0.2f, 0.4f);
				for (int i = 0; i < CurrentPatternTemplate.PathPlatforms.Count; i++)
				{
					num4 = Mathf.Max(num4 + num2, CurrentPatternTemplate.PathPlatforms[i].EnterTime);
					Gizmos.DrawWireCube(Vector3.up * (num3 + num4 + num2 / 2f) * num, new Vector3(0.5f, num2 * num, 0.5f));
				}
				Gizmos.color = Color.red;
				Gizmos.DrawWireCube(Vector3.up * (num3 + CurrentPatternTemplate.NextPatternTime) * num, new Vector3(1.9f, 0f, 1.9f));
			}
		}

		private void DrawPathOrder()
		{
			Vector3 vector = Vector3.up * 4f;
			Gizmos.color = Color.cyan;
			List<LevelCreatorPlatform> pathPlatforms = GetPathPlatforms();
			List<LevelCreatorPlatform> decoyPlatforms = GetDecoyPlatforms();
			Gizmos.color = Color.red;
			if (pathPlatforms.Count > 0)
			{
				GizmoExtensions.DrawArrowGizmo(vector, pathPlatforms[0].transform.localPosition + vector, 0.2f);
				for (int i = 0; i < pathPlatforms.Count - 1; i++)
				{
					GizmoExtensions.DrawArrowGizmo(pathPlatforms[i].transform.localPosition + vector, pathPlatforms[i + 1].transform.localPosition + vector, 0.2f);
				}
			}
			Gizmos.color = Color.Lerp(Color.red, Color.black, 0.5f);
			for (int j = 0; j < decoyPlatforms.Count; j++)
			{
				if (decoyPlatforms[j].PlatformConfiguration.PathOrder >= 0)
				{
					if (decoyPlatforms[j].PlatformConfiguration.PathOrder < pathPlatforms.Count)
					{
						GizmoExtensions.DrawArrowGizmo(decoyPlatforms[j].transform.localPosition + vector, pathPlatforms[decoyPlatforms[j].PlatformConfiguration.PathOrder].transform.localPosition + vector, 0.2f);
					}
				}
				else
				{
					GizmoExtensions.DrawArrowGizmo(decoyPlatforms[j].transform.localPosition + vector, vector, 0.2f);
				}
			}
		}

		private void DrawGizmoGrid(int xCount, int yCount)
		{
			Gizmos.color = new Color(0f, 0f, 0f, 0.5f);
			Vector3 vector = new Vector3((float)(-xCount) * dropGame.GridSpacing.x * 0.5f, 0f, (float)(-yCount) * dropGame.GridSpacing.y * 0.5f);
			Vector2 vector2 = new Vector2((float)xCount * dropGame.GridSpacing.x, (float)yCount * dropGame.GridSpacing.y);
			for (int i = 0; i <= xCount; i++)
			{
				Gizmos.DrawLine(vector + new Vector3(dropGame.GridSpacing.x * (float)i, 0f, 0f), vector + new Vector3(dropGame.GridSpacing.x * (float)i, 0f, vector2.y));
			}
			for (int j = 0; j <= yCount; j++)
			{
				Gizmos.DrawLine(vector + new Vector3(0f, 0f, dropGame.GridSpacing.y * (float)j), vector + new Vector3(vector2.x, 0f, dropGame.GridSpacing.y * (float)j));
			}
		}
	}
}
