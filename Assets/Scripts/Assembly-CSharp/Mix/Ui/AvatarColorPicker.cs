using System.Collections.Generic;
using Mix.AvatarInternal;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class AvatarColorPicker : MonoBehaviour
	{
		private Transform contentHolder;

		private IAvatarColorListener listener;

		private List<AvatarColorChip> colors;

		private bool IsInitialized;

		private int currentSelection = -1;

		public GameObject colorInst;

		private void Start()
		{
			Debug.Log($"[AvatarColorPicker] Start called activeSelf={base.gameObject.activeSelf} activeInHierarchy={base.gameObject.activeInHierarchy}");
		}

		private void Update()
		{
		}

		public void Init(IAvatarColorListener aListener)
		{
			Debug.Log($"[AvatarColorPicker] Init called activeSelf={base.gameObject.activeSelf} activeInHierarchy={base.gameObject.activeInHierarchy}");
			contentHolder = base.gameObject.transform.Find("ColorPickerContent");
			if (contentHolder == null)
			{
				Debug.LogWarning("[AvatarColorPicker] Init: ColorPickerContent not found");
			}
			else
			{
				Debug.Log($"[AvatarColorPicker] Init: contentHolder found activeSelf={contentHolder.gameObject.activeSelf} activeInHierarchy={contentHolder.gameObject.activeInHierarchy}");
			}
			colors = new List<AvatarColorChip>();
			listener = aListener;
			IsInitialized = true;
		}

		public bool HasInitialized()
		{
			return IsInitialized;
		}

		private void ClearColorContents()
		{
			Debug.Log("[AvatarColorPicker] ClearColorContents called");
			if (contentHolder == null)
			{
				Debug.LogWarning("[AvatarColorPicker] ClearColorContents: contentHolder is null");
				return;
			}
			Debug.Log($"[AvatarColorPicker] ClearColorContents: childCount={contentHolder.childCount}");
			for (int i = 0; i < contentHolder.childCount; i++)
			{
				if (contentHolder.GetChild(i).gameObject.activeSelf)
				{
					Object.Destroy(contentHolder.GetChild(i).gameObject);
				}
			}
			colors.Clear();
		}

		public void SetSelected(int index)
		{
			Debug.Log($"[AvatarColorPicker] SetSelected called: index={index}, currentSelection={currentSelection}, colorsCount={(colors != null ? colors.Count : -1)}, activeSelf={base.gameObject.activeSelf}, activeInHierarchy={base.gameObject.activeInHierarchy}");
			if (colors != null && index < colors.Count && currentSelection != index)
			{
				HorizontalLayoutGroup component = contentHolder.gameObject.GetComponent<HorizontalLayoutGroup>();
				float num = colors[index].GetButtonWidth() + (float)component.padding.left + (float)component.padding.right;
				RectTransform component2 = contentHolder.gameObject.GetComponent<RectTransform>();
				RectTransform component3 = base.gameObject.GetComponent<RectTransform>();
				Vector2 anchoredPosition = new Vector2(-1f * num * (float)index + component3.rect.width / 2f - num / 2f, component2.anchoredPosition.y);
				component2.anchoredPosition = anchoredPosition;
				colors[index].SetToggleState(true);
				currentSelection = index;
				Debug.Log($"[AvatarColorPicker] SetSelected completed: currentSelection={currentSelection}");
			}
			else
			{
				Debug.LogWarning($"[AvatarColorPicker] SetSelected ignored: colorsNull={(colors == null)}, indexValid={(colors != null && index < colors.Count)}, alreadySelected={(currentSelection == index)}");
			}
		}

		public void SetContent(string categoryName)
		{
			Debug.Log($"[AvatarColorPicker] SetContent called: categoryName={categoryName}, activeSelf={base.gameObject.activeSelf}, activeInHierarchy={base.gameObject.activeInHierarchy}");
			Color[] colorsByCategoryName = AvatarColorTints.GetColorsByCategoryName(categoryName);
			ClearColorContents();
			currentSelection = -1;
			if (colorsByCategoryName == null)
			{
				Debug.LogWarning($"[AvatarColorPicker] SetContent: no colors found for categoryName={categoryName}");
				base.gameObject.SetActive(false);
				return;
			}
			if (!base.gameObject.activeSelf)
			{
				Debug.Log("[AvatarColorPicker] SetContent: re-enabling picker gameObject");
				base.gameObject.SetActive(true);
			}
			Debug.Log($"[AvatarColorPicker] SetContent: loading {colorsByCategoryName.Length} colors");
			for (int i = 0; i < colorsByCategoryName.Length; i++)
			{
				colors.Add(new AvatarColorChip(contentHolder, i, listener, colorInst, colorsByCategoryName[i]));
			}
			Debug.Log($"[AvatarColorPicker] SetContent: finished, colorsCount={colors.Count}, contentHolderChildCount={(contentHolder != null ? contentHolder.childCount : -1)}");
		}
	}
}
