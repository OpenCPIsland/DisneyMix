using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Disney.Mix.SDK;
using Mix;
using Mix.Assets;
using Mix.Connectivity;
using Mix.Data;
using Mix.DeviceDb;
using Mix.Entitlements;
using Mix.FakeFriend.Datatypes;
using Mix.FakeFriend.Results;
using Mix.Localization;
using Mix.Session;
using Mix.Session.Extensions;
using Mix.Session.Local.Messages;
using Mix.Session.Local.Thread;
using UnityEngine;

public class FakeFriendManager : MonoSingleton<FakeFriendManager>
{
	private const string KEY_OA_PREFIX = "fakefriend_oa_messageid_prefix";

	private const string KEY_SCRIPT_DONE_TIME = "fakefriend_script_done_time";

	private const string KEY_TIME_INITED = "fakefriend_time_inited";

	private const string KEY_VISITED_CHAT = "fakefriend_visited_chat";

	private const string MESSAGE_PREFIX = "fakefriend_message_";

	private const string LAST_MESSAGE = "last_message";

	public static string OAID = "OA-72844169130089556";

	private IOfficialAccountChatThread officalAccountThread;

	private IOfficialAccount officalAccount;

	private SdkEvents eventGenerator = new SdkEvents();

	private SdkActions actionGenerator = new SdkActions();

	private bool officalAccountListenersAdded;

	public static string TYPE_ADD_FRIEND = "add_friend";

	public static string TYPE_FRIEND_BUTTON = "friend_button";

	public static string TYPE_CHAT_BACK = "chat_back";

	public static string TYPE_CHAT_INPUT = "chat_input";

	public static string TYPE_MEDIA_STICKER = "media_sticker";

	public static string TYPE_MEDIA_GAG = "media_gag";

	public static string TYPE_MEDIA_GAME = "media_game";

	public static string TYPE_MEDIA_UGC = "media_ugc";

	public FakeFriendData fakeFriend;

	public LocalChatMember fakeFriendAsChatMember;

	public FakeThread fakeThread;

	private IKeyValDatabaseApi databaseApi;

	private int pendingMessages;

	private int lastMessage;

	private Dictionary<string, bool> databaseCache = new Dictionary<string, bool>();

	private Dictionary<string, bool> messagesSent = new Dictionary<string, bool>();

	private List<int> messagesPending = new List<int>();

	private readonly Dictionary<string, int> offlineKnockKnockStages = new Dictionary<string, int>();

	private static readonly string[] offlineCharacterJokes =
	{
		"Why did the fish bring a backpack? For its school of fish!",
		"Why did the star pack a map? It wanted to explore the universe!",
		"What do you call a snowman on a warm day? A puddle with a plan.",
		"Why did the octopus bring a pencil? It had a lot of points to make."
	};

	private static readonly Dictionary<string, string[]> offlineCharacterFacts = new Dictionary<string, string[]>
	{
		{ "Baymax", new[] { "I'm a healthcare companion robot designed to help people.", "Tadashi Hamada created me to provide medical care." } },
		{ "Cheshire", new[] { "I'm a cat from Wonderland who can disappear and reappear.", "My wide grin can remain visible even when the rest of me disappears." } },
		{ "Chopper", new[] { "I'm an astromech droid who travels with the Ghost crew.", "My designation is C1-10P." } },
		{ "Disgust", new[] { "I'm one of Riley's emotions.", "My job is to help Riley avoid things she finds unpleasant." } },
		{ "Dory", new[] { "I'm a blue tang fish.", "Blue tangs have bright yellow tails." } },
		{ "Fear", new[] { "I'm one of Riley's emotions.", "I try to help Riley stay safe when something seems risky." } },
		{ "Flash", new[] { "I'm a sloth who works at the Zootopia DMV.", "Sloths are mammals known for moving slowly." } },
		{ "Hank", new[] { "I'm an octopus with seven tentacles.", "Real octopuses can change their appearance to camouflage." } },
		{ "Joy", new[] { "I'm one of Riley's emotions.", "I help Riley notice and enjoy happy moments." } },
		{ "Judy", new[] { "I'm a rabbit who became a police officer in Zootopia.", "I grew up in Bunnyburrow." } },
		{ "Marlin", new[] { "I'm a clownfish and Nemo's dad.", "Clownfish can live among sea anemones." } },
		{ "Mike", new[] { "I'm a one eyed monster who works at Monsters, Inc.", "My best friend and coworker is Sulley." } },
		{ "Nemo", new[] { "I'm a clownfish.", "One of my fins is smaller than the other, so my dad calls it my lucky fin." } },
		{ "Nick", new[] { "I'm a fox who became a police officer in Zootopia.", "I used to work as a hustler before joining the Zootopia Police Department." } },
		{ "Olaf", new[] { "I'm a snowman who is curious about summer.", "Elsa brought me to life with her magic." } },
		{ "Sabine", new[] { "I'm a Mandalorian and an artist.", "I use colorful designs to decorate my armor." } },
		{ "Sadness", new[] { "I'm one of Riley's emotions.", "Sadness can help people understand one another and ask for support." } },
		{ "Sally", new[] { "I'm a rag doll who sews patches onto myself.", "I live in Halloween Town." } },
		{ "Skellington", new[] { "I'm the Pumpkin King of Halloween Town.", "I'm known for planning Halloween celebrations." } },
		{ "Stitch", new[] { "I'm Experiment 626, an alien experiment.", "I learned to care about the people who became my family." } },
		{ "Stormtrooper", new[] { "Stormtroopers wear white armor and serve the Galactic Empire.", "My armor is designed to protect me during missions." } },
		{ "Sulley", new[] { "I'm a large blue monster who works at Monsters, Inc.", "My best friend and coworker is Mike Wazowski." } },
		{ "Zeb", new[] { "I'm a Lasat and a member of the Ghost crew.", "I was once part of the Lasan Honor Guard." } }
	};

	private static readonly Dictionary<string, string[]> offlineCharacterVocabulary = new Dictionary<string, string[]>
	{
		{ "greeting", new[] { "hello", "hey", "hiya", "howdy", "greetings", "what's up", "good morning", "good afternoon", "good evening" } },
		{ "farewell", new[] { "bye", "goodbye", "good bye", "see you later", "see ya", "catch you later", "talk to you later", "gotta go", "need to go", "have to go", "good night", "goodnight" } },
		{ "thanks", new[] { "thanks", "thanks a lot", "thank you", "thank you so much", "many thanks", "thx", "appreciate it", "appreciate you", "much appreciated", "that helped" } },
		{ "welcome", new[] { "you're welcome", "you are welcome", "no problem", "no worries", "anytime" } },
		{ "identity", new[] { "what is your name", "what's your name", "who are you" } },
		{ "how_are_you", new[] { "how are you", "how are you doing", "how's it going", "how are things" } },
		{ "favorites", new[] { "what do you like", "what do you enjoy", "what's your favorite", "what is your favorite", "favourite", "hobby", "hobbies", "what do you do for fun" } },
		{ "apology", new[] { "sorry", "my bad", "oops" } },
		{ "compliment", new[] { "you're nice", "you are nice", "you're cool", "you are cool", "i like you", "i like chatting with you" } },
		{ "bored", new[] { "i'm bored", "im bored", "bored", "nothing to do", "what should we do", "let's play" } },
		{ "unsure", new[] { "i don't know", "i dont know", "idk", "not sure" } },
		{ "joke", new[] { "tell me a joke", "tell a joke", "joke", "make me laugh", "something funny", "pun", "riddle" } },
		{ "fact", new[] { "fact", "fun fact", "tell me something about", "did you know", "trivia" } },
		{ "knock_knock", new[] { "knock knock", "knock-knock" } },
		{ "knock_knock_request", new[] { "tell me a knock knock joke", "tell a knock knock joke", "knock knock joke" } }
	};

