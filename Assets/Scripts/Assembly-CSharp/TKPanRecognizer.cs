using System;
using System.Collections.Generic;
using UnityEngine;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class TKPanRecognizer : TKAbstractGestureRecognizer
{
	public Vector2 deltaTranslation;

	public int minimumNumberOfTouches = 1;

	public int maximumNumberOfTouches = 2;

	private Vector2 _previousLocation;

	public event Action<TKPanRecognizer> gestureRecognizedEvent;

	public event Action<TKPanRecognizer> gestureCompleteEvent;

	internal override void fireRecognizedEvent()
	{
		if (this.gestureRecognizedEvent != null)
		{
			this.gestureRecognizedEvent(this);
		}
	}

	internal override bool touchesBegan(List<TKTouch> touches)
	{
		if (base.state == TKGestureRecognizerState.Possible || ((base.state == TKGestureRecognizerState.Began || base.state == TKGestureRecognizerState.RecognizedAndStillRecognizing) && _trackingTouches.Count < maximumNumberOfTouches))
		{
			for (int i = 0; i < touches.Count; i++)
			{
				if (touches[i].phase == TouchPhase.Began)
				{
					_trackingTouches.Add(touches[i]);
					if (_trackingTouches.Count == maximumNumberOfTouches)
					{
						break;
					}
				}
			}
			if (_trackingTouches.Count >= minimumNumberOfTouches)
			{
				_previousLocation = touchLocation();
				if (base.state != TKGestureRecognizerState.RecognizedAndStillRecognizing)
				{
					base.state = TKGestureRecognizerState.Began;
				}
			}
		}
		return false;
	}

	internal override void touchesMoved(List<TKTouch> touches)
	{
		if (base.state == TKGestureRecognizerState.RecognizedAndStillRecognizing || base.state == TKGestureRecognizerState.Began)
		{
			Vector2 vector = touchLocation();
			deltaTranslation = vector - _previousLocation;
			_previousLocation = vector;
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
		if (_trackingTouches.Count >= minimumNumberOfTouches)
		{
			_previousLocation = touchLocation();
			base.state = TKGestureRecognizerState.RecognizedAndStillRecognizing;
			return;
		}
		if (base.state == TKGestureRecognizerState.RecognizedAndStillRecognizing && this.gestureCompleteEvent != null)
		{
			this.gestureCompleteEvent(this);
		}
		base.state = TKGestureRecognizerState.FailedOrEnded;
	}

	public override string ToString()
	{
		return string.Format("[{0}] state: {1}, location: {2}, deltaTranslation: {3}", GetType(), base.state, touchLocation(), deltaTranslation);
	}
}
