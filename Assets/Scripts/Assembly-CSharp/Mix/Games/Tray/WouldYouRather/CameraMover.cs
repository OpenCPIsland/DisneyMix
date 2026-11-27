using System;
using DG.Tweening;
using UnityEngine;

namespace Mix.Games.Tray.WouldYouRather
{
	public class CameraMover : MonoBehaviour
	{
		public enum CameraState
		{
			NONE = 0,
			INTRO = 1,
			GAMEPLAY = 2,
			CONFIRMATION = 3,
			OUTRO = 4
		}

		public static Action OnTransitionToGameplayComplete = delegate
		{
		};

		public static Action OnTransitionToConfirmationComplete = delegate
		{
		};

		// CoreCLR / no domain reload: statics survive between play sessions, so handlers from a
		// destroyed WouldYouRatherGame could stay subscribed and throw before the live game's handler runs.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticStateForPlaySession()
		{
			OnTransitionToGameplayComplete = delegate
			{
			};
			OnTransitionToConfirmationComplete = delegate
			{
			};
		}

		[Header("Intro")]
		public MovementInfo introMovementInfo;

		[Header("Gameplay")]
		public MovementInfo gameplayMovementInfo;

		[Header("Confirmation")]
		public MovementInfo confirmationMovementInfo;

		public MovementInfo cancelMovementInfo;

		[Header("Outro")]
		public MovementInfo outroMovementInfo;

		[Header("Shuffle")]
		public MovementInfo shuffleMovementInfo;

		[HideInInspector]
		public bool snapCameraToGameplayPosition;

		private CameraState mCurrentCameraState;

		private Transform mTransform;

		private Camera mCamera;

		private void Start()
		{
			mTransform = base.transform;
			mCamera = GetComponent<Camera>();
			mCurrentCameraState = CameraState.NONE;
			GoToIntroState();
		}

		private void LateUpdate()
		{
			if (snapCameraToGameplayPosition && mCurrentCameraState == CameraState.GAMEPLAY)
			{
				base.transform.position = gameplayMovementInfo.target.position;
				base.transform.rotation = gameplayMovementInfo.target.rotation;
			}
		}

		public void GoToIntroState()
		{
			if (mCurrentCameraState == CameraState.NONE)
			{
				base.transform.position = introMovementInfo.target.position;
				base.transform.rotation = introMovementInfo.target.rotation;
				mCurrentCameraState = CameraState.INTRO;
			}
			else
			{
				Debug.LogError("Invalid Camera State.");
			}
		}

		public void GoToGameplayState()
		{
			if (mCurrentCameraState == CameraState.INTRO)
			{
				Move(gameplayMovementInfo, TransitionToGameplayComplete);
				mCurrentCameraState = CameraState.GAMEPLAY;
				snapCameraToGameplayPosition = false;
			}
			else if (mCurrentCameraState == CameraState.CONFIRMATION)
			{
				Move(cancelMovementInfo, TransitionToGameplayComplete);
				mCurrentCameraState = CameraState.GAMEPLAY;
				snapCameraToGameplayPosition = false;
			}
			else
			{
				Debug.LogError("Invalid Camera State.");
			}
		}

		public void GoToConfirmationState()
		{
			if (mCurrentCameraState == CameraState.GAMEPLAY)
			{
				Move(confirmationMovementInfo, TransitionToConfirmationComplete);
				mCurrentCameraState = CameraState.CONFIRMATION;
			}
			else if (mCurrentCameraState == CameraState.INTRO)
			{
				Move(confirmationMovementInfo, TransitionToConfirmationComplete);
				mCurrentCameraState = CameraState.CONFIRMATION;
			}
			else
			{
				Debug.LogError("Invalid Camera State.");
			}
		}

		public void GoToOutroState()
		{
			if (mCurrentCameraState == CameraState.CONFIRMATION)
			{
				Move(outroMovementInfo, null);
				mCurrentCameraState = CameraState.OUTRO;
			}
			else
			{
				Debug.LogError("Invalid Camera State.");
			}
		}

		public void GoToShuffleState()
		{
			if (mCurrentCameraState == CameraState.GAMEPLAY)
			{
				Move(shuffleMovementInfo, null);
				mCurrentCameraState = CameraState.INTRO;
			}
			else
			{
				Debug.LogError("Invalid Camera State.");
			}
		}

		public void TransitionToGameplayComplete()
		{
			OnTransitionToGameplayComplete();
			snapCameraToGameplayPosition = true;
		}

		public void TransitionToConfirmationComplete()
		{
			OnTransitionToConfirmationComplete();
		}

		private void Move(MovementInfo movementInfo, TweenCallback OnMoveCompleteDelegate)
		{
			Transform target = movementInfo.target;
			mTransform.DOMove(target.position, movementInfo.moveTime).SetDelay(movementInfo.moveDelay).SetEase(movementInfo.moveEaseType);
			Tweener t = mTransform.DORotate(target.rotation.eulerAngles, movementInfo.rotateTime).SetDelay(movementInfo.rotateDelay).SetEase(movementInfo.moveEaseType);
			Camera component = target.GetComponent<Camera>();
			if (mCamera.fieldOfView != component.fieldOfView)
			{
				mCamera.DOFieldOfView(component.fieldOfView, movementInfo.moveTime).SetDelay(movementInfo.moveDelay).SetEase(Ease.InOutQuad);
			}
			if (OnMoveCompleteDelegate != null)
			{
				t.OnComplete(OnMoveCompleteDelegate);
			}
		}
	}
}
