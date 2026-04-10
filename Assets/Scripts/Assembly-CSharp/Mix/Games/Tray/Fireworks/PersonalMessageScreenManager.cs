using DG.Tweening;
using Mix.Games.Session;
using Mix.Native;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Games.Tray.Fireworks
{
	public class PersonalMessageScreenManager : MonoBehaviour, IGameModerationResult
	{
		public FireworksGame game;

		public NativeTextView nativeTextView;

		public Camera panelCamera;

		public Button doneButton;

		public GameObject doneButtonText;

		public GameObject doneButtonSpinner;

		public Text errorText;

		public bool isWaitingForValidation;

		private ToastPanelController toastPanel;

		void IGameModerationResult.OnModerationResult(bool aIsModerated, string aModeratedText, object aUserData)
		{
			DOVirtual.DelayedCall(0.1f, delegate
			{
				if (!this.IsNullOrDisposed() && !(base.gameObject == null))
				{
					EnableValidationSpinner(false);
				}
			});
			if (!aIsModerated)
			{
				game.SaveMessage(aModeratedText);
				toastPanel.HideToastPanel();
			}
			else
			{
				MessageModerated(aModeratedText);
			}
		}

		void IGameModerationResult.OnModerationError(object aUserData)
		{
			if (DebugSceneIndicator.IsMainScene)
			{
				EnableValidationSpinner(false);
				game.GameController.PauseOnNetworkError();
			}
		}

		private void Awake()
		{
			toastPanel = GetComponent<ToastPanelController>();
			toastPanel.Init(BaseGameController.Instance.Session.StatusBarHeight, BaseGameController.Instance.Session.ScreenHeight, BaseGameController.Instance.Session.HeightScale);
			toastPanel.ToastPanelAnimationComplete += ActivateKeyboard;
			toastPanel.ToastPanelHideComplete += HideObject;
			nativeTextView.Value = BaseGameController.Instance.Session.GetLocalizedString("customtokens.game.fireworks_entermessage");
		}

		private void OnEnable()
		{
			errorText.gameObject.SetActive(false);
			doneButton.interactable = false;
			panelCamera.gameObject.SetActive(true);
			toastPanel.ToastPanelOnKeyboardReturnPressed += OnDonePressedKeyboard;
			isWaitingForValidation = false;
		}

		private void OnDisable()
		{
			toastPanel.ToastPanelOnKeyboardReturnPressed -= OnDonePressedKeyboard;
		}

		private void ActivateKeyboard()
		{
			nativeTextView.Select();
		}

		private void OnDonePressedKeyboard(NativeKeyboardReturnKey returnKey)
		{
			OnDonePressed();
		}

		public void OnDonePressed()
		{
			if (!IsValidating())
			{
				string value = nativeTextView.Value;
				EnableValidationSpinner(true);
				if (!ValidateMessage(value))
				{
					BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("MainApp/UI/InValidWord");
					MessageInvalid();
				}
				else
				{
					BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("SecretWord/SFX/ButtonUI");
					game.GameController.ModerateText(value, this, null);
				}
			}
		}

		private bool ValidateMessage(string message)
		{
			return message.Length > 0;
		}

		private void MessageInvalid()
		{
			EnableValidationSpinner(false);
			errorText.gameObject.SetActive(true);
			errorText.text = game.GameController.GetLocalizedString("customtokens.game.fireworks_invalidmessage");
			nativeTextView.Value = string.Empty;
		}

		private void MessageModerated(string aModeratedHint)
		{
			errorText.gameObject.SetActive(true);
			errorText.text = game.GameController.GetLocalizedString("customtokens.game.fireworks_invalidmessage");
			nativeTextView.Value = aModeratedHint;
		}

		private void EnableValidationSpinner(bool doSpin)
		{
			if (!this.IsNullOrDisposed() && !(base.gameObject == null))
			{
				doneButtonSpinner.SetActive(doSpin);
				doneButtonText.SetActive(!doSpin);
				doneButton.interactable = !doSpin;
				isWaitingForValidation = doSpin;
			}
		}

		private bool IsValidating()
		{
			return !doneButton.IsInteractable();
		}

		public void ShowPanel()
		{
			panelCamera.gameObject.SetActive(true);
			base.gameObject.SetActive(true);
		}

		public void HidePanel()
		{
			doneButton.interactable = false;
			toastPanel.HideToastPanel();
		}

		private void HideObject()
		{
			panelCamera.gameObject.SetActive(false);
			base.gameObject.SetActive(false);
		}

		public void OnMessageTextChanged(string message)
		{
			if (message != null)
			{
				message = message.Trim();
			}
			doneButton.interactable = !string.IsNullOrEmpty(message) && !isWaitingForValidation;
		}
	}
}
