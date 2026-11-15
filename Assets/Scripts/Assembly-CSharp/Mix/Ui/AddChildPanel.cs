using System;
using Disney.Mix.SDK;
using Mix.Native;
using Mix.Threading;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class AddChildPanel : BasePanel, IKeyboardSubmitHandler
	{
		public bool HandlesKeyboardSubmit(Selectable input)
		{
			return input == usernameField || input == passwordField;
		}

		public void SubmitFromKeyboard(Selectable input)
		{
			OnAddChildClicked();
		}

		public interface IParentControlAddChildListener
		{
			void LoginSecondAccount(string aUsername, string aPassword, string aCachePath, Action<ILoginResult> callback);

			void OnChildAdded(ILocalUser aParent);
		}

		private IParentControlAddChildListener currentListener;

		private ILocalUser parent;

		public NativeTextView usernameField;

		public NativeTextView passwordField;

		public Text Email;

		private void Start()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed += OnKeyboardReturnKey;
		}

		private void OnDestroy()
		{
			if (!MonoSingleton<NativeKeyboardManager>.Instance.IsNullOrDisposed())
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed -= OnKeyboardReturnKey;
			}
		}

		public void Init(ILocalUser aParent, IParentControlAddChildListener aListener)
		{
			parent = aParent;
			Email.text = parent.RegistrationProfile.Email;
			currentListener = aListener;
		}

		public void OnAddChildClicked()
		{
			if (!MonoSingleton<NativeKeyboardManager>.Instance.IsNullOrDisposed())
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			}

			try
			{
				Singleton<ThreadFramerateThrottler>.Instance.EnterThrottlingSection();
				currentListener.LoginSecondAccount(usernameField.Value, passwordField.Value, "/cacheForAddedChild/", delegate(ILoginResult loginResult)
				{
					Singleton<ThreadFramerateThrottler>.Instance.ExitThrottlingSection();
					if (loginResult.Success)
					{
						parent.LinkChildAccount(loginResult.Session, delegate(ILinkChildResult linkChildResult)
						{
							if (linkChildResult.Success)
							{
								currentListener.OnChildAdded(parent);
							}
						});
					}
				});
			}
			catch (Exception exception)
			{
				Singleton<ThreadFramerateThrottler>.Instance.ExitThrottlingSection();
				Log.Exception("Platform sdk threw an exception from login call!", exception);
			}
		}

		private void OnKeyboardReturnKey(object sender, NativeKeyboardReturnKeyPressedEventArgs args)
		{
			if (args.ReturnKeyType == NativeKeyboardReturnKey.Next)
			{
				passwordField.SelectInput();
			}
			else
			{
				OnAddChildClicked();
			}
		}
	}
}