	private Dictionary<string, string> messages = new Dictionary<string, string>
	{
		{
			"fakefriend_message_" + 1,
			"customtokens.fakefriend.message_1"
		},
		{
			"fakefriend_message_" + 2,
			"customtokens.fakefriend.message_2"
		},
		{
			"fakefriend_message_" + 3,
			"customtokens.fakefriend.message_3"
		},
		{
			"fakefriend_message_" + 4,
			"customtokens.fakefriend.message_4"
		},
		{
			"fakefriend_message_" + 5,
			"customtokens.fakefriend.message_5"
		},
		{
			"fakefriend_message_" + 6,
			"customtokens.fakefriend.message_6"
		},
		{
			"fakefriend_message_" + 7,
			"customtokens.fakefriend.message_7"
		},
		{
			"fakefriend_message_" + 8,
			"customtokens.fakefriend.message_8"
		},
		{
			"fakefriend_message_" + 9,
			"customtokens.fakefriend.message_9"
		},
		{
			"fakefriend_message_" + 10,
			"customtokens.fakefriend.message_10"
		},
		{
			"fakefriend_message_" + 11,
			"customtokens.fakefriend.message_11"
		},
		{
			"fakefriend_message_" + 12,
			"customtokens.fakefriend.message_12"
		},
		{
			"fakefriend_message_" + 14,
			"customtokens.fakefriend.message_14"
		},
		{
			"fakefriend_message_" + 15,
			"customtokens.fakefriend.message_15"
		},
		{
			"fakefriend_message_" + 16,
			"customtokens.fakefriend.message_16"
		},
		{
			"fakefriend_message_" + 17,
			"customtokens.fakefriend.message_17"
		},
		{
			"fakefriend_message_" + 20,
			"customtokens.fakefriend.message_20"
		},
		{
			"fakefriend_message_" + 21,
			"customtokens.fakefriend.message_21"
		},
		{
			"fakefriend_message_" + 22,
			"customtokens.fakefriend.message_22"
		},
		{
			"fakefriend_message_" + 23,
			"customtokens.fakefriend.message_23"
		},
		{
			"fakefriend_message_" + 24,
			"customtokens.fakefriend.message_24"
		},
		{
			"fakefriend_message_" + 25,
			"customtokens.fakefriend.message_25"
		},
		{
			"fakefriend_message_" + 30,
			"customtokens.fakefriend.message_30"
		},
		{
			"fakefriend_message_" + 31,
			"customtokens.fakefriend.message_31"
		},
		{
			"fakefriend_message_" + 32,
			"customtokens.fakefriend.message_32"
		},
		{
			"fakefriend_message_" + 33,
			"customtokens.fakefriend.message_33"
		},
		{
			"fakefriend_message_" + 34,
			"customtokens.fakefriend.message_34"
		},
		{
			"fakefriend_message_" + 35,
			"customtokens.fakefriend.message_35"
		},
		{
			"fakefriend_message_" + 36,
			"customtokens.fakefriend.message_36"
		},
		{
			"fakefriend_message_" + 37,
			"customtokens.fakefriend.message_37"
		},
		{
			"fakefriend_message_" + 38,
			"customtokens.fakefriend.message_38"
		},
		{
			"fakefriend_message_" + 39,
			"customtokens.fakefriend.message_39"
		},
		{
			"fakefriend_message_" + 50,
			"customtokens.fakefriend.message_50"
		},
		{
			"fakefriend_message_" + 51,
			"customtokens.fakefriend.message_51"
		},
		{
			"fakefriend_message_" + 52,
			"customtokens.fakefriend.message_52"
		},
		{
			"fakefriend_message_" + 53,
			"customtokens.fakefriend.message_53"
		},
		{
			"fakefriend_message_" + 54,
			"customtokens.fakefriend.message_54"
		},
		{
			"fakefriend_message_" + 55,
			"customtokens.fakefriend.message_55"
		},
		{
			"fakefriend_message_" + 56,
			"customtokens.fakefriend.message_56"
		},
		{
			"fakefriend_message_" + 57,
			"customtokens.fakefriend.message_57"
		},
		{
			"fakefriend_message_" + 58,
			"customtokens.fakefriend.message_58"
		},
		{
			"fakefriend_message_" + 59,
			"customtokens.fakefriend.message_59"
		},
		{
			"fakefriend_message_" + 60,
			"customtokens.fakefriend.message_60"
		},
		{
			"fakefriend_message_" + 61,
			"customtokens.fakefriend.message_61"
		},
		{
			"fakefriend_message_" + 62,
			"customtokens.fakefriend.message_62"
		},
		{
			"fakefriend_message_" + 63,
			"customtokens.fakefriend.message_63"
		}
	};

	public string DisplayName { get; set; }

	public bool IAmSending { get; set; }

	public void Init(bool aSimple = false)
	{
		DisplayName = "Mixbot";
		IAmSending = false;
		if (aSimple)
		{
			return;
		}
		if (!Singleton<SettingsManager>.Instance.GetFakeFriendsEnabled())
		{
			return;
		}

		databaseApi = Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi;
		if (databaseApi == null || MixSession.Session == null)
		{
			return;
		}
		fakeFriend = (FakeFriendData)MixSession.User.Friends.FirstOrDefault((IFriend fr) => fr is FakeFriendData);
		fakeFriendAsChatMember = new LocalChatMember(fakeFriend);
		fakeThread = (FakeThread)MixSession.User.ChatThreadsFromUsers().FirstOrDefault((IChatThread thr) => thr is FakeThread);
		databaseCache["fakefriend_visited_chat"] = databaseApi.LoadUserValueAsBool("fakefriend_visited_chat");
		messagesSent.Clear();
		messagesPending.Clear();
		foreach (KeyValuePair<string, string> message in messages)
		{
			messagesSent.Add(message.Key, false);
		}
		List<string> list = new List<string>(messagesSent.Keys);
		foreach (string item in list)
		{
			messagesSent[item] = databaseApi.LoadUserValueAsBool(item, messagesSent[item]);
		}
		lastMessage = databaseApi.LoadUserValueAsInt("last_message");
		if (string.IsNullOrEmpty(databaseApi.LoadUserValue("fakefriend_time_inited")))
		{
			databaseApi.SaveUserValue("fakefriend_time_inited", DateTime.Now.ToString("O"));
			StartCoroutine(SendMessage(1, 0f));
		}
		MixSession.User.OnAddedToOfficialAccountChatThread += eventGenerator.AddEventHandler<AbstractAddedToOfficialAccountThreadEventArgs>(null, OnAddedToOAThread);
		MixSession.OnGotNewMessages += HandleOnGotNewMessages;
		GetOfficialAccount();
	}

