using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Avatar;
using Avatar.DataTypes;
using Avatar.Interfaces;
using Avatar.Multiplane;
using Disney.Mix.SDK;
using UnityEngine;

namespace Mix.AvatarInternal
{
	public class AvatarEngine
	{
		private const float AVATAR_COMPOSITE_TIMEOUT_TIME = 6f;

		public static string[] AvatarCategoryNames = new string[10] { "Skin", "Mouth", "Nose", "Eyes", "Brow", "Accessory", "Hair", "Costume", "Glow", "Hat" };

		private static AvatarPrint printer;

		public IAssetManager assetManager;

		public bool CompositeFreely = true;

		public bool isWorking;

		public MonoBehaviour monoEngine;

		public List<KeyValuePair<string, Action<Action>>> avatarsToConstruct;

		public AvatarEngine(MonoBehaviour aMonoEngine)
		{
			monoEngine = aMonoEngine;
			avatarsToConstruct = new List<KeyValuePair<string, Action<Action>>>();
		}

		public void Init(IAssetManager aAssetManager, AvatarsInitialized aInitListener)
		{
			assetManager = aAssetManager;
			LoadShaderMaterials(aInitListener);
		}

		public static void SetLogging(AvatarPrint aPrint)
		{
			printer = aPrint;
		}

		[Conditional("LOGGING_ENABLED")]
		public static void Log(string msg, AvatarAlertLevel level = AvatarAlertLevel.DEBUG)
		{
			if (printer != null)
			{
				printer(msg, level);
			}
		}

		private List<AvatarElement> RemoveNotRandomizable(List<AvatarElement> input)
		{
			List<AvatarElement> list = new List<AvatarElement>();
			for (int i = 0; i < input.Count; i++)
			{
				if (input[i].IsRandomizable)
				{
					list.Add(input[i]);
				}
			}
			return list;
		}

		public IAvatarProperty GetRandomPropertyForCategory(string catName)
		{
			ClientAvatarProperty clientAvatarProperty = new ClientAvatarProperty();
			List<AvatarElement> myAvatarDataByCategory = assetManager.GetMyAvatarDataByCategory(catName);
			List<AvatarElement> list = RemoveNotRandomizable(myAvatarDataByCategory);
			if (list.Count != 0)
			{
				int index = UnityEngine.Random.Range(0, list.Count - 1);
				clientAvatarProperty.SelectionKey = list[index].ReferenceId.ToString();
				clientAvatarProperty.TintIndex = 0;
				Color[] colorsByCategoryName = AvatarColorTints.GetColorsByCategoryName(catName);
				if (colorsByCategoryName != null)
				{
					clientAvatarProperty.TintIndex = UnityEngine.Random.Range(0, colorsByCategoryName.Length - 1);
				}
				clientAvatarProperty.XOffset = 0.0;
				clientAvatarProperty.YOffset = 0.0;
				return clientAvatarProperty;
			}
			return null;
		}

		public IAvatar GenerateRandomDna()
		{
			ClientAvatar clientAvatar = new ClientAvatar();
			clientAvatar.Accessory = GetRandomPropertyForCategory("Accessory");
			clientAvatar.Costume = new ClientAvatarProperty("0", 0, 0.0, 0.0);
			clientAvatar.Brow = GetRandomPropertyForCategory("Brow");
			clientAvatar.Eyes = GetRandomPropertyForCategory("Eyes");
			clientAvatar.Hair = GetRandomPropertyForCategory("Hair");
			clientAvatar.Mouth = GetRandomPropertyForCategory("Mouth");
			clientAvatar.Nose = GetRandomPropertyForCategory("Nose");
			clientAvatar.Skin = GetRandomPropertyForCategory("Skin");
			clientAvatar.Hat = new ClientAvatarProperty("0", 0, 0.0, 0.0);
			return clientAvatar;
		}

		public void CancelAvatarTextures(string id)
		{
			int num = -1;
			for (int i = 0; i < avatarsToConstruct.Count; i++)
			{
				if (avatarsToConstruct[i].Key.Equals(id))
				{
					num = i;
					break;
				}
			}
			if (num != -1)
			{
				avatarsToConstruct.RemoveAt(num);
			}
		}

		public void ResetAvatarSkin(GameObject avatarObj, string texturePath, Action callback)
		{
			MultiplanePrefab multiplanePrefab = new MultiplanePrefab(monoEngine, assetManager, avatarObj);
			multiplanePrefab.ResetAvatarObject(texturePath, callback);
		}

		public void BuildAvatar(IAvatar avatar, GameObject avatarObj, string id, AvatarFlags flags, TextureCallback textureCallback = null)
		{
			avatarsToConstruct.Add(new KeyValuePair<string, Action<Action>>(id, delegate(Action callback)
			{
				if (avatarObj.IsNullOrDisposed() || avatar == null)
				{
					if (textureCallback != null)
					{
						textureCallback(false);
					}
					callback();
				}
				else
				{
					MultiplanePrefab prefab = new MultiplanePrefab(monoEngine, assetManager, avatarObj);
					bool hasFinished = false;
					monoEngine.StartCoroutine(delayedAction(6f, delegate
					{
						if (!hasFinished)
						{
							hasFinished = true;
							prefab.CancelAvatar();
							if (textureCallback != null)
							{
								textureCallback(false);
							}
							callback();
						}
					}));
					prefab.CompositeAvatar(avatar, flags, delegate(bool success)
					{
						if (!hasFinished)
						{
							hasFinished = true;
							if (textureCallback != null)
							{
								textureCallback(success);
							}
							callback();
						}
					});
				}
			}));
			if (!isWorking)
			{
				isWorking = true;
				IterateOnQueue();
			}
		}

		public IEnumerator delayedAction(float time, Action action)
		{
			yield return new WaitForSeconds(time);
			if (action != null)
			{
				action();
			}
		}

		public void IterateOnQueue()
		{
			if (avatarsToConstruct.Count > 0)
			{
				KeyValuePair<string, Action<Action>> item = avatarsToConstruct[0];
				avatarsToConstruct.Remove(item);
				item.Value(IterateOnQueue);
			}
			else
			{
				isWorking = false;
			}
		}

		private void LoadShaderMaterials(AvatarsInitialized aInitListener)
		{
			aInitListener();
		}
	}
}
