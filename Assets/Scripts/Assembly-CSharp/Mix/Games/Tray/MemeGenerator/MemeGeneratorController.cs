using System;
using System.Collections.Generic;
using System.Linq;
using LitJson;
using Mix.Games.Data;
using Mix.Games.Session;
using UnityEngine;

namespace Mix.Games.Tray.MemeGenerator
{
    public class MemeGeneratorController : BaseGameController, IMemeGeneration, IMixGame, IMixGameDataRequest
    {
        public MemeImageSelectionController ImageSelectorController;

        public MemeCaptionCreationController CaptionCreationController;

        protected string mBaseUrl;

        protected MemeGeneratorData mData;

        protected List<MixGameContentData> mMemeGeneratorData;

        MemeGeneratorController IMemeGeneration.GetMemeGeneratorController()
        {
            Debug.Log("[MemeGeneratorController] GetMemeGeneratorController called");
            return this;
        }

        void IMemeGeneration.QuitMemeGeneration()
        {
            Debug.Log("[MemeGeneratorController] QuitMemeGeneration called");
            CloseGame();
        }

        void IMemeGeneration.SetMemeCaption(Captions aCaptions)
        {
            Debug.Log($"[MemeGeneratorController] SetMemeCaption called: TopCaption={aCaptions.TopCaption}, BottomCaption={aCaptions.BottomCaption}");
            mData.BottomCaption = aCaptions.BottomCaption;
            mData.TopCaption = aCaptions.TopCaption;
            mData.Status = MixGameSessionStatus.COMPLETE;
            Debug.Log($"[MemeGeneratorController] mData updated: TopCaption={mData.TopCaption}, BottomCaption={mData.BottomCaption}, Status={mData.Status}");
            GameOver(mData);
        }

        void IMemeGeneration.SetMemeImage(string aImageUrl)
        {
            Debug.Log($"[MemeGeneratorController] SetMemeImage called: aImageUrl={aImageUrl}");
            mData.ImageUrl = aImageUrl;
            Debug.Log($"[MemeGeneratorController] mData.ImageUrl set: {mData.ImageUrl}");
            ImageSelectorController.gameObject.SetActive(false);
            ToastPanelController component = CaptionCreationController.gameObject.GetComponent<ToastPanelController>();
            Debug.Log("[MemeGeneratorController] ToastPanelController component initialized");
            component.Init(BaseGameController.Instance.Session.StatusBarHeight, BaseGameController.Instance.Session.ScreenHeight, BaseGameController.Instance.Session.HeightScale);
            CaptionCreationController.gameObject.SetActive(true);
            CaptionCreationController.Initialize(this, aImageUrl);
        }

        void IMemeGeneration.HideMemeImage()
        {
            Debug.Log("[MemeGeneratorController] HideMemeImage called");
            ImageSelectorController.gameObject.SetActive(true);
            CaptionCreationController.gameObject.SetActive(false);
        }

        void IMixGame.Initialize(MixGameData aData)
        {
            Debug.Log("[MemeGeneratorController] Initialize called");
            mData = GetGameData<MemeGeneratorData>();
            Debug.Log($"[MemeGeneratorController] mData initialized: {mData}");
            if (aData == null)
            {
                Debug.Log("[MemeGeneratorController] aData is null");
                if (DebugSceneIndicator.IsDebugScene)
                {
                    Debug.Log("[MemeGeneratorController] DebugSceneIndicator.IsDebugScene is true");
                    TestEntitlementGameData entitlement = new TestEntitlementGameData("gogs/meme_generator/game_assets", "meme_generator", false);
                    mSession.Entitlement = entitlement;
                    mBaseUrl = mSession.Entitlement.GetBaseUrl() + '/';
                    Debug.Log($"[MemeGeneratorController] mBaseUrl set (debug): {mBaseUrl}");
                }
                else
                {
                    mBaseUrl = mSession.Entitlement.GetBaseUrl();
                    Debug.Log($"[MemeGeneratorController] mBaseUrl set: {mBaseUrl}");
                }
                Debug.Log("[MemeGeneratorController] Loading meme generator data...");
                LoadData(this, "data/Meme_Generator_Data/Meme_Generator_Data.gz", "Meme_Generator_Data.txt", ParseJsonData);
            }
        }

        void IMixGame.Play()
        {
            Debug.Log("[MemeGeneratorController] Play called");
        }

