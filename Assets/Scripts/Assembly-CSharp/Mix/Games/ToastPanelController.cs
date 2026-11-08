using DG.Tweening;
using Mix.Games.Tray;
using Mix.Native;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Games
{
	public class ToastPanelController : MonoBehaviour
	{
		public delegate void OnAnimationComplete();

		public delegate void OnToastPanelOnKeyboardReturnPressed(NativeKeyboardReturnKey aKey);

		private const float ANIMATE_TIME = 0.375f;

		private const float PANEL_RETRY_TIME = 0.5f;

		// If the native keyboard never reports a height (Editor / no plugin), open the panel anyway after this delay.
		private const float NO_NATIVE_KEYBOARD_FALLBACK_TIME = 0.3f;

		private NativeKeyboard keyboard;

		private float currentKeyboardHeight;

		public GameObject contentObject;

		public GameObject basePanelObject;

		private RectTransform contentRect;

		private RectTransform panelRect;

		private InputField nativeTextView;

		private int statusBarHeight;

		private int screenHeight;

		private float heightScale;

		private bool toastShown;

		public event OnAnimationComplete ToastPanelAnimationComplete;

		public event OnAnimationComplete ToastPanelHideComplete;

		public event OnToastPanelOnKeyboardReturnPressed ToastPanelOnKeyboardReturnPressed;

		private void Awake()
		{
			keyboard = MonoSingleton<NativeKeyboardManager>.Instance.Keyboard;
			contentRect = contentObject.GetComponent<RectTransform>();
			panelRect = basePanelObject.GetComponent<RectTransform>();
			nativeTextView = base.gameObject.GetComponentInChildren<InputField>();
		}

		private void OnEnable()
		{
			toastShown = false;
			if (keyboard != null)
			{
				// -= first so re-enabling never double-subscribes
				keyboard.OnNativeKeyboardHeightChanged -= ShowToastPanel;
				keyboard.OnNativeKeyboardReturnKeyPressed -= ToastPanelOnKeyboardReturn;
				keyboard.OnNativeKeyboardHeightChanged += ShowToastPanel;
				keyboard.OnNativeKeyboardReturnKeyPressed += ToastPanelOnKeyboardReturn;
			}
			else
			{
				Debug.LogWarning("[ToastPanelController] Native keyboard is null; using fallback to show the panel.");
			}
			panelRect.sizeDelta = Vector2.zero;
			ShowKeyboard();
			Invoke(nameof(ShowWithoutNativeKeyboard), NO_NATIVE_KEYBOARD_FALLBACK_TIME);
		}

		private void ShowWithoutNativeKeyboard()
		{
			if (!toastShown)
			{
				ShowToastPanelWithHeight(0);
			}
		}

		public void ShowKeyboardRetry()
		{
			if (isSelectable(nativeTextView))
			{
				ShowKeyboard();
			}
			else
			{
				HideComplete();
			}
		}

		public void Init(int aStatusBarHeight, int aScreenHeight, float aHeightScale)
		{
			statusBarHeight = aStatusBarHeight;
			screenHeight = aScreenHeight;
			heightScale = aHeightScale;
		}

		public void ShowKeyboard()
		{
			if (!isSelectable(nativeTextView))
			{
				Invoke("ShowKeyboardRetry", 0.5f);
				return;
			}
			nativeTextView.Select();
			//nativeTextView.Selected = false;
		}

		private void OnDisable()
		{
			CancelInvoke();
			DOTween.Kill(base.gameObject);
		}

		private bool isSelectable(InputField aNativeTextView)
		{
			return !aNativeTextView.isFocused && aNativeTextView.interactable;
		}

		// Called by the native keyboard plugin when its height is known.
		private void ShowToastPanel(object sender, NativeKeyboardHeightChangedEventArgs args)
		{
			ShowToastPanelWithHeight(args.Height);
		}

		private void ShowToastPanelWithHeight(int num)
		{
			toastShown = true;
			CancelInvoke(nameof(ShowWithoutNativeKeyboard));
			MonoSingleton<GameManager>.Instance.IsToastPanelActive = true;
			if (DebugSceneIndicator.IsMainScene)
			{
				num = (int)((float)num * heightScale);
			}
			if (!this.IsNullOrDisposed() && !contentRect.IsNullOrDisposed())
			{
				currentKeyboardHeight = num;
				float num2 = (float)screenHeight * heightScale;
				float num3 = (float)statusBarHeight * heightScale;
				float height = contentRect.rect.height;
				float num4 = (float)num + height;
				float num5 = num2 - num3;
				if (num4 > num5)
				{
					height = num2 - (float)num - num3;
					contentRect.sizeDelta = new Vector2(contentRect.rect.width, height);
				}
				Vector2 endValue = new Vector2(0f, currentKeyboardHeight + contentRect.rect.height);
				panelRect.DOSizeDelta(endValue, 0.375f, true).SetEase(Ease.OutBack).OnComplete(ShowComplete)
					.OnUpdate(RepositionTextView)
					.SetId(base.gameObject);
			}
		}

		public void HideToastPanel()
		{
			MonoSingleton<GameManager>.Instance.IsToastPanelActive = false;
			panelRect.DOSizeDelta(Vector2.zero, 0.375f, true).SetEase(Ease.InQuad).OnComplete(HideComplete)
				.OnUpdate(RepositionTextView)
				.SetId(base.gameObject);
			if (keyboard != null)
			{
				keyboard.Hide();
			}
		}

		private void RepositionTextView()
		{
			nativeTextView.ForceLabelUpdate();
		}

		private void ShowComplete()
		{
			if (keyboard != null)
			{
				keyboard.OnNativeKeyboardHeightChanged -= ShowToastPanel;
			}
			if (this.ToastPanelAnimationComplete != null)
			{
				this.ToastPanelAnimationComplete();
			}
		}

		private void HideComplete()
		{
			if (keyboard != null)
			{
				keyboard.OnNativeKeyboardHeightChanged -= ShowToastPanel;
				keyboard.OnNativeKeyboardReturnKeyPressed -= ToastPanelOnKeyboardReturn;
			}
			if (this.ToastPanelHideComplete != null)
			{
				this.ToastPanelHideComplete();
			}
		}

		private void ToastPanelOnKeyboardReturn(object sender, NativeKeyboardReturnKeyPressedEventArgs args)
		{
			if (this.ToastPanelOnKeyboardReturnPressed != null)
			{
				this.ToastPanelOnKeyboardReturnPressed(args.ReturnKeyType);
			}
		}
	}
}