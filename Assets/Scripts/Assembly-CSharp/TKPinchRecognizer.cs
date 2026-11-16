using System;
using System.Collections.Generic;
using UnityEngine;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class TKPinchRecognizer : TKAbstractGestureRecognizer
{
	public float deltaScale;

	private float _intialDistance;

	private float _previousDistance;

	public event Action<TKPinchRecognizer> gestureRecognizedEvent;

	public event Action<TKPinchRecognizer> gestureCompleteEvent;

	private float distanceBetweenTrackedTouches()
	{
		return Vector2.Distance(_trackingTouches[0].position, _trackingTouches[1].position);
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
					if (_trackingTouches.Count == 2)
					{
						break;
					}
				}
			}
			if (_trackingTouches.Count == 2)
			{
				deltaScale = 0f;
				_intialDistance = distanceBetweenTrackedTouches();
				_previousDistance = _intialDistance;
				base.state = TKGestureRecognizerState.RecognizedAndStillRecognizing;
			}
		}
		return false;
	}

	internal override void touchesMoved(List<TKTouch> touches)
	{
		if (base.state == TKGestureRecognizerState.RecognizedAndStillRecognizing)
		{
			float num = distanceBetweenTrackedTouches();
			deltaScale = (num - _previousDistance) / _intialDistance;
			_previousDistance = num;
			base.state = TKGestureRecognizerState.RecognizedAndStillRecognizing;
		}
	}

	internal override void touchesEnded(List<TKTouch> touches)
	{
		for (int i = 0; i < touches.Count; i++)
		{
			if (touches[i].phase == TouchPhase.Ended)
			{
				_trackingTouches.Remove(touches[i]);
			}
		}
		if (base.state == TKGestureRecognizerState.RecognizedAndStillRecognizing && this.gestureCompleteEvent != null)
		{
			this.gestureCompleteEvent(this);
		}
		if (_trackingTouches.Count == 1)
		{
			base.state = TKGestureRecognizerState.Possible;
			deltaScale = 1f;
		}
		else
		{
			base.state = TKGestureRecognizerState.FailedOrEnded;
		}
	}

	public override string ToString()
	{
		return string.Format("[{0}] state: {1}, deltaScale: {2}", GetType(), base.state, deltaScale);
	}
}
