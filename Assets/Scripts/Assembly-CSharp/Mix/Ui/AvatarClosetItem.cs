using System;
using Avatar;
using Disney.Mix.SDK;
using Mix.Avatar;
using Mix.DeviceDb;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class AvatarClosetItem : MonoBehaviour, IEventSystemHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
	{
		private const string FileName = "slot";

		private const string FileExt = ".json";

		public AvatarDragger avatarToDrag;

		public Image imageTarget;

		public Image imageTargetGeo;

		public IAvatar slotDna;

		public bool IsOpenSlot = true;

		private int slot = -1;

		private Action cancelAvatar;

		private Vector2 curMousePos;

		public Image AvatarImageTarget
		{
			get
			{
				return getImageTarget(slotDna);
			}
		}

		private void OnDestroy()
		{
			if (cancelAvatar != null)
			{
				cancelAvatar();
			}
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (!IsOpenSlot && avatarToDrag.OnBeginDrag(slot, slotDna, AvatarImageTarget.sprite, eventData))
			{
				AvatarImageTarget.gameObject.SetActive(false);
			}
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (!IsOpenSlot)
			{
				avatarToDrag.OnDrag(slot, eventData);
			}
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (!IsOpenSlot)
			{
				avatarToDrag.OnEndDrag(slot, eventData);
			}
		}

		public RectTransform GetBoundingBox()
		{
			return base.gameObject.GetComponent<RectTransform>();
		}

		public void OnAvatarDragged(int aSlot, IAvatar dna)
		{
			SaveSlot(aSlot, dna);
		}

		public void DeleteSlot(int index)
		{
			string key = "slot" + index.ToString("D2") + ".json";
			Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi.RemoveUserKey(key);
			AvatarImageTarget.gameObject.SetActive(false);
			getOtherImageTarget().SetActive(false);
			IsOpenSlot = true;
		}

		public void OnAvatarReturned()
		{
			AvatarImageTarget.gameObject.SetActive(true);
		}

		public void SaveSlot(int index, IAvatar aDna)
		{
			string text = string.Empty;
			if (MonoSingleton<AvatarManager>.Instance != null && MonoSingleton<AvatarManager>.Instance.api != null)
			{
				text = MonoSingleton<AvatarManager>.Instance.api.SerializeAvatar(aDna);
			}
			slotDna = AvatarApi.DeserializeAvatar(text);
			IsOpenSlot = false;
			if (Singleton<MixDocumentCollections>.Instance != null && Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi != null)
			{
				string key = "slot" + index.ToString("D2") + ".json";
				Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi.SaveUserValue(key, text);
				Debug.Log("[AvatarCloset] Saved slot " + index + " (" + text.Length + " chars)");
			}
			else
			{
				Debug.LogWarning("[AvatarCloset] Slot " + index + " not saved: local store unavailable");
			}
			Analytics.LogAvatarOutfitSaveAction();
			SetAvatarIcon(aDna);
		}

		public void LoadSlot(int index)
		{
			slot = index;
			string key = "slot" + index.ToString("D2") + ".json";
			string text = Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi.LoadUserValue(key);
			if (!string.IsNullOrEmpty(text))
			{
				IAvatar avatar = slotDna;
				try
				{
					slotDna = AvatarApi.DeserializeAvatar(text);
				}
				catch (Exception ex)
				{
					Debug.LogWarning("[AvatarCloset] Slot " + index + " has unreadable data: " + ex.Message);
					slotDna = null;
				}
				if (AvatarApi.ValidateAvatar(slotDna) && MonoSingleton<AvatarManager>.Instance.api.IsAvatarMultiplane(slotDna))
				{
					IsOpenSlot = false;
					SetAvatarIcon(slotDna);
					return;
				}
				Debug.LogWarning("[AvatarCloset] Slot " + index + " was saved but failed validation (valid=" + AvatarApi.ValidateAvatar(slotDna) + "): " + text);
				slotDna = avatar;
			}
			AvatarImageTarget.gameObject.SetActive(false);
			getOtherImageTarget().SetActive(false);
			IsOpenSlot = true;
		}

		private void SetAvatarIcon(IAvatar aDna)
		{
			if (cancelAvatar != null)
			{
				cancelAvatar();
			}
			int size = (int)AvatarImageTarget.GetComponent<RectTransform>().rect.height;
			SnapshotCallback snapshotCallback = delegate(bool success, Sprite sprite)
			{
				cancelAvatar = null;
				// The outfit is already saved; the icon is only its picture.
				// A snapshot can render but fail to be cached (success false
				// with a sprite), so show any sprite we got, and never mark a
				// saved slot as open because its picture failed: that made the
				// slot look empty and the next tap overwrote it.
				if (sprite != null)
				{
					if (!success)
					{
						Debug.LogWarning("[AvatarCloset] Slot " + slot + " snapshot rendered but wasn't cached");
					}
					AvatarImageTarget.sprite = sprite;
					AvatarImageTarget.gameObject.SetActive(true);
					getOtherImageTarget().SetActive(false);
				}
				else
				{
					Debug.LogWarning("[AvatarCloset] Slot " + slot + " snapshot failed (success=" + success + "); the outfit is still saved");
					AvatarImageTarget.gameObject.SetActive(false);
					getOtherImageTarget().SetActive(false);
				}
			};
			cancelAvatar = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(aDna, (AvatarFlags)0, size, snapshotCallback);
		}

		private Image getImageTarget(IAvatar avatar)
		{
			return (!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(avatar)) ? imageTarget : imageTargetGeo;
		}

		private GameObject getOtherImageTarget()
		{
			return (!(AvatarImageTarget != imageTarget)) ? imageTargetGeo.gameObject : imageTarget.gameObject;
		}
	}
}