	private void HandleOnGotNewMessages(bool success, bool areNewMessages)
	{
		if (this.IsNullOrDisposed() || !areNewMessages || !success || officalAccountThread == null)
		{
			return;
		}
		IEnumerable<IChatMessage> enumerable = officalAccountThread.ChatMessages.Skip(Math.Max(0, officalAccountThread.ChatMessages.Count() - 25));
		foreach (IChatMessage item in enumerable)
		{
			OnOAMessageAdded(item);
		}
	}

	private void OnAddedToOAThread(object obj, AbstractAddedToOfficialAccountThreadEventArgs args)
	{
		if (!this.IsNullOrDisposed() && args.ChatThread.OfficialAccount.AccountId.Equals(OAID))
		{
			officalAccountThread = args.ChatThread;
			AddThreadListeners();
		}
	}

	private void GetOfficialAccount()
	{
		officalAccount = null;
		RemoveThreadListeners();
		officalAccountThread = MixSession.User.OfficialAccountChatThreads.FirstOrDefault((IOfficialAccountChatThread x) => x.OfficialAccount.AccountId.Equals(OAID));
		if (officalAccountThread != null)
		{
			if (!messagesSent["fakefriend_message_" + 1])
			{
				LocalTextMessage message = new LocalTextMessage(fakeFriendAsChatMember.Id, ParseMessage(Singleton<Localizer>.Instance.getString(messages["fakefriend_message_" + 1]), null));
				fakeThread.FakeMessageIncoming(message);
			}
			officalAccount = officalAccountThread.OfficialAccount;
			KillScript();
			return;
		}
		if (messagesSent["fakefriend_message_" + 12])
		{
			FollowMixbotOA();
		}
		MixSession.User.GetAllOfficialAccounts(actionGenerator.CreateAction(delegate(IGetAllOfficialAccountsResult aResult)
		{
			if (aResult.Success)
			{
				officalAccount = aResult.OfficialAccounts.FirstOrDefault((IOfficialAccount x) => x.AccountId.Equals(OAID));
			}
		}));
	}

	private void AddThreadListeners()
	{
		if (officalAccountThread != null && !officalAccountListenersAdded)
		{
			IChatMessageRetriever chatMessageRetriever = officalAccountThread.CreateChatMessageRetriever(25);
			chatMessageRetriever.RetrieveMessages(actionGenerator.CreateAction<IRetrieveChatThreadMessagesResult>(OARetrieverCallback), actionGenerator.CreateAction<IRetrieveChatThreadMessagesResult>(OARetrieverCallback));
			officalAccountThread.OnTextMessageAdded += eventGenerator.AddEventHandler<AbstractChatThreadTextMessageAddedEventArgs>(officalAccountThread, OnOATextMessageAdded);
			officalAccountThread.OnStickerMessageAdded += eventGenerator.AddEventHandler<AbstractChatThreadStickerMessageAddedEventArgs>(officalAccountThread, OnOAStickerMessageAdded);
			officalAccountThread.OnPhotoMessageAdded += eventGenerator.AddEventHandler<AbstractChatThreadPhotoMessageAddedEventArgs>(officalAccountThread, OnOAPhotoMessageAdded);
			officalAccountThread.OnVideoMessageAdded += eventGenerator.AddEventHandler<AbstractChatThreadVideoMessageAddedEventArgs>(officalAccountThread, OnOAVideoMessageAdded);
			officalAccountListenersAdded = true;
		}
	}

	private void RemoveThreadListeners()
	{
		if (officalAccountThread != null && officalAccountListenersAdded)
		{
			officalAccountThread.OnTextMessageAdded -= eventGenerator.GetEventHandler<AbstractChatThreadTextMessageAddedEventArgs>(officalAccountThread, OnOATextMessageAdded);
			officalAccountThread.OnStickerMessageAdded -= eventGenerator.GetEventHandler<AbstractChatThreadStickerMessageAddedEventArgs>(officalAccountThread, OnOAStickerMessageAdded);
			officalAccountThread.OnPhotoMessageAdded -= eventGenerator.GetEventHandler<AbstractChatThreadPhotoMessageAddedEventArgs>(officalAccountThread, OnOAPhotoMessageAdded);
			officalAccountThread.OnVideoMessageAdded -= eventGenerator.GetEventHandler<AbstractChatThreadVideoMessageAddedEventArgs>(officalAccountThread, OnOAVideoMessageAdded);
			officalAccountListenersAdded = false;
		}
	}

	private void OARetrieverCallback(IRetrieveChatThreadMessagesResult aResult)
	{
		if (aResult == null || !aResult.Success)
		{
			return;
		}
		foreach (IChatMessage chatMessage in aResult.ChatMessages)
		{
			OnOAMessageAdded(chatMessage);
		}
	}

	private void OnOAMessageAdded(IChatMessage aMessage)
	{
		if (aMessage.IsMine() || !messagesSent["fakefriend_message_" + 12])
		{
			return;
		}
		string text = databaseApi.LoadUserValue("fakefriend_script_done_time");
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		DateTime value = DateTime.Parse(text).ToUniversalTime().Add(MixSession.Session.ServerTimeOffset);
		if (aMessage.TimeSent.CompareTo(value) < 0)
		{
			return;
		}
		string text2 = databaseApi.LoadUserValue("fakefriend_oa_messageid_prefix_" + aMessage.Id);
		if (text2 == null)
		{
			databaseApi.SaveUserValue("fakefriend_oa_messageid_prefix_" + aMessage.Id, "Sent");
			IChatMessage chatMessage = null;
			if (aMessage is ITextMessage)
			{
				chatMessage = new LocalTextMessage(fakeFriendAsChatMember.Id, ((ITextMessage)aMessage).Text, aMessage.TimeSent);
			}
			else if (aMessage is IStickerMessage)
			{
				chatMessage = new LocalStickerMessage(((IStickerMessage)aMessage).ContentId, fakeFriendAsChatMember.Id, aMessage.TimeSent);
			}
			else if (aMessage is IPhotoMessage)
			{
				chatMessage = new LocalPhotoMessage(aMessage.Id, fakeFriendAsChatMember.Id, aMessage.TimeSent);
			}
			else if (aMessage is IVideoMessage)
			{
				chatMessage = new LocalVideoMessage(aMessage.Id, fakeFriendAsChatMember.Id, aMessage.TimeSent);
			}
			if (chatMessage != null)
			{
				fakeThread.FakeMessageIncoming(chatMessage);
			}
		}
	}

