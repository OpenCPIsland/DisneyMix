using System;
using System.Collections.Generic;
using System.Text;
using Avatar.DataTypes;
using Avatar.Interfaces;
using Disney.Mix.SDK;
using LitJson;
using Mix.AvatarInternal;
using UnityEngine;

namespace Avatar
{
	public class AvatarApi
	{
		private const float FLOAT_EPSILON = 0.0001f;

		private AvatarEngine engine;

		private IAssetManager assetManager;

		private Dictionary<string, TextureCallback> textureCallbacks;

		public AvatarApi(MonoBehaviour monoEngine)
		{
			Debug.Log("[AvatarApi] Constructor called");
			textureCallbacks = new Dictionary<string, TextureCallback>();
			engine = new AvatarEngine(monoEngine);
		}

		public void InitializeAvatars(IAssetManager assets, AvatarPrint logging, AvatarsInitialized aListener)
		{
			Debug.Log("[AvatarApi] InitializeAvatars called");
			assetManager = assets;
			AvatarEngine.SetLogging(logging);
			engine.Init(assets, aListener);
		}

		public void SetProcessing(bool value)
		{
			//Debug.Log($"[AvatarApi] SetProcessing: {value}");
			engine.CompositeFreely = value;
		}

		public void CancelAvatarComposite(IAvatar avatar, GameObject avatarObj, AvatarFlags flags, TextureCallback textureCallback)
		{
			Debug.Log("[AvatarApi] CancelAvatarComposite called");
			if (avatar == null)
			{
				Debug.LogWarning("[AvatarApi] CancelAvatarComposite: avatar is null");
				return;
			}
			string shaString = assetManager.GetShaString(string.Concat(flags, SerializeAvatar(avatar), avatarObj.GetHashCode()));
			if (textureCallbacks.ContainsKey(shaString))
			{
				Dictionary<string, TextureCallback> dictionary2;
				Dictionary<string, TextureCallback> dictionary = (dictionary2 = textureCallbacks);
				string key2;
				string key = (key2 = shaString);
				TextureCallback source = dictionary2[key2];
				dictionary[key] = (TextureCallback)Delegate.Remove(source, textureCallback);
				if (textureCallbacks[shaString] == null || textureCallbacks[shaString].GetInvocationList().Length == 0)
				{
					textureCallbacks.Remove(shaString);
					engine.CancelAvatarTextures(shaString);
				}
			}
		}

		public IAvatar GenerateRandomDna()
		{
			Debug.Log("[AvatarApi] GenerateRandomDna called");
			return engine.GenerateRandomDna();
		}

		public void ResetAvatarTextures(GameObject avatarObj, string texturePath, Action callback)
		{
			Debug.Log($"[AvatarApi] ResetAvatarTextures called: texturePath={texturePath}");
			engine.ResetAvatarSkin(avatarObj, texturePath, callback);
		}

		public void CompositeTextures(IAvatar avatar, GameObject avatarObj, AvatarFlags flags, TextureCallback textureCallback)
		{
			Debug.Log("[AvatarApi] CompositeTextures called");
			if (avatar == null)
			{
				Debug.LogWarning("[AvatarApi] CompositeTextures: avatar is null");
				if (textureCallback != null)
				{
					textureCallback(false, string.Empty);
				}
				return;
			}
			string shaString = assetManager.GetShaString(string.Concat(flags, SerializeAvatar(avatar), avatarObj.GetHashCode()));
			if (textureCallbacks.ContainsKey(shaString))
			{
				Dictionary<string, TextureCallback> dictionary2;
				Dictionary<string, TextureCallback> dictionary = (dictionary2 = textureCallbacks);
				string key2;
				string key = (key2 = shaString);
				TextureCallback a = dictionary2[key2];
				dictionary[key] = (TextureCallback)Delegate.Combine(a, textureCallback);
			}
			else
			{
				textureCallbacks.Add(shaString, textureCallback);
				CompositeTextureWrapper(avatar, avatarObj, flags, shaString);
			}
		}

		private void CompositeTextureWrapper(IAvatar avatar, GameObject avatarObj, AvatarFlags flags, string aCacheId)
		{
			Debug.Log("[AvatarApi] CompositeTextureWrapper called");
			engine.BuildAvatar(avatar, avatarObj, aCacheId, flags, delegate(bool aIsSuccess)
			{
				ReportTextureResults(aIsSuccess, flags, aCacheId);
			});
		}

