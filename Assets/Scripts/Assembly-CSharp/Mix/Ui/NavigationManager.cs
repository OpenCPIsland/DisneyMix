using System.Collections;
using System.Collections.Generic;
using Disney.Native;
using Mix.GagManagement;
using Mix.Native;
using Mix.Tracking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mix.Ui
{
	public class NavigationManager : MonoSingleton<NavigationManager>, INavigationManagerListener
	{
		private List<NavigationRequest> requests = new List<NavigationRequest>();

		private List<NavigationRequest> history = new List<NavigationRequest>();

		private Dictionary<string, GameObject> screenCache = new Dictionary<string, GameObject>();

		private NavigationRequest currentRequest;

		private GameObject uiScrim;

		private GameObject loadingScreen;

		private bool backButtonDown;

		private bool mIsOverlayRequest;

		public bool IsOverlayRequest
		{
			get
			{
				return mIsOverlayRequest;
			}
		}

		public event OnScreenLoaded ScreenLoadedEvent;

		GameObject INavigationManagerListener.GetCachedScreen(string aPrefabPath)
		{
			return (!screenCache.ContainsKey(aPrefabPath)) ? null : screenCache[aPrefabPath];
		}

		void INavigationManagerListener.OnRequestComplete(NavigationRequest aRequest)
		{
			if ((aRequest.PrefabPath.Equals("Prefabs/Screens/Profile/ProfileScreen") || aRequest.PrefabPath.Equals("Prefabs/Screens/Conversations/ConversationsScreen") || aRequest.PrefabPath.Equals("Prefabs/Screens/Friends/FriendsScreen")) && !screenCache.ContainsKey(aRequest.PrefabPath))
			{
				screenCache.Add(aRequest.PrefabPath, aRequest.ScreenHolder.PrefabObject);
			}
			if (!aRequest.IsOverlay)
			{
				if (aRequest.IsGoingBack)
				{
					history[history.Count - 2].SendEvent("OnUIUnLoaded");
					history[history.Count - 2].ScreenHolder.Destroy();
					history.RemoveAt(history.Count - 2);
					history.RemoveAt(history.Count - 2);
				}
				else if (history.Count > 1)
				{
					history[history.Count - 2].SendEvent("OnUIUnLoaded");
					history[history.Count - 2].ScreenHolder.Destroy();
					if (aRequest.PrefabPath == "Prefabs/Screens/Login/LoginScreen" && history.Count > 1)
					{
						history.Clear();
						history.Add(aRequest);
					}
					else if (aRequest.PopLastRequest)
					{
						history.RemoveAt(history.Count - 2);
					}
				}
			}
			else if (aRequest.Remove)
			{
				aRequest.ScreenHolder.Destroy();
			}
			requests.Remove(aRequest);
			StartCoroutine(SendEndEvent(aRequest));
		}

		List<ScreenHolder> INavigationManagerListener.GetCurrentGameObjects()
		{
			List<ScreenHolder> list = new List<ScreenHolder>();
			foreach (NavigationRequest item in history)
			{
				if (item.ScreenHolder.IsActive())
				{
					list.Add(item.ScreenHolder);
				}
			}
			list.Reverse();
			return list;
		}

		ScreenHolder INavigationManagerListener.GetLastScreenHolder()
		{
			if (currentRequest != null && currentRequest.IsOverlay)
			{
				return history[history.Count - 1].ScreenHolder;
			}
			if (history.Count < 2)
			{
				return null;
			}
			NavigationRequest navigationRequest = history[history.Count - 2];
			return navigationRequest.ScreenHolder;
		}

		public void AddRequest(NavigationRequest aRequest, bool onlyAddIfNotPresent = false)
		{
			if (onlyAddIfNotPresent)
			{
				NavigationRequest lastProcessedRequest = GetLastProcessedRequest();
				if (lastProcessedRequest != null && lastProcessedRequest.PrefabPath == aRequest.PrefabPath)
				{
					return;
				}
			}
			aRequest.Caller = this;
			requests.Add(aRequest);
			SetUiScrimVisible(true);
		}

		public void RemoveOverlay(NavigationRequest aRequest, BaseNavigationTransition aTransition)
		{
			if (aRequest.IsOverlay)
			{
				aRequest.Reset();
				aRequest.Transition = aTransition;
				aRequest.Remove = true;
				AddRequest(aRequest);
			}
		}

		public bool SendData(string aPrefabPath, string aToken, object aData)
		{
			bool result = false;
			foreach (NavigationRequest request in requests)
			{
				if (request.CurrentState > NavigationRequest.State.loading && request.PrefabPath.Equals(aPrefabPath))
				{
					request.AddData(aToken, aData);
					result = true;
				}
			}
			foreach (NavigationRequest item in history)
			{
				if (item.ScreenHolder.IsActive() && item.PrefabPath.Equals(aPrefabPath))
				{
					item.AddData(aToken, aData);
					item.ProcessDataMessages();
					result = true;
				}
			}
			return result;
		}

		public NavigationRequest GetLastProcessedRequest()
		{
			NavigationRequest result = null;
			if (history.Count >= 1)
			{
				result = history[history.Count - 1];
			}
			return result;
		}

		public NavigationRequest FindCurrentRequest()
		{
			return (requests.Count <= 0) ? null : requests[0];
		}

		public T GetLastProcessedRequestController<T>()
		{
			NavigationRequest lastProcessedRequest = GetLastProcessedRequest();
			if (lastProcessedRequest != null && lastProcessedRequest.ScreenHolder != null)
			{
				T component = lastProcessedRequest.ScreenHolder.MainGameObject.GetComponent<T>();
				if (component != null)
				{
					return component;
				}
			}
			return default(T);
		}

		private void Start()
		{
			uiScrim = GameObject.Find("/UI_Scrim_Layer/UI_Scrim");
			loadingScreen = GameObject.Find("/Screens/LoadingScreen");
		}

		private void Update()
		{
			if (UnityEngine.Application.platform == RuntimePlatform.Android || UnityEngine.Application.platform != RuntimePlatform.IPhonePlayer)
			{
				if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Escape].isPressed && !backButtonDown)
				{
					backButtonDown = true;
					if (GetLastProcessedRequest() != null)
					{
						if (!MonoSingleton<NativeVideoPlaybackManager>.Instance.IsNullOrDisposed() && MonoSingleton<NativeVideoPlaybackManager>.Instance.Native.IsFullScreen())
						{
							MonoSingleton<NativeVideoPlaybackManager>.Instance.Native.Unload();
						}
						else
						{
							GetLastProcessedRequest().SendEvent("OnAndroidBackButtonClicked");
						}
					}
				}
				else if (UnityEngine.InputSystem.Keyboard.current == null || !UnityEngine.InputSystem.Keyboard.current[UnityEngine.InputSystem.Key.Escape].isPressed)
				{
					backButtonDown = false;
				}
			}
			currentRequest = FindCurrentRequest();
			if (currentRequest == null)
			{
				SetUiScrimVisible(false);
				return;
			}
			SetUiScrimVisible(true);
			if (currentRequest.CurrentState == NavigationRequest.State.pending)
			{
				if (!MonoSingleton<NativeKeyboardManager>.Instance.IsNullOrDisposed())
				{
					MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
				}
				StartRequest(currentRequest);
			}
			else if (currentRequest.CurrentState >= NavigationRequest.State.loading)
			{
				if (loadingScreen != null)
				{
					Object.DestroyImmediate(loadingScreen);
					loadingScreen = null;
				}
				currentRequest.UpdateRequest();
			}
		}

		private void StartRequest(NavigationRequest aRequest)
		{
			mIsOverlayRequest = ((!aRequest.Remove && aRequest.IsOverlay) ? true : false);
			if (!aRequest.IsOverlay)
			{
				MonoSingleton<GagManager>.Instance.ClearGags();
				history.Add(aRequest);
			}
			aRequest.StartRequest();
		}

		private void SetUiScrimVisible(bool visible)
		{
			if (uiScrim != null)
			{
				uiScrim.SetActive(visible);
			}
		}

		private IEnumerator SendEndEvent(NavigationRequest aRequest)
		{
			yield return new WaitForSeconds(0f);
			if (loadingScreen != null)
			{
				Object.DestroyImmediate(loadingScreen);
				loadingScreen = null;
			}
			if (this.ScreenLoadedEvent != null)
			{
				this.ScreenLoadedEvent(aRequest.PrefabPath);
			}
			aRequest.SendEvent("OnUITransitionEnd");
			Singleton<TechAnalytics>.Instance.SetAppLoaded();
		}
	}
}