	private void OnOATextMessageAdded(object s, AbstractChatThreadTextMessageAddedEventArgs args)
	{
		OnOAMessageAdded(args.Message);
	}

	private void OnOAStickerMessageAdded(object s, AbstractChatThreadStickerMessageAddedEventArgs args)
	{
		OnOAMessageAdded(args.Message);
	}

	private void OnOAPhotoMessageAdded(object s, AbstractChatThreadPhotoMessageAddedEventArgs args)
	{
		OnOAMessageAdded(args.Message);
	}

	private void OnOAVideoMessageAdded(object s, AbstractChatThreadVideoMessageAddedEventArgs args)
	{
		OnOAMessageAdded(args.Message);
	}

	public bool IsFake(IChatThread aThread)
	{
		Init(true);
		return aThread is FakeThread;
	}

	public bool IsFake(IFriend aUser)
	{
		Init(true);
		return aUser is FakeFriendData;
	}

	public void ChatVisited(IChatThread aChatThread)
	{
		if (IsFake(aChatThread))
		{
			databaseCache["fakefriend_visited_chat"] = true;
			databaseApi.SaveUserValueFromBool("fakefriend_visited_chat", true);
		}
	}

	public bool ProcessChatMessage(IChatThread aThread, IChatMessage aMessage, Action<bool> callback)
	{
		if (IsFake(aThread))
		{
			bool offlineCharacter = aThread is FakeThread && ((FakeThread)aThread).IsOfflineCharacter;
			if (aMessage is LocalTextMessage)
			{
				LocalTextMessage txtMsg = (LocalTextMessage)aMessage;
				string text = txtMsg.Text;
				aThread.ModerateTextMessage(text, delegate(ITextModerationResult result)
				{
					if (result.Success)
					{
						txtMsg.Text = result.ModeratedText;
						txtMsg.Sent = true;
						MonoSingleton<FakeFriendManager>.Instance.MessageSent(aMessage);
					}
					StartCoroutine(RunCallback(result.Success, callback));
				});
			}
			else if (offlineCharacter)
			{
				bool supportedOfflineMessage = aMessage is LocalStickerMessage || aMessage is LocalGagMessage || aMessage is LocalGameStateMessage;
				LocalStickerMessage stickerMessage = aMessage as LocalStickerMessage;
				if (stickerMessage != null)
				{
					stickerMessage.Sent = supportedOfflineMessage;
				}
				LocalGagMessage gagMessage = aMessage as LocalGagMessage;
				if (gagMessage != null)
				{
					gagMessage.Sent = supportedOfflineMessage;
				}
				LocalGameStateMessage gameStateMessage = aMessage as LocalGameStateMessage;
				if (gameStateMessage != null)
				{
					gameStateMessage.Sent = supportedOfflineMessage;
				}
				if ((stickerMessage != null || gagMessage != null) && supportedOfflineMessage)
				{
					StartCoroutine(CompleteOfflineCharacterMediaReply((FakeThread)aThread, aMessage));
				}
				StartCoroutine(RunCallback(supportedOfflineMessage, callback));
			}
			else
			{
				if (MonoSingleton<ConnectionManager>.Instance.IsConnected)
				{
					MonoSingleton<FakeFriendManager>.Instance.MessageSent(aMessage);
				}
				StartCoroutine(RunCallback(MonoSingleton<ConnectionManager>.Instance.IsConnected, callback));
			}
			if (officalAccountThread != null && !offlineCharacter)
			{
				if (aMessage is ITextMessage)
				{
					officalAccountThread.SendTextMessage(((ITextMessage)aMessage).Text, actionGenerator.CreateAction<ISendTextMessageResult>(delegate
					{
					}));
				}
				else if (aMessage is IStickerMessage)
				{
					officalAccountThread.SendStickerMessage(((IStickerMessage)aMessage).ContentId, actionGenerator.CreateAction<ISendStickerMessageResult>(delegate
					{
					}));
				}
				else if (aMessage is IGameStateMessage)
				{
					IGameStateMessage gameStateMessage = (IGameStateMessage)aMessage;
					officalAccountThread.SendGameStateMessage(gameStateMessage.GameName, gameStateMessage.State, actionGenerator.CreateAction<ISendGameStateMessageResult>(delegate
					{
					}));
				}
			}
			return true;
		}
		return false;
	}

	public void ProcessOfflineCharacterMessage(FakeThread thread, LocalTextMessage message, Action<ISendTextMessageResult> callback)
	{
		StartCoroutine(CompleteOfflineCharacterMessage(thread, message, callback));
	}

	private IEnumerator CompleteOfflineCharacterMessage(FakeThread thread, LocalTextMessage message, Action<ISendTextMessageResult> callback)
	{
		yield return new WaitForSeconds(0.25f);
		if (thread == null || message == null)
		{
			callback(new FakeSendTextMessageResult(false));
			yield break;
		}
		message.Sent = true;
		callback(new FakeSendTextMessageResult(true));
		thread.NotifyOfflineTextMessageSent(message);
		yield return new WaitForSeconds(0.25f);
		thread.AddOfflineCharacterReply(GetOfflineCharacterReply(thread, thread.RemoteMembers.First().FirstName, message.Text));
	}

	private IEnumerator CompleteOfflineCharacterMediaReply(FakeThread thread, IChatMessage message)
	{
		yield return new WaitForSeconds(message is LocalGagMessage ? 5f : 0.7f);
		if (thread == null || !IsSentOfflineCharacterMediaMessage(message))
		{
			yield break;
		}
		LocalStickerMessage incomingSticker = message as LocalStickerMessage;
		LocalGagMessage incomingGag = message as LocalGagMessage;
		IRemoteChatMember character = thread.RemoteMembers.FirstOrDefault();
		if (character == null)
		{
			yield break;
		}
		EntitlementsManager entitlementsManager = Singleton<EntitlementsManager>.Instance;
		AssetManager assetManager = MonoSingleton<AssetManager>.Instance;
		List<Sticker> stickers = new List<Sticker>();
		HashSet<string> stickerIds = new HashSet<string>();
		if (entitlementsManager != null)
		{
			List<Sticker_Pack> stickerPacks = entitlementsManager.GetMyStickerPacks();
			if (stickerPacks != null)
			{
				foreach (Sticker_Pack stickerPack in stickerPacks)
				{
					foreach (Sticker sticker in entitlementsManager.GetOrderedStickersFromPack(stickerPack))
					{
						if (IsCachedCharacterReplyAvailable(sticker, assetManager) && stickerIds.Add(sticker.GetUid()))
						{
							stickers.Add(sticker);
						}
					}
				}
			}
		}
		List<Gag> gags = entitlementsManager != null ? entitlementsManager.GetMyGags() : null;
		if (gags != null)
		{
			gags = gags.FindAll((Gag gag) => IsCachedCharacterReplyAvailable(gag, assetManager));
		}
		else
		{
			gags = new List<Gag>();
		}
		if (incomingGag != null)
		{
			Gag gagReply = gags.Count > 0 ? gags[UnityEngine.Random.Range(0, gags.Count)] : null;
			string gagId = gagReply != null ? gagReply.GetUid() : incomingGag.ContentId;
			if (!string.IsNullOrEmpty(gagId))
			{
				thread.AddOfflineCharacterGagReply(gagId);
				yield break;
			}
		}
		if (incomingSticker != null)
		{
			Sticker stickerReply = stickers.Count > 0 ? stickers[UnityEngine.Random.Range(0, stickers.Count)] : null;
			string stickerId = stickerReply != null ? stickerReply.GetUid() : incomingSticker.ContentId;
			if (!string.IsNullOrEmpty(stickerId))
			{
				thread.AddOfflineCharacterStickerReply(stickerId);
				yield break;
			}
		}
		thread.AddOfflineCharacterReply(character.FirstName == "Hank" ? "That was fun!" : "I liked that! It made me smile.");
	}