		private void ReportTextureResults(bool aIsSuccess, AvatarFlags flags, string aCacheId)
		{
			Debug.Log($"[AvatarApi] ReportTextureResults: aIsSuccess={aIsSuccess}, flags={flags}, aCacheId={aCacheId}");
			string cacheId = (((flags & AvatarFlags.WithoutCaching) != 0) ? null : aCacheId);
			if (textureCallbacks.ContainsKey(aCacheId) && textureCallbacks[aCacheId] != null)
			{
				TextureCallback textureCallback = textureCallbacks[aCacheId];
				textureCallbacks.Remove(aCacheId);
				textureCallback(aIsSuccess, cacheId);
			}
		}

		public static bool AreAvatarsEqual(IAvatar avatar1, IAvatar avatar2)
		{
			if (avatar1 == null || avatar2 == null)
			{
				return avatar1 == null && avatar2 == null;
			}
			return AreAvatarPropertiesEqual(avatar1.Accessory, avatar2.Accessory) && AreAvatarPropertiesEqual(avatar1.Brow, avatar2.Brow) && AreAvatarPropertiesEqual(avatar1.Costume, avatar2.Costume) && AreAvatarPropertiesEqual(avatar1.Eyes, avatar2.Eyes) && AreAvatarPropertiesEqual(avatar1.Hair, avatar2.Hair) && AreAvatarPropertiesEqual(avatar1.Mouth, avatar2.Mouth) && AreAvatarPropertiesEqual(avatar1.Nose, avatar2.Nose) && AreAvatarPropertiesEqual(avatar1.Skin, avatar2.Skin) && AreAvatarPropertiesEqual(avatar1.Hat, avatar2.Hat);
		}

		public static bool AreAvatarPropertiesEqual(IAvatarProperty prop1, IAvatarProperty prop2)
		{
			if (prop1 == null || prop2 == null)
			{
				return prop1 == null && prop2 == null;
			}
			return prop1.SelectionKey.Equals(prop2.SelectionKey) && prop1.TintIndex == prop2.TintIndex && Math.Abs(prop1.XOffset - prop2.XOffset) <= 9.999999747378752E-05 && Math.Abs(prop1.YOffset - prop2.YOffset) <= 9.999999747378752E-05;
		}

		public static bool ValidateAvatar(IAvatar avatar)
		{
			return avatar != null && ValidateAvatarProperty(avatar.Accessory) && ValidateAvatarProperty(avatar.Brow) && ValidateAvatarProperty(avatar.Costume) && ValidateAvatarProperty(avatar.Eyes) && ValidateAvatarProperty(avatar.Hair) && ValidateAvatarProperty(avatar.Mouth) && ValidateAvatarProperty(avatar.Nose) && ValidateAvatarProperty(avatar.Skin) && ValidateAvatarProperty(avatar.Hat);
		}

		public static bool ValidateAvatarProperty(IAvatarProperty prop)
		{
			return prop != null && !string.IsNullOrEmpty(prop.SelectionKey);
		}

		private bool IsAvatarPropMultiplane(IAvatarProperty prop)
		{
			return assetManager.GetAvatarData(prop.SelectionKey) != null;
		}

		public bool IsAvatarMultiplane(IAvatar avatar)
		{
			return avatar != null && IsAvatarPropMultiplane(avatar.Accessory) && IsAvatarPropMultiplane(avatar.Brow) && IsAvatarPropMultiplane(avatar.Costume) && IsAvatarPropMultiplane(avatar.Eyes) && IsAvatarPropMultiplane(avatar.Hair) && IsAvatarPropMultiplane(avatar.Mouth) && IsAvatarPropMultiplane(avatar.Nose) && IsAvatarPropMultiplane(avatar.Skin) && IsAvatarPropMultiplane(avatar.Hat);
		}