        void IMixGame.Quit()
        {
            Debug.Log("[MemeGeneratorController] Quit called");
        }

        void IMixGame.Pause()
        {
            Debug.Log("[MemeGeneratorController] Pause called");
        }

        void IMixGame.Resume()
        {
            Debug.Log("[MemeGeneratorController] Resume called");
        }

        void IMixGameDataRequest.OnDataLoaded(object aObject, object aUserData)
        {
            Debug.Log("[MemeGeneratorController] OnDataLoaded called");
            ContentObj contentObj = aObject as ContentObj;
            try
            {
                if (contentObj == null)
                {
                    Debug.LogError("[MemeGeneratorController] contentObj is null");
                    PauseOnNetworkError();
                    return;
                }
                Debug.Log($"[MemeGeneratorController] contentObj.content.objects.meme.Count = {contentObj.content.objects.meme.Count}");
                if (contentObj.content.objects.meme.Count == 0)
                {
                    Debug.LogWarning("[MemeGeneratorController] Meme list is empty, pausing on network error");
                    PauseOnNetworkError();
                }
                else
                {
                    Debug.Log("[MemeGeneratorController] Meme data loaded, calling OnMemeDataLoaded");
                    OnMemeDataLoaded(contentObj);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MemeGeneratorController] Exception in OnDataLoaded: {ex}");
                PauseOnNetworkError();
            }
        }

        public override void SetupGameData()
        {
            Debug.Log("[MemeGeneratorController] SetupGameData called");
            mGameData = new MemeGeneratorData();
            mGameData.Type = MixGameDataType.INVITE;
            Debug.Log($"[MemeGeneratorController] mGameData initialized: Type={mGameData.Type}");
        }

        public override void SetupGameData(MixGameData aData)
        {
            Debug.Log("[MemeGeneratorController] SetupGameData(MixGameData) called");
            SetupGameData();
        }

        public override void SetupGameData(string aGameDataJson)
        {
            Debug.Log($"[MemeGeneratorController] SetupGameData(string) called: aGameDataJson={aGameDataJson}");
            mGameData = JsonMapper.ToObject<MemeGeneratorData>(aGameDataJson);
            mGameData.Type = MixGameDataType.RESULT;
            Debug.Log($"[MemeGeneratorController] mGameData initialized from JSON: Type={mGameData.Type}");
        }

        public override void Initialize(GameSession aSession, string aEntitlementId)
        {
            Debug.Log($"[MemeGeneratorController] Initialize(GameSession, string) called: aEntitlementId={aEntitlementId}");
            BaseGameController.mInstance = this;
            SetupGameData();
            mGameData.Entitlement = aEntitlementId;
            mSession = aSession;
            Debug.Log("[MemeGeneratorController] CaptionCreationController.gameObject.SetActive(false)");
            CaptionCreationController.gameObject.SetActive(false);
            MixGame = this;
            MixGame.Initialize();
        }

        private void OnMemeDataLoaded(ContentObj contentData)
        {
            Debug.Log("[MemeGeneratorController] OnMemeDataLoaded called");
            if (mMemeGeneratorData == null)
            {
                Debug.Log("[MemeGeneratorController] mMemeGeneratorData is null, initializing...");
                mMemeGeneratorData = new List<MixGameContentData>();
                var memeList = contentData.content.objects.meme.Cast<MixGameContentData>().ToList();
                Debug.Log($"[MemeGeneratorController] memeList.Count = {memeList.Count}");
                MixGameContentDataHelper.SetVisibleAndOwnedContent(memeList, ref mMemeGeneratorData);
                Debug.Log($"[MemeGeneratorController] mMemeGeneratorData.Count after SetVisibleAndOwnedContent = {mMemeGeneratorData.Count}");
                ImageSelectorController.Initialize(this, mMemeGeneratorData.Cast<Meme_Generator_Data>().ToList(), mBaseUrl);
                Debug.Log("[MemeGeneratorController] ImageSelectorController initialized");
            }
            else
            {
                Debug.Log("[MemeGeneratorController] mMemeGeneratorData already initialized");
            }
        }

        public object ParseJsonData(string json)
        {
            Debug.Log($"[MemeGeneratorController] ParseJsonData called: json={json}");
            var obj = JsonMapper.ToObject<ContentObj>(json);
            Debug.Log("[MemeGeneratorController] ParseJsonData completed");
            return obj;
        }
    }
}
