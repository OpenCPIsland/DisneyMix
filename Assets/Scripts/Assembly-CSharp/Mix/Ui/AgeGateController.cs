using System.Text;
using Mix.Localization;
using Mix.Native;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class AgeGateController : MonoBehaviour, IKeyboardSubmitHandler
	{
		public bool HandlesKeyboardSubmit(Selectable input)
		{
			return NumberInput != null && input == NumberInput.GetComponent<NativeTextView>();
		}

		public void SubmitFromKeyboard(Selectable input)
		{
			OnNextButtonPressed();
		}

		public GameObject ErrorMessageObject;

		public GameObject AgeGateString;

		public GameObject NumberInput;

		private IAgeGateController caller;

		private string destinationUrl;

		private string destinationTitle;

		private string generatedAgeGateString;

		private string[] AGE_GATE_NUMBERS = new string[10] { "customtokens.agegate.number_zero", "customtokens.agegate.number_one", "customtokens.agegate.number_two", "customtokens.agegate.number_three", "customtokens.agegate.number_four", "customtokens.agegate.number_five", "customtokens.agegate.number_six", "customtokens.agegate.number_seven", "customtokens.agegate.number_eight", "customtokens.agegate.number_nine" };

		public void Show(IAgeGateController aCaller, string aDestinationUrl, string aDestinationTitle)
		{
			base.gameObject.SetActive(true);
			caller = aCaller;
			destinationUrl = aDestinationUrl;
			destinationTitle = aDestinationTitle;
			GenerateAgeGateString();
		}

		public void Hide()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			base.gameObject.SetActive(false);
		}

		public void OnNextButtonPressed()
		{
			NativeTextView component = NumberInput.GetComponent<NativeTextView>();
			string value = component.Value;
			if (value != generatedAgeGateString)
			{
				ErrorMessageObject.SetActive(true);
			}

			else
			{
				caller.OnSuccess(destinationUrl, destinationTitle);
			}
		}

		private void GenerateAgeGateString()
		{
			Text component = AgeGateString.GetComponent<Text>();
			StringBuilder stringBuilder = new StringBuilder();
			int[] array = new int[4]
			{
				Random.Range(0, 9),
				Random.Range(0, 9),
				Random.Range(0, 9),
				Random.Range(0, 9)
			};
			stringBuilder.AppendFormat("{0} {1} {2} {3}", Singleton<Localizer>.Instance.getString(AGE_GATE_NUMBERS[array[0]]), Singleton<Localizer>.Instance.getString(AGE_GATE_NUMBERS[array[1]]), Singleton<Localizer>.Instance.getString(AGE_GATE_NUMBERS[array[2]]), Singleton<Localizer>.Instance.getString(AGE_GATE_NUMBERS[array[3]]));
			generatedAgeGateString = null;
			int[] array2 = array;
			foreach (int num in array2)
			{
				generatedAgeGateString += num;
			}
			component.text = stringBuilder.ToString();
		}
	}
}
