using System;
using System.Collections.Generic;
using System.Linq;
using Disney.Mix.SDK;
using LitJson;
using Mix.Connectivity;
using Mix.DeviceDb;
using Mix.FakeFriend.Messages;
using Mix.FakeFriend.Results;
using Mix.Session;
using Mix.Session.Local;
using Mix.Session.Local.Messages;
using Mix.Session.Local.Thread;

namespace Mix.FakeFriend.Datatypes
{
	public class FakeThread : IChatThread, IOneOnOneChatThread
	{
		private LocalUserWrapper User;

		private LocalTrustLevel trustLevel;

		private LocalThreadNickname nickname;

		private IEnumerable<IRemoteChatMember> members;

		private List<IChatMessage> messages;

		private sealed class FakeUpdateGameStateMessageResult : IUpdateGameStateMessageResult
		{
			public bool Success { get; private set; }

			public string Result { get; private set; }

			public FakeUpdateGameStateMessageResult(bool success, string result)
			{
				Success = success;
				Result = result;
			}
		}

		public bool IsOfflineCharacter { get; private set; }

		private string CharacterName
		{
			get
			{
				FakeFriendData character = members.FirstOrDefault() as FakeFriendData;
				return character != null ? character.FirstName : string.Empty;
			}
		}

		private SdkActions actionGenerator = new SdkActions();

		public IChatThreadTrustLevel TrustLevel
		{
			get
			{
				return trustLevel;
			}
		}

		public IEnumerable<IChatMessage> ChatMessages
		{
			get
			{
				return messages;
			}
		}

		public IEnumerable<IRemoteChatMember> RemoteMembers
		{
			get
			{
				return members;
			}
		}

		public IEnumerable<IRemoteChatMember> FormerRemoteMembers
		{
			get
			{
				return new List<IRemoteChatMember>();
			}
		}

		public IChatThreadNickname Nickname
		{
			get
			{
				return nickname;
			}
		}

		public string Id
		{
			get
			{
				return IsOfflineCharacter ? "OfflineCharacterThread:" + members.First().Id : "FakeThreadId";
			}
		}

		public static bool IsOfflineCharacterMessageId(string messageId)
		{
			return !string.IsNullOrEmpty(messageId) && messageId.StartsWith("OfflineCharacterThread:", StringComparison.Ordinal);
		}

		public bool OwnsOfflineCharacterMessageId(string messageId)
		{
			return IsOfflineCharacter && !string.IsNullOrEmpty(messageId) && messageId.StartsWith(Id + "|", StringComparison.Ordinal);
		}

		public uint UnreadMessageCount { get; private set; }

		public event EventHandler<AbstractChatThreadTrustStatusChangedEventArgs> OnTrustStatusChanged = delegate
		{
		};

		public event EventHandler<AbstractChatThreadNicknameChangedEventArgs> OnNicknameChanged = delegate
		{
		};

		public event EventHandler<AbstractChatThreadMemberAddedEventArgs> OnMemberAdded = delegate
		{
		};

		public event EventHandler<AbstractChatThreadMemberRemovedEventArgs> OnMemberRemoved = delegate
		{
		};

		public event EventHandler<AbstractChatThreadMemberAddedMessageAddedEventArgs> OnMemberAddedMessageAdded = delegate
		{
		};

		public event EventHandler<AbstractChatThreadMemberRemovedMessageAddedEventArgs> OnMemberRemovedMessageAdded = delegate
		{
		};

		public event EventHandler<AbstractChatThreadStickerMessageAddedEventArgs> OnStickerMessageAdded = delegate
		{
		};

		public event EventHandler<AbstractChatThreadTextMessageAddedEventArgs> OnTextMessageAdded = delegate
		{
		};

		public event EventHandler<AbstractChatThreadGameStateMessageAddedEventArgs> OnGameStateMessageAdded = delegate
		{
		};

		public event EventHandler<AbstractChatThreadGameEventMessageAddedEventArgs> OnGameEventMessageAdded = delegate
		{
		};

		public event EventHandler<AbstractChatThreadGameStateMessageUpdatedEventArgs> OnGameStateMessageUpdated = delegate
		{
		};

