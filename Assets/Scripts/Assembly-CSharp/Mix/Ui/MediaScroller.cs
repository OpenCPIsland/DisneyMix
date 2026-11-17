using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Mix.Ui
{
	public class MediaScroller : MonoBehaviour
	{
		public enum DragState
		{
			Idle = 0,
			Preview = 1,
			Vertical = 2,
			Horizontal = 3
		}

		private enum Direction
		{
			Left = 0,
			Right = 1,
			Middle = 2
		}

		public BaseMediaTray MediaTray;

		public RectTransform scrollerMiddle;

		public RectTransform scrollerLeft;

		public RectTransform scrollerRight;

		private float delay;

		private bool dragging;

		private bool previewCheck;

		private float offsetMiddleX;

		private float offsetLeftX;

		private float offsetRightX;

		private float SNAP_THRESHOLD;

		private Vector2 startPosition;

		private ScrollRect scrollRectMiddle;

		private Direction animationDirection;

		private List<float> dragHistory = new List<float>();

		private float lastPosistion;

		private int touchIndex = -1;

		public bool IsAnimating { get; set; }

		public DragState CurrentDragState { get; set; }

		private static bool TryGetTouch(int touchId, out Touch touch)
		{
			foreach (Touch activeTouch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
			{
				if (activeTouch.touchId == touchId)
				{
					touch = activeTouch;
					return true;
				}
			}
			touch = default(Touch);
			return false;
		}

		private static bool TryGetTouchBegan(out Touch touch)
		{
			foreach (Touch activeTouch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
			{
				if (activeTouch.phase == UnityEngine.InputSystem.TouchPhase.Began)
				{
					touch = activeTouch;
					return true;
				}
			}
			touch = default(Touch);
			return false;
		}

		public void ResetPositions()
		{
			SNAP_THRESHOLD = MixConstants.CANVAS_WIDTH / 3f;
			scrollerLeft.sizeDelta = new Vector2(MixConstants.CANVAS_WIDTH, scrollerLeft.sizeDelta.y);
			scrollerLeft.anchoredPosition = new Vector2(0f - scrollerMiddle.sizeDelta.x, scrollerMiddle.anchoredPosition.y);
			scrollerMiddle.sizeDelta = new Vector2(MixConstants.CANVAS_WIDTH, scrollerMiddle.sizeDelta.y);
			scrollerMiddle.anchoredPosition = Vector2.zero;
			scrollerRight.sizeDelta = new Vector2(MixConstants.CANVAS_WIDTH, scrollerRight.sizeDelta.y);
			scrollerRight.anchoredPosition = new Vector2(scrollerMiddle.sizeDelta.x, scrollerMiddle.anchoredPosition.y);
			HideScroller(scrollerLeft);
			HideScroller(scrollerRight);
		}

		private void Start()
		{
			IsAnimating = false;
			ResetPositions();
		}

		private void FixedUpdate()
		{
			if (!IsAnimating)
			{
				return;
			}
			Vector2 vector = new Vector2(0f - scrollerLeft.sizeDelta.x, 0f);
			Vector2 vector2 = Vector2.zero;
			Vector2 vector3 = new Vector2(scrollerLeft.sizeDelta.x, 0f);
			float speed = 20f;
			switch (animationDirection)
			{
			case Direction.Left:
				vector = new Vector2(0f - scrollerLeft.sizeDelta.x * 2f, scrollerLeft.anchoredPosition.y);
				vector2 = new Vector2(0f - scrollerMiddle.sizeDelta.x, scrollerMiddle.anchoredPosition.y);
				vector3 = Vector2.zero;
				break;
			case Direction.Right:
				vector = Vector2.zero;
				vector2 = new Vector2(scrollerMiddle.sizeDelta.x, scrollerMiddle.anchoredPosition.y);
				vector3 = new Vector2(scrollerRight.sizeDelta.x * 2f, scrollerLeft.anchoredPosition.y);
				break;
			}
			scrollerLeft.anchoredPosition = Util.Vector2Update(scrollerLeft.anchoredPosition, vector, speed);
			scrollerMiddle.anchoredPosition = Util.Vector2Update(scrollerMiddle.anchoredPosition, vector2, speed);
			scrollerRight.anchoredPosition = Util.Vector2Update(scrollerRight.anchoredPosition, vector3, speed);
			if (Vector2.Distance(scrollerMiddle.anchoredPosition, vector2) < 8f)
			{
				scrollerLeft.anchoredPosition = vector;
				scrollerMiddle.anchoredPosition = vector2;
				scrollerRight.anchoredPosition = vector3;
				IsAnimating = false;
				if (animationDirection != Direction.Middle)
				{
					scrollRectMiddle.content.anchoredPosition = Vector2.zero;
					ResetPositions();
					MediaTray.LoadContent((animationDirection == Direction.Left) ? 1 : (-1));
				}
			}
		}

		private void Update()
		{
			if (IsAnimating)
			{
				if (scrollRectMiddle != null)
				{
					scrollRectMiddle.vertical = false;
				}
				return;
			}
			Vector2 vector = new Vector2(0f, 0f);
			bool flag = false;
			if (dragging)
			{
				if (touchIndex >= 0)
				{
					if (TryGetTouch(touchIndex, out Touch touch) && touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
					{
						vector = touch.screenPosition;
					}
					else
					{
						touchIndex = -1;
						flag = true;
					}
				}
				else if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.isPressed)
				{
					vector = UnityEngine.InputSystem.Pointer.current != null ? UnityEngine.InputSystem.Pointer.current.position.ReadValue() : Vector2.zero;
				}
				else
				{
					flag = true;
				}
			}
			if (!dragging)
			{
				bool touchBegan = TryGetTouchBegan(out Touch touch);
				Vector2 pointerPosition = touchBegan ? touch.screenPosition : UnityEngine.InputSystem.Pointer.current != null ? UnityEngine.InputSystem.Pointer.current.position.ReadValue() : Vector2.zero;
				if ((touchBegan || UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame) && Util.GetRectInScreenSpace(GetComponent<RectTransform>()).Contains(pointerPosition))
				{
					CurrentDragState = DragState.Idle;
					vector = pointerPosition;
					touchIndex = touchBegan ? touch.touchId : -1;
					scrollRectMiddle = scrollerMiddle.GetComponent<ScrollRect>();
					startPosition = GetCanvasPointFromScreenPoint(vector);
					offsetMiddleX = scrollerMiddle.anchoredPosition.x;
					offsetLeftX = scrollerLeft.anchoredPosition.x;
					offsetRightX = scrollerRight.anchoredPosition.x;
					lastPosistion = (delay = 0f);
					previewCheck = false;
					dragging = true;
					dragHistory.Clear();
				}
				return;
			}
			if (flag)
			{
				if (CurrentDragState == DragState.Preview)
				{
					MediaTray.CancelPreview();
				}
				if (CurrentDragState == DragState.Horizontal)
				{
					SnapToNewView();
				}
				dragging = false;
				CurrentDragState = DragState.Idle;
				scrollRectMiddle.vertical = true;
				return;
			}
			Vector2 canvasPointFromScreenPoint = GetCanvasPointFromScreenPoint(vector);
			float x = (startPosition - canvasPointFromScreenPoint).x;
			float y = (startPosition - canvasPointFromScreenPoint).y;
			float num = Vector2.Distance(startPosition, canvasPointFromScreenPoint);
			delay += Time.deltaTime;
			if (delay > 0.35f && !previewCheck)
			{
				previewCheck = true;
				if (num < 15f)
				{
					CurrentDragState = DragState.Preview;
				}
			}
			if (CurrentDragState == DragState.Idle && num >= 15f && delay > 0.05f)
			{
				CurrentDragState = ((!(Mathf.Abs(x) > Mathf.Abs(y * 2f))) ? DragState.Vertical : DragState.Horizontal);
			}
			switch (CurrentDragState)
			{
			case DragState.Horizontal:
				scrollRectMiddle.vertical = false;
				dragHistory.Add(lastPosistion - canvasPointFromScreenPoint.x);
				lastPosistion = canvasPointFromScreenPoint.x;
				while (dragHistory.Count > 3)
				{
					dragHistory.RemoveAt(0);
				}
				if (dragHistory.Sum() < 0f)
				{
					ShowScroller(scrollerLeft);
				}
				else if (dragHistory.Sum() > 0f)
				{
					ShowScroller(scrollerRight);
				}
				scrollerMiddle.anchoredPosition = new Vector2(offsetMiddleX - x, scrollerMiddle.anchoredPosition.y);
				scrollerLeft.anchoredPosition = new Vector2(offsetLeftX - x, scrollerMiddle.anchoredPosition.y);
				scrollerRight.anchoredPosition = new Vector2(offsetRightX - x, scrollerMiddle.anchoredPosition.y);
				break;
			case DragState.Vertical:
				scrollRectMiddle.vertical = true;
				break;
			case DragState.Preview:
				scrollRectMiddle.vertical = false;
				MediaTray.ShowPreview(vector);
				break;
			}
		}

		private void HideScroller(RectTransform scroller)
		{
			if (scroller.gameObject.activeSelf)
			{
				scroller.gameObject.SetActive(false);
			}
		}

		private void ShowScroller(RectTransform scroller)
		{
			if (!scroller.gameObject.activeSelf)
			{
				scroller.gameObject.SetActive(true);
			}
		}

		public void Shown()
		{
			scrollerMiddle.anchoredPosition = Vector2.zero;
			scrollerLeft.anchoredPosition = new Vector2(0f - scrollerLeft.sizeDelta.x, 0f);
			scrollerRight.anchoredPosition = new Vector2(scrollerLeft.sizeDelta.x, 0f);
			scrollerMiddle.Find("MediaContent").GetComponent<RectTransform>().anchoredPosition = new Vector2(scrollerMiddle.Find("MediaContent").GetComponent<RectTransform>().anchoredPosition.x, 0f);
			scrollerLeft.Find("MediaContent").GetComponent<RectTransform>().anchoredPosition = new Vector2(scrollerLeft.Find("MediaContent").GetComponent<RectTransform>().anchoredPosition.x, 0f);
			scrollerRight.Find("MediaContent").GetComponent<RectTransform>().anchoredPosition = new Vector2(scrollerRight.Find("MediaContent").GetComponent<RectTransform>().anchoredPosition.x, 0f);
			dragging = false;
			CurrentDragState = DragState.Idle;
			dragHistory.Clear();
		}

		private void SnapToNewView()
		{
			IsAnimating = true;
			bool flag = dragHistory.Sum() < -10f;
			bool flag2 = dragHistory.Sum() > 10f;
			if ((flag || scrollerMiddle.anchoredPosition.x > SNAP_THRESHOLD) && scrollerLeft.Find("MediaContent").childCount > 0)
			{
				animationDirection = Direction.Right;
			}
			else if ((flag2 || scrollerMiddle.anchoredPosition.x < 0f - SNAP_THRESHOLD) && scrollerRight.Find("MediaContent").childCount > 0)
			{
				animationDirection = Direction.Left;
			}
			else
			{
				animationDirection = Direction.Middle;
			}
		}

		private Vector2 GetCanvasPointFromScreenPoint(Vector2 aScreenPoint)
		{
			Canvas parentCanvas = Util.GetParentCanvas(base.gameObject);
			RectTransform component = parentCanvas.GetComponent<RectTransform>();
			Vector2 localPoint;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(component, aScreenPoint, parentCanvas.worldCamera, out localPoint);
			return localPoint;
		}
	}
}
