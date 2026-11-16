using System;
using System.Collections;
using Avatar;
using DG.Tweening;
using Disney.Mix.SDK;
using Mix;
using Mix.Avatar;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mix.Ui
{
	public class AvatarSpinner : MonoBehaviour
	{
		public enum SpinState
		{
			SPINNING_UP = 0,
			SPIN_MAX_SPEED = 1,
			SPINNING_DOWN = 2,
			SPINNING_DONE = 3,
			PANNING = 4,
			DETERMINE_MOMENTUM = 5,
			FREE_SPIN = 6
		}

		private const float maxSpeed = 1260f;

		private const float deceleration = 360f;

		public SlideRecognizer SwipeBounds;

		public GameObject Avatar;

		public float TweenDelay = 1f;

		public float EaseDuration = 1f;

		public AnimationCurve easeCurve;

		private SpinState spinState = SpinState.SPINNING_DONE;

		private float speed;

		public float SpeedMod = 0.5f;

		private float momentum;

		private float lastSpin;

		private bool avatarCompositing;

		private bool faceForwardWhenSpinStops = true;

		private bool mousePanning;

		private Vector2 lastMousePosition;

		private ISpinnerListener Listener;

		private float upDownAngle;

		private AvatarAnimationHelper animHelper;

		private void Start()
		{
			SlideRecognizer swipeBounds = SwipeBounds;
			swipeBounds.onSlide = (SlideRecognizer.OnSlide)Delegate.Combine(swipeBounds.onSlide, new SlideRecognizer.OnSlide(OnPanSlide));
			SlideRecognizer swipeBounds2 = SwipeBounds;
			swipeBounds2.onDown = (SlideRecognizer.OnDown)Delegate.Combine(swipeBounds2.onDown, new SlideRecognizer.OnDown(OnPointerDown));
			SlideRecognizer swipeBounds3 = SwipeBounds;
			swipeBounds3.onUp = (SlideRecognizer.OnUp)Delegate.Combine(swipeBounds3.onUp, new SlideRecognizer.OnUp(OnPanSlideComplete));
		}

		public void InitSpinner(GameObject avatarObject)
		{
			Avatar = avatarObject;
			upDownAngle = Avatar.transform.rotation.eulerAngles.x;
			animHelper = Avatar.GetComponent<AvatarAnimationHelper>();
			if (animHelper.IsNullOrDisposed())
			{
				animHelper = Avatar.AddComponent<AvatarAnimationHelper>();
			}
			StartCoroutine(SetupAnimator());
		}

		public IEnumerator SetupAnimator()
		{
			yield return new WaitForEndOfFrame();
			Animator anim = Avatar.GetComponent<Animator>();
			if (anim != null)
			{
				animHelper.SetAnimator(anim);
			}
		}

		public void OnPointerDown(PointerEventData pd)
		{
			Avatar.transform.DOKill();
			if (Avatar.gameObject.activeInHierarchy && spinState != SpinState.PANNING && spinState != SpinState.SPINNING_UP)
			{
				faceForwardWhenSpinStops = pd.pointerId != -1;
				mousePanning = pd.pointerId == -1;
				lastMousePosition = pd.position;
				momentum = 0f;
				spinState = SpinState.PANNING;
			}
		}

		public void OnPanSlide(GameObject inst, Vector2 aMoveVector)
		{
			if (Avatar.gameObject.activeInHierarchy && spinState == SpinState.PANNING && spinState != SpinState.SPINNING_UP && spinState != SpinState.SPIN_MAX_SPEED && !avatarCompositing)
			{
				lastSpin += SpeedMod * (0f - aMoveVector.x);
			}
		}

		public void OnPanSlideComplete(PointerEventData pd)
		{
			mousePanning = false;
			if (Avatar.gameObject.activeInHierarchy && spinState == SpinState.PANNING)
			{
				spinState = SpinState.DETERMINE_MOMENTUM;
			}
		}

		private void Update()
		{
			if (mousePanning && Input.GetMouseButton(0))
			{
				Vector2 mousePosition = Input.mousePosition;
				Vector2 mouseDelta = mousePosition - lastMousePosition;
				lastMousePosition = mousePosition;
				mouseDelta.x *= Singleton<SettingsManager>.Instance.GetWidthScale();
				mouseDelta.y *= Singleton<SettingsManager>.Instance.GetHeightScale();
				OnPanSlide(base.gameObject, mouseDelta);
			}

			int num = 1;
			switch (spinState)
			{
			case SpinState.PANNING:
				speed = lastSpin / Time.deltaTime;
				if (lastSpin * momentum <= 0f && Math.Abs(lastSpin / Time.deltaTime) > 10f)
				{
					momentum = 0f;
				}
				if (Math.Abs(momentum) > Math.Abs(lastSpin))
				{
					momentum = applyDeceleration(momentum, Time.deltaTime);
				}
				else
				{
					num = ((!(lastSpin < 0f)) ? 1 : (-1));
					momentum = ((!(Math.Abs(lastSpin / Time.deltaTime) <= 1260f)) ? (1260f * (float)num) : (lastSpin / Time.deltaTime));
				}
				lastSpin = 0f;
				break;
			case SpinState.DETERMINE_MOMENTUM:
			{
				speed = applyDeceleration(momentum, Time.deltaTime);
				for (float num2 = Avatar.transform.eulerAngles.y % 360f - 180f; num2 < 0f; num2 += 360f)
				{
				}
				momentum = 0f;
				spinState = SpinState.FREE_SPIN;
				break;
			}
			case SpinState.SPINNING_UP:
				speed += 1260f * Time.deltaTime;
				if (speed > 1260f)
				{
					spinState = SpinState.SPIN_MAX_SPEED;
					if (Listener != null)
					{
						Listener.AtMaxSpin();
					}
				}
				break;
			case SpinState.SPINNING_DONE:
				if (Listener != null)
				{
					Listener.AtRest();
				}
				speed = 0f;
				break;
			default:
				speed = applyDeceleration(speed, Time.deltaTime);
				if (Math.Abs(speed) <= float.Epsilon)
				{
					spinState = SpinState.SPINNING_DONE;
					if (faceForwardWhenSpinStops)
					{
						SetEaseOnObject(Avatar, easeCurve, TweenDelay, EaseDuration);
					}
				}
				break;
			case SpinState.SPIN_MAX_SPEED:
				break;
			}
			Avatar.transform.Rotate(0f, speed * Time.deltaTime, 0f);
		}

		public void SetEaseOnObject(GameObject go, AnimationCurve curve, float delay, float duration)
		{
			go.transform.DORotate(new Vector3(upDownAngle, -180f, 0f), duration).SetEase(curve).SetDelay(delay);
		}

		public void LoadDnaOnAvatar(IAvatar dna, AvatarFlags flags, Action<bool, string> callback)
		{
			avatarCompositing = true;
			MonoSingleton<AvatarManager>.Instance.SkinAvatar(Avatar, dna, flags, delegate(bool s, string d)
			{
				SkinningCallback(s, d, callback);
			});
		}

		public void HitBrakes()
		{
			if (spinState == SpinState.SPIN_MAX_SPEED)
			{
				spinState = SpinState.SPINNING_DOWN;
			}
		}

		private void SkinningCallback(bool success, string dnaSha, Action<bool, string> callback)
		{
			if (success)
			{
				avatarCompositing = false;
				if (Listener != null)
				{
					callback(success, dnaSha);
				}
			}
			else
			{
				callback(success, dnaSha);
			}
		}

		public void SetListener(ISpinnerListener aListener)
		{
			Listener = aListener;
		}

		public SpinState GetState()
		{
			return spinState;
		}

		public void SetState(SpinState aState)
		{
			spinState = aState;
		}

		public void PlayAnimationTrigger(string trigger)
		{
			if (!animHelper.IsNullOrDisposed())
			{
				animHelper.SetTrigger(trigger);
			}
		}

		public void AvatarFaceForward(bool faceInstantly = false)
		{
			if (faceInstantly)
			{
				SetEaseOnObject(Avatar, easeCurve, 0f, 0f);
			}
			else
			{
				SetEaseOnObject(Avatar, easeCurve, TweenDelay, EaseDuration);
			}
		}

		private float applyDeceleration(float aSpeed, float deltaTime)
		{
			if (aSpeed > 0f && aSpeed > 360f * deltaTime)
			{
				return aSpeed - 360f * deltaTime;
			}
			if (aSpeed < 0f && Math.Abs(aSpeed) > 360f * deltaTime)
			{
				return aSpeed + 360f * deltaTime;
			}
			return 0f;
		}
	}
}
