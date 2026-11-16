using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Mix.Games.Tray.Friendzy.Menu
{
	[RequireComponent(typeof(Collider2D))]
	[RequireComponent(typeof(FlipPanel))]
	public class MenuButton : MonoBehaviour
	{
		private const float DEPRESSED_AMOUNT = 60f;

		private bool ClickedDown;

		public UnityEvent onClick;

		private FlipPanel mFlipPanel;

		private Collider2D mCollider;

		public Color DEPRESSED_COLOR;

		public Color NORMAL_COLOR;

		private void Awake()
		{
			mFlipPanel = GetComponent<FlipPanel>();
			mCollider = GetComponent<Collider2D>();
			InitButtonBehavior();
		}

		private void InitButtonBehavior()
		{
			if (onClick == null)
			{
				onClick = new UnityEvent();
			}
			ClickedDown = false;
		}

		private void Update()
		{
			bool pointerOverButton = IsPointerOverButton();
			if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame && pointerOverButton && MenuAnimator.InputEnabled)
			{
				ClickedDown = true;
				DepressFlipBar(DEPRESSED_AMOUNT);
			}
			if (!ClickedDown)
			{
				return;
			}
			if (!pointerOverButton)
			{
				ClickedDown = false;
				DepressFlipBar(0f);
				return;
			}
			if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame)
			{
				ClickedDown = false;
				DepressFlipBar(0f);
				onClick.Invoke();
			}
		}

		private bool IsPointerOverButton()
		{
			UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
			return mainCamera != null && mCollider.OverlapPoint(mainCamera.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero));
		}

		private void DepressFlipBar(float aAmount)
		{
			Color aColor;
			if (aAmount > 1f)
			{
				aColor = DEPRESSED_COLOR;
				FriendzyGame.PlaySound("ClickIn", FriendzyGame.SOUND_PREFIX);
			}
			else
			{
				aColor = NORMAL_COLOR;
				FriendzyGame.PlaySound("ClickOut", FriendzyGame.SOUND_PREFIX);
			}
			AlterFrontPanelColor(aColor);
			mFlipPanel.transformToFollow.DOLocalMoveZ(aAmount, 0.15f);
		}

		public void AlterFrontPanelColor(Color aColor)
		{
			mFlipPanel.AlterFrontPanelColor(aColor);
		}
	}
}
