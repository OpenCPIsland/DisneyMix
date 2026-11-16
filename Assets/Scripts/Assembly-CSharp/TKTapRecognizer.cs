using System;
using System.Collections.Generic;
using UnityEngine;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class TKTapRecognizer : TKAbstractGestureRecognizer
{
	public int numberOfTapsRequired = 1;

	public int numberOfTouchesRequired = 1;

	private float _maxDurationForTapConsideration = 0.5f;

	private float _maxDeltaMovementForTapConsideration = 5f;

	private float _touchBeganTime;

	public event Action<TKTapRecognizer> gestureRecognizedEvent;

	public TKTapRecognizer()
		: this(0.5f, 5f)
	{
	}

	public TKTapRecognizer(float maxDurationForTapConsideration, float maxDeltaMovementForTapConsideration)
	{
		_maxDurationForTapConsideration = maxDurationForTapConsideration;
		_maxDeltaMovementForTapConsideration = maxDeltaMovementForTapConsideration * TouchKit.instance.runtimeDistanceModifier;
	}

	internal override void fireRecognizedEvent()
	{
		if (this.gestureRecognizedEvent != null)
		{
			this.gestureRecognizedEvent(this);
		}
	}

	internal override bool touchesBegan(List<TKTouch> touches)
	{
		if (base.state == TKGestureRecognizerState.Possible)
		{
			for (int i = 0; i < touches.Count; i++)
			{
				if (touches[i].phase == TouchPhase.Began)
				{
					_trackingTouches.Add(touches[i]);
					if (_trackingTouches.Count == numberOfTouchesRequired)
					{
						break;
					}
				}
			}
			if (_trackingTouches.Count == numberOfTouchesRequired)
			{
				_touchBeganTime = Time.time;
				base.state = TKGestureRecognizerState.Began;
				return true;
			}
		}
		return false;
	}

	internal override void touchesMoved(List<TKTouch> touches)
	{
		if (base.state != TKGestureRecognizerState.Began)
		{
			return;
		}
		for (int i = 0; i < touches.Count; i++)
		{
			if (touches[i].deltaPosition.sqrMagnitude > _maxDeltaMovementForTapConsideration)
			{
				base.state = TKGestureRecognizerState.FailedOrEnded;
				break;
			}
		}
	}

	internal override void touchesEnded(List<TKTouch> touches)
	{
		if (base.state == TKGestureRecognizerState.Began && Time.time <= _touchBeganTime + _maxDurationForTapConsideration)
		{
			base.state = TKGestureRecognizerState.Recognized;
		}
		else
		{
			base.state = TKGestureRecognizerState.FailedOrEnded;
		}
	}
}
