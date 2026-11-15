using Mix.Native;
using Mix.Session;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class ParentLoginPanel : BasePanel, IKeyboardSubmitHandler
	{
		public bool HandlesKeyboardSubmit(Selectable input)
		{
			return input == passwordField;
		}

		public void SubmitFromKeyboard(Selectable input)
		{
			OnLoginClicked();
		}

		public interface IParentLoginPanelListener
		{
			void ParentRelogin(string aPassword);

			void ForgotPassword();
		}

		public Text Email;

		public NativeTextView passwordField;

		public RectTransform Content;

		public GameObject ScrollViewGameObject;

		private IParentLoginPanelListener currentListener;

		private float contentAnchorY;

		private void Start()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed += OnKeyboardReturnKey;
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardHeightChanged += OnKeyboardResize;
		}

		private void OnDestroy()
		{
			if (!MonoSingleton<NativeKeyboardManager>.Instance.IsNullOrDisposed())
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed -= OnKeyboardReturnKey;
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardHeightChanged -= OnKeyboardResize;
			}
		}

		public void Init(IParentLoginPanelListener aListener)
		{
			currentListener = aListener;
			contentAnchorY = Content.anchoredPosition.y;
			Email.text = MixSession.User.RegistrationProfile.Email;
		}

		public void OnLoginClicked()
		{
			string value = passwordField.Value;
			if (!MonoSingleton<NativeKeyboardManager>.Instance.IsNullOrDisposed())
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			}

			currentListener.ParentRelogin(value);
		}

		public void OnForgotPassword()
		{
			currentListener.ForgotPassword();
		}

		private void OnKeyboardReturnKey(object sender, NativeKeyboardReturnKeyPressedEventArgs args)
		{
			OnLoginClicked();
		}

		private void OnKeyboardResize(object sender, NativeKeyboardHeightChangedEventArgs args)
		{
			if (!(ScrollViewGameObject == null) && !(ScrollViewGameObject.GetComponent<ScrollRect>() == null))
			{
				if (args.Height <= 0)
				{
					Content.anchoredPosition = new Vector2(Content.anchoredPosition.x, contentAnchorY);
				}
				else if (passwordField.Selected)
				{
					passwordField.ScrollToInput(Content, args.Height);
				}
			}
		}
	}
}
