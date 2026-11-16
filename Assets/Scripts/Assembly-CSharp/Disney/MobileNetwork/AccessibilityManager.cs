using System;
using UnityEngine;

namespace Disney.MobileNetwork
{
	public class AccessibilityManager : MonoBehaviour, IInitializable, IPlugin
	{
		private LoggerHelper m_logger = new LoggerHelper();

		public static AccessibilityManager Instance;

		public LoggerHelper Logger
		{
			get
			{
				return m_logger;
			}
		}

		public static event Action<string> FontSizeChangedEvent
		{
			add { }
			remove { }
		}

		public void SetLogger(LoggerHelper.LoggerDelegate loggerMessageHandler)
		{
			m_logger.LogMessageHandler += loggerMessageHandler;
		}

		public virtual void Init()
		{
		}

		private void Awake()
		{
			Instance = this;
			base.name = "AccessibilityManager";
			Init();
		}

		public virtual float GetAdjustedFontSize(float aFontSize)
		{
			return aFontSize;
		}

		public virtual bool IsOtherAudioPlaying()
		{
			return false;
		}

		public virtual Vector2 GetScreenSize()
		{
			return new Vector2(Display.displays[0].systemWidth, Display.displays[0].systemHeight);
		}

		public virtual Vector2 GetScreenSizeWithSoftKeys()
		{
			return new Vector2(Display.displays[0].systemWidth, Display.displays[0].systemHeight);
		}

		public virtual void Speak(string aTextToSpeak)
		{
		}

		public virtual int GetStatusBarHeight()
		{
			return 0;
		}

		public virtual string GetProfileId()
		{
			return string.Empty;
		}
	}
}
