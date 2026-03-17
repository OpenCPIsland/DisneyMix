using System;
using Disney.Mix.SDK.Internal.GuestControllerDomain;

namespace Disney.Mix.SDK.Internal
{
	public static class ParentalApprovalEmailSender
	{
		public static void SendParentalApprovalEmail(AbstractLogger logger, IGuestControllerClient guestControllerClient, Action<ISendParentalApprovalEmailResult> callback)
		{
			if (callback != null)
			{
				callback(new SendParentalApprovalEmailResult(false));
			}
		}	
	}
}
