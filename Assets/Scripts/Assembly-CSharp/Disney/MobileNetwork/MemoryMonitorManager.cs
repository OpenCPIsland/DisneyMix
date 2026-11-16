using UnityEngine;

namespace Disney.MobileNetwork
{
	public class MemoryMonitorManager : MonoBehaviour
	{
		public void Awake()
		{
			Service.Set(this);
			Init();
			Object.DontDestroyOnLoad(base.gameObject);
		}

		protected virtual void Init()
		{
		}

		public virtual ulong GetHeapSize()
		{
			return 10uL;
		}

		public virtual ulong GetFreeBytes()
		{
			return 1000000000uL;
		}

		public virtual ulong GetTotalBytes()
		{
			return 1000000000uL;
		}

		public virtual float GetBatteryPercent()
		{
			return 1f;
		}

		public virtual ulong GetProcessUsedBytes()
		{
			return 0uL;
		}
	}
}
