#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX_ARM || UNITY_EDITOR_OSX_ARM
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Disney.MobileNetwork
{
	public class MemoryMonitorWindowsManager : MemoryMonitorManager
	{
		public static bool Enabled = true;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
		[DllImport("MemoryMonitorWindows")]
		private static extern ulong _getProcessUsedBytes();
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
		[DllImport("libMemoryMonitorLinux", EntryPoint = "get_process_used_bytes")]
		private static extern ulong _getProcessUsedBytes();
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX_ARM || UNITY_EDITOR_OSX_ARM
		[DllImport("libMemoryMonitorOSX", EntryPoint = "get_process_used_bytes")]
		private static extern ulong _getProcessUsedBytes();
#endif

		protected override void Init()
		{
			try
			{
				GetProcessUsedBytes();
			}
			catch (Exception exception)
			{
				Enabled = false;
				Debug.LogException(exception);
			}
		}

		public override ulong GetProcessUsedBytes()
		{
			return Enabled ? _getProcessUsedBytes() : base.GetProcessUsedBytes();
		}
	}
}
#endif
