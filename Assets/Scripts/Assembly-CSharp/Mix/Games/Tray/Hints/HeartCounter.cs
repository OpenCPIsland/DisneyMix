using System;
using System.Collections.Generic;
using Mix.Ui;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Games.Tray.Hints
{
	public class HeartCounter : AnchorUIElement
	{
		public Action OnHideComplete = delegate
		{
		};

		public Action OnShowComplete = delegate
		{
		};

		public Action OnHeartIconDeactivated = delegate
		{
		};

		public Action OnHeartIconActivated = delegate
		{
		};

		[Space(10f)]
		public Sprite activeHeartSprite;

		public Sprite inactiveHeartSprite;

		[Header("Internal Refernces")]
		public GameObject originalHeartObject;

		public RectTransform rectTransform;

		[Header("Update Bounce")]
		public float updateBounceAmount = 0.5f;

		public float updateBounceTime = 1f;

		[Range(1f, 50f)]
		public int updateBounceVibrato = 10;

		[Header("Show / Hide")]
		public Ease showAnimationEase = Ease.OutBack;

		public float showAnimationDuration = 0.2f;

		[Space(10f)]
		public Ease hideAnimationEase = Ease.InBack;

		public float hideAnimationDuration = 0.2f;

		public Vector3 hiddenAnchoredPosition = new Vector2(0f, 100f);

		public float loseHeartShakeAmount = 0.5f;

		public float loseHeartShakeTime = 0.5f;

		[Range(1f, 50f)]
		public int loseHeartShakeVibrato = 10;

		private List<Image> mHearts;

		private int mCurrentHeartCount;

		private int mMaxHeartCount;

		private Vector2 mNormalAnchoredPosition;

		private Vector2 mTopRightAnchoredPosition;

		private float mLastLandscapeInset = -1f;

		private bool mTracksTopRightAnchor;

		private bool mLastLandscapeLayout;

		public bool IsShowing { get; private set; }

		public bool IsTransitionComplete { get; private set; }

		public bool IsFullyHidden
		{
			get
			{
				return !IsShowing && IsTransitionComplete;
			}
		}

		public bool IsFullyShowing
		{
			get
			{
				return IsShowing && IsTransitionComplete;
			}
		}

		public new void SetAnchor(AnchorStyle style)
		{
			base.SetAnchor(style);
			if (style == AnchorStyle.TOP_RIGHT)
			{
				mTopRightAnchoredPosition = rectTransform.anchoredPosition;
				mTracksTopRightAnchor = true;
				ApplyLandscapeInset();
			}
		}

		private void Update()
		{
			if (!mTracksTopRightAnchor)
			{
				return;
			}

			bool isLandscape = ResponsiveCanvasScaler.IsLandscapeLayout();
			float inset = isLandscape ? GetLandscapeInset(rectTransform) : 0f;
			if (isLandscape != mLastLandscapeLayout || !Mathf.Approximately(inset, mLastLandscapeInset))
			{
				ApplyLandscapeInset();
			}
		}

		private void ApplyLandscapeInset()
		{
			bool isLandscape = ResponsiveCanvasScaler.IsLandscapeLayout();
			float inset = isLandscape ? GetLandscapeInset(rectTransform) : 0f;
			rectTransform.anchoredPosition = mTopRightAnchoredPosition + new Vector2(-inset, 0f);
			mNormalAnchoredPosition = rectTransform.anchoredPosition;
			mLastLandscapeLayout = isLandscape;
			mLastLandscapeInset = inset;
		}

		public void Initialize(int maxHearts)
		{
			mHearts = new List<Image>();
			for (int i = 0; i < maxHearts; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(originalHeartObject);
				gameObject.transform.SetParent(originalHeartObject.transform.parent, false);
				gameObject.name = "Heart " + i;
				Image component = gameObject.GetComponent<Image>();
				component.sprite = activeHeartSprite;
				mHearts.Add(component);
			}
			mMaxHeartCount = maxHearts;
			mCurrentHeartCount = mMaxHeartCount;
			originalHeartObject.SetActive(false);
			mNormalAnchoredPosition = rectTransform.anchoredPosition;
		}

		public void SetHeartCount(int newCount, bool skipVibrate = false)
		{
			newCount = Mathf.Clamp(newCount, 0, mMaxHeartCount);
			if (mCurrentHeartCount == newCount)
			{
				return;
			}
			bool flag = false;
			bool flag2 = false;
			for (int i = 0; i < mMaxHeartCount; i++)
			{
				flag = i < mCurrentHeartCount;
				flag2 = i < newCount;
				if (flag && !flag2)
				{
					DeactivateHeart(i, skipVibrate);
				}
				else if (!flag && flag2)
				{
					ActivateHeart(i);
				}
			}
			mCurrentHeartCount = newCount;
		}

		private void DeactivateHeart(int index, bool skipVibrate = false)
		{
			Sequence s = DOTween.Sequence();
			mHearts[index].transform.localScale = Vector3.one;
			if (!skipVibrate)
			{
				s.Append(mHearts[index].transform.DOShakePosition(loseHeartShakeTime, Vector3.one * loseHeartShakeAmount, loseHeartShakeVibrato));
			}
			s.AppendCallback(delegate
			{
				OnHeartIconDeactivated();
				mHearts[index].sprite = inactiveHeartSprite;
			});
			s.Append(mHearts[index].transform.DOPunchScale(Vector3.one * updateBounceAmount, updateBounceTime, updateBounceVibrato));
		}

		private void ActivateHeart(int index)
		{
			mHearts[index].sprite = activeHeartSprite;
			mHearts[index].transform.localScale = Vector3.one;
			mHearts[index].transform.DOPunchScale(Vector3.one * updateBounceAmount, updateBounceTime, updateBounceVibrato);
			OnHeartIconActivated();
		}

		public void Show(bool immediate = false)
		{
			IsShowing = true;
			if (immediate)
			{
				rectTransform.anchoredPosition = mNormalAnchoredPosition;
				IsTransitionComplete = true;
				OnShowComplete();
				return;
			}
			IsTransitionComplete = false;
			rectTransform.DOAnchorPos(mNormalAnchoredPosition, showAnimationDuration).SetEase(showAnimationEase).OnComplete(delegate
			{
				IsTransitionComplete = true;
				OnShowComplete();
			});
		}

		public void Hide(bool immediate = false)
		{
			IsShowing = false;
			if (immediate)
			{
				rectTransform.anchoredPosition = hiddenAnchoredPosition;
				IsTransitionComplete = true;
				return;
			}
			IsTransitionComplete = false;
			rectTransform.DOAnchorPos(hiddenAnchoredPosition, hideAnimationDuration).SetEase(hideAnimationEase).OnComplete(delegate
			{
				IsTransitionComplete = true;
				OnHideComplete();
			});
		}
	}
}
