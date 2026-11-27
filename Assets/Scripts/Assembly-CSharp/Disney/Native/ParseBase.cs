using System.Collections.Generic;
using Mix;
using UnityEngine;
using UnityEngine.UI;

namespace Disney.Native
{
	public class ParseBase<T>
	{
		protected IAccessibilityLocalization localization;

		protected Dictionary<int, T> items = new Dictionary<int, T>();

		public ParseBase(IAccessibilityLocalization aLocalization)
		{
			localization = aLocalization;
		}

		public void Clear()
		{
			items.Clear();
		}

		public string GetTokenText(AccessibilitySettings aSettings)
		{
			string text = string.Empty;
			if (aSettings == null)
			{
				return text;
			}
			if (aSettings is ScrollerAccessibilitySettings && ((ScrollerAccessibilitySettings)aSettings).birthdateScroller != null)
			{
				return ((ScrollerAccessibilitySettings)aSettings).GetSayScrollText(0);
			}
			if (!string.IsNullOrEmpty(aSettings.DynamicText))
			{
				return aSettings.DynamicText;
			}
			if (!string.IsNullOrEmpty(aSettings.CustomToken))
			{
				text = localization.GetString(aSettings.CustomToken);
			}
			if (aSettings.ReferenceToken != null)
			{
				Text componentInChildren = aSettings.ReferenceToken.GetComponentInChildren<Text>();
				if (componentInChildren != null)
				{
					text = text + " " + componentInChildren.text;
				}
			}
			return text;
		}

		public void Parse(GameObject[] gameObjects)
		{
			List<int> list = new List<int>();
			foreach (GameObject gameObject in gameObjects)
			{
				if (!gameObject.activeSelf)
				{
					continue;
				}
				T[] componentsInChildren = gameObject.GetComponentsInChildren<T>(false);
				T[] array = componentsInChildren;
				foreach (T val in array)
				{
					Object obj = val as Object;
					if (obj == null)
					{
						continue;
					}
					AccessibilitySettings component = GetGameobject(val).GetComponent<AccessibilitySettings>();
					if (!(component != null) || component.IgnoreText || (component.VoiceOnly && MonoSingleton<NativeAccessiblityManager>.Instance.AccessibilityLevel != NativeAccessibilityLevel.VOICE) || component.DontRender)
					{
						continue;
					}
					if (GetGameobject(val).transform.parent.gameObject.activeSelf && component.VisibleOnlyForSwitchControl)
					{
						GetGameobject(val).SetActive(true);
					}
					if (!GetGameobject(val).activeSelf)
					{
						continue;
					}
					int instanceID = obj.GetEntityId().GetHashCode();
					if (MonoSingleton<NativeAccessiblityManager>.Instance.HiddenScrollItemIds.IndexOf(component) <= -1)
					{
						list.Add(instanceID);
						if (items.ContainsKey(instanceID))
						{
							Update(val);
							continue;
						}
						Add(val, instanceID, component.VoiceOnly);
						items.Add(instanceID, val);
					}
				}
			}
			Dictionary<int, T> dictionary = new Dictionary<int, T>();
			foreach (KeyValuePair<int, T> item in items)
			{
				if (item.Value != null && list.Contains(item.Key))
				{
					dictionary.Add(item.Key, item.Value);
				}
				else
				{
					Remove(item.Key);
				}
			}
			items = dictionary;
		}

		protected virtual GameObject GetGameobject(T aItem)
		{
			return (aItem as Component).gameObject;
		}

		protected virtual void Add(T aItem, int aId, bool isVoiceOnly)
		{
			AccessibilitySettings component = GetGameobject(aItem).GetComponent<AccessibilitySettings>();
			Rect rectInPhysicalScreenSpace = Util.GetRectInPhysicalScreenSpace(GetGameobject(aItem).GetComponent<RectTransform>());
			string tokenText = GetTokenText(component);
			if (isVoiceOnly)
			{
				MonoSingleton<NativeAccessiblityManager>.Instance.Native.RenderText(aId, rectInPhysicalScreenSpace, tokenText);
			}
			else
			{
				MonoSingleton<NativeAccessiblityManager>.Instance.Native.RenderButton(aId, rectInPhysicalScreenSpace, tokenText);
			}
		}

		protected virtual void Update(T aItem)
		{
			AccessibilitySettings component = GetGameobject(aItem).GetComponent<AccessibilitySettings>();
			Rect rectInPhysicalScreenSpace = Util.GetRectInPhysicalScreenSpace(GetGameobject(aItem).GetComponent<RectTransform>());
			MonoSingleton<NativeAccessiblityManager>.Instance.Native.UpdateView(GetGameobject(aItem).GetEntityId().GetHashCode(), rectInPhysicalScreenSpace, GetTokenText(component));
		}

		protected virtual void Remove(int aId)
		{
			MonoSingleton<NativeAccessiblityManager>.Instance.Native.RemoveView(aId);
		}
	}
}
