using Disney.MobileNetwork;
using Fabric;
using UnityEngine;

namespace Mix
{
	public class SoundManager : Singleton<SoundManager>
	{
		public void BuildSoundSticker(GameObject aGameObject, bool aAutoplay)
		{
			AudioStickerController audioStickerController = aGameObject.AddComponent<AudioStickerController>();
			audioStickerController.autoplay = aAutoplay;
		}

		public void StopAllAudioSourcesFromRoot(Transform mRoot, GameObject mExemption = null)
		{
			AudioSource[] array = ((!(mRoot != null)) ? Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.InstanceID) : mRoot.GetComponentsInChildren<AudioSource>(false));
			AudioSource[] array2 = array;
			foreach (AudioSource audioSource in array2)
			{
				if (!audioSource.gameObject.Equals(mExemption))
				{
					audioSource.Stop();
				}
			}
		}

		public void PlaySoundEvent(string eventName)
		{
			EventManager.Instance.PostEvent(eventName);
		}

		public void SetVolumeEvent(string eventName, float volume)
		{
			EventManager.Instance.PostEvent(eventName, EventAction.SetVolume, volume);
		}

		public void PlayMusic(string eventName, GameObject gameObject = null)
		{
			if ((bool)gameObject)
			{
				PlaySoundEvent(eventName, gameObject);
			}
			else
			{
				PlaySoundEvent(eventName);
			}
		}

		public void PlaySoundEvent(string eventName, GameObject gameObject)
		{
			EventManager.Instance.PostEvent(eventName, gameObject);
		}

		public void PlaySoundEvent(string eventName, object parameter)
		{
			EventManager.Instance.PostEvent(eventName, EventAction.PlaySound, parameter);
		}

		public void StopSoundEvent(string eventName)
		{
			EventManager.Instance.PostEvent(eventName, EventAction.StopSound);
		}

		public void StopSoundEvent(string eventName, GameObject gameObject)
		{
			EventManager.Instance.PostEvent(eventName, EventAction.StopSound, gameObject);
		}

		public void PauseSoundEvent(string eventName, GameObject gameObject)
		{
			EventManager.Instance.PostEvent(eventName, EventAction.PauseSound, gameObject);
		}

		public void UnpauseSoundEvent(string eventName, GameObject gameObject)
		{
			EventManager.Instance.PostEvent(eventName, EventAction.UnpauseSound, gameObject);
		}

		public void SetSwitchEvent(string eventName, string switchEventName, GameObject gameObject)
		{
			EventManager.Instance.PostEvent(eventName, EventAction.SetSwitch, switchEventName, gameObject);
		}

		public void DisableSound()
		{
			AudioListener.volume = 0f;
		}

		public void EnableSound()
		{
			AudioListener.volume = 1f;
		}

		public void OnApplicationPause(bool paused)
		{
			if (!paused && AccessibilityManager.Instance.IsOtherAudioPlaying())
			{
				StopAllAudioSourcesFromRoot(null);
			}
		}
	}
}
