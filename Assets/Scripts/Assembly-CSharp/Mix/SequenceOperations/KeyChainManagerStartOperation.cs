using Disney.MobileNetwork;
using UnityEngine;

namespace Mix.SequenceOperations
{
	public class KeyChainManagerStartOperation : SequenceOperation
	{
		public KeyChainManagerStartOperation(IOperationCompleteHandler aCaller)
			: base(aCaller)
		{
		}

		public override void StartOperation()
		{
			BaseStartOperation();
			InitKeyChainPlugin();
			finish(OperationStatus.STATUS_SUCCESSFUL);
		}

		private void InitKeyChainPlugin()
		{
			KeyChainManager instance = KeyChainManager.Instance;
		}
	}
}
