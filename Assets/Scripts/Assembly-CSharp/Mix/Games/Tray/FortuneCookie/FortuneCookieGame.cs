using System.Collections.Generic;
using DG.Tweening;
using Mix.Games.Data;
using Mix.Games.Tray.Common;
using Mix.Ui;
using UnityEngine;

namespace Mix.Games.Tray.FortuneCookie
{
	public class FortuneCookieGame : MonoBehaviour, IMixGame
	{
		private const string MUSIC_CUE_NAME = "FortuneCookie/MUS/Music";

		private const float CameraZoomDistance = 12f;

		public Camera GameCamera;

		public PaperGenerator PaperGenerator;

		public GameObject CookiePrefab;

		public CookieConfirmation Confirmation;

		public ToolTips FortuneCookieToolTips;

		public Transform CookieContainer;

		public Transform SpawnPointGroup;

		private Transform[] mSpawnPoints;

		private AnimationEvents mIntroCameraAnimEvents;

		public float TiltFactor;

		[Range(0f, 60f)]
		public float MaxHorizontalTilt;

		[Range(0f, 60f)]
		public float MaxVerticalTilt;

		public int MinCookies;

		public int MaxCookies;

		private int mNumberOfCookies;

		protected int mNumTrayCollisions;

		protected List<int> mUsedSpawnPoints;

		private FortuneCookieData mData;

		private FortuneCookieGameController mGameController;

		private string mFortune;

		private Cookie mSelectedCookie;

		private int mSelectionCount;

		public FortuneCookieGameController GameController
		{
			get
			{
				return mGameController;
			}
		}

		void IMixGame.Initialize(MixGameData aData)
		{
			mGameController = base.transform.parent.gameObject.GetComponent<FortuneCookieGameController>();
			mData = mGameController.GetGameData<FortuneCookieData>();
			mSpawnPoints = SpawnPointGroup.GetComponentsInChildren<Transform>();
			mIntroCameraAnimEvents = GetComponent<AnimationEvents>();
			if (mData.Type == MixGameDataType.INVITE)
			{
				mFortune = null;
				return;
			}
			GameOverResponse(true);
			PaperGenerator.RegenerateFortune(mData.Fortune);
		}

		void IMixGame.Pause()
		{
			BaseGameController.Instance.Session.SessionSounds.PauseSoundEvent("FortuneCookie/MUS/Music", base.gameObject);
			DOTween.PauseAll();
		}

		void IMixGame.Play()
		{
			DOVirtual.DelayedCall(0.1f, delegate
			{
				BaseGameController.Instance.Session.SessionSounds.PlayMusic("FortuneCookie/MUS/Music", base.gameObject);
			});
		}

		void IMixGame.Quit()
		{
			BaseGameController.Instance.Session.SessionSounds.StopSoundEvent("FortuneCookie/MUS/Music", base.gameObject);
			DOTween.KillAll();
		}

		void IMixGame.Resume()
		{
			DOVirtual.DelayedCall(0.1f, delegate
			{
				BaseGameController.Instance.Session.SessionSounds.UnpauseSoundEvent("FortuneCookie/MUS/Music", base.gameObject);
			});
			DOTween.PlayAll();
		}

		private void OnEnable()
		{
			TrayGamePhysics.ResetAllGameLayers();
			Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Game3D"), LayerMask.NameToLayer("Ignore Raycast"), false);
			if (mIntroCameraAnimEvents != null)
			{
				mIntroCameraAnimEvents.OnAnimationEnd += ShowTutorialToolTip;
			}
		}

		private void OnDisable()
		{
			if (mIntroCameraAnimEvents != null)
			{
				mIntroCameraAnimEvents.OnAnimationEnd -= ShowTutorialToolTip;
			}
		}

		private void Update()
		{
			if (mSelectedCookie == null)
			{
				TiltInput();
			}
			TouchInput();
		}

		private void TiltInput()
		{
			Vector3 acceleration = UnityEngine.InputSystem.Accelerometer.current != null ? UnityEngine.InputSystem.Accelerometer.current.acceleration.ReadValue() : Vector3.zero;
			Vector3 vector = new Vector3(acceleration.y, 0f - acceleration.x, 0f) * TiltFactor;
			float num = base.transform.rotation.eulerAngles.x + vector.x;
			if (num >= 360f - MaxVerticalTilt)
			{
				num -= 360f;
			}
			else if (num >= 270f)
			{
				num = 0f - MaxVerticalTilt;
			}
			num = Mathf.Clamp(num, 0f - MaxVerticalTilt, MaxVerticalTilt);
			float num2 = base.transform.rotation.eulerAngles.z + vector.y;
			if (num2 >= 360f - MaxHorizontalTilt)
			{
				num2 -= 360f;
			}
			else if (num2 >= 270f)
			{
				num2 = 0f - MaxHorizontalTilt;
			}
			num2 = Mathf.Clamp(num2, 0f - MaxHorizontalTilt, MaxHorizontalTilt);
			base.transform.rotation = Quaternion.Euler(num, 0f, num2);
		}

