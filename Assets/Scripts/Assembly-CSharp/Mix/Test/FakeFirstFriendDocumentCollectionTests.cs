using System.Collections.Generic;
using System.Diagnostics;
using Mix.DeviceDb;
using UnityEngine;

namespace Mix.Test
{
	public class FakeFirstFriendDocumentCollectionTests : MonoBehaviour
	{
		private void Start()
		{
			Application.TestingDirectory = "/FakeFirstFriendDocumentCollectionTests/";
			Singleton<MixDocumentCollections>.Instance.ClearFakeFirstFriendDocumentCollection();
			FakeFirstFriendDocumentCollectionApi fakeFirstFriendDocumentCollectionApi = Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi;
			fakeFirstFriendDocumentCollectionApi.AddGag(true, 1001L, true, true, "uniqueid_gag_", "contentid");
			BaseFakeFirstFriendDocument gag = fakeFirstFriendDocumentCollectionApi.GetGag("uniqueid_gag_");
			if (gag.ContentId_or_GameName_or_Text_or_MessageId != "contentid" || gag.timeSent != 1001 || !gag.isFromFriend || gag.uniqueId != "uniqueid_gag_")
			{
				FailTest("gag not correct " + new StackTrace(true).GetFrame(0).GetFileLineNumber());
				return;
			}
			fakeFirstFriendDocumentCollectionApi.AddGag(true, 1002L, true, true, "uniqueid_gag_1", "contentid");
			fakeFirstFriendDocumentCollectionApi.AddGag(true, 1003L, true, true, "uniqueid_gag_2", "contentid");
			fakeFirstFriendDocumentCollectionApi.AddGag(true, 1003L, true, true, "uniqueid_gag_3", "contentid");
			List<BaseFakeFirstFriendDocument> allGags = fakeFirstFriendDocumentCollectionApi.GetAllGags();
			if (allGags.Count != 4)
			{
				FailTest("incorrect number of gags returned " + new StackTrace(true).GetFrame(0).GetFileLineNumber());
				return;
			}
			fakeFirstFriendDocumentCollectionApi.AddText(true, 1002L, true, true, "uniqueid", "text");
			BaseFakeFirstFriendDocument text = fakeFirstFriendDocumentCollectionApi.GetText("uniqueid");
			if (text.uniqueId != "uniqueid" || !text.isFromFriend || text.timeSent != 1002 || text.ContentId_or_GameName_or_Text_or_MessageId != "text")
			{
				FailTest("text not correct " + new StackTrace(true).GetFrame(0).GetFileLineNumber());
				return;
			}
			fakeFirstFriendDocumentCollectionApi.AddText(false, 1003L, true, true, "uniqueid", "updated text");
			text = fakeFirstFriendDocumentCollectionApi.GetText("uniqueid");
			if (text.isFromFriend || text.timeSent != 1003 || !text.isSent || text.ContentId_or_GameName_or_Text_or_MessageId != "updated text")
			{
				FailTest("text update not correct " + new StackTrace(true).GetFrame(0).GetFileLineNumber());
				return;
			}
			fakeFirstFriendDocumentCollectionApi.AddText(true, 1001L, true, true, "uniqueid1", "text1");
			fakeFirstFriendDocumentCollectionApi.AddText(true, 1001L, true, true, "uniqueid2", "text2");
			fakeFirstFriendDocumentCollectionApi.AddText(true, 1001L, true, true, "uniqueid3", "text3");
			List<BaseFakeFirstFriendDocument> allTexts = fakeFirstFriendDocumentCollectionApi.GetAllTexts();
			if (allTexts.Count != 4)
			{
				FailTest("incorrect number of texts returned " + new StackTrace(true).GetFrame(0).GetFileLineNumber());
				return;
			}
			fakeFirstFriendDocumentCollectionApi.AddSticker(true, 1001L, true, true, "uniqueid_sticker_", "contentid");
			BaseFakeFirstFriendDocument sticker = fakeFirstFriendDocumentCollectionApi.GetSticker("uniqueid_sticker_");
			if (sticker.ContentId_or_GameName_or_Text_or_MessageId != "contentid" || sticker.timeSent != 1001 || !sticker.isFromFriend || sticker.uniqueId != "uniqueid_sticker_")
			{
				FailTest("sticker not correct " + new StackTrace(true).GetFrame(0).GetFileLineNumber());
				return;
			}
			fakeFirstFriendDocumentCollectionApi.AddSticker(true, 1002L, true, true, "uniqueid_sticker_1", "contentid");
			fakeFirstFriendDocumentCollectionApi.AddSticker(true, 1003L, true, true, "uniqueid_sticker_2", "contentid");
			fakeFirstFriendDocumentCollectionApi.AddSticker(true, 1003L, true, true, "uniqueid_sticker_3", "contentid");
			List<BaseFakeFirstFriendDocument> allStickers = fakeFirstFriendDocumentCollectionApi.GetAllStickers();
			if (allStickers.Count != 4)
			{
				FailTest("incorrect number of stickers returned " + new StackTrace(true).GetFrame(0).GetFileLineNumber());
				return;
			}
			fakeFirstFriendDocumentCollectionApi.RemoveDocumentByType("FakeTextDb", "uniqueid2");
			fakeFirstFriendDocumentCollectionApi.RemoveDocument("uniqueid_gag_");
			fakeFirstFriendDocumentCollectionApi.RemoveDocument("uniqueid_sticker_");
			List<BaseFakeFirstFriendDocument> allDocuments = fakeFirstFriendDocumentCollectionApi.GetAllDocuments();
			if (allDocuments.Count != 9)
			{
				FailTest("incorrect number of documents returned " + new StackTrace(true).GetFrame(0).GetFileLineNumber());
				return;
			}
			fakeFirstFriendDocumentCollectionApi.LogOut();
			if (fakeFirstFriendDocumentCollectionApi.collections.Count > 0)
			{
				FailTest("log out didnt remove all collections.");
				return;
			}
			BaseFakeFirstFriendDocument reloadedText = fakeFirstFriendDocumentCollectionApi.GetText("uniqueid");
			if (reloadedText == null || reloadedText.ContentId_or_GameName_or_Text_or_MessageId != "updated text")
			{
				FailTest("text was not preserved after reopening the collection.");
				return;
			}
			else
			{
				PassTest();
			}
		}

		private void Update()
		{
		}

		public void FailTest(string reason)
		{
			Singleton<MixDocumentCollections>.Instance.ClearFakeFirstFriendDocumentCollection();
			Application.TestingDirectory = string.Empty;
			IntegrationTest.Fail(base.gameObject, reason);
		}

		public void PassTest()
		{
			Singleton<MixDocumentCollections>.Instance.ClearFakeFirstFriendDocumentCollection();
			Application.TestingDirectory = string.Empty;
			IntegrationTest.Pass(base.gameObject);
		}
	}
}
