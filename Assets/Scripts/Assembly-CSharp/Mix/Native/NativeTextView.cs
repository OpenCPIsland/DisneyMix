using Disney.MobileNetwork;
using Mix.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if TMP_PRESENT
using TMPro;
#endif

namespace Mix.Native
{
    public class NativeTextView : Selectable, IEventSystemHandler, IPointerClickHandler, INativeKeyboardEvents
    {
        public delegate void OnKeyboardHeightChanged(NativeTextView aInput, int aHeight);

        public delegate void OnKeyboardFocusChanged(NativeTextView aInput, bool aFocus);

        public delegate void OnKeyboardValueChanged(NativeTextView aInput, string aText);

#if TMP_PRESENT
        public TMP_Text TextComponent;
#else
        public Text TextComponent;
#endif

        public NativeKeyboardEntryType keyboardEntryType;

        public NativeKeyboardAlignment alignment;

        public NativeKeyboardReturnKey returnKeyType;

        public int maxCharacters;

        public bool suggestions;

        public bool multipleLines;

        [HideInInspector]
        public bool Selected;

        [HideInInspector]
        public Color32 TextBGColor = Color.white;

        public bool isPassword;

        private string currentValue = string.Empty;

        private Image textImage;

        private RectTransform textRectTransform;

        private Color32 textColor;

        private int textSize;

        private bool inited;

        public string DefaultText { get; set; }

        public float StartHeight { get; private set; }

        public string Value
        {
            get
            {
                return currentValue;
            }
            set
            {
                bool flag = value == null || !value.Equals(currentValue);
                currentValue = value;
                Init();
                if (flag)
                {
                    UpdateImage();
                }
                if (Selected)
                {
                    MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.SetText(currentValue);
                }
            }
        }

        public bool IsPassword
        {
            get
            {
                return isPassword;
            }
            set
            {
                isPassword = value;
                UpdateImage();
            }
        }

        public event OnKeyboardHeightChanged KeyboardHeightChanged;

        public event OnKeyboardFocusChanged KeyboardFocusChanged;

        public event OnKeyboardValueChanged KeyboardValueChanged;

        void INativeKeyboardEvents.OnNativeKeyboardHiding()
        {
            if (!Equals(null) && !(base.gameObject == null))
            {
                UpdateImage();
                ToggleUnityView(true);
            }
        }

        void INativeKeyboardEvents.OnNativeKeyboardHidden()
        {
            if (!Equals(null) && !(base.gameObject == null) && Selected)
            {
                Selected = false;
                SendFocusEvent();
            }
        }

        void INativeKeyboardEvents.OnNativeKeyboardShowing()
        {
        }

        void INativeKeyboardEvents.OnNativeKeyboardShown(int aHeight)
        {
            if (!Equals(null) && !(textRectTransform == null) && !(base.gameObject == null))
            {
                if (this.KeyboardHeightChanged != null)
                {
                    this.KeyboardHeightChanged(this, (int)((float)aHeight * Singleton<SettingsManager>.Instance.GetHeightScale()));
                }
                Canvas.ForceUpdateCanvases();
                Rect rectInPhysicalScreenSpace = Util.GetRectInPhysicalScreenSpace(textRectTransform);
                string aFontColor = string.Format("#{0}{1}{2}", textColor.r.ToString("X2"), textColor.g.ToString("X2"), textColor.b.ToString("X2"));
                Selected = true;
                MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.ShowInput(Value, DefaultText, (int)rectInPhysicalScreenSpace.x, (int)rectInPhysicalScreenSpace.y, (int)rectInPhysicalScreenSpace.width, (int)rectInPhysicalScreenSpace.height, (int)MixConstants.CANVAS_HEIGHT, textSize, aFontColor, string.Empty);
                ToggleUnityView(false);
            }
        }

        void INativeKeyboardEvents.OnNativeKeyboardKeyPressed(string aString)
        {
            if (!Equals(null) && aString != currentValue && this.KeyboardValueChanged != null)
            {
                this.KeyboardValueChanged(this, aString);
            }
            currentValue = aString;
        }

        void INativeKeyboardEvents.OnNativeKeyboardHeightChanged(int aHeight)
        {
            if (this.KeyboardHeightChanged != null)
            {
                this.KeyboardHeightChanged(this, (int)((float)aHeight * Singleton<SettingsManager>.Instance.GetHeightScale()));
            }
        }

        void INativeKeyboardEvents.OnNativeKeyboardReturnKeyPressed(NativeKeyboardReturnKey aReturnKey)
        {
        }

        void INativeKeyboardEvents.OnNativeKeyboardTypeChanged(NativeKeyboardType aNativeKeyboardType)
        {
        }

        protected override void Awake()
        {
            Init();
        }

        protected override void Start()
        {
            if (UnityEngine.Application.isPlaying)
            {
                UpdateImage();
            }
        }

        protected override void OnDestroy()
        {
        }

