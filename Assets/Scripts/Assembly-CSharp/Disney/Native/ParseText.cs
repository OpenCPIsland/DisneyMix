using Mix;
using UnityEngine;
using UnityEngine.UI;

namespace Disney.Native
{
	public class ParseText : ParseBase<Text>
	{
		public ParseText(IAccessibilityLocalization aLocalization)
			: base(aLocalization)
		{
		}

		public void Click(int aId)
		{
		}

		protected override GameObject GetGameobject(Text aItem)
		{
			return aItem.gameObject;
		}

		protected override void Add(Text aItem, int aId, bool isVoiceOnly)
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

		protected override void Update(Text aItem)
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
