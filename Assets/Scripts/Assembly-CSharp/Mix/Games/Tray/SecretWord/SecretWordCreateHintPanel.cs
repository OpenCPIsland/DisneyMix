using DG.Tweening;
using Mix.Games.Session;
using Mix.Native;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Games.Tray.SecretWord
{
	public class SecretWordCreateHintPanel : MonoBehaviour, IGameModerationResult
	{
		private sealed class OnModerationResult_003Ec__AnonStorey70
		{
			internal bool aIsModerated;

			internal string aModeratedText;

			internal SecretWordCreateHintPanel _003C_003Ef__this;

			internal void _003C_003Em__86()
			{
				_003C_003Ef__this.EnableValidationSpinner(false);
				if (!aIsModerated)
				{
					_003C_003Ef__this.game.Word = _003C_003Ef__this.word;
					_003C_003Ef__this.game.Hint = aModeratedText;
					_003C_003Ef__this.game.SendDataAndQuit();
				}
				else
				{
					_003C_003Ef__this.HintModerated(aModeratedText);
				}
			}
		}

		[HideInInspector]
		public string word;

		public SecretWordGame game;

		public GameObject wordHolder;

		public SecretWordLetterHolder letterHolder;

		public InputField nativeTextView;

		public Camera panelCamera;

		public Button doneButton;

		public Button backButton;

		public GameObject doneButtonText;

		public GameObject doneButtonSpinner;

		public Text errorText;

		private ToastPanelController toastPanel;

		private GameObject[] letters;

		void IGameModerationResult.OnModerationResult(bool aIsModerated, string aModeratedText, object aUserData)
		{
			OnModerationResult_003Ec__AnonStorey70 CS_0024_003C_003E8__locals12 = new OnModerationResult_003Ec__AnonStorey70();
			CS_0024_003C_003E8__locals12.aIsModerated = aIsModerated;
			CS_0024_003C_003E8__locals12.aModeratedText = aModeratedText;
			CS_0024_003C_003E8__locals12._003C_003Ef__this = this;
			DOVirtual.DelayedCall(0.1f, delegate
			{
				CS_0024_003C_003E8__locals12._003C_003Ef__this.EnableValidationSpinner(false);
				if (!CS_0024_003C_003E8__locals12.aIsModerated)
				{
					CS_0024_003C_003E8__locals12._003C_003Ef__this.game.Word = CS_0024_003C_003E8__locals12._003C_003Ef__this.word;
					CS_0024_003C_003E8__locals12._003C_003Ef__this.game.Hint = CS_0024_003C_003E8__locals12.aModeratedText;
					CS_0024_003C_003E8__locals12._003C_003Ef__this.game.SendDataAndQuit();
				}
				else
				{
					CS_0024_003C_003E8__locals12._003C_003Ef__this.HintModerated(CS_0024_003C_003E8__locals12.aModeratedText);
				}
			});
		}

		void IGameModerationResult.OnModerationError(object aUserData)
		{
			if (DebugSceneIndicator.IsMainScene)
			{
				EnableValidationSpinner(false);
				game.gameController.PauseOnNetworkError();
			}
		}

		private void Awake()
		{
			letters = new GameObject[game.maxWordLength];
			letters[0] = letterHolder.gameObject;
			letters[0].transform.SetParent(wordHolder.transform, false);
			for (int i = 1; i < letters.Length; i++)
			{
				letters[i] = Object.Instantiate(letterHolder.gameObject);
				letters[i].transform.SetParent(wordHolder.transform, false);
			}
			nativeTextView.text = BaseGameController.Instance.Session.GetLocalizedString("customtokens.secretword.enter_hint");
			toastPanel = GetComponent<ToastPanelController>();
			toastPanel.ToastPanelAnimationComplete += ActivateKeyboard;
			toastPanel.ToastPanelHideComplete += HideObject;
		}

		private void OnEnable()
		{
			float num = 1f;
			int i;
			for (i = 0; i < word.Length; i++)
			{
				GameObject gameObject = letters[i];
				gameObject.SetActive(true);
				gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, num * (float)Random.Range(1, 7));
				SecretWordLetterHolder component = gameObject.GetComponent<SecretWordLetterHolder>();
				component.letterText.text = word[i].ToString();
				num *= -1f;
			}
			for (; i < game.maxWordLength; i++)
			{
				GameObject gameObject = letters[i];
				gameObject.SetActive(false);
			}
			nativeTextView.text = game.Hint;
			errorText.gameObject.SetActive(false);
			doneButton.interactable = true;
			backButton.interactable = true;
			panelCamera.gameObject.SetActive(true);
			toastPanel.ToastPanelOnKeyboardReturnPressed += OnDonePressedKeyboard;
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
				string value = nativeTextView.text;
				EnableValidationSpinner(true);
				if (!ValidateHint(value))
				{
					BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("MainApp/UI/InValidWord");
					HintInvalid();
				}
				else
				{
					BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("SecretWord/SFX/ButtonUI");
					game.gameController.ModerateText(value, this, null);
				}
			}
		}

		private bool ValidateHint(string hint)
		{
			string text = hint.ToUpper();
			return hint.Length > 0 && !text.Contains(word);
		}

		private void HintInvalid()
		{
			EnableValidationSpinner(false);
			errorText.gameObject.SetActive(true);
			errorText.text = BaseGameController.Instance.Session.GetLocalizedString("customtokens.secretword.invalid_hint");
			nativeTextView.text = string.Empty;
		}

		private void HintModerated(string aModeratedHint)
		{
			errorText.gameObject.SetActive(true);
			errorText.text = BaseGameController.Instance.Session.GetLocalizedString("customtokens.secretword.invalid_hint");
			nativeTextView.text = aModeratedHint;
		}

		private void EnableValidationSpinner(bool doSpin)
		{
			doneButtonSpinner.SetActive(doSpin);
			doneButtonText.SetActive(!doSpin);
			doneButton.interactable = !doSpin;
			backButton.interactable = !doSpin;
		}

		private bool IsValidating()
		{
			return !doneButton.IsInteractable();
		}

		public void ShowPanel()
		{
			panelCamera.gameObject.SetActive(true);
			toastPanel = GetComponent<ToastPanelController>();
			toastPanel.Init(BaseGameController.Instance.Session.StatusBarHeight, BaseGameController.Instance.Session.ScreenHeight, BaseGameController.Instance.Session.HeightScale);
			base.gameObject.SetActive(true);
		}

		public void HidePanel()
		{
			doneButton.interactable = false;
			backButton.interactable = false;
			toastPanel.HideToastPanel();
		}

		private void HideObject()
		{
			panelCamera.gameObject.SetActive(false);
			base.gameObject.SetActive(false);
		}
	}
}