		private void TouchInput()
		{
			if (UnityEngine.InputSystem.Mouse.current == null || !UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
			{
				return;
			}
			Ray ray = GameCamera.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero);
			Debug.DrawLine(ray.origin, ray.origin + ray.direction * 100f, Color.red);
			RaycastHit hitInfo;
			if (!Physics.Raycast(ray, out hitInfo, 100f))
			{
				return;
			}
			Cookie component = hitInfo.transform.GetComponent<Cookie>();
			if (component != null)
			{
				mSelectionCount++;
				if (mSelectionCount == 1)
				{
					FortuneCookieToolTips.TutorialToolTip.Hide();
				}
				if (component.State == CookieState.Selected)
				{
					component.UpdateState(CookieState.NotSelected);
					Confirmation.HideAllButtons();
					mSelectedCookie = null;
				}
				else if (component.State == CookieState.NotSelected)
				{
					component.UpdateState(CookieState.Selected);
					Confirmation.ShowAllButtons();
					string[] array = new string[5] { "CookiePickUp_1", "CookiePickUp_2", "CookiePickUp_3", "CookiePickUp_4", "CookiePickUp_5" };
					BaseGameController.Instance.AudioManager.PlaySound(array[Random.Range(0, array.Length)]);
					mSelectedCookie = component;
				}
			}
		}

		protected void SpawnCookies()
		{
			BaseGameController.Instance.AudioManager.PlayMusic("Main");
			if (mNumberOfCookies != 0)
			{
				return;
			}
			BaseGameController.Instance.AudioManager.PlaySound("PlateSet");
			mUsedSpawnPoints = new List<int>();
			mNumberOfCookies = Random.Range(MinCookies, MaxCookies + 1);
			for (int i = 0; i < mNumberOfCookies; i++)
			{
				GameObject gameObject = Object.Instantiate(CookiePrefab);
				Cookie component = gameObject.GetComponent<Cookie>();
				component.GameCamera = GameCamera;
				int num = 0;
				while (mUsedSpawnPoints.Contains(num))
				{
					num = Random.Range(0, mSpawnPoints.Length);
				}
				mUsedSpawnPoints.Add(num);
				component.transform.position = mSpawnPoints[num].position;
				component.transform.Rotate(Random.insideUnitSphere * 360f);
				component.transform.SetParent(CookieContainer, true);
				component.CookieCollisionWithCookieEnter += OnCookieCollisionWithCookie;
				component.CookieCollisionWithTrayEnter += OnCookieCollisionWithTray;
			}
		}

		public void OnCookieConfirmed()
		{
			mFortune = PaperGenerator.GenerateRandomFortune();
			GameOverPost(mFortune);
		}

		public void OnCookieCanceled()
		{
			Confirmation.HideAllButtons();
			mSelectedCookie.UpdateState(CookieState.NotSelected);
			BaseGameController.Instance.AudioManager.PlaySound("CancelXButton");
			mSelectedCookie = null;
		}

		private void GameOverPost(string aString)
		{
			if (DebugSceneIndicator.IsMainScene)
			{
				mData.Fortune = aString;
				GameController.GameOver(mData);
			}
		}

		private void GameOverResponse(bool aResult)
		{
			if (DebugSceneIndicator.IsMainScene)
			{
				FortuneCookieResponse myResponse = mData.GetMyResponse(mData.Responses, GameController.PlayerId);
				myResponse.Fortune = mData.Fortune;
				myResponse.Attempts = 0;
				GameController.GameOver(myResponse);
			}
		}

		private void ShowTutorialToolTip()
		{
			if (mSelectionCount < 1)
			{
				FortuneCookieToolTips.TutorialToolTip.Show();
			}
		}

		protected void OnCookieCollisionWithCookie(object sender, CookieCollisionEnterEventArgs e)
		{
		}

		protected void OnCookieCollisionWithTray(object sender, CookieCollisionEnterEventArgs e)
		{
		}
	}
}
