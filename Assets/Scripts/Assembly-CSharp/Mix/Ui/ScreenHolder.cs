using Mix.AssetBundles;
using Mix.Assets;
using UnityEngine;

namespace Mix.Ui
{
	public class ScreenHolder : IBundleObject
	{
		private IScreenHolder caller;

		private string prefabPath;

		private Vector2 position;

		private GameObject mainHolder;

		private Canvas[] mainCanvases;

		public GameObject PrefabObject;

		public Vector2 Position
		{
			get
			{
				return position;
			}
			set
			{
				position = value;
				UpdatePosition();
			}
		}

		public GameObject MainGameObject
		{
			get
			{
				return mainHolder;
			}
		}

		public ScreenHolder(IScreenHolder aCaller, string aPrefabPath, GameObject aPrefab = null)
		{
			prefabPath = aPrefabPath;
			caller = aCaller;
			PrefabObject = aPrefab;
		}

		public void Load()
		{
			if (mainHolder != null)
			{
				caller.OnScreenHolderReady();
			}
			else if (PrefabObject != null)
			{
				OnBundleAssetObject(PrefabObject, null);
			}
			else if (prefabPath.EndsWith(".unity3d"))
			{
				MonoSingleton<AssetManager>.Instance.LoadABundle(this, prefabPath, null, string.Empty);
			}
			else
			{
				GenerateItems();
			}
		}

		public void Destroy()
		{
			PrefabObject = null;
			Object.Destroy(mainHolder);
		}

		public bool IsActive()
		{
			return mainHolder != null;
		}

		public void Hide()
		{
			mainHolder.SetActive(false);
		}

		public void Show()
		{
			mainHolder.SetActive(true);
		}

		public void SendEvent(string aEventName, NavigationRequest aRequest)
		{
			BaseController component = mainHolder.GetComponent<BaseController>();
			if (!(component == null))
			{
				if (aEventName.Equals("OnUILoaded"))
				{
					component.OnUILoaded(aRequest);
				}
				else if (aEventName.Equals("OnUIUnLoaded"))
				{
					component.OnUIUnLoaded(aRequest);
				}
				else if (aEventName.Equals("OnUITransitionStart"))
				{
					component.OnUITransitionStart();
				}
				else if (aEventName.Equals("OnUITransitionEnd"))
				{
					component.OnUITransitionEnd();
				}
				else if (aEventName.Equals("OnAndroidBackButtonClicked"))
				{
					component.OnAndroidBackButtonClicked();
				}
			}
		}

		public void SendDataEvent(string aToken, object aData)
		{
			BaseController component = mainHolder.GetComponent<BaseController>();
			if (component != null)
			{
				component.OnDataReceived(aToken, aData);
			}
		}

		public GameObject FindGameObjectByName(string aName)
		{
			return Util.FindGameObjectByName(mainHolder, aName);
		}

		public void OnBundleAssetObject(Object aGameObject, object aUserData)
		{
			GenerateItems((GameObject)aGameObject);
		}

		private void GenerateItems(GameObject aResource = null)
		{
			if (aResource == null)
			{
				aResource = Resources.Load<GameObject>(prefabPath);
				PrefabObject = aResource;
			}
			if (aResource == null)
			{
				return;
			}
			mainHolder = Object.Instantiate(aResource);
			mainHolder.SetActive(false);
			ResponsiveCanvasScaler.Ensure(mainHolder);
			mainHolder.name = aResource.name;
			mainHolder.transform.SetParent(GameObject.Find("Screens").transform, false);
			mainCanvases = mainHolder.GetComponentsInChildren<Canvas>(true);
			for (int i = 0; i < mainCanvases.Length; i++)
			{
				if (mainCanvases[i].gameObject.activeSelf && mainCanvases[i].name.Contains("Holder"))
				{
					mainCanvases[i].worldCamera = GameObject.Find(mainCanvases[i].name.Replace("Holder", "Camera")).GetComponent<Camera>();
				}
			}
			mainHolder.SetActive(true);
			caller.OnScreenHolderReady();
		}

		private void UpdatePosition()
		{
			Canvas[] array = mainCanvases;
			foreach (Canvas canvas in array)
			{
				for (int j = 0; j < canvas.transform.childCount; j++)
				{
					if (canvas.transform.GetChild(j).GetComponent<RectTransform>() != null)
					{
						canvas.transform.GetChild(j).GetComponent<RectTransform>().anchoredPosition = position;
					}
				}
			}
		}
	}
}
