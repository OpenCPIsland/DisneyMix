using System.Collections.Generic;
using Mix.Native;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

#if TMP_PRESENT
using TMPro;
#endif

namespace Mix.Ui
{
	public interface IKeyboardSubmitHandler
	{
		bool HandlesKeyboardSubmit(Selectable input);

		void SubmitFromKeyboard(Selectable input);
	}

	[DefaultExecutionOrder(-2000)]
	public class KeyboardTextInputNavigation : MonoBehaviour
	{
		public static void Ensure(EventSystem eventSystem)
		{
			if (eventSystem != null && eventSystem.GetComponent<KeyboardTextInputNavigation>() == null)
			{
				eventSystem.gameObject.AddComponent<KeyboardTextInputNavigation>();
			}
		}

		private void Update()
		{
			EventSystem eventSystem = EventSystem.current;
			if (eventSystem == null)
			{
				return;
			}

			bool tabPressed = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Tab].wasPressedThisFrame;
			bool enterPressed = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Enter].wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.NumpadEnter].wasPressedThisFrame;
			if (!tabPressed && !enterPressed)
			{
				return;
			}

			Selectable selectedInput = GetTextInput(eventSystem.currentSelectedGameObject);
			if (selectedInput == null)
			{
				selectedInput = GetSelectedNativeTextInput();
			}

			if (selectedInput == null)
			{
				return;
			}

			if (tabPressed)
			{
				MoveFocus(eventSystem, selectedInput, UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.LeftShift].isPressed || UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.RightShift].isPressed);
				return;
			}

			if (enterPressed)
			{
				Submit(eventSystem, selectedInput);
			}
		}

		private static Selectable GetSelectedNativeTextInput()
		{
			NativeTextView[] nativeTextViews = FindObjectsOfType<NativeTextView>();
			foreach (NativeTextView nativeTextView in nativeTextViews)
			{
				if (nativeTextView.Selected && IsUsable(nativeTextView))
				{
					return nativeTextView;
				}
			}
			return null;
		}

		private static Selectable GetTextInput(GameObject gameObject)
		{
			if (gameObject == null)
			{
				return null;
			}

			NativeTextView nativeTextView = gameObject.GetComponentInParent<NativeTextView>();
			if (IsUsable(nativeTextView))
			{
				return nativeTextView;
			}

			InputField inputField = gameObject.GetComponentInParent<InputField>();
			if (IsUsable(inputField))
			{
				return inputField;
			}

#if TMP_PRESENT
			TMP_InputField tmpInputField = gameObject.GetComponentInParent<TMP_InputField>();
			if (IsUsable(tmpInputField))
			{
				return tmpInputField;
			}
#endif
			return null;
		}

		private static bool IsUsable(Selectable input)
		{
			return input != null && input.IsActive() && input.IsInteractable();
		}

		private static void MoveFocus(EventSystem eventSystem, Selectable currentInput, bool moveBackward)
		{
			BasePanel panel = currentInput.GetComponentInParent<BasePanel>();
			Canvas canvas = currentInput.GetComponentInParent<Canvas>();
			Transform scopeLimit = panel != null ? panel.transform : (canvas != null ? canvas.transform : currentInput.transform.root);
			Transform scope = currentInput.transform.parent;
			List<Selectable> inputs = null;
			while (scope != null)
			{
				inputs = GetTextInputs(scope);
				if (inputs.Count > 1)
				{
					break;
				}
				if (scope == scopeLimit)
				{
					return;
				}
				scope = scope.parent;
			}

			if (inputs == null || inputs.Count < 2)
			{
				return;
			}

			int currentIndex = inputs.IndexOf(currentInput);
			if (currentIndex < 0)
			{
				return;
			}
			int nextIndex = (currentIndex + (moveBackward ? inputs.Count - 1 : 1)) % inputs.Count;
			Selectable nextInput = inputs[nextIndex];

			DeactivateInput(currentInput);
			eventSystem.SetSelectedGameObject(nextInput.gameObject);
			ActivateInput(nextInput);
		}

		private static List<Selectable> GetTextInputs(Transform root)
		{
			List<Selectable> inputs = new List<Selectable>();
			Selectable[] selectables = root.GetComponentsInChildren<Selectable>(false);
			foreach (Selectable selectable in selectables)
			{
				if (!IsUsable(selectable))
				{
					continue;
				}
				if (selectable is NativeTextView)
				{
					inputs.Add(selectable);
					continue;
				}
				if (selectable.GetComponentInParent<NativeTextView>() != null)
				{
					continue;
				}
				if (selectable is InputField)
				{
					inputs.Add(selectable);
					continue;
				}
#if TMP_PRESENT
				if (selectable is TMP_InputField)
				{
					inputs.Add(selectable);
				}
#endif
			}
			return inputs;
		}

		private static void ActivateInput(Selectable input)
		{
			input.Select();
			NativeTextView nativeTextView = input as NativeTextView;
			if (nativeTextView != null)
			{
				nativeTextView.SelectInput();
				return;
			}

			InputField inputField = input as InputField;
			if (inputField != null)
			{
				inputField.ActivateInputField();
				return;
			}
#if TMP_PRESENT
			TMP_InputField tmpInputField = input as TMP_InputField;
			if (tmpInputField != null)
			{
				tmpInputField.ActivateInputField();
			}
#endif
		}

		private static void DeactivateInput(Selectable input)
		{
			InputField inputField = input as InputField;
			if (inputField != null)
			{
				inputField.DeactivateInputField();
				return;
			}
#if TMP_PRESENT
			TMP_InputField tmpInputField = input as TMP_InputField;
			if (tmpInputField != null)
			{
				tmpInputField.DeactivateInputField();
				return;
			}
#endif
			NativeTextView nativeTextView = input as NativeTextView;
			if (nativeTextView != null)
			{
				DeactivateNativeTextView(nativeTextView);
			}
		}

		private static void DeactivateNativeTextView(NativeTextView nativeTextView)
		{
#if TMP_PRESENT
			TMP_InputField tmpInputField = nativeTextView.TextComponent.GetComponent<TMP_InputField>();
			if (tmpInputField != null && tmpInputField.isFocused)
			{
				string value = tmpInputField.text;
				tmpInputField.DeactivateInputField();
				nativeTextView.Value = value;
			}
#else
			InputField inputField = nativeTextView.TextComponent.GetComponent<InputField>();
			if (inputField != null && inputField.isFocused)
			{
				string value = inputField.text;
				inputField.DeactivateInputField();
				nativeTextView.Value = value;
			}
#endif
			if (!MonoSingleton<NativeKeyboardManager>.Instance.IsNullOrDisposed())
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			}
		}

		private static void Submit(EventSystem eventSystem, Selectable input)
		{
			IKeyboardSubmitHandler handler = FindSubmitHandler(input);
			if (handler == null)
			{
				if (input is NativeTextView)
				{
					DeactivateInput(input);
					eventSystem.SetSelectedGameObject(null);
				}
				return;
			}

			DeactivateInput(input);
			eventSystem.SetSelectedGameObject(null);
			handler.SubmitFromKeyboard(input);
		}

		private static IKeyboardSubmitHandler FindSubmitHandler(Selectable input)
		{
			for (Transform current = input.transform; current != null; current = current.parent)
			{
				MonoBehaviour[] components = current.GetComponents<MonoBehaviour>();
				foreach (MonoBehaviour component in components)
				{
					IKeyboardSubmitHandler handler = component as IKeyboardSubmitHandler;
					if (component != null && component.isActiveAndEnabled && handler != null && handler.HandlesKeyboardSubmit(input))
					{
						return handler;
					}
				}
			}
			return null;
		}
	}
}
