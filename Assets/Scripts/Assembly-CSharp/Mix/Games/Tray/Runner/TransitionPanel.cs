using System;
using DG.Tweening;
using UnityEngine;

namespace Mix.Games.Tray.Runner
{
	public class TransitionPanel : MonoBehaviour
	{
		public Action OnClose = delegate
		{
		};

		public Action OnCloseComplete = delegate
		{
		};

		public Action OnOpen = delegate
		{
		};

		public Action OnOpenComplete = delegate
		{
		};

		public RectTransform leftPanel;

		public RectTransform rightPanel;

		public Vector3 leftPanelOpenPosition;

		public Vector3 rightPanelOpenPosition;

		public float closeTime;

		public float openTime;

		public bool startClosed;

		public Vector3 customLeftOpenPosition = new Vector3(-1000f, 0f, 0f);
        public Vector3 customRightOpenPosition = new Vector3(1000f, 0f, 0f);


        private void Start()
		{
			if (startClosed)
			{
				leftPanel.localPosition = Vector3.zero;
				rightPanel.localPosition = Vector3.zero;
			}
			else
			{
				leftPanel.transform.localPosition = leftPanelOpenPosition;
				rightPanel.transform.localPosition = rightPanelOpenPosition;
			}
		}

		public void Open(float delay = 0f)
		{
			OnOpen();
			leftPanel.transform.DOLocalMove(customLeftOpenPosition, openTime).SetDelay(delay).SetEase(Ease.OutQuad);
			rightPanel.transform.DOLocalMove(customRightOpenPosition, openTime).SetDelay(delay).SetEase(Ease.OutQuad);
			DOVirtual.DelayedCall(openTime, delegate
			{
				OnOpenComplete();
			});
		}

		public void Close()
		{
			OnClose();
			leftPanel.transform.DOLocalMove(Vector3.zero, closeTime).SetEase(Ease.InOutBounce);
			rightPanel.transform.DOLocalMove(Vector3.zero, closeTime).SetEase(Ease.InOutBounce);
			DOVirtual.DelayedCall(closeTime, delegate
			{
				OnCloseComplete();
			});
		}
	}
}