        private void Init()
        {
            if (!UnityEngine.Application.isPlaying || inited)
            {
                return;
            }
            if (DefaultText == null && TextComponent.gameObject.GetComponent<LocalizedText>() != null)
            {
                if (!string.IsNullOrEmpty(TextComponent.gameObject.GetComponent<LocalizedText>().token))
                {
                    DefaultText = Singleton<Localizer>.Instance.getString(TextComponent.gameObject.GetComponent<LocalizedText>().token);
                }
                else
                {
                    DefaultText = string.Empty;
                }
            }
            else if (DefaultText == null)
            {
                DefaultText = string.Empty;
            }
#if TMP_PRESENT
            TextComponent.text = string.Empty;
            textColor = TextComponent.color;
            textSize = (int)TextComponent.fontSize;
            textRectTransform = TextComponent.GetComponent<RectTransform>();
#else
            TextComponent.text = string.Empty;
            textColor = TextComponent.color;
            textSize = TextComponent.fontSize;
            textRectTransform = TextComponent.GetComponent<RectTransform>();
#endif
            StartHeight = textRectTransform.sizeDelta.y;
            inited = true;
        }

        public void Reposition()
        {
            if (Selected)
            {
                Canvas.ForceUpdateCanvases();
                Rect rectInPhysicalScreenSpace = Util.GetRectInPhysicalScreenSpace(textRectTransform);
                MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Reposition((int)rectInPhysicalScreenSpace.x, (int)rectInPhysicalScreenSpace.y, (int)rectInPhysicalScreenSpace.width, (int)rectInPhysicalScreenSpace.height);
            }
        }

        public bool SelectInput()
        {
            if (Selected || !base.interactable)
            {
                return false;
            }
            OnPointerClick(null);
            return true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!Selected && base.interactable)
            {
                Init();
                Selected = true;
                SendFocusEvent();
#if UNITY_EDITOR
                if (UnityEngine.Application.isEditor && textRectTransform != null)
                {
#if TMP_PRESENT
                    var inputField = textRectTransform.GetComponent<TMP_InputField>();
#else
                    var inputField = textRectTransform.GetComponent<InputField>();
#endif
                    if (inputField != null)
                        inputField.Select();
                }
#endif
                MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.ShowKeyboard(this, alignment, keyboardEntryType, returnKeyType, maxCharacters, suggestions, multipleLines, isPassword);
            }
        }

        public void ScrollToInput(RectTransform aRect, int aHeight)
        {
            aHeight = (int)((float)aHeight * Singleton<SettingsManager>.Instance.GetHeightScale());
            RectTransform component = aRect.transform.parent.GetComponent<RectTransform>();
            RectTransform component2 = base.transform.parent.GetComponent<RectTransform>();
            float num = component2.rect.height * component2.pivot.y;
            float num2 = num + (0f - component2.anchoredPosition.y);
            float num3 = aRect.rect.height - (component.anchoredPosition.y + (float)aHeight);
            float num4 = MixConstants.CANVAS_HEIGHT - (component.anchoredPosition.y + (float)aHeight);
            if (num2 < num3)
            {
                if (num2 < num4 / 2f)
                {
                    aRect.anchoredPosition = new Vector2(aRect.anchoredPosition.x, 0f);
                }
                else
                {
                    aRect.anchoredPosition = new Vector2(aRect.anchoredPosition.x, Mathf.Abs(num4 / 2f - num2));
                }
                Reposition();
            }
        }

        private void UpdateImage()
        {
            string text = ((!string.IsNullOrEmpty(currentValue)) ? currentValue : DefaultText);
            if (isPassword && text.Length > 0 && !text.Equals(DefaultText))
            {
                text = new string('*', text.Length);
            }
            Canvas.ForceUpdateCanvases();
            if (!multipleLines)
            {
                TextComponent.text = text;
                bool flag = false;
#if TMP_PRESENT
                while (textRectTransform.rect.width > 0f && TextComponent.preferredWidth > textRectTransform.rect.width)
#else
                while (textRectTransform.rect.width > 0f && TextComponent.preferredWidth > textRectTransform.rect.width)
#endif
                {
                    TextComponent.text = TextComponent.text.Substring(0, TextComponent.text.Length - 1);
                    flag = true;
                }
                if (flag)
                {
                    TextComponent.text = TextComponent.text.Substring(0, TextComponent.text.Length - 3) + "...";
                }
            }
            else
            {
#if !UNITY_EDITOR
                float width = textRectTransform.rect.width;
                if (textImage == null)
                {
                    TextComponent.text = text;
                    textImage = Service.Get<SystemTextManager>().GenerateSystemText(TextComponent, TextBGColor, width, multipleLines);
                }
                else
                {
                    Service.Get<SystemTextManager>().UpdateSystemText(textImage, text, textSize, width, textColor, TextBGColor, multipleLines);
                }
                MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.TextHeightChanged(string.Empty + (int)((float)textImage.mainTexture.height / Singleton<SettingsManager>.Instance.GetHeightScale()));
#endif
            }
        }

        private void ToggleUnityView(bool aState)
        {
            if (textImage != null)
            {
                textImage.enabled = aState;
            }
            if (TextComponent != null)
            {
                TextComponent.enabled = aState;
            }
        }

        private void SendFocusEvent()
        {
            if (this.KeyboardFocusChanged != null)
            {
                this.KeyboardFocusChanged(this, Selected);
            }
        }
    }
}