		public event EventHandler<AbstractChatThreadGagMessageAddedEventArgs> OnGagMessageAdded = delegate
		{
		};

		public event EventHandler<AbstractChatThreadPhotoMessageAddedEventArgs> OnPhotoMessageAdded = delegate
		{
		};

		public event EventHandler<AbstractChatThreadVideoMessageAddedEventArgs> OnVideoMessageAdded = delegate
		{
		};

		public event EventHandler<AbstractUnreadMessageCountChangedEventArgs> OnUnreadMessageCountChanged = delegate
		{
		};

		public event EventHandler<AbstractChatHistoryClearedEventArgs> OnChatHistoryCleared = delegate
		{
		};

		public FakeThread(LocalUserWrapper user, IEnumerable<IRemoteChatMember> members, bool isOfflineCharacter = false)
		{
			User = user;
			UnreadMessageCount = 0u;
			IsOfflineCharacter = isOfflineCharacter;
			trustLevel = new LocalTrustLevel();
			trustLevel.AllMembersTrustEachOther = true;
			nickname = new LocalThreadNickname();
			this.members = members;
			messages = new List<IChatMessage>();
		}

		public IChatMessageRetriever CreateChatMessageRetriever(int maximumMessageCount)
		{
			return new LocalChatMessageRetriever(maximumMessageCount, messages);
		}

		public IChatThreadNickname SetNickname(string nickname, Action<ISetChatThreadNicknameResult> callback)
		{
			this.nickname.Nickname = nickname;
			callback(null);
			return this.nickname;
		}

		public void RemoveNickname(Action<IRemoveChatThreadNicknameResult> callback)
		{
			callback(null);
		}

		public IStickerMessage SendStickerMessage(string contentId, Action<ISendStickerMessageResult> callback)
		{
			LocalStickerMessage fakeMsg = new LocalStickerMessage(contentId, User.Id);
			fakeMsg.Read = true;
			if (IsOfflineCharacter)
			{
				fakeMsg.Id = CreateOfflineCharacterMessageId();
			}
			messages.Add(fakeMsg);
			HandleMessage(fakeMsg, delegate(bool r)
			{
				callback(new FakeSendStickerMessageResult(r));
				Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi.AddSticker(false, fakeMsg.TimeSent.Ticks, r, fakeMsg.Read, fakeMsg.Id, contentId);
			});
			return fakeMsg;
		}

		public IGagMessage SendGagMessage(string contentId, IRemoteChatMember target, Action<ISendGagMessageResult> callback)
		{
			LocalGagMessage fakeMsg = new LocalGagMessage(contentId, target.Id, User.Id);
			fakeMsg.Read = true;
			if (IsOfflineCharacter)
			{
				fakeMsg.Id = CreateOfflineCharacterMessageId();
			}
			messages.Add(fakeMsg);
			HandleMessage(fakeMsg, delegate(bool r)
			{
				callback(new FakeSendGagMessageResult(r));
				Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi.AddGag(false, fakeMsg.TimeSent.Ticks, r, fakeMsg.Read, fakeMsg.Id, contentId);
			});
			return fakeMsg;
		}

		public IGagMessage SendGagMessage(string contentId, IFriend target, Action<ISendGagMessageResult> callback)
		{
			LocalGagMessage fakeMsg = new LocalGagMessage(contentId, target.Id, User.Id);
			fakeMsg.Read = true;
			if (IsOfflineCharacter)
			{
				fakeMsg.Id = CreateOfflineCharacterMessageId();
			}
			messages.Add(fakeMsg);
			HandleMessage(fakeMsg, delegate(bool r)
			{
				callback(new FakeSendGagMessageResult(r));
				Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi.AddGag(false, fakeMsg.TimeSent.Ticks, r, fakeMsg.Read, fakeMsg.Id, contentId);
			});
			return fakeMsg;
		}