	private bool IsSentOfflineCharacterMediaMessage(IChatMessage message)
	{
		LocalStickerMessage stickerMessage = message as LocalStickerMessage;
		if (stickerMessage != null)
		{
			return stickerMessage.Sent;
		}
		LocalGagMessage gagMessage = message as LocalGagMessage;
		return gagMessage != null && gagMessage.Sent;
	}

	private bool IsCachedCharacterReplyAvailable(BaseContentData entitlement, AssetManager assetManager)
	{
		return entitlement != null && !string.IsNullOrEmpty(entitlement.GetUid()) && !string.IsNullOrEmpty(entitlement.GetHd()) &&
			assetManager != null &&
			(assetManager.IsBundleCached(entitlement.GetHd()) || assetManager.IsBundleAssetCached(entitlement.GetHd()));
	}

	private string GetOfflineCharacterReply(FakeThread thread, string characterName, string message)
	{
		string normalized = message.ToLowerInvariant();
		if (MatchesOfflineVocabulary(normalized, "farewell"))
		{
			offlineKnockKnockStages.Remove(thread.Id);
			if (characterName == "Hank")
			{
				return "All right, talk later. Try not to make a big deal out of it.";
			}
			return "Talk to you later! I'll be here whenever you want to chat.";
		}
		int knockKnockStage;
		if (offlineKnockKnockStages.TryGetValue(thread.Id, out knockKnockStage))
		{
			if (knockKnockStage == 1)
			{
				offlineKnockKnockStages[thread.Id] = 2;
				string setup = message.Trim().TrimEnd('?', '!', '.');
				if (characterName == "Hank")
				{
					return string.IsNullOrWhiteSpace(setup) ? "Yeah, yeah. Who?" : setup + " who? Hmph.";
				}
				return string.IsNullOrWhiteSpace(setup) ? "Who?" : setup + " who?";
			}
			if (knockKnockStage == 3)
			{
				offlineKnockKnockStages[thread.Id] = 4;
				if (characterName == "Hank")
				{
					return "A little fish. That's the joke, apparently.";
				}
				return "A little fish.";
			}
			offlineKnockKnockStages.Remove(thread.Id);
			if (characterName == "Hank")
			{
				return knockKnockStage == 2 ? "Fine, that's a good one, I guess. Want another?" : "A little fish you a wonderful day. There, happy?";
			}
			return knockKnockStage == 2 ? "That's a good one! Want to hear another?" : "A little fish you a wonderful day!";
		}
		if (MatchesOfflineVocabulary(normalized, "knock_knock_request"))
		{
			offlineKnockKnockStages[thread.Id] = 3;
			if (characterName == "Hank")
			{
				return "Knock knock. Don't keep me waiting.";
			}
			return "Knock knock!";
		}
		if (MatchesOfflineVocabulary(normalized, "knock_knock"))
		{
			offlineKnockKnockStages[thread.Id] = 1;
			if (characterName == "Hank")
			{
				return "Yeah, yeah. Who's there?";
			}
			return "Who's there?";
		}
		if (MatchesOfflineVocabulary(normalized, "joke"))
		{
			string joke = offlineCharacterJokes[UnityEngine.Random.Range(0, offlineCharacterJokes.Length)];
			return characterName == "Hank" ? "All right, here's one: " + joke : joke;
		}
		if (MatchesOfflineVocabulary(normalized, "fact"))
		{
			string[] facts;
			if (!offlineCharacterFacts.TryGetValue(characterName, out facts) || facts.Length == 0)
			{
				return "I can't think of a fact right now. Try asking me again later.";
			}
			string fact = facts[UnityEngine.Random.Range(0, facts.Length)];
			return characterName == "Hank" ? "Fine, here's a fact: " + fact : "Here's a fact: " + fact;
		}
		if (characterName == "Hank")
		{
			return GetGrumpyHankReply(normalized);
		}
		if (MatchesOfflineVocabulary(normalized, "thanks"))
		{
			return "You're welcome! I'm glad we got to chat.";
		}
		if (MatchesOfflineVocabulary(normalized, "welcome"))
		{
			return "Thanks! Let me know if you'd like to talk about anything else.";
		}
		if (MatchesOfflineVocabulary(normalized, "identity"))
		{
			return "I'm " + characterName + ". It's nice to meet you!";
		}
		if (MatchesOfflineVocabulary(normalized, "how_are_you"))
		{
			return "I'm doing well, thanks for asking! How are you?";
		}
		if (MatchesOfflineVocabulary(normalized, "favorites"))
		{
			return "I like hearing about the things my friends enjoy. What's your favorite thing to do?";
		}
		if (MatchesOfflineVocabulary(normalized, "apology"))
		{
			return "No worries! Everyone has moments like that.";
		}
		if (MatchesOfflineVocabulary(normalized, "compliment"))
		{
			return "Aw, thanks! I like chatting with you too.";
		}
		if (MatchesOfflineVocabulary(normalized, "bored"))
		{
			return "We could play a word game or talk about something you enjoy. What sounds fun?";
		}
		if (MatchesOfflineVocabulary(normalized, "unsure"))
		{
			return "That's okay! We can talk about an adventure, a favorite place, or a joke.";
		}
		if (normalized == "hi" || normalized.StartsWith("hi ") || normalized.StartsWith("hi!") || normalized.StartsWith("hi,") || normalized.StartsWith("hi.")
			|| MatchesOfflineVocabulary(normalized, "greeting"))
		{
			if (characterName == "Dory")
			{
				return "Hi there, friend! I'm really happy to chat with you. What should we talk about?";
			}
			if (characterName == "Nemo")
			{
				return "Hi! I'm glad you stopped by. Want to explore the reef or talk about your day?";
			}
			return "Hi! I'm glad we can chat. What would you like to talk about?";
		}
		if (characterName == "Dory" && ContainsAny(normalized, "quote", "movie", "swim", "swimming", "keep going", "carry on"))
		{
			return "Just keep swimming!";
		}
		if (characterName == "Dory")
		{
			if (ContainsAny(normalized, "sad", "lonely", "upset", "unhappy", "down", "blue", "discouraged", "frustrated", "bad day"))
			{
				return "I'm sorry you're feeling that way. Want to tell me what's on your mind?";
			}
			if (ContainsAny(normalized, "remember", "remembering", "memory", "recall", "forget", "forgot", "forgetting"))
			{
				return "Sometimes it helps to take things one little step at a time. What's one thing you remember?";
			}
			if (ContainsAny(normalized, "ocean", "sea", "underwater", "marine", "aquatic", "fish", "reef", "coral", "aquarium", "turtle", "shark"))
			{
				return "The ocean is huge, but it's nicer when you have a friend to explore with.";
			}
			return "That is a very interesting thought! What else can you tell me?";
		}
		if (characterName == "Nemo" && ContainsAny(normalized, "family", "parent", "parents", "sibling", "siblings", "brother", "sister", "dad", "father", "mom", "mother"))
		{
			return "My family means a lot to me. Good friends can make any adventure feel less scary.";
		}
		if (ContainsAny(normalized, "ocean", "sea", "underwater", "marine", "aquatic", "fish", "reef", "coral", "aquarium", "turtle", "shark")
			&& ContainsAny(characterName.ToLowerInvariant(), "nemo", "marlin", "hank"))
		{
			return "The reef is full of little places to explore. What would you like to discover?";
		}
		if (ContainsAny(normalized, "adventure", "explore", "exploration", "journey", "mission", "challenge", "scared", "afraid", "fearful", "brave", "courage", "courageous")
			&& ContainsAny(characterName.ToLowerInvariant(), "nemo", "judy", "sabine"))
		{
			return "That sounds like a big adventure! Let's take it one step at a time.";
		}
		if (characterName == "Baymax" && ContainsAny(normalized, "sad", "hurt", "injured", "pain", "sick", "ill", "unwell", "doctor", "health", "tired", "exhausted", "worried", "anxious", "stressed"))
		{
			return "Thank you for sharing that with me. What would help you feel a little better?";
		}
		if (characterName == "Olaf" && ContainsAny(normalized, "snow", "snowman", "winter", "ice", "cold", "summer", "warm", "hot", "sunny", "beach", "vacation"))
		{
			return "That sounds delightful! What is your favorite thing to do with a friend?";
		}
		if (characterName == "Stitch" && ContainsAny(normalized, "friend", "friends", "friendship", "family", "together", "support", "help", "team", "teamwork"))
		{
			return "Friends make every day more fun. What do you like doing together?";
		}
		if (ContainsAny(characterName.ToLowerInvariant(), "joy", "sadness", "fear", "disgust")
			&& ContainsAny(normalized, "feel", "feeling", "emotion", "emotions", "happy", "excited", "calm", "sad", "angry", "mad", "scared", "nervous", "worried", "proud", "frustrated", "disappointed"))
		{
			return "Feelings can tell us something important. What do you think yours are telling you?";
		}
		if (ContainsAny(characterName.ToLowerInvariant(), "mike", "sulley")
			&& ContainsAny(normalized, "fun", "funny", "laugh", "laughing", "giggle", "humor", "comedy", "amusing", "joke", "game", "play"))
		{
			return "Having fun with friends is the best. What always makes you laugh?";
		}
		if (ContainsAny(characterName.ToLowerInvariant(), "chopper", "stormtrooper", "sabine", "zeb")
			&& ContainsAny(normalized, "space", "galaxy", "universe", "star", "starship", "spaceship", "spacecraft", "rocket", "astronaut", "droid", "planet", "travel"))
		{
			return "There are so many places to explore. Where would you want to go first?";
		}
		return "That sounds interesting. Tell me a little more. What happened next?";
	}

