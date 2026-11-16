using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class TKTouch
{
	public readonly int fingerId;

	public Vector2 position;

	public Vector2 deltaPosition;

	public float deltaTime;

	public int tapCount;

	public TouchPhase phase = TouchPhase.Ended;

	public Vector2 previousPosition
	{
		get
		{
			return position - deltaPosition;
		}
	}

	public TKTouch(int fingerId)
	{
		this.fingerId = fingerId;
	}

	public TKTouch populateWithTouch(Touch touch)
	{
		position = touch.screenPosition;
		deltaPosition = touch.delta;
		deltaTime = Time.unscaledDeltaTime;
		tapCount = touch.tapCount;
		if (touch.phase == TouchPhase.Canceled)
		{
			phase = TouchPhase.Ended;
		}
		else
		{
			phase = touch.phase;
		}
		return this;
	}

	public override string ToString()
	{
		return string.Format("[TKTouch] fingerId: {0}, phase: {1}, position: {2}", fingerId, phase, position);
	}
}
