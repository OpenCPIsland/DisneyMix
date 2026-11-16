using System;
using System.Collections.Generic;
using System.IO;
using Disney.Native;
using Mix;
using Mix.Ui;
using UnityEngine;
using UnityEngine.UI;

public class SaveMediaController : MonoBehaviour
{
	private const float CLICK_LENGTH = 1f;

	public GameObject ContextMenu;

	public Button SaveButton;

	public Button CancelButton;

	private float mouseDownDeltaTime;

	private bool isMouseDown;

	private Texture2D currentTexture;

	private bool removeListeners;

	public bool AllowShowingOfMenu { get; set; }

	public event EventHandler OnPhotoSaved = delegate
	{
	};

	private void Start()
	{
		AllowShowingOfMenu = true;
	}

	private void Update()
	{
		if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
		{
			isMouseDown = true;
			mouseDownDeltaTime = 0f;
		}
		if (isMouseDown)
		{
			if (mouseDownDeltaTime > 1f)
			{
				RectTransform component = GetComponent<RectTransform>();
				if (Util.GetRectInScreenSpace(component).Contains(UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero) && AllowShowingOfMenu)
				{
					ContextMenu.SetActive(true);
				}
				isMouseDown = false;
			}
			mouseDownDeltaTime += Time.deltaTime;
		}
		if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame)
		{
			isMouseDown = false;
		}
	}

	public void OnSavePhotoClicked(Texture2D aTexture, bool aRemoveListeners = false)
	{
		currentTexture = aTexture;
		removeListeners = aRemoveListeners;
		if (MonoSingleton<NativeUtilitiesManager>.Instance.Native.HasPermissions(new List<string> { "write_external_storage" }))
		{
			UserHasPermission();
		}
		else if (!MonoSingleton<NativeUtilitiesManager>.Instance.Native.AskForPermissions(new List<string> { "write_external_storage" }, OnPermissionResult))
		{
			UserDoesntHavePermission();
		}
	}

	private void UserHasPermission()
	{
		if (currentTexture != null)
		{
			string text = Mix.Application.PersistentDataPath + "/tempSaveMediaImage.jpeg";
			File.WriteAllBytes(text, currentTexture.EncodeToJPG());
			OnSavePhotoClicked(text, removeListeners, true);
			currentTexture = null;
			removeListeners = false;
		}
	}

	private void OnPermissionResult(NativeUtilitiesPermissionResult aResult)
	{
		if (aResult.HasPermission)
		{
			UserHasPermission();
		}
		else
		{
			UserDoesntHavePermission();
		}
	}

	private void UserDoesntHavePermission()
	{
		GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
		Singleton<PanelManager>.Instance.SetupTokens(new Dictionary<Text, string>
		{
			{ genericPanel.TitleText, null },
			{ genericPanel.MessageText, "customtokens.panels.enable_storage" },
			{ genericPanel.ButtonOneText, "customtokens.panels.button_ok" }
		});
	}

	public void OnSavePhotoClicked(string aImagePath, bool aRemoveListeners = false, bool aDeleteAfterDone = false)
	{
		EtceteraAndroid.saveImageToGallery(aImagePath, string.Empty);
		if (aDeleteAfterDone)
		{
			File.Delete(aImagePath);
		}
		if (this.OnPhotoSaved != null)
		{
			this.OnPhotoSaved(this, new EventArgs());
		}
		Close(aRemoveListeners);
	}

	public void CloseContextMenu()
	{
		isMouseDown = false;
		ContextMenu.SetActive(false);
	}

	private void Close(bool aRemoveListeners)
	{
		if (aRemoveListeners)
		{
			SaveButton.onClick.RemoveAllListeners();
		}
		CancelButton.onClick.Invoke();
	}
}