		public static IAvatar DeserializeAvatar(string avatarString)
		{
			ClientAvatar clientAvatar = new ClientAvatar();
			JsonData jsonData = JsonMapper.ToObject(avatarString);
			JsonData propJson = jsonData[0]["Accessory"];
			clientAvatar.Accessory = DeserializeAvatarProperty(propJson);
			JsonData propJson2 = jsonData[1]["Brow"];
			clientAvatar.Brow = DeserializeAvatarProperty(propJson2);
			JsonData propJson3 = jsonData[2]["Costume"];
			clientAvatar.Costume = DeserializeAvatarProperty(propJson3);
			JsonData propJson4 = jsonData[3]["Eyes"];
			clientAvatar.Eyes = DeserializeAvatarProperty(propJson4);
			JsonData propJson5 = jsonData[4]["Hair"];
			clientAvatar.Hair = DeserializeAvatarProperty(propJson5);
			JsonData propJson6 = jsonData[5]["Mouth"];
			clientAvatar.Mouth = DeserializeAvatarProperty(propJson6);
			JsonData propJson7 = jsonData[6]["Nose"];
			clientAvatar.Nose = DeserializeAvatarProperty(propJson7);
			JsonData propJson8 = jsonData[7]["Skin"];
			clientAvatar.Skin = DeserializeAvatarProperty(propJson8);
			if (jsonData.Count == 9)
			{
				JsonData propJson9 = jsonData[8]["Hat"];
				clientAvatar.Hat = DeserializeAvatarProperty(propJson9);
			}
			return clientAvatar;
		}

		public static IAvatarProperty DeserializeAvatarProperty(JsonData propJson)
		{
			ClientAvatarProperty clientAvatarProperty = new ClientAvatarProperty();
			clientAvatarProperty.SelectionKey = (string)propJson["SelectionKey"];
			clientAvatarProperty.TintIndex = (int)propJson["TintIndex"];
			clientAvatarProperty.XOffset = (double)propJson["XOffset"];
			clientAvatarProperty.YOffset = (double)propJson["YOffset"];
			return clientAvatarProperty;
		}

		public string SerializeAvatar(IAvatar avatar, bool propertyOnly = false)
		{
			StringBuilder stringBuilder = new StringBuilder();
			JsonWriter jsonWriter = new JsonWriter(stringBuilder);
			avatar = avatar ?? new ClientAvatar();
			jsonWriter.WriteArrayStart();
			SerializeProperty(jsonWriter, avatar.Accessory, "Accessory", propertyOnly);
			SerializeProperty(jsonWriter, avatar.Brow, "Brow", propertyOnly);
			SerializeProperty(jsonWriter, avatar.Costume, "Costume", propertyOnly);
			SerializeProperty(jsonWriter, avatar.Eyes, "Eyes", propertyOnly);
			SerializeProperty(jsonWriter, avatar.Hair, "Hair", propertyOnly);
			SerializeProperty(jsonWriter, avatar.Mouth, "Mouth", propertyOnly);
			SerializeProperty(jsonWriter, avatar.Nose, "Nose", propertyOnly);
			SerializeProperty(jsonWriter, avatar.Skin, "Skin", propertyOnly);
			SerializeProperty(jsonWriter, avatar.Hat, "Hat", propertyOnly);
			jsonWriter.WriteArrayEnd();
			return stringBuilder.ToString();
		}

		public void SerializeProperty(JsonWriter writer, IAvatarProperty prop, string propName, bool propertyOnly = false)
		{
			if (prop == null)
			{
				prop = new ClientAvatarProperty(string.Empty, 0, 0.0, 0.0);
			}
			if (propertyOnly)
			{
				writer.Write(prop.SelectionKey);
				return;
			}
			writer.WriteObjectStart();
			writer.WritePropertyName(propName);
			writer.WriteObjectStart();
			writer.WritePropertyName("SelectionKey");
			writer.Write(prop.SelectionKey);
			writer.WritePropertyName("TintIndex");
			writer.Write(prop.TintIndex);
			writer.WritePropertyName("XOffset");
			writer.Write(prop.XOffset);
			writer.WritePropertyName("YOffset");
			writer.Write(prop.YOffset);
			if (assetManager != null)
			{
				writer.WritePropertyName("VersionInfo");
				writer.Write(assetManager.GetAvatarElementVersionInfo(assetManager.GetAvatarData(prop.SelectionKey)));
			}
			writer.WriteObjectEnd();
			writer.WriteObjectEnd();
		}
	}
}
