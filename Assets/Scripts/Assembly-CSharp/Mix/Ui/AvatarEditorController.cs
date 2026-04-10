using System;
using System.Collections;
using System.Collections.Generic;
using Avatar;
using Avatar.DataTypes;
using Disney.Mix.SDK;
using Mix.Avatar;
using Mix.AvatarInternal;
using Mix.Data;
using Mix.Entitlements;
using Mix.Session;
using Mix.Tracking;
using Mix.User;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
    public class AvatarEditorController : BaseController, IAvatarColorListener, ISpinnerListener, IMediaTray, IDisplayNamePanelCallback
    {
        public enum EDITOR_MODES
        {
            EDITOR = 0,
            CREATOR = 1
        }

        private const int MAX_NUM_RETRIES = 2;
        private const string avatarCreatedEvent = "UI/Registration/NextPage/AfterCreatingAvatar";
        private const string avatarSavedEvent = "MainApp/UI/SFX_2_SendDisney";

        private string lastCategory = string.Empty;
        private bool avatarCompositing;
        private bool selectionPending;
        private bool IgnoreColorAudio;
        private EDITOR_MODES mode = EDITOR_MODES.CREATOR;
        private string previousScreen = string.Empty;
        private IChatThread previousThread;

        public ClientAvatar avatarDna;
        private string item = "0";
        private int tintIndex;
        private bool showDisplayNamePanel;
        private int retryAttempts;

        public GameObject resetButton;
        public GameObject colorBar;
        public GameObject ProgressBar;
        public GameObject MixelDust;
        public GameObject removeButton;
        public Image backgroundImage;
        public Text categoryTitle;
        public Button randomButton;
        public Button doneButton;
        public AvatarObjectSpawner AvatarSpawner;
        public AvatarMediaTray mediaTray;
        public AvatarColorPicker ColorPicker;
        [SerializeField] private string initialCategoryName = "Costume";

        private bool startingUp = true;
        private float lockUi = -1f;
        public float SliderUpdateDelay = 0.2f;
        public Slider horizontalSlider;
        public Slider verticalSlider;

        private AvatarSpinner avatarSpinner;
        private IRegistrationProfile profileInfo;
        private AvatarFlags flags = AvatarFlags.WithoutCaching;
        private bool isDirty;
        private bool categoryTrayInitialized;

        private bool IsDirty
        {
            get { return isDirty; }
            set
            {
                if (isDirty != value)
                {
                    Debug.Log($"[AvatarEditor] IsDirty setter called - Dirty state changed to: {value}");
                }
                isDirty = value;
                if (mode != EDITOR_MODES.CREATOR)
                {
                    Debug.Log($"[AvatarEditor] IsDirty setter - Setting resetButton active: {isDirty}");
                    resetButton.SetActive(isDirty);
                }
            }
        }

        void IAvatarColorListener.OnColorClicked(int index)
        {
            Debug.Log($"[AvatarEditor] OnColorClicked called - Index: {index}");
            if (!(lockUi >= 0f))
            {
                tintIndex = index;
                if (IgnoreColorAudio)
                {
                    Debug.Log("[AvatarEditor] OnColorClicked - Ignoring color audio");
                    IgnoreColorAudio = false;
                    return;
                }
                Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/AvatarCreateTopLevel");
                recompositeAvatar();
            }
        }

        private void Awake()
        {
            Debug.Log("[AvatarEditor] Awake called - Initializing Avatar Spawner and Spinner");
            GameObject avatarObject = AvatarSpawner.Init();
            avatarSpinner = GetComponent<AvatarSpinner>();

            if (avatarSpinner == null)
            {
                Debug.LogError("[AvatarEditor] Awake - AvatarSpinner component missing from Controller GameObject!");
            }

            avatarSpinner.InitSpinner(avatarObject);
            avatarSpinner.SetListener(this);

            if (AvatarApi.ValidateAvatar(MonoSingleton<AvatarManager>.Instance.CurrentDna) && MonoSingleton<AvatarManager>.Instance.api.IsAvatarMultiplane(MonoSingleton<AvatarManager>.Instance.CurrentDna))
            {
                Debug.Log("[AvatarEditor] Awake - Existing DNA valid, cloning and loading...");
                avatarDna = CloneAvatar(MonoSingleton<AvatarManager>.Instance.CurrentDna);
                if (avatarSpinner != null && avatarSpinner.Avatar != null)
                {
                    avatarSpinner.Avatar.SetActive(false);
                    avatarSpinner.LoadDnaOnAvatar(avatarDna, flags, HandleRegularAvatarLoaded);
                }
            }
            else if (avatarSpinner != null && avatarSpinner.Avatar != null)
            {
                Debug.Log("[AvatarEditor] Awake - No valid DNA found, waiting for transition to generate random.");
                avatarSpinner.Avatar.SetActive(false);
            }
            ProgressBar.GetComponent<Animator>().Play("ProgressBar_2-3");
        }

        private void LoadCategoryOnAwake()
        {
            Debug.Log($"[AvatarEditor] LoadCategoryOnAwake called - initialCategoryName: {initialCategoryName}");
            if (categoryTrayInitialized || mediaTray == null || mediaTray.categorySelector == null)
            {
                return;
            }

            mediaTray.Init(this);
            mediaTray.categorySelector.Setup(mediaTray);
            mediaTray.categorySelector.LoadCategory(initialCategoryName);
            categoryTrayInitialized = true;
        }

        private ClientAvatar CloneAvatar(IAvatar toClone)
        {
            Debug.Log("[AvatarEditor] CloneAvatar called");
            ClientAvatar clientAvatar = new ClientAvatar();
            clientAvatar.Accessory = CloneProperty(toClone.Accessory);
            clientAvatar.Brow = CloneProperty(toClone.Brow);
            clientAvatar.Costume = CloneProperty(toClone.Costume);
            clientAvatar.Eyes = CloneProperty(toClone.Eyes);
            clientAvatar.Hair = CloneProperty(toClone.Hair);
            clientAvatar.Mouth = CloneProperty(toClone.Mouth);
            clientAvatar.Nose = CloneProperty(toClone.Nose);
            clientAvatar.Skin = CloneProperty(toClone.Skin);
            clientAvatar.Hat = CloneProperty(toClone.Hat);
            return clientAvatar;
        }

        private ClientAvatarProperty CloneProperty(IAvatarProperty toClone)
        {
            Debug.Log("[AvatarEditor] CloneProperty called");
            ClientAvatarProperty clientAvatarProperty = new ClientAvatarProperty();
            if (toClone == null) return clientAvatarProperty;
            clientAvatarProperty.SelectionKey = toClone.SelectionKey;
            clientAvatarProperty.TintIndex = toClone.TintIndex;
            clientAvatarProperty.XOffset = toClone.XOffset;
            clientAvatarProperty.YOffset = toClone.YOffset;
            return clientAvatarProperty;
        }

        public override void OnDataReceived(string aToken, object aData)
        {
            Debug.Log($"[AvatarEditor] OnDataReceived called - Token: {aToken}, Data: {aData}");
            switch (aToken)
            {
                case "mode":
                    mode = (EDITOR_MODES)(int)aData;
                    doneButton.gameObject.SetActive(mode != EDITOR_MODES.CREATOR);
                    break;
                case "return":
                    previousScreen = (string)aData;
                    break;
                case "returnThread:":
                    previousThread = (IChatThread)aData;
                    break;
                case "missingInfo":
                    mode = EDITOR_MODES.CREATOR;
                    profileInfo = (IRegistrationProfile)aData;
                    break;
                case "missingDisplayName":
                    showDisplayNamePanel = true;
                    break;
            }
        }

        public override void OnUITransitionEnd()
        {
            Debug.Log($"[AvatarEditor] OnUITransitionEnd called - Mode: {mode}");
            if (mode == EDITOR_MODES.CREATOR)
            {
                Analytics.LogCreateAvatarPageView();
            }
            else
            {
                Analytics.LogAvatarEditorPageView();
            }

            if (!AvatarApi.ValidateAvatar(MonoSingleton<AvatarManager>.Instance.CurrentDna) || !MonoSingleton<AvatarManager>.Instance.api.IsAvatarMultiplane(MonoSingleton<AvatarManager>.Instance.CurrentDna))
            {
                Debug.Log("[AvatarEditor] OnUITransitionEnd - Initializing with Random DNA");
                avatarDna = CloneAvatar(MonoSingleton<AvatarManager>.Instance.GenerateRandomDna());
                avatarSpinner.SetState(AvatarSpinner.SpinState.SPINNING_UP);
                lockUi = 0f;
            }

            if (showDisplayNamePanel)
            {
                Debug.Log("[AvatarEditor] OnUITransitionEnd - Handling display name status");
                handleDisplayNameStatus();
            }
            backgroundImage.enabled = false;
            LoadCategoryOnAwake();
        }

        public override void OnAndroidBackButtonClicked()
        {
            Debug.Log("[AvatarEditor] OnAndroidBackButtonClicked called");
            if (base.gameObject.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("AvatarEditorScreen_OnRegComplete"))
            {
                GameObject.Find("Overlay_AvatarEditorScreen/DoneBtn").GetComponent<Button>().onClick.Invoke();
                return;
            }
            if (!Singleton<PanelManager>.Instance.FindPanel(typeof(DisplayNamePanel)) && mode != EDITOR_MODES.CREATOR)
            {
                Debug.Log("[AvatarEditor] OnAndroidBackButtonClicked - Calling OnBackBtnClicked");
                OnBackBtnClicked();
                return;
            }
            BasePanel panelWithId = Singleton<PanelManager>.Instance.GetPanelWithId(10);
            if (panelWithId != null)
            {
                Debug.Log("[AvatarEditor] OnAndroidBackButtonClicked - Closing panel with id 10");
                panelWithId.ClosePanel();
                return;
            }
            Debug.Log("[AvatarEditor] OnAndroidBackButtonClicked - Showing generic panel");
            GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC, true, 10);
            Singleton<PanelManager>.Instance.SetupTokens(new Dictionary<Text, string>
            {
                { genericPanel.TitleText, "customtokens.panels.leaving_app" },
                { genericPanel.MessageText, null },
                { genericPanel.ButtonOneText, "customtokens.login.logout_yes" },
                { genericPanel.ButtonTwoText, "customtokens.login.logout_no" }
            });
            Singleton<PanelManager>.Instance.SetupButtons(new Dictionary<Button, Action> { { genericPanel.ButtonOne, Application.Quit } });
        }

        public void OnStopSpinning() { Debug.Log("[AvatarEditor] OnStopSpinning called"); }
        public void AtRest() { Debug.Log("[AvatarEditor] AtRest called"); }

        public void AtMaxSpin()
        {
            Debug.Log("[AvatarEditor] AtMaxSpin called - Generating Random Avatar");
            retryAttempts = 0;
            LoadRandomAvatar();
            avatarSpinner.LoadDnaOnAvatar(avatarDna, flags, HandleRandomAvatarLoaded);
        }

        public void HandleRegularAvatarLoaded(bool success, string dnaSha) { Debug.Log($"[AvatarEditor] HandleRegularAvatarLoaded called - success: {success}, dnaSha: {dnaSha}"); HandleAvatarLoaded(success, dnaSha, false); }
        public void HandleRandomAvatarLoaded(bool success, string dnaSha) { Debug.Log($"[AvatarEditor] HandleRandomAvatarLoaded called - success: {success}, dnaSha: {dnaSha}"); HandleAvatarLoaded(success, dnaSha, true); }

        public void HandleAvatarLoaded(bool success, string dnaSha, bool random)
        {
            Debug.Log($"[AvatarEditor] HandleAvatarLoaded called - success: {success}, dnaSha: {dnaSha}, random: {random}");
            if (this.IsNullOrDisposed() || avatarSpinner.IsNullOrDisposed()) return;

            if (success)
            {
                Debug.Log("[AvatarEditor] HandleAvatarLoaded - DNA Loaded Successfully");
                avatarSpinner.HitBrakes();
                if (avatarSpinner != null && avatarSpinner.Avatar != null)
                {
                    avatarSpinner.Avatar.SetActive(true);
                }
                lockUi = -1f;
                avatarCompositing = false;
                if (startingUp)
                {
                    Debug.Log("[AvatarEditor] HandleAvatarLoaded - Starting up, skipping setDefaultsUp");
                    startingUp = false;
                    selectionPending = false;
                }
                else
                {
                    Debug.Log("[AvatarEditor] HandleAvatarLoaded - Calling setDefaultsUp");
                    setDefaultsUp();
                }
            }
            else if (retryAttempts < MAX_NUM_RETRIES)
            {
                retryAttempts++;
                Debug.LogWarning($"[AvatarEditor] HandleAvatarLoaded - DNA Load Failed. Retry attempt {retryAttempts}/{MAX_NUM_RETRIES}");
                if (random)
                {
                    LoadRandomAvatar();
                    avatarSpinner.LoadDnaOnAvatar(avatarDna, flags, HandleRandomAvatarLoaded);
                }
                else
                {
                    avatarSpinner.LoadDnaOnAvatar(avatarDna, flags, HandleRegularAvatarLoaded);
                }
            }
            else if (retryAttempts == MAX_NUM_RETRIES)
            {
                retryAttempts++;
                Debug.LogError("[AvatarEditor] HandleAvatarLoaded - DNA Load failed after retries. Falling back to default avatar.");
                avatarDna = DefaultAvatar.GetDefaultAvatar();
                avatarSpinner.LoadDnaOnAvatar(avatarDna, flags, HandleRegularAvatarLoaded);
            }
            else
            {
                Debug.LogError("[AvatarEditor] HandleAvatarLoaded - Critical error: Could not load any avatar dna.");
                MonoSingleton<AvatarManager>.Instance.ResetAvatarHead(avatarSpinner.Avatar);
                avatarSpinner.HitBrakes();
            }
        }

        private void Update()
        {
            Debug.Log("[AvatarEditor] Update called - Tracking FPS and handling UI lock/selectionPending");
            Singleton<TechAnalytics>.Instance.TrackFPSOnAvatarEditor();
            if (lockUi >= 0f)
            {
                lockUi += Time.deltaTime;
                if (lockUi > 60f)
                {
                    Debug.LogWarning("[AvatarEditor] Update - UI Lock Timeout reached (60s). Unlocking.");
                    MonoSingleton<AvatarManager>.Instance.ResetAvatarHead(avatarSpinner.Avatar);
                    avatarSpinner.HitBrakes();
                    lockUi = -1f;
                }
            }
            if (selectionPending && !avatarCompositing)
            {
                Debug.Log("[AvatarEditor] Update - Processing pending selection");
                recompositeAvatar();
                selectionPending = false;
            }
        }

        private IAvatarProperty GetPropertyFromString(string categoryName, ClientAvatar avatar)
        {
            Debug.Log($"[AvatarEditor] GetPropertyFromString called - categoryName: {categoryName}");
            switch (categoryName)
            {
                case "Accessory": return avatar.Accessory;
                case "Brow": return avatar.Brow;
                case "Eyes": return avatar.Eyes;
                case "Hair": return avatar.Hair;
                case "Mouth": return avatar.Mouth;
                case "Nose": return avatar.Nose;
                case "Skin": return avatar.Skin;
                case "Costume": return avatar.Costume;
                case "Hat": return avatar.Hat ?? new ClientAvatarProperty("0", 0, 0.0, 0.0);
                default: return null;
            }
        }

        private void SetPropertyFromString(string categoryName, ClientAvatar avatar, IAvatarProperty newProp)
        {
            Debug.Log($"[AvatarEditor] SetPropertyFromString called - categoryName: {categoryName}, newProp: {newProp}");
            switch (categoryName)
            {
                case "Accessory": avatar.Accessory = newProp; break;
                case "Brow": avatar.Brow = newProp; break;
                case "Eyes": avatar.Eyes = newProp; break;
                case "Hair": avatar.Hair = newProp; break;
                case "Mouth": avatar.Mouth = newProp; break;
                case "Nose": avatar.Nose = newProp; break;
                case "Skin": avatar.Skin = newProp; break;
                case "Costume": avatar.Costume = newProp; break;
                case "Hat": avatar.Hat = newProp; break;
            }
        }

        private string GetSelectionKeyFromReferenceId(string refId)
        {
            Debug.Log($"[AvatarEditor] GetSelectionKeyFromReferenceId called - refId: {refId}");
            Avatar_Multiplane avatarByReferenceId = Singleton<EntitlementsManager>.Instance.GetAvatarByReferenceId(refId);
            return avatarByReferenceId != null ? avatarByReferenceId.GetUid() : string.Empty;
        }

        private string GetReferenceIdFromSelectionKey(string selectionKey)
        {
            Debug.Log($"[AvatarEditor] GetReferenceIdFromSelectionKey called - selectionKey: {selectionKey}");
            Avatar_Multiplane avatarData = Singleton<EntitlementsManager>.Instance.GetAvatarData(selectionKey);
            return avatarData != null ? avatarData.GetReferenceId().ToString() : string.Empty;
        }

        public void RemoveItem()
        {
            Debug.Log("[AvatarEditor] RemoveItem called");
            if (!(lockUi >= 0f) && !string.IsNullOrEmpty(mediaTray.categorySelector.GetDisableId(0)))
            {
                item = mediaTray.categorySelector.GetDisableId(0);
                Debug.Log($"[AvatarEditor] RemoveItem - Setting selected item to: {item}");
                mediaTray.SetSelected(item);
                recompositeAvatar();
                setDefaultsUp();
            }
        }

        public void GenerateRandom()
        {
            Debug.Log("[AvatarEditor] GenerateRandom called");
            if (lockUi < 0f)
            {
                Debug.Log("[AvatarEditor] GenerateRandom - Randomize button clicked");
                OnRandomizeConfirmation();
            }
            Analytics.LogRandomizeAvatarAction(mode);
        }

        public void LoadRandomAvatar()
        {
            Debug.Log("[AvatarEditor] LoadRandomAvatar called");
            avatarDna = CloneAvatar(MonoSingleton<AvatarManager>.Instance.GenerateRandomDna());
            IsDirty = !AvatarApi.AreAvatarsEqual(avatarDna, MonoSingleton<AvatarManager>.Instance.CurrentDna);
        }

        public void OnHorizontalSliderChange()
        {
            Debug.Log("[AvatarEditor] OnHorizontalSliderChange called");
            if (!(lockUi >= 0f)) StartCoroutine(DelaySliderInput(horizontalSlider));
        }

        public void OnVerticalSliderChange()
        {
            Debug.Log("[AvatarEditor] OnVerticalSliderChange called");
            if (!(lockUi >= 0f)) StartCoroutine(DelaySliderInput(verticalSlider));
        }

        public IEnumerator DelaySliderInput(Slider sliderToCheck)
        {
            Debug.Log("[AvatarEditor] DelaySliderInput called");
            float oldValue = sliderToCheck.value;
            yield return new WaitForSeconds(SliderUpdateDelay);
            if (Mathf.Abs(sliderToCheck.value - oldValue) <= float.Epsilon)
            {
                Debug.Log("[AvatarEditor] DelaySliderInput - Value stable, recompositing avatar");
                recompositeAvatar();
            }
        }

        public void OnEntitlementClicked(BaseContentData aEntitlement)
        {
            Debug.Log($"[AvatarEditor] OnEntitlementClicked called - aEntitlement: {aEntitlement}");
            if (lockUi >= 0f) return;

            string newId = aEntitlement.GetUid();
            Debug.Log($"[AvatarEditor] OnEntitlementClicked - Item Selected: {newId}");
            if (newId == item)
            {
                Debug.Log("[AvatarEditor] OnEntitlementClicked - Item already selected, removing item");
                RemoveItem();
                return;
            }

            item = newId;
            Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/AvatarCreateMiddleLevel");
            recompositeAvatar();
            setDefaultsUp();
        }

        private void setDefaultsUp()
        {
            Debug.Log("[AvatarEditor] setDefaultsUp called");
            if (this.IsNullOrDisposed() || categoryTitle == null || mediaTray == null) return;

            var content = mediaTray.categorySelector.CurrentContent;
            string categoryName = content.categoryName;
            categoryTitle.text = content.locCategoryName;

            removeButton.GetComponent<Button>().interactable = categoryName != "Eyes";

            if (lastCategory != content.locCategoryName)
            {
                Debug.Log($"[AvatarEditor] setDefaultsUp - Switched category to: {categoryName}");
                bool isTintable = !categoryName.Equals("Accessory") && !categoryName.Equals("Hat");
                colorBar.gameObject.SetActive(isTintable);
                if (isTintable)
                {
                    if (!ColorPicker.HasInitialized()) ColorPicker.Init(this);
                    ColorPicker.SetContent(categoryName);
                }
                IgnoreColorAudio = true;
            }

            if (avatarDna != null)
            {
                IAvatarProperty prop = GetPropertyFromString(categoryName, avatarDna);
                if (prop != null)
                {
                    item = GetSelectionKeyFromReferenceId(prop.SelectionKey);
                    tintIndex = prop.TintIndex;
                    verticalSlider.value = (float)((prop.YOffset + 50.0) / 100.0);
                    horizontalSlider.value = (float)((prop.XOffset + 50.0) / 100.0);
                }
                else
                {
                    item = content.GetDefaultUid();
                    tintIndex = 0;
                    verticalSlider.value = 0.5f;
                    horizontalSlider.value = 0.5f;
                }

                Debug.Log($"[AvatarEditor] setDefaultsUp - Setting selected item: {item}, tintIndex: {tintIndex}");
                mediaTray.SetSelected(item);
                ColorPicker.SetSelected(tintIndex);
            }

            verticalSlider.gameObject.SetActive(!new List<string> { "Nose", "Hair", "Skin", "Costume", "Hat" }.Contains(categoryName));
            horizontalSlider.gameObject.SetActive(categoryName == "Mouth");
            lastCategory = categoryTitle.text;
        }

        public void OnContentHolderChanged()
        {
            Debug.Log("[AvatarEditor] OnContentHolderChanged called");
            Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/AvatarCreateBottomLevel");
            setDefaultsUp();
        }

        public void OnShowPreviewPanel(BaseContentData aEntitlement) { Debug.Log("[AvatarEditor] OnShowPreviewPanel called"); }
        public void OnHidePreviewPanel() { Debug.Log("[AvatarEditor] OnHidePreviewPanel called"); }

        private void recompositeAvatar()
        {
            Debug.Log("[AvatarEditor] recompositeAvatar called");
            if (!avatarCompositing && avatarDna != null && horizontalSlider != null && verticalSlider != null && avatarSpinner != null)
            {
                avatarCompositing = true;
                string categoryStr = mediaTray.categorySelector.CurrentContent.categoryName;
                IAvatarProperty propertyFromString = GetPropertyFromString(categoryStr, avatarDna);
                bool hasChanged = false;

                if (propertyFromString != null)
                {
                    ClientAvatarProperty clientAvatarProperty = new ClientAvatarProperty();
                    clientAvatarProperty.SelectionKey = GetReferenceIdFromSelectionKey(item);
                    clientAvatarProperty.TintIndex = tintIndex;
                    clientAvatarProperty.XOffset = horizontalSlider.value * 100f - 50f;
                    clientAvatarProperty.YOffset = verticalSlider.value * 100f - 50f;
                    hasChanged = item != propertyFromString.SelectionKey || tintIndex != propertyFromString.TintIndex;
                    SetPropertyFromString(categoryStr, avatarDna, clientAvatarProperty);
                }

                Debug.Log("[AvatarEditor] recompositeAvatar - Loading DNA on avatar");
                avatarSpinner.LoadDnaOnAvatar(avatarDna, flags, delegate (bool success, string sha)
                {
                    if (hasChanged)
                    {
                        Debug.Log("[AvatarEditor] recompositeAvatar - Playing category changed animation");
                        avatarSpinner.PlayAnimationTrigger("*Category*Changed".Replace("*Category*", categoryStr));
                    }
                    HandleRegularAvatarLoaded(success, sha);
                });

                if (MonoSingleton<AvatarManager>.Instance == null || MonoSingleton<AvatarManager>.Instance.CurrentDna == null)
                {
                    Debug.Log("[AvatarEditor] recompositeAvatar - Marking as dirty (no current DNA)");
                    IsDirty = true;
                }
                else
                {
                    Debug.Log("[AvatarEditor] recompositeAvatar - Comparing DNA for dirty state");
                    IsDirty = !AvatarApi.AreAvatarsEqual(MonoSingleton<AvatarManager>.Instance.CurrentDna, avatarDna);
                }
            }
            else
            {
                Debug.Log("[AvatarEditor] recompositeAvatar - Compositing in progress or missing data, setting selectionPending");
                selectionPending = true;
            }
        }

        public void ResetAvatarToDefault()
        {
            Debug.Log("[AvatarEditor] ResetAvatarToDefault called");
            if (!(mediaTray == null) && !(horizontalSlider == null) && !(verticalSlider == null))
            {
                avatarDna = CloneAvatar(MonoSingleton<AvatarManager>.Instance.CurrentDna);
                IsDirty = false;
                IAvatarProperty propertyFromString = GetPropertyFromString(mediaTray.categorySelector.CurrentContent.categoryName, avatarDna);
                if (propertyFromString != null)
                {
                    item = GetSelectionKeyFromReferenceId(propertyFromString.SelectionKey);
                    tintIndex = propertyFromString.TintIndex;
                    verticalSlider.value = (float)((propertyFromString.YOffset + 50.0) / 100.0);
                    horizontalSlider.value = (float)((propertyFromString.XOffset + 50.0) / 100.0);
                    Debug.Log($"[AvatarEditor] ResetAvatarToDefault - Setting selected item: {item}");
                    mediaTray.SetSelected(item);
                }
                recompositeAvatar();
            }
        }

        public void OnDoneClicked()
        {
            Debug.Log($"[AvatarEditor] OnDoneClicked called. Mode: {mode}, IsDirty: {IsDirty}");
            if (IsDirty)
            {
                Debug.Log("[AvatarEditor] OnDoneClicked - Saving avatar");
                SaveAvatar();
            }
            if (mode == EDITOR_MODES.EDITOR)
            {
                Debug.Log("[AvatarEditor] OnDoneClicked - Going to profile");
                GoToProfile();
            }
            else if (!MixSession.IsValidSession || profileInfo != null)
            {
                Debug.Log("[AvatarEditor] OnDoneClicked - Going to registration");
                GoToRegistration();
            }
            else
            {
                Debug.Log("[AvatarEditor] OnDoneClicked - Handling display name status");
                handleDisplayNameStatus();
            }
            Singleton<SoundManager>.Instance.PlaySoundEvent((mode == EDITOR_MODES.EDITOR) ? "MainApp/UI/SFX_2_SendDisney" : "UI/Registration/NextPage/AfterCreatingAvatar");
        }

        private void handleDisplayNameStatus()
        {
            Debug.Log("[AvatarEditor] handleDisplayNameStatus called");
            if (!(Singleton<PanelManager>.Instance.FindPanel(typeof(DisplayNamePanel)) != null))
            {
                base.gameObject.GetComponent<Animator>().enabled = true;
                if (!showDisplayNamePanel)
                {
                    base.gameObject.GetComponent<Animator>().Play("AvatarEditorScreen_ToSubmitDisplayName");
                }
                DisplayNamePanel displayNamePanel = (DisplayNamePanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.DISPLAY_NAME_SUBMITTED);
                displayNamePanel.Init(this, MixSession.Session.LocalUser.RegistrationProfile);
            }
        }

        public void OnRandomizeConfirmation()
        {
            Debug.Log("[AvatarEditor] OnRandomizeConfirmation called");
            lockUi = 0f;
            avatarSpinner.SetState(AvatarSpinner.SpinState.SPINNING_UP);
        }

        public void OnBackBtnClicked()
        {
            Debug.Log("[AvatarEditor] OnBackBtnClicked called");
            if (mode == EDITOR_MODES.CREATOR)
            {
                Debug.Log("[AvatarEditor] OnBackBtnClicked - Going to login");
                GoToLogin();
            }
            else
            {
                Debug.Log("[AvatarEditor] OnBackBtnClicked - Going to profile");
                GoToProfile();
            }
        }

        public void DnaLoadError(string swid)
        {
            Debug.LogError($"[AvatarEditor] DnaLoadError called for SWID: {swid}");
        }

        private void SaveAvatar()
        {
            Debug.Log("[AvatarEditor] SaveAvatar called - Saving Avatar DNA to Profile");
            Analytics.LogCreateAvatarSuccess(mode, avatarDna);
            MonoSingleton<AvatarManager>.Instance.setCurrentUsersDna(avatarDna);
            MonoSingleton<AvatarManager>.Instance.saveCurrentUsersDna();
        }

        private void GoToFatalError()
        {
            Debug.LogError("[AvatarEditor] GoToFatalError called - Navigating to Login.");
            if (avatarSpinner != null && avatarSpinner.Avatar != null)
            {
                avatarSpinner.Avatar.SetActive(true);
            }
            NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/Login/LoginScreen", new TransitionNone());
            navigationRequest.AddData("fatalError", true);
            navigationRequest.PopLastRequest = true;
            MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
            MonoSingleton<LoginManager>.Instance.Logout();
        }

        public void OnDisplayNameUpdated(DisplayNamePanel dispName)
        {
            Debug.Log("[AvatarEditor] OnDisplayNameUpdated called - Display name updated callback");
            ProgressBar.GetComponent<Animator>().Play("ProgressBar_3-4");
            base.gameObject.GetComponent<Animator>().enabled = true;
            base.gameObject.GetComponent<Animator>().Play("AvatarEditorScreen_OnRegComplete");
        }

        private void GoToProfile(ProfileController.PROFILE_MODES aMode = ProfileController.PROFILE_MODES.PROFILE)
        {
            Debug.Log("[AvatarEditor] GoToProfile called");
            if (avatarSpinner != null && avatarSpinner.Avatar != null) avatarSpinner.Avatar.SetActive(true);
            NavigationRequest navigationRequest;
            if (previousScreen == "Prefabs/Screens/ChatMix/ChatMixScreen")
            {
                Debug.Log("[AvatarEditor] GoToProfile - Navigating to chat screen");
                navigationRequest = ChatHelper.NavigateToChatScreen(previousThread, new TransitionInLeft(), false, false);
                navigationRequest.PopLastRequest = true;
            }
            else
            {
                Debug.Log("[AvatarEditor] GoToProfile - Navigating to profile screen");
                navigationRequest = new NavigationRequest("Prefabs/Screens/Profile/ProfileScreen", new TransitionAnimations());
                navigationRequest.AddData("mode", aMode);
                navigationRequest.PopLastRequest = true;
            }
            MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
        }

        private void GoToLogin()
        {
            Debug.Log("[AvatarEditor] GoToLogin called");
            NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/Login/LoginScreen", new TransitionAnimations(false, "Intro", "ToLogin"));
            navigationRequest.PopLastRequest = true;
            MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
        }

        private void GoToRegistration()
        {
            Debug.Log("[AvatarEditor] GoToRegistration called");
            NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/CreateAccount/CreateAccountScreen", new TransitionInLeft());
            if (profileInfo != null) navigationRequest.AddData("missingInfo", profileInfo);
            MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
        }

        public void GoToChat()
        {
            Debug.Log("[AvatarEditor] GoToChat called - Navigating to Chat");
            if (avatarSpinner != null && avatarSpinner.Avatar != null) avatarSpinner.Avatar.SetActive(true);
            NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/Conversations/ConversationsScreen", new TransitionAnimations(false, "FromSoftLogin", "ToChat"));
            navigationRequest.PopLastRequest = true;
            MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
            LoginController.StopLoginMusic();
            Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/LetsGoButton");
        }

        private void ChangeParticlesToColor()
        {
            Debug.Log("[AvatarEditor] ChangeParticlesToColor called");
            MixelDust.GetComponent<Animator>().Play("ParticleToColor");
        }
    }
}