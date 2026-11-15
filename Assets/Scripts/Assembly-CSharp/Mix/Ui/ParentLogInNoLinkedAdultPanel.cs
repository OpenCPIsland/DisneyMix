using System;
using Disney.Mix.SDK;
using Mix.Native;
using Mix.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class ParentLogInNoLinkedAdultPanel : BasePanel, IKeyboardSubmitHandler
	{
		public bool HandlesKeyboardSubmit(Selectable input)
		{
			return input == usernameField || input == passwordField;
		}

		public void SubmitFromKeyboard(Selectable input)
		{
			OnParentLoginClicked();
		}

		public interface IParentLogInNoLinkedAdultPanelListener
		{
			void LoginSecondAccount(string aUsername, string aPassword, string aCachePath, Action<ILoginResult> callback);

			void ParentLoggedIn(ILocalUser aParent, bool aFromNoLinkedLogin);

			void CreateAccount();
		}

		private IParentLogInNoLinkedAdultPanelListener currentListener;

		private float contentAnchorY;

		public NativeTextView usernameField;

		public NativeTextView passwordField;

		public RectTransform Content;

		public GameObject ScrollViewGameObject;

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

		public void Init(IParentLogInNoLinkedAdultPanelListener aListener)
		{
			currentListener = aListener;
			contentAnchorY = Content.anchoredPosition.y;
		}

		public void OnParentLoginClicked()
		{
			if (!MonoSingleton<NativeKeyboardManager>.Instance.IsNullOrDisposed())
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			}

			try
			{
				Singleton<ThreadFramerateThrottler>.Instance.EnterThrottlingSection();
				currentListener.LoginSecondAccount(usernameField.Value, passwordField.Value, "/cacheForGuardian/", delegate(ILoginResult loginResult)
				{
					Singleton<ThreadFramerateThrottler>.Instance.ExitThrottlingSection();
					if (loginResult.Success)
					{
						currentListener.ParentLoggedIn(loginResult.Session.LocalUser, true);
					}
				});
			}
			catch (Exception exception)
			{
				Singleton<ThreadFramerateThrottler>.Instance.ExitThrottlingSection();
				Log.Exception("Platform sdk threw an exception from login call!", exception);
			}
		}

		public void OnCreateAccountClicked()
		{
			currentListener.CreateAccount();
		}

		private void OnKeyboardReturnKey(object sender, NativeKeyboardReturnKeyPressedEventArgs args)
		{
			if (args.ReturnKeyType == NativeKeyboardReturnKey.Next)
			{
				passwordField.SelectInput();
			}
			else
			{
				OnParentLoginClicked();
			}
		}

		private void OnKeyboardResize(object sender, NativeKeyboardHeightChangedEventArgs args)
		{
			if (!(ScrollViewGameObject == null) && !(ScrollViewGameObject.GetComponent<ScrollRect>() == null))
			{
				if (args.Height <= 0)
				{
					Content.anchoredPosition = new Vector2(Content.anchoredPosition.x, contentAnchorY);
				}
				else if (usernameField.Selected)
				{
					usernameField.ScrollToInput(Content, args.Height);
				}
				else if (passwordField.Selected)
				{
					passwordField.ScrollToInput(Content, args.Height);
				}
			}
		}
	}
}
