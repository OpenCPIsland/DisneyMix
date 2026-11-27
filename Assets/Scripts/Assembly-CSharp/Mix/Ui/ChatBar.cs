using System.Collections;
using Disney.Mix.SDK;
using Mix.Localization;
using Mix.Native;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class ChatBar : MonoBehaviour, IKeyboardSubmitHandler
	{
		public bool HandlesKeyboardSubmit(Selectable input)
		{
			return input == InputField;
		}

		public void SubmitFromKeyboard(Selectable input)
		{
			SendChat();
		}

		public GameObject SendButton;

		public InputField InputField;

		public KeyboardTray KeyboardTray;

		public GameObject GroupBevel;

		private IChatBar listener;

		private IChatThread clientThread;

		private bool listenersActive;

		private bool disabled;

		private float padding;

		private float inputStartHeight;

		private void Start()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardHeightChanged += OnKeyboardHeightChanged;
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardInputHeightChanged += OnKeyboardInputResize;
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed += OnKeyboardReturnKey;
			inputStartHeight = InputField.GetComponent<RectTransform>().rect.height;
			padding = base.gameObject.GetComponent<LayoutElement>().minHeight - inputStartHeight;
			listenersActive = true;
			// On phones the Unity keyboard's Send/Done key ends up here. The
			// native keyboard plugin that used to send on the return key is
			// stubbed out, and KeyboardTextInputNavigation only sees hardware
			// keyboards, so without this the key did nothing on Android.
			if (UnityEngine.Application.isMobilePlatform)
			{
				InputField.onSubmit.AddListener(delegate
				{
					SendChat();
				});
			}
		}

		private void Update()
		{
			if (disabled)
			{
				SendButton.SetActive(false);
			}
			else
			{
				SendButton.SetActive(!string.IsNullOrEmpty(InputField.text));
			}
		}

		private void OnDestroy()
		{
			if (MonoSingleton<NativeKeyboardManager>.Instance != null)
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardHeightChanged -= OnKeyboardHeightChanged;
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardInputHeightChanged -= OnKeyboardInputResize;
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed -= OnKeyboardReturnKey;
			}
			listenersActive = false;
		}

		public void Init(IChatBar aListener, IChatThread aClientThread)
		{
			listener = aListener;
			clientThread = aClientThread;
			if (clientThread is IGroupChatThread)
			{
				GroupBevel.SetActive(true);
			}
		}

		public void ToggleState(bool aState)
		{
			if (disabled == !aState)
			{
				return;
			}
			if (!aState)
			{
				if (clientThread is IOfficialAccountChatThread)
				{
					InputField.text = Singleton<Localizer>.Instance.getString("customtokens.friends.oa_chat_notfollowing");
				}
				else
				{
					InputField.text = Singleton<Localizer>.Instance.getString("customtokens.chat.no_longer_friends");
				}
			}
			else if (disabled)
			{
				InputField.text = string.Empty;
			}
			InputField.interactable = aState;
			disabled = !aState;
		}

		public void ToggleListeners(bool aState)
		{
			listenersActive = aState;
		}

		public void ToggleInput(bool aState)
		{
			if (!aState)
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
				return;
			}

			if (!base.gameObject.activeInHierarchy || !base.isActiveAndEnabled || InputField == null || !InputField.interactable)
			{
				return;
			}

			InputField.Select();
			InputField.ActivateInputField();

			if (!InputField.isFocused)
			{
				StartCoroutine(SelectInputDelayed());
			}
		}

		private IEnumerator SelectInputDelayed()
		{
			yield return new WaitForSeconds(0.5f);

			if (!base.gameObject.activeInHierarchy || !base.isActiveAndEnabled)
			{
				yield break;
			}

			if (InputField != null && InputField.interactable)
			{
				InputField.Select();
				InputField.ActivateInputField();
			}
		}

		public void SendChat()
		{
			string text = InputField.text.TrimEnd(' ', '\n').TrimStart(' ', '\n');
			if (text.Length >= 1)
			{
				if (listener == null)
				{
					Debug.LogError("[ChatBar] SendChat: no chat listener, message not sent");
					return;
				}
				try
				{
					listener.OnSendTextMessage(text);
				}
				catch (System.Exception ex)
				{
					// Keep the text so the user can try again, and leave a trace:
					// this used to fail silently.
					Debug.LogError("[ChatBar] Sending the message failed: " + ex);
					return;
				}
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_4_SendChat");
				Analytics.LogSendChat(clientThread);
				InputField.text = string.Empty;
				OnKeyboardInputResize(null, new NativeKeyboardInputHeightChangedEventArgs((int)(inputStartHeight / Singleton<SettingsManager>.Instance.GetHeightScale())));
			}

		}

		public void OnChatInputClicked()
		{
			ToggleInput(true);
		}

		public void UpdateState(ChatController.UIState aState)
		{
			base.gameObject.SetActive(aState.Equals(ChatController.UIState.Default) || aState.Equals(ChatController.UIState.KeyboardTray) || aState.Equals(ChatController.UIState.GamePausedTray));
		}

		private void OnKeyboardInputResize(object sender, NativeKeyboardInputHeightChangedEventArgs args)
		{
			if (listenersActive)
			{
				float num = padding + (float)args.Height * Singleton<SettingsManager>.Instance.GetHeightScale();
				base.gameObject.GetComponent<LayoutElement>().minHeight = ((!(num < inputStartHeight)) ? num : inputStartHeight);
			}
		}

		private void OnKeyboardReturnKey(object sender, NativeKeyboardReturnKeyPressedEventArgs args)
		{
			SendChat();
		}

		private void OnKeyboardHeightChanged(object sender, NativeKeyboardHeightChangedEventArgs args)
		{
			int aHeight = args.Height;
			if (aHeight > 0 && listener != null)
			{
				listener.OnHidePreviewPanel();
			}
			if (listenersActive)
			{
				KeyboardTray.SetSize(aHeight);
				if (InputField != null && InputField.isFocused)
				{
					listener.OnKeyboardShown();
				}
				else
				{
					listener.OnKeyboardHidden();
				}
			}
		}
	}
}
