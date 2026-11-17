using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Disney.Mix.SDK;
using Disney.MobileNetwork;
using Disney.Native;
using Mix;
using Mix.Assets;
using Mix.DeviceDb;
using Mix.Session.Extensions;
using Mix.Ui;
using UnityEngine;
using UnityEngine.UI;

public class MixChatItem : BaseChatItem, IScrollItem, IScrollItemHelper
{
	private const float AVATAR_OFFSET = 100f;

	private const float HPAD = 60f;

	private const float VPAD = 40f;

	private static GameObject leftPrefab;

	private static GameObject rightPrefab;

	private GameObject chatContent;

	private IKeyValDatabaseApi databaseApi;

	private MatchCollection urlMatches;

	private string spriteName;

	private static long StaticCount;

	private long ClearCacheCount = 20L;

	private Color32 fontColor;

	private int imageWidth;

	private int imageHeight;

	private int fontSize;

	private float maxWidth;

	private float minBubbleWidth = 65f;

	private int imageTargetSize;

	public MixChatItem(IChatThread aThread, IChatMessage aMessage, ScrollView aScrollView)
		: base(aThread, aMessage, aScrollView)
	{
		databaseApi = Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi;
		string text = databaseApi.LoadUserValue("default_primary_color");
		spriteName = message.GetHashCode() + "|" + ((text == null) ? "1C97D4" : text);
		if (message.IsMine() && rightPrefab == null)
		{
			rightPrefab = Resources.Load<GameObject>("Prefabs/Screens/ChatMix/ChatBubbleRight");
		}
		else if (leftPrefab == null)
		{
			leftPrefab = Resources.Load<GameObject>("Prefabs/Screens/ChatMix/ChatBubbleLeft");
		}
	}

	float IScrollItemHelper.GetGameObjectHeight()
	{
		float height = Singleton<MixDocumentCollections>.Instance.chatMetaDataDocumentCollectionApi.GetHeight(message.Id);
		return (!(height > 0f)) ? (-1f) : height;
	}

	GameObject IScrollItem.GenerateGameObject(bool aGenerateForHeightOnly)
	{
		base.instance = Object.Instantiate((!message.IsMine()) ? leftPrefab : rightPrefab);
		chatContent = base.instance.transform.Find("Content/ChatContent").gameObject;
		float height = Singleton<MixDocumentCollections>.Instance.chatMetaDataDocumentCollectionApi.GetHeight(message.Id);
		if (height > 0f)
		{
			RectTransform component = base.instance.GetComponent<RectTransform>();
			component.sizeDelta = new Vector2(component.sizeDelta.x + ((thread is IOneOnOneChatThread) ? 0f : 100f), height);
			if (aGenerateForHeightOnly)
			{
				return base.instance;
			}
		}
		SetupOffline("Content/ChatContent/ResendBtn", string.Empty, string.Empty, string.Empty);
		string text = (message.IsMine() ? "Content/PlaceholderAvatarRight/AvatarHeadRight/ImageTarget" : "Content/PlaceholderAvatarLeft/AvatarHeadLeft/ImageTarget");
		if (!(thread is IOneOnOneChatThread))
		{
			text += "_Geo";
		}
		imageTargetSize = (int)base.instance.transform.Find(text).GetComponent<RectTransform>().rect.height;
		if (!string.IsNullOrEmpty((message as ITextMessage).Text) && base.instance != null)
		{
			Text componentInChildren = base.instance.GetComponentInChildren<Text>();
			if (componentInChildren != null)
			{
				AccessibilitySettings accessibilitySettings = componentInChildren.gameObject.AddComponent<AccessibilitySettings>();
				accessibilitySettings.DynamicText = (message as ITextMessage).Text;
				accessibilitySettings.VoiceOnly = true;
			}
		}
		if (!aGenerateForHeightOnly)
		{
			SetupAndSkinAvatar();
		}
		GenerateAndCacheImageText(false, aGenerateForHeightOnly);
		ResizeChatBubble();
		return base.instance;
	}

	void IScrollItem.Destroy()
	{
		OnDestroy();
	}

