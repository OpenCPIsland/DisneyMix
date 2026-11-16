using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mix.AssetBundles;
using Mix.Assets;
using Mix.Data;
using Mix.Entitlements;
using Mix.Localization;
using Mix.Native;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Mix.Ui
{
	public class BottomNav : MonoBehaviour, IBundleObject, IBaseThumb, IKeyboardSubmitHandler
	{
		public bool HandlesKeyboardSubmit(Selectable input)
		{
			return input == nativeText;
		}

		public void SubmitFromKeyboard(Selectable input)
		{
			if (nativeText == null)
			{
				return;
			}
			if (string.IsNullOrEmpty(nativeText.Value))
			{
				previousStickers.Clear();
				PopulateSearchIcons();
				usingSearchIcons = true;
			}
			else
			{
				DoSearch(nativeText.Value);
			}
		}

		private sealed class OnBundleAssetObject_003Ec__AnonStorey292
		{
			internal Sticker sticker;

			internal BottomNav _003C_003Ef__this;

			internal void _003C_003Em__571()
			{
				_003C_003Ef__this.OnSearchButtonClick(sticker);
			}
		}

		public static GameObject SearchThumbHolder;

		public KeyboardTray KeyboardTray;

		public GameObject UiBgChat;

		public GameObject MediaInput;

		public GameObject PackScroller;

		public Toggle SearchButton;

		public GameObject SearchField;

		public GameObject MiddleScroller;

		public GameObject MediaScroller;

		public GameObject SearchScroller;

		public HorizontalLayoutGroup SearchScrollLayout;

		public int LayoutPadding;

		public float LayoutSpacing;

		private Toggle searchToggle;

		private NativeTextView nativeText;

		private StickerSearch search;

		private List<Sticker> previousStickers = new List<Sticker>();

		private List<Sticker> stickers = new List<Sticker>();

		private ScrollRect scroller;

		private List<BaseThumb> thumbs = new List<BaseThumb>();

		private List<Sticker> searchThumbs = new List<Sticker>();

		public List<Sticker> loadedSearchStickers = new List<Sticker>();

		public List<GameObject> searchThumbHolders = new List<GameObject>();

		private bool usingSearchIcons;

		private IMediaTray listener;

		private float delay;

		private int touchIndex = -1;

		private Vector2 startPosition;

		private bool previewCheck;

		private bool dragging;

		private bool isPreviewMode;

		private static bool TryGetTouch(int touchId, out Touch touch)
		{
			foreach (Touch activeTouch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
			{
				if (activeTouch.touchId == touchId)
				{
					touch = activeTouch;
					return true;
				}
			}
			touch = default(Touch);
			return false;
		}

		private static bool TryGetTouchBegan(out Touch touch)
		{
			foreach (Touch activeTouch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
			{
				if (activeTouch.phase == UnityEngine.InputSystem.TouchPhase.Began)
				{
					touch = activeTouch;
					return true;
				}
			}
			touch = default(Touch);
			return false;
		}

		void IBundleObject.OnBundleAssetObject(Object aGameObject, object aUserData)
		{
			Hashtable hashtable = (Hashtable)aUserData;
			if (hashtable == null)
			{
				return;
			}
			OnBundleAssetObject_003Ec__AnonStorey292 CS_0024_003C_003E8__locals7 = new OnBundleAssetObject_003Ec__AnonStorey292();
			CS_0024_003C_003E8__locals7._003C_003Ef__this = this;
			CS_0024_003C_003E8__locals7.sticker = (Sticker)hashtable["sticker"];
			GameObject gameObject = (GameObject)hashtable["holder"];
			if (CS_0024_003C_003E8__locals7.sticker == null || !(gameObject != null))
			{
				return;
			}
			gameObject.transform.Find("ContextualLoader").gameObject.SetActive(false);
			GameObject gameObject2 = (GameObject)MonoSingleton<AssetManager>.Instance.GetBundleInstance(CS_0024_003C_003E8__locals7.sticker.GetHd());
			if (gameObject2 != null)
			{
				loadedSearchStickers.Add(CS_0024_003C_003E8__locals7.sticker);
				gameObject2.transform.SetParent(scroller.content.transform, false);
				gameObject2.transform.SetSiblingIndex(gameObject.transform.GetSiblingIndex());
				searchThumbHolders.Remove(gameObject);
				Object.Destroy(gameObject);
				searchThumbHolders.Add(gameObject2);
				gameObject2.GetComponent<Button>().onClick.AddListener(delegate
				{
					CS_0024_003C_003E8__locals7._003C_003Ef__this.OnSearchButtonClick(CS_0024_003C_003E8__locals7.sticker);
				});
			}
			else
			{
				gameObject.transform.Find("Error").gameObject.SetActive(true);
			}
		}

		void IBaseThumb.OnBaseThumbClicked(BaseContentData aEntitlement, object aUserData)
		{
			listener.OnEntitlementClicked(aEntitlement);
		}

		void IBaseThumb.OnBaseThumbLoaded(bool error)
		{
		}

		public virtual void Init(IMediaTray aListener)
		{
			listener = aListener;
		}

		private void Start()
		{
			SearchThumbHolder = (GameObject)Resources.Load("brand_stickerName_thumb", typeof(GameObject));
			search = new StickerSearch(Singleton<EntitlementsManager>.Instance.GetAllStickerTags());
			search.StartNewSearch();
			searchToggle = SearchButton.GetComponent<Toggle>();
			if (searchToggle != null)
			{
				searchToggle.onValueChanged.AddListener(delegate
				{
					OnSearchButtonClick(searchToggle.isOn);
				});
			}
			nativeText = SearchField.GetComponentInChildren<NativeTextView>();
			if (nativeText != null)
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardKeyPressed += OnKeyPressed;
				nativeText.KeyboardHeightChanged += OnKeyboardHeightChanged;
			}
			scroller = SearchScroller.GetComponentInChildren<ScrollRect>();
			List<Sticker> allStickers = Singleton<EntitlementsManager>.Instance.GetAllStickers();
			foreach (Sticker item in allStickers)
			{
				if (item.GetStickerType().Equals("search"))
				{
					searchThumbs.Add(item);
				}
			}
		}

		private void OnDestroy()
		{
			if (this.IsNullOrDisposed())
			{
				return;
			}
			RemoveAllContent(true);
			if (nativeText != null)
			{
				if (!MonoSingleton<NativeKeyboardManager>.Instance.IsNullOrDisposed())
				{
					MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardKeyPressed -= OnKeyPressed;
				}
				nativeText.KeyboardHeightChanged -= OnKeyboardHeightChanged;
			}
		}

		private void PopulateSearchIcons()
		{
			RemoveAllContent();
			SetLayoutParams(true);
			foreach (Sticker searchThumb in searchThumbs)
			{
				GameObject gameObject = Object.Instantiate(SearchThumbHolder);
				Image component = gameObject.GetComponent<Image>();
				component.sprite = null;
				component.color = Color.clear;
				gameObject.transform.SetParent(scroller.content.transform, false);
				Hashtable hashtable = new Hashtable();
				hashtable["holder"] = gameObject;
				hashtable["sticker"] = searchThumb;
				searchThumbHolders.Add(gameObject);
				MonoSingleton<AssetManager>.Instance.LoadABundle(this, searchThumb.GetHd(), hashtable, string.Empty);
			}
		}

		public void OnSearchButtonClick(Sticker sticker)
		{
			usingSearchIcons = true;
			string text = sticker.GetName();
			string[] array = text.Split('_');
			string text2 = array[array.Length - 1];
			text2 = text2.Replace('.', ' ');
			string text3 = "stickeremotion_#emotion#.stickeremotion_#emotion#.emotiontext";
			text3 = text3.Replace("#emotion#", text2);
			string text4 = Singleton<Localizer>.Instance.getString(text3);
			nativeText.TextComponent.text = text4;
			previousStickers.Clear();
			nativeText.Value = text4;
			DoSearch(text4);
		}

		private void OnKeyPressed(object sender, NativeKeyboardKeyPressedEventArgs e)
		{
			previousStickers = stickers;
			string inputText = e.InputText;
			if (string.IsNullOrEmpty(inputText))
			{
				previousStickers.Clear();
				PopulateSearchIcons();
				usingSearchIcons = true;
			}
			else
			{
				DoSearch(inputText);
			}
		}

		public void DoSearch(string aSearchTerm)
		{
			if (usingSearchIcons)
			{
				usingSearchIcons = false;
				RemoveAllContent();
				SetLayoutParams(false);
			}
			search.StartNewSearch();
			stickers = search.AddStringToCurrentEntry(aSearchTerm.ToLower());
			bool flag = false;
			if (previousStickers != null && stickers.Count == previousStickers.Count)
			{
				flag = true;
				for (int i = 0; i < stickers.Count; i++)
				{
					if (stickers[i] != previousStickers[i])
					{
						flag = false;
						break;
					}
				}
			}
			if (!flag)
			{
				RemoveAllContent();
				LoadStickers();
			}
		}

		public void LoadStickers()
		{
			foreach (Sticker sticker in stickers)
			{
				if (sticker != null)
				{
					string cpipePrefix = MonoSingleton<AssetManager>.Instance.GetCpipePrefix(sticker.GetHd());
					if (!cpipePrefix.Equals(MonoSingleton<AssetManager>.Instance.GetCpipeUrl(cpipePrefix)))
					{
						thumbs.Add(new BaseThumb(scroller.content.transform, 0, this, sticker));
					}
				}
			}
		}

		private void Update()
		{
			Vector2 vector = new Vector2(0f, 0f);
			bool flag = false;
			if (dragging)
			{
				if (touchIndex >= 0 && TryGetTouch(touchIndex, out Touch touch) && touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
				{
					vector = touch.screenPosition;
				}
				else if (touchIndex < 0 && UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
				{
					vector = UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero;
				}
				else
				{
					touchIndex = -1;
					flag = true;
				}
			}
			if (flag)
			{
				isPreviewMode = false;
				scroller.enabled = true;
				listener.OnHidePreviewPanel();
				dragging = false;
				return;
			}
			if (!dragging)
			{
				bool touchBegan = TryGetTouchBegan(out Touch touch);
				Vector2 pointerPosition = touchBegan ? touch.screenPosition : UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero;
				if ((touchBegan || UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) && Util.GetRectInScreenSpace(scroller.content.GetComponent<RectTransform>()).Contains(pointerPosition))
				{
					vector = pointerPosition;
					touchIndex = touchBegan ? touch.touchId : -1;
					delay = 0f;
					startPosition = GetCanvasPointFromScreenPoint(vector);
					previewCheck = false;
					dragging = true;
				}
				return;
			}
			Vector2 canvasPointFromScreenPoint = GetCanvasPointFromScreenPoint(vector);
			float num = Vector2.Distance(startPosition, canvasPointFromScreenPoint);
			delay += Time.deltaTime;
			if (delay > 0.35f && !previewCheck)
			{
				previewCheck = true;
				if (num < 15f)
				{
					scroller.StopMovement();
					scroller.enabled = false;
					isPreviewMode = true;
				}
			}
			if (isPreviewMode)
			{
				CheckForStickerAtPoint(vector);
			}
		}

		private void CheckForStickerAtPoint(Vector2 aMousePosition)
		{
			foreach (BaseThumb thumb2 in thumbs)
			{
				if (thumb2 == null)
				{
					continue;
				}
				GameObject thumb = thumb2.thumb;
				if (thumb == null)
				{
					continue;
				}
				RectTransform component = thumb.GetComponent<RectTransform>();
				if (!Util.GetRectInScreenSpace(component).Contains(aMousePosition))
				{
					continue;
				}
				if (thumb2.entitlement != null && !(thumb2.entitlement is Game))
				{
					listener.OnShowPreviewPanel(thumb2.entitlement);
				}
				break;
			}
		}

		private Vector2 GetCanvasPointFromScreenPoint(Vector2 aScreenPoint)
		{
			Canvas parentCanvas = Util.GetParentCanvas(base.gameObject);
			RectTransform component = parentCanvas.GetComponent<RectTransform>();
			Vector2 localPoint;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(component, aScreenPoint, parentCanvas.worldCamera, out localPoint);
			return localPoint;
		}

		public void RemoveAllContent(bool isDestroy = false)
		{
			if (this.IsNullOrDisposed() || searchThumbHolders == null || loadedSearchStickers == null || thumbs == null)
			{
				return;
			}
			foreach (GameObject searchThumbHolder in searchThumbHolders)
			{
				if (searchThumbHolder != null)
				{
					Object.Destroy(searchThumbHolder);
				}
			}
			if (isDestroy)
			{
				foreach (Sticker loadedSearchSticker in loadedSearchStickers)
				{
					if (loadedSearchSticker != null && !MonoSingleton<AssetManager>.Instance.IsNullOrDisposed())
					{
						MonoSingleton<AssetManager>.Instance.DestroyBundleInstance(loadedSearchSticker.GetHd());
					}
				}
			}
			foreach (BaseThumb thumb in thumbs)
			{
				if (thumb != null)
				{
					thumb.Clean();
				}
			}
			thumbs.Clear();
		}

		private void SetLayoutParams(bool isSearch)
		{
			if (isSearch)
			{
				SearchScrollLayout.padding.left = 0;
				SearchScrollLayout.padding.right = 0;
				SearchScrollLayout.spacing = 0f;
			}
			else
			{
				SearchScrollLayout.padding.left = LayoutPadding;
				SearchScrollLayout.padding.right = LayoutPadding;
				SearchScrollLayout.spacing = LayoutSpacing;
			}
		}

		private void OnSearchButtonClick(bool isOn)
		{
			if (isOn)
			{
				if (nativeText.Value.Length == 0)
				{
					PopulateSearchIcons();
				}
				else
				{
					DoSearch(nativeText.Value);
				}
				PackScroller.SetActive(false);
				SearchField.SetActive(true);
				SearchScrollerVisible(true);
			}
			else
			{
				HideSearch();
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
				SearchScrollerVisible(false);
			}
		}

		public void HideSearch()
		{
			if (searchToggle != null)
			{
				searchToggle.isOn = false;
			}
			PackScroller.SetActive(true);
			SearchField.SetActive(false);
			KeyboardTray.UpdateState(false);
		}

		public void OnSearchInputClicked()
		{
			ToggleInput(true);
		}

		public void ToggleInput(bool aState)
		{
			if (aState)
			{
				NativeTextView componentInChildren = SearchField.GetComponentInChildren<NativeTextView>();
				if (!componentInChildren.SelectInput())
				{
					componentInChildren.Invoke("SelectInput", 0.5f);
				}
			}
			else
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			}
		}

		public void SearchScrollerVisible(bool isVisible)
		{
			SearchScroller.SetActive(isVisible);
			SearchField.SetActive(isVisible);
			MediaScroller.SetActive(!isVisible);
			PackScroller.SetActive(!isVisible);
		}

		private void OnKeyboardHeightChanged(NativeTextView aField, int aHeight)
		{
			if (aField == nativeText)
			{
				if (aHeight > 0)
				{
					KeyboardTray.UpdateState(true);
				}
				else
				{
					KeyboardTray.UpdateState(false);
				}
				KeyboardTray.SetSize(aHeight);
				aField.Reposition();
				if (aField.TextComponent.text.Length == 0)
				{
					PopulateSearchIcons();
				}
			}
		}
	}
}
