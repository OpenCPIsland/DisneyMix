using System.Collections.Generic;
using System.Globalization;
using Mix.Data;
using Mix.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Mix
{
	public static class Util
	{
		public const string ON = "fj4";

		public static Rect GetRectInPhysicalScreenSpace(RectTransform aRectTransform)
		{
			if (aRectTransform == null || aRectTransform.gameObject == null)
			{
				return new Rect(0f, 0f, 0f, 0f);
			}
			Canvas parentCanvas = GetParentCanvas(aRectTransform.gameObject);
			if (parentCanvas == null)
			{
				return new Rect(0f, 0f, 0f, 0f);
			}
			Camera worldCamera = parentCanvas.worldCamera;
			Vector3[] array = new Vector3[4];
			aRectTransform.GetWorldCorners(array);
			Vector2 vector = RectTransformUtility.WorldToScreenPoint(worldCamera, array[0]);
			Vector2 vector2 = RectTransformUtility.WorldToScreenPoint(worldCamera, array[1]);
			Vector2 vector3 = RectTransformUtility.WorldToScreenPoint(worldCamera, array[2]);
			float num = (float)Singleton<SettingsManager>.Instance.GetScreenWidth() / (float)Screen.width;
			float num2 = (float)Singleton<SettingsManager>.Instance.GetScreenHeight() / (float)Screen.height;
			int num3 = (int)((vector3.x - vector2.x) * num);
			int num4 = (int)((vector2.y - vector.y) * num2);
			int num5 = (int)(vector2.x * num);
			int num6 = (int)(vector2.y * num2);
			num6 = Singleton<SettingsManager>.Instance.GetScreenHeight() - num6;
			return new Rect(num5, num6, num3, num4);
		}

		public static Rect GetRectInScreenSpace(RectTransform aRectTransform)
		{
			Vector3[] array = new Vector3[4];
			aRectTransform.GetWorldCorners(array);
			Canvas parentCanvas = GetParentCanvas(aRectTransform.gameObject);
			if (parentCanvas == null)
			{
				return new Rect(0f, 0f, 0f, 0f);
			}
			Camera worldCamera = parentCanvas.worldCamera;
			Vector2 vector = RectTransformUtility.WorldToScreenPoint(worldCamera, array[0]);
			Vector2 vector2 = RectTransformUtility.WorldToScreenPoint(worldCamera, array[1]);
			int num = (int)(RectTransformUtility.WorldToScreenPoint(worldCamera, array[2]).x - vector2.x);
			int num2 = (int)(vector2.y - vector.y);
			int num3 = (int)vector.x;
			int num4 = (int)vector.y;
			return new Rect(num3, num4, num, num2);
		}

		public static void EllipsizeText(ref Text aText)
		{
			bool flag = false;
			aText.CalculateLayoutInputHorizontal();
			aText.CalculateLayoutInputVertical();
			if (!(aText.rectTransform.rect.width <= 0f))
			{
				while (aText.preferredWidth > aText.rectTransform.rect.width)
				{
					aText.text = aText.text.Substring(0, aText.text.Length - 1);
					flag = true;
				}
				if (flag && aText.text.Length > 3)
				{
					aText.text = aText.text.Substring(0, aText.text.Length - 3) + "...";
				}
			}
		}

		public static Rect ExpandRect(Rect aSource, float aSize)
		{
			return ExpandRect(aSource, aSize, aSize, aSize, aSize);
		}

		public static Rect ExpandRect(Rect aSource, float aLeft, float aRight, float aTop, float aBottom)
		{
			return new Rect(aSource.x - aLeft, aSource.y - aBottom, aSource.width + aLeft + aRight, aSource.height + aTop + aBottom);
		}

		public static List<T> DeepCopyList<T>(List<T> input)
		{
			List<T> list = new List<T>();
			foreach (T item in input)
			{
				list.Add(item);
			}
			return list;
		}

		public static Canvas GetParentCanvas(GameObject aGameObject)
		{
			Canvas canvas = null;
			Transform parent = aGameObject.transform.parent;
			while (canvas == null && parent != null)
			{
				canvas = parent.GetComponent<Canvas>();
				parent = parent.parent;
			}
			return canvas;
		}

		public static Color HexToColor(string aColorString)
		{
			if (aColorString == null || (aColorString.Length != 6 && aColorString.Length != 7))
			{
				return Color.clear;
			}
			if (aColorString.Length == 7)
			{
				aColorString = aColorString.Substring(1, 6);
			}
			byte r = byte.Parse(aColorString.Substring(0, 2), NumberStyles.HexNumber);
			byte g = byte.Parse(aColorString.Substring(2, 2), NumberStyles.HexNumber);
			byte b = byte.Parse(aColorString.Substring(4, 2), NumberStyles.HexNumber);
			return new Color32(r, g, b, byte.MaxValue);
		}

		public static void SetLayerRecursively(GameObject gameObj, int layer)
		{
			if (gameObj == null)
			{
				return;
			}
			gameObj.layer = layer;
			foreach (Transform item in gameObj.transform)
			{
				if (!(item == null))
				{
					SetLayerRecursively(item.gameObject, layer);
				}
			}
		}

		public static Vector2 Vector2Update(Vector2 currentValue, Vector2 targetValue, float speed)
		{
			Vector2 vector = targetValue - currentValue;
			currentValue += vector * speed * Time.deltaTime;
			return currentValue;
		}

		public static Vector2 HalfDistance(Vector2 currentValue, Vector2 targetValue)
		{
			Vector2 vector = targetValue - currentValue;
			currentValue += vector / 2f;
			return currentValue;
		}

		public static GameObject FindGameObjectByName(GameObject aGameObject, string aName)
		{
			Transform[] componentsInChildren = aGameObject.GetComponentsInChildren<Transform>(true);
			Transform[] array = componentsInChildren;
			foreach (Transform transform in array)
			{
				if (transform.name.Equals(aName))
				{
					return transform.gameObject;
				}
			}
			return null;
		}

		public static string AddOrdinal(int num)
		{
			if (num <= 0)
			{
				return num.ToString();
			}
			switch (num % 100)
			{
			case 11:
			case 12:
			case 13:
				return num + "th";
			default:
				switch (num % 10)
				{
				case 1:
					return num + "st";
				case 2:
					return num + "nd";
				case 3:
					return num + "rd";
				default:
					return num + "th";
				}
			}
		}

		public static void UpdateTintablesForOfficialAccount(Official_Account aAccount, GameObject aParent)
		{
			TintImage[] componentsInChildren = aParent.GetComponentsInChildren<TintImage>(true);
			TintImage[] array = componentsInChildren;
			foreach (TintImage tintImage in array)
			{
				tintImage.officialAccount = aAccount;
				tintImage.OnColorThemeChanged();
			}
			TintText[] componentsInChildren2 = aParent.GetComponentsInChildren<TintText>();
			TintText[] array2 = componentsInChildren2;
			foreach (TintText tintText in array2)
			{
				tintText.officialAccount = aAccount;
				tintText.OnColorThemeChanged();
			}
		}

		public static bool IsLowEndDevice()
		{
			return SystemInfo.processorCount == 1;
		}

		public static bool IsNullOrDisposed(this Object o)
		{
			return o == null || o.Equals(null);
		}
	}
}