		public IGameStateMessage SendGameStateMessage(string aGameName, Dictionary<string, object> aPayload, Action<ISendGameStateMessageResult> callback)
		{
			LocalGameStateMessage fakeMsg = new LocalGameStateMessage(aGameName, aPayload, User.Id);
			fakeMsg.Read = true;
			if (IsOfflineCharacter)
			{
				fakeMsg.Id = CreateOfflineCharacterMessageId();
			}
			messages.Add(fakeMsg);
			HandleMessage(fakeMsg, delegate(bool r)
			{
				callback(new FakeSendGameStateMessageResult(r));
				string aGameData = JsonMapper.ToJson(aPayload);
				Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi.AddGameState(false, fakeMsg.TimeSent.Ticks, r, fakeMsg.Read, fakeMsg.Id, aGameName, aGameData);
			});
			return fakeMsg;
		}

		public void UpdateGameStateMessage(IGameStateMessage gameStateMessage, Dictionary<string, object> payload, Action<IUpdateGameStateMessageResult> callback)
		{
			if (IsOfflineCharacter)
			{
				LocalGameStateMessage localMessage = gameStateMessage as LocalGameStateMessage;
				if (localMessage == null || localMessage.State == null || payload == null)
				{
					callback(new FakeUpdateGameStateMessageResult(false, null));
					return;
				}
				foreach (KeyValuePair<string, object> stateEntry in payload)
				{
					localMessage.State[stateEntry.Key] = stateEntry.Value;
				}
				string gameData = JsonMapper.ToJson(localMessage.State);
				Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi.AddGameState(false, localMessage.TimeSent.Ticks, localMessage.Sent, localMessage.Read, localMessage.Id, localMessage.GameName, gameData);
				callback(new FakeUpdateGameStateMessageResult(true, gameData));
				return;
			}
			FakeGameEventMessage item = new FakeGameEventMessage(User.Id);
			messages.Add(item);
		}

		public IPhotoMessage SendPhotoMessage(string photoPath, PhotoEncoding encoding, int width, int height, Action<ISendPhotoMessageResult> callback)
		{
			LocalPhotoMessage fakeMsg = new LocalPhotoMessage(photoPath, encoding, width, height, User.Id);
			fakeMsg.Read = true;
			messages.Add(fakeMsg);
			HandleMessage(fakeMsg, delegate(bool r)
			{
				callback(new FakeSendPhotoMessageResult(r));
				Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi.AddPhoto(false, fakeMsg.TimeSent.Ticks, r, fakeMsg.Read, fakeMsg.Id, photoPath, encoding, width, height);
			});
			return fakeMsg;
		}

		public ITextMessage SendTextMessage(string text, Action<ISendTextMessageResult> callback)
		{
			LocalTextMessage fakeMsg = new LocalTextMessage(User.Id, text);
			fakeMsg.Read = true;
			if (IsOfflineCharacter)
			{
				fakeMsg.Id = CreateOfflineCharacterMessageId();
			}
			messages.Add(fakeMsg);
			if (IsOfflineCharacter)
			{
				PersistOfflineCharacterTextMessage(fakeMsg, false);
				MonoSingleton<FakeFriendManager>.Instance.ProcessOfflineCharacterMessage(this, fakeMsg, callback);
				return fakeMsg;
			}
			HandleMessage(fakeMsg, delegate(bool r)
			{
				callback(new FakeSendTextMessageResult(r));
				Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi.AddText(false, fakeMsg.TimeSent.Ticks, r, fakeMsg.Read, fakeMsg.Id, fakeMsg.Text);
				this.OnTextMessageAdded(this, new FakeAddTextMessageArgs(fakeMsg));
			});
			return fakeMsg;
		}

		public void NotifyOfflineTextMessageSent(LocalTextMessage message)
		{
			PersistOfflineCharacterTextMessage(message, false);
			this.OnTextMessageAdded(this, new FakeAddTextMessageArgs(message));
		}

		public void AddOfflineCharacterReply(string text)
		{
			LocalTextMessage message = new LocalTextMessage(members.First().Id, text);
			message.Id = CreateOfflineCharacterMessageId();
			message.Read = true;
			message.Sent = true;
			messages.Add(message);
			PersistOfflineCharacterTextMessage(message, true);
			this.OnTextMessageAdded(this, new FakeAddTextMessageArgs(message));
		}

