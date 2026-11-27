using Mix;
using UnityEngine;

namespace Disney.Native
{
	public class ParseChatText : ParseBase<AccessibilitySettings>
	{
		public ParseChatText(IAccessibilityLocalization aLocalization)
			: base(aLocalization)
		{
		}

		public void Click(int aId)
		{
		}

		protected override GameObject GetGameobject(AccessibilitySettings aItem)
		{
			return aItem.gameObject;
		}

		protected override void Add(AccessibilitySettings aItem, int aId, bool isVoiceOnly)
		{
			AccessibilitySettings component = aItem.GetComponent<AccessibilitySettings>();
			string tokenText = GetTokenText(component);
			Rect rectInPhysicalScreenSpace = Util.GetRectInPhysicalScreenSpace(aItem.GetComponent<RectTransform>());
			if (isVoiceOnly)
			{
				MonoSingleton<NativeAccessiblityManager>.Instance.Native.RenderText(aId, rectInPhysicalScreenSpace, tokenText);
			}
			else
			{
				MonoSingleton<NativeAccessiblityManager>.Instance.Native.RenderButton(aId, rectInPhysicalScreenSpace, tokenText);
			}
		}

		protected override void Update(AccessibilitySettings aItem)
		{
			AccessibilitySettings component = aItem.GetComponent<AccessibilitySettings>();
			Rect rectInPhysicalScreenSpace = Util.GetRectInPhysicalScreenSpace(aItem.GetComponent<RectTransform>());
			MonoSingleton<NativeAccessiblityManager>.Instance.Native.UpdateView(aItem.GetEntityId().GetHashCode(), rectInPhysicalScreenSpace, GetTokenText(component));
		}

		protected override void Remove(int aId)
		{
			MonoSingleton<NativeAccessiblityManager>.Instance.Native.RemoveView(aId);
		}
	}
}
