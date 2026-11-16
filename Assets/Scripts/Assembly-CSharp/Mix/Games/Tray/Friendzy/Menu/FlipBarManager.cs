using System.Collections.Generic;
using Mix.Games.Tray.Friendzy.Data;
using UnityEngine;

namespace Mix.Games.Tray.Friendzy.Menu
{
	[RequireComponent(typeof(MenuSystem))]
	public class FlipBarManager : MonoBehaviour
	{
		private MenuSystem mMenuSystem;

		[Header("For Instantiating Flip Bars")]
		private List<FlipPanel> mFlipBars = new List<FlipPanel>();

		private Vector3 mInstantiationPoint;

		public GameObject FlipBarPrefab;

		public GameObject SnappingPointPrefab;

		public Transform FlipBarParent;

		public ContentFitter SnappingParent;

		private FlipPanel mRandomFlipBar;

		public Sprite RandomImage;

		public List<FlipPanel> FlipBars
		{
			get
			{
				return mFlipBars;
			}
		}

		public int ClickedFlipBarIndex { get; set; }

		private void Awake()
		{
			ClickedFlipBarIndex = -1;
			mMenuSystem = GetComponent<MenuSystem>();
			mInstantiationPoint = base.transform.position + base.transform.forward * 500f;
		}

		public void PopulateCategories(Category[] aCategories)
		{
			for (int i = 0; i < aCategories.Length; i++)
			{
				FlipPanel availableFlipBar = GetAvailableFlipBar(i);
				PopulateCategory(availableFlipBar, aCategories[i]);
			}
			mRandomFlipBar = GetAvailableFlipBar(aCategories.Length);
			mRandomFlipBar.CategoryImage.sprite = RandomImage;
			SnappingParent.UpdateSize(0, aCategories.Length + 1);
		}

		private void PopulateCategory(FlipPanel aFlipPanel, Category aCategory)
		{
			aFlipPanel.SetCategory(aCategory);
		}

		public void PopulateQuizzes(string[] aQuizTitles)
		{
			for (int i = 0; i < aQuizTitles.Length; i++)
			{
				FlipPanel availableFlipBar = GetAvailableFlipBar(i);
				string text = aQuizTitles[i];
				availableFlipBar.QuizText.text = text;
			}
			SnappingParent.UpdateSize(0, aQuizTitles.Length);
		}

		public FlipPanel GetAvailableFlipBar(int aIndex)
		{
			if (aIndex < mFlipBars.Count)
			{
				return mFlipBars[aIndex];
			}
			return CreateBarAndSnapPoint();
		}

		private FlipPanel CreateBarAndSnapPoint()
		{
			GameObject gameObject = (GameObject)Object.Instantiate(FlipBarPrefab, mInstantiationPoint, Quaternion.identity);
			FlipPanel component = gameObject.GetComponent<FlipPanel>();
			mFlipBars.Add(component);
			component.name = FlipBarPrefab.name;
			component.transform.SetParent(FlipBarParent);
			component.transform.localScale = Vector3.one;
			Transform transform = Object.Instantiate(SnappingPointPrefab).transform;
			transform.name = SnappingPointPrefab.name;
			transform.SetParent(SnappingParent.transform);
			transform.position = SnappingParent.transform.position;
			transform.localScale = Vector3.one;
			component.transformToFollow = transform;
			int flipBarIndex = mFlipBars.Count - 1;
			component.GetComponent<MenuButton>().onClick.AddListener(delegate
			{
				FlipBarClicked(flipBarIndex);
			});
			return component;
		}

		private void FlipBarClicked(int aFlipBarIndex)
		{
			ClickedFlipBarIndex = aFlipBarIndex;
			if (mMenuSystem.MenuFSM.CurrentState == mMenuSystem.CategoryState && mFlipBars[aFlipBarIndex] == mRandomFlipBar)
			{
				mMenuSystem.RandomlySelectQuiz();
				return;
			}
			FlipPanel flipPanel = mFlipBars[aFlipBarIndex];
			string frontPanelText = flipPanel.GetFrontPanelText();
			if (string.Empty.CompareTo(frontPanelText) != 0)
			{
				mMenuSystem.ReceiveCategoryOrIPNameFromButton(frontPanelText);
			}
		}
	}
}