		private string CreateOfflineCharacterMessageId()
		{
			return Id + "|" + Guid.NewGuid().ToString("N");
		}

		private void PersistOfflineCharacterTextMessage(LocalTextMessage message, bool isFromCharacter)
		{
			Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi.AddText(
				isFromCharacter,
				message.TimeSent.Ticks,
				message.Sent,
				message.Read,
				message.Id,
				message.Text);
		}

		public IVideoMessage SendVideoMessage(string videoPath, VideoFormat format, int bitrate, int duration, int videoWidth, int videoHeight, string thumbnailPath, PhotoEncoding thumbnailEncoding, int thumbnailWidth, int thumbnailHeight, Action<ISendVideoMessageResult> callback)
		{
			LocalVideoMessage fakeMsg = new LocalVideoMessage(videoPath, format, bitrate, duration, videoWidth, videoHeight, thumbnailPath, thumbnailEncoding, thumbnailWidth, thumbnailHeight, User.Id);
			fakeMsg.Read = true;
			messages.Add(fakeMsg);
			HandleMessage(fakeMsg, delegate(bool r)
			{
				callback(new FakeSendVideoMessageResult(r));
				Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi.AddVideo(false, fakeMsg.TimeSent.Ticks, r, fakeMsg.Read, fakeMsg.Id, videoPath, format, bitrate, duration, videoWidth, videoHeight, thumbnailPath, thumbnailEncoding, thumbnailWidth, thumbnailHeight);
			});
			return fakeMsg;
		}

		public void ResendChatMessage(IChatMessage message, Action<IResendChatMessageResult> callback)
		{
			if (!message.Sent && message is LocalTextMessage)
			{
				LocalTextMessage txtMessage = (LocalTextMessage)message;
				User.ModerateText(txtMessage.Text, true, actionGenerator.CreateAction(delegate(ITextModerationResult r)
				{
					if (r.Success)
					{
						txtMessage.Sent = true;
						txtMessage.Text = r.ModeratedText;
						callback(new FakeResendChatMessageResult(true));
						MonoSingleton<FakeFriendManager>.Instance.MessageSent(txtMessage);
					}
					else
					{
						callback(new FakeResendChatMessageResult(false));
					}
				}));
			}
			else if (MonoSingleton<ConnectionManager>.Instance.IsConnected)
			{
				if (message is LocalStickerMessage)
				{
					((LocalStickerMessage)message).Sent = true;
				}
				else if (message is LocalGagMessage)
				{
					((LocalGagMessage)message).Sent = true;
				}
				else if (message is LocalGameStateMessage)
				{
					((LocalGameStateMessage)message).Sent = true;
				}
				else if (message is LocalPhotoMessage)
				{
					((LocalPhotoMessage)message).Sent = true;
				}
				else if (message is LocalVideoMessage)
				{
					((LocalVideoMessage)message).Sent = true;
				}
				MonoSingleton<FakeFriendManager>.Instance.MessageSent(message);
				callback(new FakeResendChatMessageResult(true));
			}
			else
			{
				callback(new FakeResendChatMessageResult(false));
			}
		}

		public void FakeMessageIncoming(IChatMessage message)
		{
			HandleFakeMessage(message, true);
		}

		private void UpdateFakeMessage(IChatMessage message)
		{
			HandleFakeMessage(message, false);
		}

