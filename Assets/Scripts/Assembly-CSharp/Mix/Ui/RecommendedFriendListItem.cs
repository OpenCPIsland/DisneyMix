using System;
using Avatar;
using Disney.Mix.SDK;
using Disney.MobileNetwork;
using Mix.Avatar;
using Mix.Session;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class RecommendedFriendListItem : BaseFriendsListItem, IScrollItem, IScrollItemHelper
	{
		private enum AnimationType
		{
			Idle = 0,
			Show = 1,
			Hide = 2
		}

		public interface IRecommendedFriendListItem
		{
			void OnRecommendedFriendListItemClicked(IUnidentifiedUser user);
		}

		private sealed class GenerateGameObject_003Ec__AnonStorey2C6
		{
			internal Transform imageTarget;

			internal EventHandler<AbstractAvatarChangedEventArgs> avatarUpdater;

			internal RecommendedFriendListItem _003C_003Ef__this;

			internal void _003C_003Em__5F8()
			{
				_003C_003Ef__this.listener.OnRecommendedFriendListItemClicked(_003C_003Ef__this.User);
			}

			internal void _003C_003Em__5F9(bool success, Sprite sprite)
			{
				_003C_003Ef__this.cancelSnapshot = null;
				if (sprite != null && imageTarget != null && imageTarget.GetComponent<Image>() != null)
				{
					imageTarget.GetComponent<Image>().sprite = sprite;
					imageTarget.gameObject.SetActive(true);
				}
			}

			internal void _003C_003Em__5FA(object sender, AbstractAvatarChangedEventArgs args)
			{
				if (_003C_003Ef__this.cancelSnapshot != null)
				{
					_003C_003Ef__this.cancelSnapshot();
				}
				if (imageTarget != null && MonoSingleton<AvatarManager>.Instance != null)
				{
					_003C_003Ef__this.cancelSnapshot = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(args.Avatar, (AvatarFlags)0, _003C_003Ef__this.imageTargetSize, _003C_003Ef__this.snapshotDelegate);
				}
			}

			internal void _003C_003Em__5FB()
			{
				_003C_003Ef__this.User.OnAvatarChanged -= _003C_003Ef__this.eventGenerator.GetEventHandler(_003C_003Ef__this.User, avatarUpdater);
			}
		}

		private Toggle toggle;

		private GameObject prefab;

		private IRecommendedFriendListItem listener;

		private GameObject instance;

		private string layer;

		private Action cancelSnapshot;

		private Action avatarUpdateCleanup;

		private int imageTargetSize;

		private SnapshotCallback snapshotDelegate;

		private SdkEvents eventGenerator = new SdkEvents();

		public IUnidentifiedUser User { get; set; }

		public RecommendedFriendListItem(GameObject aPrefab, IUnidentifiedUser user, IRecommendedFriendListItem aListener, string aLayer = "UI")
		{
			prefab = aPrefab;
			User = user;
			listener = aListener;
			layer = aLayer;
		}

		GameObject IScrollItem.GenerateGameObject(bool aGenerateForHeightOnly)
		{
			GenerateGameObject_003Ec__AnonStorey2C6 CS_0024_003C_003E8__locals22 = new GenerateGameObject_003Ec__AnonStorey2C6();
			CS_0024_003C_003E8__locals22._003C_003Ef__this = this;
			GameObject gameObject = UnityEngine.Object.Instantiate(prefab);
			if (BaseFriendsListItem.GAMEOBJECT_HEIGHT < 1f)
			{
				BaseFriendsListItem.GAMEOBJECT_HEIGHT = instance.GetComponent<RectTransform>().sizeDelta.y;
			}
			if (aGenerateForHeightOnly)
			{
				return gameObject;
			}
			instance = gameObject;
			instance.transform.Find("FriendInfo/FriendNameText").GetComponent<Text>().text = User.FirstName;
			instance.transform.Find("FriendInfo/DisplayNameText").GetComponent<Text>().text = MixChat.FormatDisplayName(User.DisplayName.Text);
			instance.transform.Find("FriendInfo/RequestTrustBtn").gameObject.SetActive(false);
			GameObject gameObject2 = instance.transform.Find("FriendInfo/AddFriendBtn").gameObject;
			gameObject2.SetActive(true);
			gameObject2.GetComponent<Button>().onClick.AddListener(delegate
			{
				CS_0024_003C_003E8__locals22._003C_003Ef__this.listener.OnRecommendedFriendListItemClicked(CS_0024_003C_003E8__locals22._003C_003Ef__this.User);
			});
			gameObject.GetComponent<Button>().transition = Selectable.Transition.None;
			Util.SetLayerRecursively(gameObject, LayerMask.NameToLayer(layer));
			Text component = instance.transform.Find("FriendInfo/FriendNameText").GetComponent<Text>();
			int num = component.fontSize;
			component.fontSize = ((num >= 48) ? 48 : num);
			CS_0024_003C_003E8__locals22.imageTarget = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(User.Avatar)) ? gameObject.transform.Find("FriendInfo/AvatarImage/ImageTarget") : gameObject.transform.Find("FriendInfo/AvatarImage/Mask/ImageTarget_Geo"));
			imageTargetSize = (int)CS_0024_003C_003E8__locals22.imageTarget.GetComponent<RectTransform>().rect.height;
			snapshotDelegate = delegate(bool success, Sprite sprite)
			{
				CS_0024_003C_003E8__locals22._003C_003Ef__this.cancelSnapshot = null;
				if (sprite != null && CS_0024_003C_003E8__locals22.imageTarget != null && CS_0024_003C_003E8__locals22.imageTarget.GetComponent<Image>() != null)
				{
					CS_0024_003C_003E8__locals22.imageTarget.GetComponent<Image>().sprite = sprite;
					CS_0024_003C_003E8__locals22.imageTarget.gameObject.SetActive(true);
				}
			};
			cancelSnapshot = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(User.Avatar, (AvatarFlags)0, imageTargetSize, snapshotDelegate);
			CS_0024_003C_003E8__locals22.avatarUpdater = delegate(object sender, AbstractAvatarChangedEventArgs args)
			{
				if (CS_0024_003C_003E8__locals22._003C_003Ef__this.cancelSnapshot != null)
				{
					CS_0024_003C_003E8__locals22._003C_003Ef__this.cancelSnapshot();
				}
				if (CS_0024_003C_003E8__locals22.imageTarget != null && MonoSingleton<AvatarManager>.Instance != null)
				{
					CS_0024_003C_003E8__locals22._003C_003Ef__this.cancelSnapshot = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(args.Avatar, (AvatarFlags)0, CS_0024_003C_003E8__locals22._003C_003Ef__this.imageTargetSize, CS_0024_003C_003E8__locals22._003C_003Ef__this.snapshotDelegate);
				}
			};
			User.OnAvatarChanged += eventGenerator.AddEventHandler(User, CS_0024_003C_003E8__locals22.avatarUpdater);
			avatarUpdateCleanup = delegate
			{
				CS_0024_003C_003E8__locals22._003C_003Ef__this.User.OnAvatarChanged -= CS_0024_003C_003E8__locals22._003C_003Ef__this.eventGenerator.GetEventHandler(CS_0024_003C_003E8__locals22._003C_003Ef__this.User, CS_0024_003C_003E8__locals22.avatarUpdater);
			};
			return gameObject;
		}

		void IScrollItem.Destroy()
		{
			if (cancelSnapshot != null)
			{
				cancelSnapshot();
			}
			if (avatarUpdateCleanup != null)
			{
				avatarUpdateCleanup();
			}
		}

		public float GetGameObjectHeight()
		{
			return BaseFriendsListItem.GAMEOBJECT_HEIGHT;
		}
	}
}
