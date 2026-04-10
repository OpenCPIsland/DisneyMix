using System.Collections.Generic;
using Mix.Data;
using UnityEngine;

namespace Mix.Ui
{
    public class AvatarMediaTray : BaseMediaTray, ISubSelector<AvatarCategoryTrayInfo>, IBaseThumb
    {
        public Texture2D missingTexture;

        public AvatarCategoryTray categorySelector;

        protected AvatarThumbQueuer thumbInstantiator;

        private int? moveDirection;

        private void Awake()
        {
            Debug.Log("[AvatarMediaTray] Awake called");
            thumbInstantiator = new AvatarThumbQueuer(this);
        }

        public override void LoadContent(int aDirection)
        {
            Debug.Log($"[AvatarMediaTray] LoadContent called: aDirection={aDirection} - Sets moveDirection and updates categorySelector.CurrentContent");
            moveDirection = aDirection;
            categorySelector.CurrentContent = categorySelector.GetContent(aDirection);
        }

        public void Populate(Transform aContentHolder, int direction)
        {
            Debug.Log($"[AvatarMediaTray] Populate called: aContentHolder={aContentHolder.name}, direction={direction} - Loads subContent and populates the tray side");
            List<BaseContentData> subContent = categorySelector.GetSubContent(direction);
            if (subContent == null)
            {
                Debug.LogWarning("[AvatarMediaTray] Populate: subContent is null - No content to populate");
                return;
            }
            if (aContentHolder.Equals(MiddleContentHolder))
            {
                Debug.Log("[AvatarMediaTray] Populate: Clearing middle content - Calls Clear()");
                Clear();
            }
            if (aContentHolder == MiddleContentHolder)
            {
                Debug.Log("[AvatarMediaTray] Populate: Populating middle content - Sets contentDataMiddle and starts IconLoader");
                contentDataMiddle = subContent;
                contentHolderMiddle = aContentHolder;
                stickerIconCountMiddle = 0;
                if (IsInvoking("IconLoader"))
                {
                    Debug.Log("[AvatarMediaTray] Populate: Cancelling previous IconLoader invocation");
                    CancelInvoke("IconLoader");
                }
                InvokeRepeating("IconLoader", 0f, 0.01f);
            }
            else if (aContentHolder == LeftContentHolder)
            {
                Debug.Log("[AvatarMediaTray] Populate: Populating left content - Sets contentDataLeft and starts IconLoaderLeft");
                contentDataLeft = subContent;
                contentHolderLeft = aContentHolder;
                stickerIconCountLeft = 0;
                if (IsInvoking("IconLoaderLeft"))
                {
                    Debug.Log("[AvatarMediaTray] Populate: Cancelling previous IconLoaderLeft invocation");
                    CancelInvoke("IconLoaderLeft");
                }
                InvokeRepeating("IconLoaderLeft", 0f, 0.01f);
            }
            else if (aContentHolder == RightContentHolder)
            {
                Debug.Log("[AvatarMediaTray] Populate: Populating right content - Sets contentDataRight and starts IconLoaderRight");
                contentDataRight = subContent;
                contentHolderRight = aContentHolder;
                stickerIconCountRight = 0;
                if (IsInvoking("IconLoaderRight"))
                {
                    Debug.Log("[AvatarMediaTray] Populate: Cancelling previous IconLoaderRight invocation");
                    CancelInvoke("IconLoaderRight");
                }
                InvokeRepeating("IconLoaderRight", 0f, 0.01f);
            }
        }

        public override void IconLoaderRight()
        {
            Debug.Log("[AvatarMediaTray] IconLoaderRight called - Loads next right thumb item");
            if (contentDataRight == null || stickerIconCountRight >= contentDataRight.Count)
            {
                Debug.Log("[AvatarMediaTray] IconLoaderRight: Done or no content - Cancelling invocation");
                CancelInvoke("IconLoaderRight");
                return;
            }
            Debug.Log($"[AvatarMediaTray] IconLoaderRight: Adding AvatarBaseGroupItem for index {stickerIconCountRight}");
            thumbItems.Add(new AvatarBaseGroupItem(contentHolderRight, 0, this, contentDataRight[stickerIconCountRight], missingTexture, thumbInstantiator));
            stickerIconCountRight++;
        }