		private void HandleFakeMessage(IChatMessage message, bool isNew)
		{
			Action action = delegate
			{
			};
			if (message is LocalTextMessage)
			{
				LocalTextMessage localTextMessage = message as LocalTextMessage;
				localTextMessage.Sent = true;
				Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi.AddText(true, localTextMessage.TimeSent.Ticks, true, localTextMessage.Read, localTextMessage.Id, localTextMessage.Text);
				action = delegate
				{
					this.OnTextMessageAdded(this, new FakeAddTextMessageArgs((ITextMessage)message));
				};
			}
			else if (message is LocalStickerMessage)
			{
				LocalStickerMessage localStickerMessage = message as LocalStickerMessage;
				localStickerMessage.Sent = true;
				Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi.AddSticker(true, localStickerMessage.TimeSent.Ticks, true, localStickerMessage.Read, localStickerMessage.Id, localStickerMessage.ContentId);
				action = delegate
				{
					this.OnStickerMessageAdded(this, new FakeAddStickerMessageArgs((IStickerMessage)message));
				};
			}
			else if (message is LocalGagMessage)
			{
				((LocalGagMessage)message).TargetUserId = MixSession.User.Id;
				LocalGagMessage localGagMessage = message as LocalGagMessage;
				localGagMessage.Sent = true;
				Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi.AddGag(true, localGagMessage.TimeSent.Ticks, true, localGagMessage.Read, localGagMessage.Id, localGagMessage.ContentId);
				action = delegate
				{
					this.OnGagMessageAdded(this, new FakeAddGagMessageArgs((IGagMessage)message));
				};
			}
			else if (message is LocalPhotoMessage)
			{
				LocalPhotoMessage localPhotoMessage = message as LocalPhotoMessage;
				localPhotoMessage.Sent = true;
				Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi.AddPhoto(true, localPhotoMessage.TimeSent.Ticks, true, localPhotoMessage.Read, localPhotoMessage.Id, localPhotoMessage.OAMessageId);
				action = delegate
				{
					this.OnPhotoMessageAdded(this, new FakeAddPhotoMessageArgs((IPhotoMessage)message));
				};
			}
			else if (message is LocalVideoMessage)
			{
				LocalVideoMessage localVideoMessage = message as LocalVideoMessage;
				localVideoMessage.Sent = true;
				Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi.AddVideo(true, localVideoMessage.TimeSent.Ticks, true, localVideoMessage.Read, localVideoMessage.Id, localVideoMessage.OAMessageId);
				action = delegate
				{
					this.OnVideoMessageAdded(this, new FakeAddVideoMessageArgs((IVideoMessage)message));
				};
			}
			if (isNew)
			{
				messages.Add(message);
				if (message is IReadable && !((IReadable)message).Read)
				{
					UnreadMessageCount += 1;
				}
				action();
			}
		}

		public void AddExistingMessage(IChatMessage message)
		{
			if (message is IReadable && !((IReadable)message).Read)
			{
				UnreadMessageCount += 1;
			}
			messages.Add(message);
		}

		public void SortMessagesByTime()
		{
			messages.Sort((IChatMessage m1, IChatMessage m2) => m1.TimeSent.CompareTo(m2.TimeSent));
		}

		public void HandleMessage(IChatMessage message, Action<bool> callback)
		{
			MonoSingleton<FakeFriendManager>.Instance.ProcessChatMessage(this, message, callback);
		}

		public void GetGameStateMessage(IGameEventMessage message, Action<IGetGameStateMessageResult> callback)
		{
			IChatMessage chatMessage = messages.FirstOrDefault((IChatMessage m) => message.Id == m.Id);
			callback(new FakeGetGameStateMessageResult(chatMessage != null && chatMessage is IGameEventMessage));
		}

		public void ModerateTextMessage(string text, Action<ITextModerationResult> callback)
		{
			User.ModerateText(text, true, actionGenerator.CreateAction(delegate(ITextModerationResult r)
			{
				callback(new FakeTextModerationResult(r.Success, r.IsModerated, r.ModeratedText));
			}));
		}

		public void ClearChatHistory(Action<IClearChatHistoryResult> callback)
		{
		}

		public void ClearUnreadMessageCount(Action<IClearUnreadMessageCountResult> callback)
		{
			foreach (IChatMessage message in messages)
			{
				if (message is IReadable)
				{
					IReadable readable = (IReadable)message;
					if (!readable.Read)
					{
						readable.Read = true;
						UpdateFakeMessage(message);
					}
				}
			}
			UnreadMessageCount = 0u;
			callback(new FakeClearUnreadMessageCountResult(true));
			this.OnUnreadMessageCountChanged(this, new FakeUnreadMessageCountChangedEventArgs());
		}

		public bool Equals(IOneOnOneChatThread other)
		{
			if (other == null)
			{
				return false;
			}
			if (!(other is FakeThread))
			{
				return false;
			}
			return other.Id == Id;
		}
	}
}
