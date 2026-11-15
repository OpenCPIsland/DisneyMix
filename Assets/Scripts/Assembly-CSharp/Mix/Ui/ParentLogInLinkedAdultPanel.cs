using System;
using System.Collections.Generic;
using Disney.Mix.SDK;
using Mix.Native;
using Mix.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class ParentLogInLinkedAdultPanel : BasePanel, ParentalControlGuardianItem.IParentalControlGuardianItemListener, IKeyboardSubmitHandler
	{
		public bool HandlesKeyboardSubmit(Selectable input)
		{
			return input == passwordField;
		}

		public void SubmitFromKeyboard(Selectable input)
		{
			OnParentLoginClicked();
		}

		public interface IParentLogInLinkedAdultPanelListener
		{
			void LoginSecondAccount(string aUsername, string aPassword, string aCachePath, Action<ILoginResult> callback);

			void ParentLoggedIn(ILocalUser aParent, bool aFromNoLinkedLogin);
		}

		private IParentLogInLinkedAdultPanelListener currentListener;

		private IEnumerable<ILinkedUser> guardians;

		private List<ParentalControlGuardianItem> guardianItems;

		private float contentAnchorY;

		public NativeTextView passwordField;

		public GameObject GuardianItem;

		public ScrollView ScrollView;

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

		public void Init(IEnumerable<ILinkedUser> aGuardians, IParentLogInLinkedAdultPanelListener aListener)
		{
			guardians = aGuardians;
			currentListener = aListener;
			guardianItems = new List<ParentalControlGuardianItem>();
			bool aCheck = true;
			foreach (ILinkedUser guardian in guardians)
			{
				ParentalControlGuardianItem parentalControlGuardianItem = new ParentalControlGuardianItem(GuardianItem, guardian, aCheck, this);
				aCheck = false;
				parentalControlGuardianItem.Id = ScrollView.Add(parentalControlGuardianItem, false);
				guardianItems.Add(parentalControlGuardianItem);
			}
			ScrollViewGameObject.GetComponent<ScrollRect>().enabled = false;
			contentAnchorY = Content.anchoredPosition.y;
		}

		public void OnParentLoginClicked()
		{
			string aUsername = string.Empty;
			if (!MonoSingleton<NativeKeyboardManager>.Instance.IsNullOrDisposed())
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			}

			foreach (ParentalControlGuardianItem guardianItem in guardianItems)
			{
				if (guardianItem.isChecked)
				{
					aUsername = guardianItem.Email;
					break;
				}
			}
			try
			{
				Singleton<ThreadFramerateThrottler>.Instance.EnterThrottlingSection();
				currentListener.LoginSecondAccount(aUsername, passwordField.Value, "/cacheForGuardian/", delegate(ILoginResult loginResult)
				{
					Singleton<ThreadFramerateThrottler>.Instance.ExitThrottlingSection();
					if (loginResult.Success)
					{
						currentListener.ParentLoggedIn(loginResult.Session.LocalUser, false);
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
				else if (passwordField.Selected)
				{
					passwordField.ScrollToInput(Content, args.Height);
				}
			}
		}

		public void Toggled(long aId)
		{
			foreach (ParentalControlGuardianItem guardianItem in guardianItems)
			{
				if (aId != guardianItem.Id)
				{
					guardianItem.TurnOffCheck();
				}
			}
		}
	}
}
