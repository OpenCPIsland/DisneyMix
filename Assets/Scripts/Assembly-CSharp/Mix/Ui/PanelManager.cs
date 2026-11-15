using System;
using System.Collections.Generic;
using System.Linq;
using Mix.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class PanelManager : Singleton<PanelManager>
	{
		public const int AndroidExitPanelId = 10;

		private List<BasePanel> panelList = new List<BasePanel>();

		private List<GameObject> gameObjectPanels = new List<GameObject>();

		private List<string> foregroundLayer = new List<string> { Panels.FRIEND_RESULT };

		public BasePanel ShowPanel(GameObject aPanelGameObject)
		{
			CleanUpPanels();
			ResponsiveCanvasScaler.Ensure(aPanelGameObject);
			BasePanel component = aPanelGameObject.GetComponent<BasePanel>();
			component.Setup();
			component.PanelClosedEvent += PanelClosed;
			panelList.Add(component);
			gameObjectPanels.Add(aPanelGameObject);
			aPanelGameObject.SetActive(true);
			return component;
		}

		public void HidePanel(GameObject aPanelGameObject)
		{
			if (gameObjectPanels.Contains(aPanelGameObject))
			{
				aPanelGameObject.GetComponent<BasePanel>().ClosePanel();
			}
		}

		public BasePanel ShowPanel(string aPanelType, bool aAllowMultiple = true, int aPanelId = -1)
		{
			CleanUpPanels();
			if (!aAllowMultiple && IsPanelOpen())
			{
				return null;
			}
			GameObject original = Resources.Load<GameObject>(aPanelType);
			GameObject gameObject = UnityEngine.Object.Instantiate(original);
			gameObject.SetActive(false);
			ResponsiveCanvasScaler.Ensure(gameObject);
			BasePanel component = gameObject.GetComponent<BasePanel>();
			component.panelId = aPanelId;
			component.Setup();
			component.PanelClosedEvent += PanelClosed;
			panelList.Add(component);
			Transform transform = GameObject.Find(PanelLayer(aPanelType)).transform;
			gameObject.transform.SetParent((transform.childCount != 1) ? transform : transform.GetChild(0), false);
			gameObject.SetActive(true);
			return component;
		}

		public BasePanel FindPanel(Type aPanelType)
		{
			foreach (BasePanel panel in panelList)
			{
				if (panel.GetType().Equals(aPanelType))
				{
					return panel;
				}
			}
			return null;
		}

		public bool IsPanelOpen()
		{
			CleanUpPanels();
			return panelList.Count > 0;
		}

		public BasePanel GetPanelWithId(int aPanelId)
		{
			foreach (BasePanel panel in panelList)
			{
				if (panel.panelId == aPanelId)
				{
					return panel;
				}
			}
			return null;
		}

		public bool OnAndroidBackButton()
		{
			CleanUpPanels();
			foreach (BasePanel item in Enumerable.Reverse(panelList.ToList()))
			{
				if (item.OnAndroidBackButton())
				{
					return true;
				}
			}
			return false;
		}

		public void SetupButtons(Dictionary<Button, Action> aCallbackList)
		{
			foreach (KeyValuePair<Button, Action> aCallback in aCallbackList)
			{
				if (aCallback.Value != null)
				{
					if (aCallback.Key != null)
					{
						aCallback.Key.gameObject.SetActive(true);
						aCallback.Key.onClick.AddListener(aCallback.Value.Invoke);
					}
				}
				else
				{
					aCallback.Key.gameObject.SetActive(false);
				}
			}
		}

		public void SetupTokens(Dictionary<Text, string> aTokenList)
		{
			foreach (KeyValuePair<Text, string> aToken in aTokenList)
			{
				if (aToken.Value != null)
				{
					if (aToken.Key != null)
					{
						aToken.Key.gameObject.SetActive(true);
						aToken.Key.text = Singleton<Localizer>.Instance.getString(aToken.Value);
						if (aToken.Key.text.Equals(Localizer.NO_TOKEN))
						{
							aToken.Key.text = aToken.Value;
						}
					}
				}
				else
				{
					aToken.Key.gameObject.SetActive(false);
				}
			}
		}

		public void PanelClosed(BasePanel aPanel)
		{
			aPanel.PanelClosedEvent -= PanelClosed;
			panelList.Remove(aPanel);
			if (gameObjectPanels.Contains(aPanel.gameObject))
			{
				aPanel.gameObject.SetActive(false);
				gameObjectPanels.Remove(aPanel.gameObject);
			}
			else
			{
				UnityEngine.Object.Destroy(aPanel.gameObject);
			}
		}

		private void CleanUpPanels()
		{
			List<BasePanel> list = new List<BasePanel>();
			foreach (BasePanel panel in panelList)
			{
				if (panel == null || panel.gameObject == null)
				{
					list.Add(panel);
				}
			}
			panelList.RemoveAll(list.Contains);
			gameObjectPanels.RemoveAll((GameObject item) => item == null);
		}

		private string PanelLayer(string aPanel)
		{
			return (!foregroundLayer.Contains(aPanel)) ? "Overlay_Holder" : "UI_FG_Holder";
		}
	}
}