	public override void UpdateClientMessage(IChatMessage aMessage)
	{
		message = aMessage;
		if (!base.instance.IsNullOrDisposed())
		{
			if (aMessage.IsMine())
			{
				ResendObject.SetActive(FailedToSend);
			}
			Transform transform = chatContent.transform.Find("ChatText");
			if ((bool)transform)
			{
				GenerateAndCacheImageText(true);
				ResizeChatBubble();
			}
		}
	}

	private void GenerateAndCacheImageText(bool aForce, bool aGenerateForHeightOnly = false)
	{
		GameObject textGameObject = chatContent.transform.Find("ChatText").gameObject;
		Text component = textGameObject.GetComponent<Text>();
		Sprite sprite = (Sprite)MonoSingleton<AssetManager>.Instance.assetCache.Get(spriteName);
		if (component != null && (message as ITextMessage).Text != null)
		{
			StringInfo stringInfo = new StringInfo((message as ITextMessage).Text);
			int num = ((stringInfo.LengthInTextElements != 1 || (message as ITextMessage).Text.Length <= 1) ? 1 : 4);
			fontSize = (component.fontSize * num);
			fontColor = component.color;
		}
		if (aForce && sprite != null)
		{
			Object.DestroyImmediate(sprite);
			sprite = null;
			MonoSingleton<AssetManager>.Instance.assetCache.Remove(MonoSingleton<AssetManager>.Instance.assetCache.GetIndexOfKey(spriteName));
		}
		textGameObject.GetComponent<Button>().onClick.AddListener(delegate
		{
			OnBubbleClicked(textGameObject, UnityEngine.InputSystem.Pointer.current != null ? UnityEngine.InputSystem.Pointer.current.position.ReadValue() : Vector2.zero);
		});
		GameObject gameObject = chatContent.transform.Find(message.IsMine() ? "ChatBubbleRight" : "ChatBubbleLeft").gameObject;
		Image component2 = gameObject.GetComponent<Image>();
		TintImage component3 = gameObject.GetComponent<TintImage>();
		Color32 aFontBGColor = ((!(component3 != null)) ? ((Color32)component2.color) : ((Color32)component3.color));
		if (!message.Sent)
		{
			aFontBGColor = new Color32(0, 0, 0, 0);
		}
		StaticCount++;
		if (StaticCount > ClearCacheCount)
		{
			StaticCount = 0L;
		}
		if (sprite == null)
		{
			maxWidth = ScrollView.GetComponent<RectTransform>().rect.width - 60f - ((thread is IOneOnOneChatThread) ? 0f : 100f);
			if (aGenerateForHeightOnly)
			{
				imageWidth = 4;
				imageHeight = Service.Get<SystemTextManager>().GetImageHeightForString((message as ITextMessage).Text, string.Empty, fontSize, fontColor, aFontBGColor, maxWidth, true);
				Texture2D texture = new Texture2D(imageWidth, imageHeight, TextureFormat.ARGB32, false);
				sprite = Sprite.Create(texture, new Rect(0f, 0f, imageWidth, imageHeight), new Vector2(0.5f, 0.5f));
			}
			else
			{
				Texture2D texture2D = new Texture2D(4, 4, TextureFormat.ARGB32, false);
				string path = Service.Get<SystemTextManager>().GenerateImageForString((message as ITextMessage).Text, string.Empty, fontSize, fontColor, aFontBGColor, maxWidth, true);
				texture2D.LoadImage(File.ReadAllBytes(path));
				File.Delete(path);
				imageHeight = texture2D.height;
				imageWidth = texture2D.width;
				sprite = Sprite.Create(texture2D, new Rect(0f, 0f, imageWidth, imageHeight), new Vector2(0.5f, 0.5f));
				long aSize = imageWidth * imageHeight * 4;
				MonoSingleton<AssetManager>.Instance.assetCache.Add(spriteName, sprite, aSize);
			}
		}
		else
		{
			imageHeight = sprite.texture.height;
			imageWidth = sprite.texture.width;
		}
		Image image = null;
		if (component != null)
		{
			Object.DestroyImmediate(component);
			image = textGameObject.AddComponent<Image>();
		}
		else
		{
			image = textGameObject.GetComponent<Image>();
		}
		image.sprite = sprite;
		image.preserveAspect = true;
	}

