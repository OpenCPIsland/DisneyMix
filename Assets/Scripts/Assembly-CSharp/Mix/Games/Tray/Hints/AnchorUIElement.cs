using Mix.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Games.Tray.Hints
{
	public class AnchorUIElement : MonoBehaviour
	{
		private const float LANDSCAPE_INSET_WIDTH_RATIO = 2f;

		private const float MIN_LANDSCAPE_INSET_PIXELS = 110f;

		private const float MAX_LANDSCAPE_INSET_PIXELS = 210f;

		public enum AnchorStyle
		{
			NONE = 0,
			FILL = 1,
			TOP_LEFT = 2,
			TOP_CENTER = 3,
			TOP_RIGHT = 4,
			BOTTOM_LEFT = 5,
			BOTTOM_CENTER = 6,
			BOTTOM_RIGHT = 7
		}

		public float defaultPadding = 10f;

		[Space(10f)]
		public GameObject leftSpacer;

		public GameObject rightSpacer;

		private RectTransform mRectTransform;

		public static float GetLandscapeInset(RectTransform rectTransform)
		{
			Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
			CanvasScaler canvasScaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
			float scaleFactor = canvasScaler != null && canvasScaler.scaleFactor > 0f ? canvasScaler.scaleFactor : 1f;
			float insetPixels = Mathf.Clamp(Screen.width * LANDSCAPE_INSET_WIDTH_RATIO, MIN_LANDSCAPE_INSET_PIXELS, MAX_LANDSCAPE_INSET_PIXELS);
			return insetPixels / scaleFactor;
		}

		public void SetAnchor(AnchorStyle style)
		{
			if (mRectTransform == null)
			{
				mRectTransform = GetComponent<RectTransform>();
			}
			switch (style)
			{
			case AnchorStyle.FILL:
				mRectTransform.anchorMin = new Vector2(0f, 0f);
				mRectTransform.anchorMax = new Vector2(1f, 1f);
				mRectTransform.pivot = new Vector2(0.5f, 0.5f);
				mRectTransform.offsetMin = new Vector2(0f, 0f);
				mRectTransform.offsetMax = new Vector2(0f, 0f);
				leftSpacer.SetActive(false);
				rightSpacer.SetActive(false);
				break;
			case AnchorStyle.TOP_LEFT:
				mRectTransform.anchorMin = new Vector2(0f, 1f);
				mRectTransform.anchorMax = new Vector2(1f, 1f);
				mRectTransform.pivot = new Vector2(0.5f, 1f);
				mRectTransform.anchoredPosition = new Vector2(0f, 0f);
				leftSpacer.SetActive(false);
				rightSpacer.SetActive(true);
				break;
			case AnchorStyle.TOP_CENTER:
				mRectTransform.anchorMin = new Vector2(0f, 1f);
				mRectTransform.anchorMax = new Vector2(1f, 1f);
				mRectTransform.pivot = new Vector2(0.5f, 1f);
				mRectTransform.anchoredPosition = new Vector2(0f, 0f);
				leftSpacer.SetActive(true);
				rightSpacer.SetActive(true);
				break;
			case AnchorStyle.TOP_RIGHT:
				mRectTransform.anchorMin = new Vector2(0f, 1f);
				mRectTransform.anchorMax = new Vector2(1f, 1f);
				mRectTransform.pivot = new Vector2(0.5f, 1f);
				mRectTransform.anchoredPosition = new Vector2(0f, 0f);
				leftSpacer.SetActive(true);
				rightSpacer.SetActive(false);
				break;
			case AnchorStyle.BOTTOM_LEFT:
				mRectTransform.anchorMin = new Vector2(0f, 0f);
				mRectTransform.anchorMax = new Vector2(1f, 0f);
				mRectTransform.pivot = new Vector2(0.5f, 0f);
				mRectTransform.anchoredPosition = new Vector2(0f, 0f);
				leftSpacer.SetActive(false);
				rightSpacer.SetActive(true);
				break;
			case AnchorStyle.BOTTOM_CENTER:
				mRectTransform.anchorMin = new Vector2(0f, 0f);
				mRectTransform.anchorMax = new Vector2(1f, 0f);
				mRectTransform.pivot = new Vector2(0.5f, 0f);
				mRectTransform.anchoredPosition = new Vector2(0f, 0f);
				leftSpacer.SetActive(true);
				rightSpacer.SetActive(true);
				break;
			case AnchorStyle.BOTTOM_RIGHT:
				mRectTransform.anchorMin = new Vector2(0f, 0f);
				mRectTransform.anchorMax = new Vector2(1f, 0f);
				mRectTransform.pivot = new Vector2(0.5f, 0f);
				mRectTransform.anchoredPosition = new Vector2(0f, 0f);
				leftSpacer.SetActive(true);
				rightSpacer.SetActive(false);
				break;
			}
		}
	}
}