	private string GetGrumpyHankReply(string normalized)
	{
		if (MatchesOfflineVocabulary(normalized, "thanks"))
		{
			return "Yeah, yeah. You're welcome.";
		}
		if (MatchesOfflineVocabulary(normalized, "welcome"))
		{
			return "It's nothing. Don't mention it.";
		}
		if (MatchesOfflineVocabulary(normalized, "identity"))
		{
			return "Hank. What, did you forget already?";
		}
		if (MatchesOfflineVocabulary(normalized, "how_are_you"))
		{
			return "I've had better days. But I'm all right, I guess. You?";
		}
		if (MatchesOfflineVocabulary(normalized, "favorites"))
		{
			return "Quiet corners. And not being bothered. What's your favorite?";
		}
		if (MatchesOfflineVocabulary(normalized, "apology"))
		{
			return "It's fine. Just watch it next time.";
		}
		if (MatchesOfflineVocabulary(normalized, "compliment"))
		{
			return "Yeah, sure. Don't get used to me saying thanks.";
		}
		if (MatchesOfflineVocabulary(normalized, "bored"))
		{
			return "Well, don't just sit there. Pick a game or tell me what's bothering you.";
		}
		if (MatchesOfflineVocabulary(normalized, "unsure"))
		{
			return "Take your time. No need to make a whole production out of it.";
		}
		if (normalized == "hi" || normalized.StartsWith("hi ") || normalized.StartsWith("hi!") || normalized.StartsWith("hi,") || normalized.StartsWith("hi.")
			|| MatchesOfflineVocabulary(normalized, "greeting"))
		{
			return "Oh, it's you. Hmph. What do you want? I was enjoying the quiet.";
		}
		if (ContainsAny(normalized, "sad", "lonely", "upset", "unhappy", "down", "blue", "discouraged", "frustrated", "bad day"))
		{
			return "Well, that stinks. Want to talk about it? I can listen, I guess.";
		}
		if (ContainsAny(normalized, "ocean", "sea", "underwater", "marine", "aquatic", "fish", "reef", "coral", "aquarium", "turtle", "shark"))
		{
			return "The reef's all right, I guess. Just don't expect a guided tour.";
		}
		return "Hmph. That's interesting, I suppose. Go on then.";
	}

	private bool MatchesOfflineVocabulary(string text, string category)
	{
		string[] terms;
		return offlineCharacterVocabulary.TryGetValue(category, out terms) && ContainsAny(text, terms);
	}

	private bool ContainsAny(string text, params string[] terms)
	{
		foreach (string term in terms)
		{
			if (text.Contains(term))
			{
				return true;
			}
		}
		return false;
	}

	public IEnumerator RunCallback(bool success, Action<bool> callback)
	{
		yield return new WaitForEndOfFrame();
		callback(success);
	}