        public override void IconLoaderLeft()
        {
            Debug.Log("[AvatarMediaTray] IconLoaderLeft called - Loads next left thumb item");
            if (contentDataLeft == null || stickerIconCountLeft >= contentDataLeft.Count)
            {
                Debug.Log("[AvatarMediaTray] IconLoaderLeft: Done or no content - Cancelling invocation");
                CancelInvoke("IconLoaderLeft");
                return;
            }
            Debug.Log($"[AvatarMediaTray] IconLoaderLeft: Adding AvatarBaseGroupItem for index {stickerIconCountLeft}");
            thumbItems.Add(new AvatarBaseGroupItem(contentHolderLeft, 0, this, contentDataLeft[stickerIconCountLeft], missingTexture, thumbInstantiator));
            stickerIconCountLeft++;
        }

        public override void IconLoader()
        {
            Debug.Log("[AvatarMediaTray] IconLoader called - Loads up to 4 middle thumb items per call");
            for (int i = 0; i < 4; i++)
            {
                if (contentDataMiddle == null || stickerIconCountMiddle >= contentDataMiddle.Count)
                {
                    Debug.Log("[AvatarMediaTray] IconLoader: Done or no content - Cancelling invocation");
                    CancelInvoke("IconLoader");
                    break;
                }
                Debug.Log($"[AvatarMediaTray] IconLoader: Adding AvatarBaseGroupItem for index {stickerIconCountMiddle}");
                thumbItems.Add(new AvatarBaseGroupItem(contentHolderMiddle, 0, this, contentDataMiddle[stickerIconCountMiddle], missingTexture, thumbInstantiator));
                stickerIconCountMiddle++;
            }
        }

        public void SetSelected(string entitlementId)
        {
            Debug.Log($"[AvatarMediaTray] SetSelected called: entitlementId={entitlementId} - Sets toggle state for all thumbItems");
            for (int i = 0; i < thumbItems.Count; i++)
            {
                if (thumbItems[i].entitlement != null)
                {
                    AvatarBaseGroupItem avatarBaseGroupItem = thumbItems[i] as AvatarBaseGroupItem;
                    if (avatarBaseGroupItem != null)
                    {
                        bool isSelected = thumbItems[i].entitlement.GetUid() == entitlementId;
                        Debug.Log($"[AvatarMediaTray] SetSelected: thumbItem[{i}] entitlementId={thumbItems[i].entitlement.GetUid()}, isSelected={isSelected}");
                        avatarBaseGroupItem.SetToggleState(isSelected);
                    }
                }
            }
        }

        public override void ShowPreview(Vector2 aMousePosition)
        {
            Debug.Log($"[AvatarMediaTray] ShowPreview called: aMousePosition={aMousePosition} - Shows preview for the given mouse position");
        }

        public override void CancelPreview()
        {
            Debug.Log("[AvatarMediaTray] CancelPreview called - Cancels any active preview");
        }

        public void OnSubSelected(AvatarCategoryTrayInfo aInfo)
        {
            Debug.Log("[AvatarMediaTray] OnSubSelected called - Handles subcategory selection and moves content accordingly");
            int? num = moveDirection;
            if (num.HasValue)
            {
                int? num2 = moveDirection;
                if (num2.HasValue && num2.Value < 0)
                {
                    Debug.Log("[AvatarMediaTray] OnSubSelected: Moving left - Moves content and populates left");
                    MoveContent(MiddleContentHolder, RightContentHolder);
                    MoveContent(LeftContentHolder, MiddleContentHolder);
                    Populate(LeftContentHolder, -1);
                    goto IL_010f;
                }
            }
            int? num3 = moveDirection;
            if (num3.HasValue)
            {
                int? num4 = moveDirection;
                if (num4.HasValue && num4.Value > 0)
                {
                    Debug.Log("[AvatarMediaTray] OnSubSelected: Moving right - Moves content and populates right");
                    MoveContent(MiddleContentHolder, LeftContentHolder);
                    MoveContent(RightContentHolder, MiddleContentHolder);
                    Populate(RightContentHolder, 1);
                    goto IL_010f;
                }
            }
            Debug.Log("[AvatarMediaTray] OnSubSelected: Default population - Populates all trays");
            categorySelector.CurrentContent = aInfo;
            Populate(MiddleContentHolder, 0);
            Populate(LeftContentHolder, -1);
            Populate(RightContentHolder, 1);
            goto IL_010f;
            IL_010f:
            Debug.Log("[AvatarMediaTray] OnSubSelected: Notifying listener of content holder change");
            listener.OnContentHolderChanged();
            moveDirection = null;
        }
    }
}
