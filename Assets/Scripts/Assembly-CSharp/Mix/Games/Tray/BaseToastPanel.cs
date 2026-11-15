using Mix.Games.Session;
using Mix.Native;
using Mix.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Games.Tray
{
	public class BaseToastPanel : MonoBehaviour, IGameModerationResult, IKeyboardSubmitHandler
	{
		public bool HandlesKeyboardSubmit(Selectable input)
		{
			return input == NativeTextView;
		}

		public void SubmitFromKeyboard(Selectable input)
		{
			OnDonePressed();
		}

		public BaseGameController GameController;

		public Camera PanelCamera;

		public NativeTextView NativeTextView;

		public Button DoneButton;

		public Button BackButton;

		public GameObject DoneButtonText;

		public GameObject DoneButtonSpinner;

		public Text ErrorText;

		private ToastPanelController mToastPanel;

		void IGameModerationResult.OnModerationResult(bool aIsModerated, string aModeratedText, object aUserData)
		{
			EnableValidationSpinner(false);
			if (aIsModerated)
			{
				TextModerated(aModeratedText);
			}
		}

		void IGameModerationResult.OnModerationError(object aUserData)
		{
			if (DebugSceneIndicator.IsMainScene)
			{
				EnableValidationSpinner(false);
				GameController.PauseOnNetworkError();
			}
		}

		protected virtual void Awake()
		{
			mToastPanel = GetComponent<ToastPanelController>();
			mToastPanel.ToastPanelAnimationComplete += ActivateKeyboard;
			mToastPanel.ToastPanelHideComplete += HideObject;
		}

		protected virtual void OnEnable()
		{
			ErrorText.gameObject.SetActive(false);
			DoneButton.interactable = true;
			BackButton.interactable = true;
			if (PanelCamera != null)
			{
				PanelCamera.gameObject.SetActive(true);
			}
			mToastPanel.ToastPanelOnKeyboardReturnPressed += OnDonePressedKeyboard;
		}

		protected virtual void OnDisable()
		{
			mToastPanel.ToastPanelOnKeyboardReturnPressed -= OnDonePressedKeyboard;
		}

		protected void ActivateKeyboard()
		{
			NativeTextView.SelectInput();
		}

		protected void OnDonePressedKeyboard(NativeKeyboardReturnKey returnKey)
		{
			OnDonePressed();
		}

		public void OnDonePressed()
		{
			if (!IsValidating())
			{
				string value = NativeTextView.Value;
				EnableValidationSpinner(true);
				GameController.Session.SessionSounds.PlaySoundEvent("SecretWord/SFX/ButtonUI");
				GameController.ModerateText(value, this, null);
			}

		}

		public void ShowPanel()
		{
			if (PanelCamera != null)
			{
				PanelCamera.gameObject.SetActive(true);
			}
			base.gameObject.SetActive(true);
		}

		public void HidePanel()
		{
			DoneButton.interactable = false;
			BackButton.interactable = false;
			mToastPanel.HideToastPanel();
		}

		protected void TextModerated(string aModeratedHint)
		{
			ErrorText.gameObject.SetActive(true);
			ErrorText.text = GameController.GetLocalizedString("customtokens.game.moderated_text");
			NativeTextView.Value = aModeratedHint;
		}

		protected void EnableValidationSpinner(bool doSpin)
		{
			DoneButtonSpinner.SetActive(doSpin);
			DoneButtonText.SetActive(!doSpin);
			DoneButton.interactable = !doSpin;
			BackButton.interactable = !doSpin;
		}

		protected bool IsValidating()
		{
			return !DoneButton.IsInteractable();
		}

		protected void HideObject()
		{
			if (PanelCamera != null)
			{
				PanelCamera.gameObject.SetActive(false);
			}
			base.gameObject.SetActive(false);
		}
	}
}
