using System;
using System.Collections.Generic;
using UnityEngine;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class TKRotationRecognizer : TKAbstractGestureRecognizer
{
	public float deltaRotation;

	public float minimumDeltaRotationToRecognize;

	protected float _previousRotation;

	public event Action<TKRotationRecognizer> gestureRecognizedEvent;

	public event Action<TKRotationRecognizer> gestureCompleteEvent;

	public static float angleBetweenPoints(Vector2 position1, Vector2 position2)
	{
		Vector2 vector = position2 - position1;
		Vector2 vector2 = new Vector2(1f, 0f);
		float num = Vector2.Angle(vector, vector2);
		if (Vector3.Cross(vector, vector2).z > 0f)
		{
			num = 360f - num;
		}
		return num;
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
				deltaRotation = 0f;
				_previousRotation = angleBetweenPoints(_trackingTouches[0].position, _trackingTouches[1].position);
				base.state = TKGestureRecognizerState.Began;
			}
		}
		return false;
	}

	internal override void touchesMoved(List<TKTouch> touches)
	{
		if (base.state == TKGestureRecognizerState.RecognizedAndStillRecognizing || base.state == TKGestureRecognizerState.Began)
		{
			float num = angleBetweenPoints(_trackingTouches[0].position, _trackingTouches[1].position);
			deltaRotation = Mathf.DeltaAngle(num, _previousRotation);
			_previousRotation = num;
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
			deltaRotation = 0f;
		}
		else
		{
			base.state = TKGestureRecognizerState.FailedOrEnded;
		}
	}

	public override string ToString()
	{
		return string.Format("[{0}] state: {1}, location: {2}, rotation: {3}", GetType(), base.state, touchLocation(), deltaRotation);
	}
}