	private void ResizeChatBubble()
	{
		Transform transform = chatContent.transform.Find("ChatText");
		if ((bool)transform)
		{
			if (message is ITextMessage)
			{
				transform.GetComponent<Button>().onClick.AddListener(OnUrlClicked);
			}
			if (!(thread is IOneOnOneChatThread))
			{
				minBubbleWidth = 165f;
			}
			else
			{
				minBubbleWidth = 65f;
			}
			maxWidth = ScrollView.GetComponent<RectTransform>().rect.width - 60f;
			float num = (float)imageWidth * 0.8f + 60f + ((thread is IOneOnOneChatThread) ? 0f : 100f);
			float num2 = (float)imageHeight * 0.8f + 40f;
			if (num < minBubbleWidth)
			{
				num = minBubbleWidth;
			}
			else if (num > maxWidth)
			{
				num = maxWidth;
			}
			if (!(thread is IOneOnOneChatThread) && num2 < (float)imageTargetSize)
			{
				num2 = imageTargetSize;
			}
			base.instance.GetComponent<RectTransform>().sizeDelta = new Vector2(num, num2 + avatarNameHeight);
			SaveHeight(base.instance.GetComponent<RectTransform>().sizeDelta.y);
		}
		Transform transform2 = base.instance.transform.Find("Content/ChatContent/ChatBubbleRight");
		if (!(transform2 == null))
		{
			TintImage component = transform2.GetComponent<TintImage>();
			if (component != null && !message.Sent)
			{
				component.color = new Color(component.color.r, component.color.g, component.color.b, 0.5f);
			}
			else if (component != null)
			{
				component.color = new Color(component.color.r, component.color.g, component.color.b, 1f);
			}
		}
	}

	private void OnUrlClicked()
	{
		if (urlMatches != null && urlMatches.Count > 0)
		{
			string value = urlMatches[0].Value;
			if (DataChecker.IsValidEmail(value))
			{
				Mix.Application.OpenUrl("mailto:" + value);
			}
			else if (value.StartsWith("https://") || value.StartsWith("http://"))
			{
				Mix.Application.OpenUrl(value);
			}
			else
			{
				Mix.Application.OpenUrl("http://" + value);
			}
		}
	}

	protected override void OnResendClicked()
	{
		ResendObject.SetActive(false);
		ResendFailedMessage(message);
	}

	private void OnBubbleClicked(GameObject aObject, Vector3 aMousePosition)
	{
		Canvas parentCanvas = Util.GetParentCanvas(aObject);
		Vector2 localPoint;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(aObject.GetComponent<RectTransform>(), aMousePosition, parentCanvas.worldCamera, out localPoint);
		localPoint.y *= -1f;
		float aMaxWidth = maxWidth;
		ITextMessage textMessage = message as ITextMessage;
		if (textMessage != null)
		{
			int indexInTextAtPoint = Service.Get<SystemTextManager>().GetIndexInTextAtPoint(textMessage.Text, string.Empty, fontSize, aMaxWidth, true, localPoint.x, localPoint.y);
			CheckForUrl(textMessage.Text, indexInTextAtPoint);
		}
	}

	private void CheckForUrl(string aText, int aIndex)
	{
		int num = 0;
		int num2 = aText.Length;
		for (int num3 = aIndex - 1; num3 > -1; num3--)
		{
			if (aText.Substring(num3, 1).Equals(" "))
			{
				num = num3;
				break;
			}
		}
		for (int i = num + 1; i < aText.Length - 1; i++)
		{
			if (aText.Substring(i, 1).Equals(" "))
			{
				num2 = i;
				break;
			}
		}
		string text = aText.Substring(num, num2 - num);
		if (!string.IsNullOrEmpty(text))
		{
			urlMatches = DataChecker.GetEmailsAndUrls(text);
			if (urlMatches != null && urlMatches.Count > 0)
			{
				OnUrlClicked();
			}
		}
	}
}
