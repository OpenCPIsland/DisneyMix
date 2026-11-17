using DG.Tweening;
using Mix.Games.Tray.Friendzy.FSM;
using Mix.Games.Tray.Friendzy.Message;
using UnityEngine;

namespace Mix.Games.Tray.Friendzy.ResultAnimator
{
	public class ResultDeterminedState : IState
	{
		private ResultsAnimator mResultsAnim;

		public ResultDeterminedState(ResultsAnimator aResultsAnim)
		{
			mResultsAnim = aResultsAnim;
		}

		void IState.Enter()
		{
			mResultsAnim.ActivateDescriptionCanvas((int)(mResultsAnim.Player - 1), true);
		}

		void IState.Update()
		{
			if (mResultsAnim.LightAnim.SequenceStarted)
			{
				if (mResultsAnim.LightAnim.SequenceEnded)
				{
					mResultsAnim.EnterState(mResultsAnim.CameraMovement);
				}
				else if (mResultsAnim.LightAnim.SequenceSkippable && UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
				{
					DOTween.KillAll();
					FriendzyGame.PlaySound("ResultPositive", FriendzyGame.SOUND_PREFIX);
					mResultsAnim.LightAnim.SelectResult();
					mResultsAnim.EnterState(mResultsAnim.CameraMovement);
				}
			}
		}

		void IState.Exit()
		{
		}

		void IState.ReceiveMessage(IMessage aEventMessage)
		{
		}
	}
}
