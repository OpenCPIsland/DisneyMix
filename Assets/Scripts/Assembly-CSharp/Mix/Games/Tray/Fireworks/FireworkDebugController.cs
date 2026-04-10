using UnityEngine;
using System.Collections;

namespace Mix.Games.Tray.Fireworks
{
	public class FireworkDebugController : MonoBehaviour
	{
		public Firework launcher;

		private Vector3 l;

		private void Start()
		{
			// Log all GameObjects with this script attached
			var allControllers = FindObjectsOfType<FireworkDebugController>();
			foreach (var controller in allControllers)
			{
				Debug.Log("FireworkDebugController is attached to GameObject: " + controller.gameObject.name);
			}

			// Look for "SceneBackground" prefab in the scene, but not under uiRoot
			var allSceneBackgrounds = FindObjectsOfType<GameObject>();
			foreach (var go in allSceneBackgrounds)
			{
				if (go.name == "SceneBackground" && (go.transform.parent == null || go.transform.parent.name != "uiRoot"))
				{
					Debug.Log("Found SceneBackground GameObject (not under uiRoot): " + go.name);
					StartCoroutine(SetOptimizedUIShaderAfterDelay(go, 1f));
				}
			}

			if (launcher != null)
			{
				l = launcher.transform.localPosition;
			}
		}

		private IEnumerator SetOptimizedUIShaderAfterDelay(GameObject go, float delay)
		{
			yield return new WaitForSeconds(delay);
			var renderers = go.GetComponentsInChildren<Renderer>(true);
			Shader optimizedShader = Shader.Find("Custom/UI/OptimizedUIShader");
			foreach (var renderer in renderers)
			{
				foreach (var mat in renderer.sharedMaterials)
				{
					if (mat != null && optimizedShader != null)
					{
						mat.shader = optimizedShader;
						Debug.Log($"Assigned Custom/UI/OptimizedUIShader to {renderer.gameObject.name}");
					}
				}
			}
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space) && launcher != null)
			{
				if (launcher.transform.GetChild(0).GetChild(1).GetComponent<FontFirework>() != null && !launcher.transform.GetChild(0).GetChild(1).GetComponent<FontFirework>()
					.built)
				{
					launcher.transform.GetChild(0).GetChild(1).GetComponent<FontFirework>()
						.BuildFirework();
				}
				launcher.Launch(l);
			}
		}
	}
}