	public void HighlightAnimator(string aType, Animator aAnimator, bool aDisable = true)
	{
		if (this.IsNullOrDisposed() || !MixSession.IsValidSession || MixSession.User == null || MixSession.User.Friends == null || aAnimator == null)
		{
			return;
		}
		bool flag = false;
		string stateName = "Highlight";
		if (aType.Equals(TYPE_ADD_FRIEND) && MixSession.User.Friends.Count() < 2)
		{
			stateName = (aDisable ? "idle" : "Highlight");
			flag = true;
		}
		if (aType.Equals(TYPE_FRIEND_BUTTON) && ShouldHighlightFriends())
		{
			flag = true;
		}
		if (aType.Equals(TYPE_CHAT_BACK) && ShouldHighlightChatBackButton())
		{
			flag = true;
		}
		if (aType.StartsWith("media") && ShouldHighlightMediaTray(aType))
		{
			stateName = (aDisable ? "idle" : "Highlight");
			flag = true;
		}
		if (aType.Equals(TYPE_CHAT_INPUT) && ShouldHighlightChatBar())
		{
			flag = true;
			stateName = "Highlight";
		}
		if (flag)
		{
			aAnimator.enabled = true;
			if (!aAnimator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
			{
				aAnimator.Play(stateName);
			}
		}
		else if (aDisable)
		{
			aAnimator.enabled = false;
		}
		else if (!aAnimator.GetCurrentAnimatorStateInfo(0).IsName("idle"))
		{
			aAnimator.Play("idle");
		}
	}

	public bool ShouldHighlightChatBar()
	{
		bool flag = messagesSent["fakefriend_message_" + 2];
		bool flag2 = messagesSent["fakefriend_message_" + 5];
		bool flag3 = messagesSent["fakefriend_message_" + 7];
		bool flag4 = messagesSent["fakefriend_message_" + 9];
		bool flag5 = messagesSent["fakefriend_message_" + 10];
		bool flag6 = messagesSent["fakefriend_message_" + 11];
		bool flag7 = messagesSent["fakefriend_message_" + 12];
		if (!flag && !flag2 && !flag3 && !flag4 && !flag5)
		{
			return true;
		}
		if (flag6 && !flag7)
		{
			return true;
		}
		return false;
	}

	public bool ShouldHighlightChatBackButton()
	{
		if (!MixSession.IsValidSession || databaseApi == null)
		{
			return false;
		}
		return messagesSent["fakefriend_message_" + 12] && MixSession.User.Friends.Count() < 2;
	}

	public bool ShouldHighlightMediaTray(string aType)
	{
		bool flag = messagesSent["fakefriend_message_" + 4];
		bool flag2 = messagesSent["fakefriend_message_" + 5];
		bool flag3 = messagesSent["fakefriend_message_" + 7];
		bool flag4 = messagesSent["fakefriend_message_" + 9];
		bool flag5 = messagesSent["fakefriend_message_" + 11];
		if (lastMessage > 0 && !flag5)
		{
			if (lastMessage == 14 && aType.Equals(TYPE_MEDIA_STICKER))
			{
				return true;
			}
			if (lastMessage == 15 && aType.Equals(TYPE_MEDIA_GAG))
			{
				return true;
			}
			if (lastMessage == 16 && aType.Equals(TYPE_MEDIA_GAME))
			{
				return true;
			}
		}
		if (flag && !flag2 && !flag3 && !flag4 && aType.Equals(TYPE_MEDIA_STICKER))
		{
			return true;
		}
		return false;
	}

	public bool ShouldHighlightFriends()
	{
		if (this.IsNullOrDisposed() || !MixSession.IsValidSession || MixSession.User == null || MixSession.User.Friends == null || databaseApi == null || !databaseCache.ContainsKey("fakefriend_visited_chat"))
		{
			return false;
		}
		return MixSession.User.Friends.Count() < 2 && databaseCache["fakefriend_visited_chat"];
	}

	public void MessageSent(IChatMessage aMessage)
	{
		if (ShouldSendMessageTwo(aMessage))
		{
			StartCoroutine(SendMessage(2, 1.5f, aMessage));
			StartCoroutine(SendMessage(3, 3f));
			StartCoroutine(SendMessage(4, 4.5f));
		}
		else if (ShouldSendMessageFiveSevenNineOrTen(aMessage))
		{
			float aDelay = 4.5f;
			if (aMessage is IStickerMessage)
			{
				if (messagesSent["fakefriend_message_" + 5])
				{
					aDelay = 1.5f;
				}
				else
				{
					StartCoroutine(SendMessage(5));
					StartCoroutine(SendMessage(6, 3f));
				}
			}
			else if (aMessage is IGagMessage)
			{
				if (messagesSent["fakefriend_message_" + 7])
				{
					aDelay = 1.5f;
				}
				else
				{
					StartCoroutine(SendMessage(7));
					StartCoroutine(SendMessage(8, 5f));
					aDelay = 10f;
				}
			}
			else if (aMessage is IGameStateMessage)
			{
				if (messagesSent["fakefriend_message_" + 9])
				{
					aDelay = 1.5f;
				}
				else
				{
					StartCoroutine(SendMessage(9));
					aDelay = 3f;
				}
			}
			else if (aMessage is IVideoMessage || aMessage is IPhotoMessage)
			{
				if (messagesSent["fakefriend_message_" + 10])
				{
					aDelay = 1.5f;
				}
				else
				{
					StartCoroutine(SendMessage(10));
					aDelay = 3f;
				}
			}
			int nextMessageToSend = GetNextMessageToSend();
			if (nextMessageToSend > 0)
			{
				StartCoroutine(SendMessage(nextMessageToSend, aDelay));
			}
		}
		else if (ShouldSendMessageTwelve(aMessage))
		{
			StartCoroutine(SendMessage(12));
		}
		else if (ShouldSendRandomReply())
		{
			int aMessageId = ((UnityEngine.Random.Range(0, 100) <= 80) ? UnityEngine.Random.Range(50, 64) : UnityEngine.Random.Range(30, 37));
			StartCoroutine(SendMessage(aMessageId, 1.5f, null, true));
		}
	}

	private int GetNextMessageToSend()
	{
		if (!messagesSent["fakefriend_message_" + 5] && !messagesPending.Contains(5))
		{
			return (!messagesSent["fakefriend_message_" + 14]) ? 14 : 0;
		}
		if (!messagesSent["fakefriend_message_" + 7] && !messagesPending.Contains(7))
		{
			return (!messagesSent["fakefriend_message_" + 15]) ? 15 : 0;
		}
		if (!messagesSent["fakefriend_message_" + 9] && !messagesPending.Contains(9))
		{
			return (!messagesSent["fakefriend_message_" + 16]) ? 16 : 0;
		}
		if (!messagesSent["fakefriend_message_" + 11])
		{
			return 11;
		}
		return 0;
	}

	private bool ShouldSendRandomReply()
	{
		return false;
	}

	private bool ShouldSendMessageTwelve(IChatMessage aMessage)
	{
		if (messagesSent["fakefriend_message_" + 12])
		{
			return false;
		}
		if (!messagesSent["fakefriend_message_" + 11])
		{
			return false;
		}
		return true;
	}

	private bool ShouldSendMessageFiveSevenNineOrTen(IChatMessage aMessage)
	{
		if ((aMessage is IStickerMessage && messagesSent["fakefriend_message_" + 5]) || (aMessage is IGagMessage && messagesSent["fakefriend_message_" + 7]) || (aMessage is IGameStateMessage && messagesSent["fakefriend_message_" + 9]) || ((aMessage is IVideoMessage || aMessage is IPhotoMessage) && messagesSent["fakefriend_message_" + 10]))
		{
			return false;
		}
		if (aMessage is ITextMessage)
		{
			return false;
		}
		return true;
	}

	private bool ShouldSendMessageTwo(IChatMessage aMessage)
	{
		if (messagesSent["fakefriend_message_" + 2] || messagesSent["fakefriend_message_" + 5] || messagesSent["fakefriend_message_" + 7] || messagesSent["fakefriend_message_" + 9] || messagesSent["fakefriend_message_" + 10])
		{
			return false;
		}
		if (!(aMessage is ITextMessage))
		{
			return false;
		}
		return true;
	}

	public void CheckTimeBasedMessages()
	{
		if (databaseApi != null && MixSession.IsValidSession)
		{
			string text = databaseApi.LoadUserValue("fakefriend_time_inited");
			DateTime dateTime = DateTime.Now;
			if (!string.IsNullOrEmpty(text))
			{
				dateTime = DateTime.ParseExact(text, "O", CultureInfo.InvariantCulture);
			}
			if (MixSession.User.Friends.Count() < 2 && dateTime.AddHours(12.0) < DateTime.Now)
			{
				StartCoroutine(SendMessage(21, 0f));
			}
			if (MixSession.User.Friends.Count() < 2 && dateTime.AddHours(24.0) < DateTime.Now)
			{
				StartCoroutine(SendMessage(22, 0f));
			}
			if (dateTime.AddHours(24.0) < DateTime.Now && !messagesSent["fakefriend_message_" + 12])
			{
				KillScript();
			}
			if (MixSession.User.Friends.Count() < 2 && dateTime.AddHours(72.0) < DateTime.Now)
			{
				StartCoroutine(SendMessage(23, 0f));
			}
			if (MixSession.User.Friends.Count() < 2 && dateTime.AddDays(7.0) < DateTime.Now)
			{
				StartCoroutine(SendMessage(24, 0f));
			}
			if (MixSession.User.Friends.Count() < 2 && dateTime.AddDays(14.0) < DateTime.Now)
			{
				StartCoroutine(SendMessage(25, 0f));
			}
		}
	}

	private string ParseMessage(string aMessage, IChatMessage aLastMessage)
	{
		Regex regex = new Regex("\\[Y:[^\\]]+\\]");
		Regex regex2 = new Regex("\\[N:[^\\]]+\\]");
		bool flag = true;
		if (aLastMessage != null && aLastMessage is ITextMessage)
		{
			ITextMessage textMessage = (ITextMessage)aLastMessage;
			if (textMessage != null)
			{
				flag = !textMessage.Text.ToLower().StartsWith("n");
			}
		}
		string text = ((!flag) ? regex.Replace(aMessage, string.Empty) : regex2.Replace(aMessage, string.Empty));
		return text.Replace("[Y:", string.Empty).Replace("[N:", string.Empty).Replace("]", string.Empty);
	}

	private IEnumerator SendMessage(int aMessageId, float aDelay = 1.5f, IChatMessage aLastMessage = null, bool aAllowMultiple = false)
	{
		pendingMessages++;
		messagesPending.Add(aMessageId);
		string messageKey = "fakefriend_message_" + aMessageId;
		yield return new WaitForSeconds(aDelay);
		if (!Singleton<SettingsManager>.Instance.GetFakeFriendsEnabled())
		{
			pendingMessages--;
			messagesPending.Remove(aMessageId);
			yield break;
		}
		if (!aAllowMultiple && messagesSent[messageKey])
		{
			pendingMessages--;
			messagesPending.Remove(aMessageId);
			yield break;
		}
		IAmSending = true;
		databaseApi.SaveUserValueFromBool(messageKey, true);
		messagesSent[messageKey] = true;
		if (aMessageId == 12)
		{
			KillScript();
		}
		if (aMessageId >= 14 && aMessageId <= 17)
		{
			databaseApi.SaveUserValueFromInt("last_message", aMessageId);
			lastMessage = aMessageId;
		}
		switch (aMessageId)
		{
		case 3:
		case 6:
		case 30:
		case 31:
		case 32:
		case 33:
		case 34:
		case 35:
		case 36:
		case 37:
		case 38:
		case 39:
		{
			LocalStickerMessage message3 = new LocalStickerMessage(Singleton<Localizer>.Instance.getString(messages[messageKey]), fakeFriendAsChatMember.Id);
			fakeThread.FakeMessageIncoming(message3);
			break;
		}
		default:
			if (aMessageId == 8)
			{
				LocalGagMessage message = new LocalGagMessage(Singleton<Localizer>.Instance.getString(messages[messageKey]), null, fakeFriendAsChatMember.Id);
				fakeThread.FakeMessageIncoming(message);
			}
			else
			{
				LocalTextMessage message2 = new LocalTextMessage(fakeFriendAsChatMember.Id, ParseMessage(Singleton<Localizer>.Instance.getString(messages[messageKey]), aLastMessage));
				fakeThread.FakeMessageIncoming(message2);
			}
			break;
		}
		IAmSending = false;
		pendingMessages--;
		messagesPending.Remove(aMessageId);
	}

	private void KillScript()
	{
		for (int i = 1; i <= 12; i++)
		{
			databaseApi.SaveUserValueFromBool("fakefriend_message_" + i, true);
			messagesSent["fakefriend_message_" + i] = true;
		}
		if (string.IsNullOrEmpty(databaseApi.LoadUserValue("fakefriend_script_done_time")))
		{
			databaseApi.SaveUserValue("fakefriend_script_done_time", DateTime.UtcNow.ToString("u"));
		}
		FollowMixbotOA();
	}

	private void FollowMixbotOA()
	{
		officalAccountThread = MixSession.User.OfficialAccountChatThreads.FirstOrDefault((IOfficialAccountChatThread x) => x.OfficialAccount.AccountId.Equals(OAID));
		if (officalAccountThread != null)
		{
			AddThreadListeners();
		}
		else
		{
			if (officalAccount == null)
			{
				return;
			}
			MixSession.User.FollowOfficialAccount(officalAccount, actionGenerator.CreateAction(delegate(IFollowOfficialAccountResult aResult)
			{
				if (aResult.Success)
				{
					Analytics.LogFollowOfficialAccount(officalAccount);
					AddThreadListeners();
				}
			}));
		}
	}
}
