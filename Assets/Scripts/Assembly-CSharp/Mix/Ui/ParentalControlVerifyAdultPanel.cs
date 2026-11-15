using System;
using Disney.Mix.SDK;
using Mix.Native;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class ParentalControlVerifyAdultPanel : BasePanel, IKeyboardSubmitHandler
	{
		public bool HandlesKeyboardSubmit(Selectable input)
		{
			return input == address1Field || input == address2Field || input == cityField || input == stateField || input == zipField || input == ssnField;
		}

		public void SubmitFromKeyboard(Selectable input)
		{
			OnVerifyClicked();
		}

		public interface IParentalControlVerifyAdultListener
		{
			void OnAdultVerified(ILocalUser aParent);
		}

		public NativeTextView address1Field;

		public NativeTextView address2Field;

		public NativeTextView cityField;

		public NativeTextView stateField;

		public NativeTextView zipField;

		public NativeTextView ssnField;

		private IParentalControlVerifyAdultListener currentListener;

		private ILocalUser parent;

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

		public void Init(ILocalUser aParent, IParentalControlVerifyAdultListener aListener)
		{
			parent = aParent;
			currentListener = aListener;
		}

		public void OnVerifyClicked()
		{
			if (!MonoSingleton<NativeKeyboardManager>.Instance.IsNullOrDisposed())
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			}

			parent.GetVerifyAdultForm(delegate
			{
				IWritableVerifyAdultFormUnitedStates writableVerifyAdultFormUnitedStates = new IWritableVerifyAdultFormUnitedStates();
				writableVerifyAdultFormUnitedStates.AddressLine1 = address1Field.Value;
				writableVerifyAdultFormUnitedStates.PostalCode = zipField.Value;
				writableVerifyAdultFormUnitedStates.Ssn = ssnField.Value;
				writableVerifyAdultFormUnitedStates.FirstName = parent.RegistrationProfile.FirstName;
				writableVerifyAdultFormUnitedStates.LastName = parent.RegistrationProfile.LastName;
				writableVerifyAdultFormUnitedStates.DateOfBirth = parent.RegistrationProfile.DateOfBirth.Value;
				writableVerifyAdultFormUnitedStates.AddressLine1 = "3";
				writableVerifyAdultFormUnitedStates.PostalCode = "06067";
				writableVerifyAdultFormUnitedStates.Ssn = "8001";
				writableVerifyAdultFormUnitedStates.FirstName = "DAI";
				writableVerifyAdultFormUnitedStates.LastName = "MARKS";
				writableVerifyAdultFormUnitedStates.DateOfBirth = new DateTime(1974, 5, 24);
				parent.VerifyAdult(writableVerifyAdultFormUnitedStates, delegate(IVerifyAdultResult verifyAdultResult)
				{
					if (!verifyAdultResult.Success)
					{
						Debug.Log("request failed");
					}
					else
					{
						Debug.Log("success");
						currentListener.OnAdultVerified(parent);
					}
				});
			});
		}

		private void OnKeyboardReturnKey(object sender, NativeKeyboardReturnKeyPressedEventArgs args)
		{
			if (args.ReturnKeyType == NativeKeyboardReturnKey.Next)
			{
				if (address1Field.Selected)
				{
					address2Field.SelectInput();
				}
				else if (address2Field.Selected)
				{
					cityField.SelectInput();
				}
				else if (cityField.Selected)
				{
					stateField.SelectInput();
				}
				else if (stateField.Selected)
				{
					zipField.SelectInput();
				}
				else if (zipField.Selected)
				{
					ssnField.SelectInput();
				}
			}
			else
			{
				OnVerifyClicked();
			}
		}
	}
}
