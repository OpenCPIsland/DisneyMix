using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class PressureSensitiveButton : Button, IPointerDownHandler, IEventSystemHandler, IPointerUpHandler
{
	public float Pressure;

	public bool HardPress;

	private bool press;

	private int pressId;

	private float pressureThreshholdPercent = 0.65f;

	private void Update()
	{
		if (press)
		{
			if (!TryGetTouch(pressId, out Touch touch))
			{
				press = false;
				return;
			}
			if (touch.pressure > Pressure)
			{
				Pressure = touch.pressure;
			}
			if (touch.pressure >= pressureThreshholdPercent)
			{
				HardPress = true;
				press = false;
				base.onClick.Invoke();
			}
		}
	}

	public override void OnPointerDown(PointerEventData data)
	{
		HardPress = false;
		base.OnPointerDown(data);
		ExtendedPointerEventData pointerData = data as ExtendedPointerEventData;
		Touch touch;
		if (pointerData != null && pointerData.pointerType == UIPointerType.Touch && TryGetTouch(pointerData.touchId, out touch))
		{
			press = true;
			pressId = touch.touchId;
			Pressure = 0f;
		}
	}

	public override void OnPointerUp(PointerEventData data)
	{
		press = false;
		HardPress = false;
		base.OnPointerUp(data);
	}

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
}
