using System;
using System.Collections.Generic;
using Disney.Mix.SDK;

namespace Mix.Session.Local.Messages
{
	public class LocalGameStateMessage : LocalChatMessage<IGameStateMessage>, ILocalMessageReference<IGameStateMessage>, IChatMessage, IGameStateMessage, IReadable
	{
		private string gameName;

		private Dictionary<string, object> payload;

		public string GameName
		{
			get
			{
				return gameName;
			}
		}

		public IGameStateMessage SdkReference { get; set; }

		public Dictionary<string, object> State
		{
			get
			{
				return payload;
			}
		}

		 bool IReadable.Read
		{
			get
			{
				return base.Read;
			}
			set
			{
				base.Read = value;
			}
		}

		public event EventHandler<AbstractChatThreadGameStateMessageUpdatedEventArgs> OnStateUpdated
		{
			add { }
			remove { }
		}

		public LocalGameStateMessage(string aGameName, Dictionary<string, object> aPayload, string senderId)
		{
			base.SenderId = senderId;
			base.TimeSent = DateTime.UtcNow;
			base.Id = LocalUserWrapper.fakeObjectIds.ToString();
			LocalUserWrapper.fakeObjectIds++;
			base.Sent = false;
			base.Read = false;
			gameName = aGameName;
			payload = aPayload;
		}

		public LocalGameStateMessage(string aGameName, Dictionary<string, object> aPayload, string senderId, DateTime timesent, bool sent, bool read, string id)
		{
			gameName = aGameName;
			payload = aPayload;
			base.SenderId = senderId;
			base.TimeSent = timesent;
			base.Id = id;
			base.Sent = sent;
			base.Read = read;
		}

		public override bool Equals(IChatMessage other)
		{
			if (other != null && other is LocalGameStateMessage && SenderId == other.SenderId && TimeSent.Equals(other.TimeSent))
			{
				return true;
			}
			return false;
		}
	}
}
