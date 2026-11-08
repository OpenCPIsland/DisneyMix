using Mix.Native;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Games.Tray.MemeGenerator
{
	public class MemeCaptionText : MonoBehaviour
	{
		private Text mText;

		private bool mIsDirty;

		private Outline highlightedTextComponent;

		private bool mIsActive;

		private string mDefaultText;

		private string mCurrentText;

		public InputField mNativeTextView;

		public Color textEnteredColor;

		public Color defaultColor;

		public float activeWobbleFrequency = 5f;

		public float activeWobbleAmount = 1f;

		public string DefaultMessageToken;

		public bool IsDirty
		{
			get
			{
				return mIsDirty;
			}
		}

		public string Caption
		{
			get
			{
				return mCurrentText;
			}
			set
			{
				if (value == null)
				{
					mCurrentText = string.Empty;
				}
				else
				{
					mCurrentText = value;
				}
				string text = mCurrentText;
				if (string.IsNullOrEmpty(text))
				{
					text = mDefaultText;
				}
				if (mText.text != text)
				{
					mText.text = text;
					mIsDirty = true;
				}
			}
		}

		public bool MatchesSelected(Text aSelected)
		{
			return mText == aSelected;
		}

public void SetActive(bool aIsActive)
{
    // Lazy fallback: ensure mText is cached if Awake hasn't run yet
    if (mText == null)
    {
        mText = GetComponent<Text>();
    }

    if (aIsActive)
    {
        if (mText != null)
        {
            mText.color = textEnteredColor;
        }

        if (highlightedTextComponent != null)
        {
            highlightedTextComponent.enabled = true;
        }

        if (mNativeTextView != null)
        {
            mNativeTextView.gameObject.SetActive(true);

            if (IsEmpty())
            {
                mNativeTextView.text = string.Empty;
            }
            else
            {
                mNativeTextView.text = Caption;
            }

            mNativeTextView.onValueChanged.RemoveListener(OnKeyboardValueChangedHandler);
            mNativeTextView.onValueChanged.AddListener(OnKeyboardValueChangedHandler);

            mNativeTextView.Select();
            mNativeTextView.ActivateInputField();
        }
    }
    else
    {
        if (IsEmpty())
        {
            Caption = string.Empty;
            if (mText != null)
            {
                mText.color = defaultColor;
            }
        }

        if (highlightedTextComponent != null)
        {
            highlightedTextComponent.enabled = false;
        }

        if (mNativeTextView != null)
        {
            mNativeTextView.onValueChanged.RemoveListener(OnKeyboardValueChangedHandler);
            mNativeTextView.gameObject.SetActive(false);
        }
    }

    mIsActive = aIsActive;
}

// Helper handler method matching UnityAction<string>
private void OnKeyboardValueChangedHandler(string aValue)
{
    OnKeyboardValueChanged(mNativeTextView, aValue);
}
		public bool IsEmpty()
		{
			if (string.IsNullOrEmpty(mCurrentText.Trim()))
			{
				return true;
			}
			return false;
		}

		public void PostModeration()
		{
			mIsDirty = false;
		}

		public void InitializeHighlightOutline(Color aColor, Vector2 aOffset)
		{
			highlightedTextComponent = base.gameObject.AddComponent<Outline>();
			highlightedTextComponent.effectColor = aColor;
			highlightedTextComponent.effectDistance = aOffset;
			highlightedTextComponent.enabled = false;
		}

		public void Reset()
		{
			Caption = string.Empty;
			SetActive(false);
		}

		private void OnKeyboardValueChanged(InputField aInput, string aValue)
		{
			Caption = aValue.ToUpper();
		}

		private void Awake()
		{
			mText = GetComponent<Text>();
			mDefaultText = BaseGameController.Instance.Session.GetLocalizedString(DefaultMessageToken);
			Caption = string.Empty;
		}

		private void Update()
		{
			if (mIsActive)
			{
				base.transform.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(Time.time * activeWobbleFrequency) * activeWobbleAmount);
				if (!mNativeTextView.IsNullOrDisposed() && !string.Equals(Caption, mNativeTextView.text))
				{
					Caption = mNativeTextView.text;
				}
			}
			else
			{
				base.transform.localEulerAngles = Vector3.zero;
			}
		}

		private void OnDestroy()
		{
			if (highlightedTextComponent != null)
			{
				Object.Destroy(highlightedTextComponent);
				highlightedTextComponent = null;
			}
		}
	}
}
