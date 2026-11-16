using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mix.Avatar;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class ScrollView : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IScrollHandler
	{
		public delegate void OnPointerDownDelegate(PointerEventData eventData);

		public delegate void OnUserScrolledToTopDelegate();

		public OnPointerDownDelegate OnPointerDownDelegates;

		public OnUserScrolledToTopDelegate OnUserScrolledToTopDelegates;

		public Transform container;

		public int itemPadding = 5;

		public int offsetBuffer = 400;

		public int visibleOffsetBuffer = 200;

		public int bottomOffset = 100;

		public bool bottomAlign;

		private float currentPosition;

		private float currentHeight;

		private float parentHeight;

		private float desiredPosition;

		private bool autoScrollInProgress;

		private int lastStartIndex;

		private int lastEndIndex;

		private int lastStartVisibleIndex;

		private int lastEndVisibleIndex;

		private float staticHeight;

		private RectTransform parentContainerRectTransform;

		private RectTransform containerRectTransform;

		private long startId;

		private LinkedList<ScrollItem> items = new LinkedList<ScrollItem>();

		private Coroutine updateLoop;

		public bool NeedsReposition;

		private bool checkForUserScrollToTop;

		public LinkedList<ScrollItem> ScrollItems
		{
			get
			{
				return items;
			}
		}

		private void Awake()
		{
			parentContainerRectTransform = container.transform.parent.GetComponent<RectTransform>();
			containerRectTransform = container.GetComponent<RectTransform>();
		}

		private void Start()
		{
			parentHeight = parentContainerRectTransform.rect.height;
			if (bottomAlign)
			{
				containerRectTransform.sizeDelta = new Vector2(containerRectTransform.sizeDelta.x, parentHeight);
			}
			currentHeight = containerRectTransform.sizeDelta.y;
			updateLoop = StartCoroutine(UpdateList());
		}

		private void OnDestroy()
		{
			if (updateLoop != null)
			{
				StopCoroutine(updateLoop);
			}
			foreach (ScrollItem item in items)
			{
				if (item.ItemGameObject != null)
				{
					item.Destroy();
				}
			}
		}

		public void Update()
		{
			ScrollRect component = base.gameObject.GetComponent<ScrollRect>();
			if (component != null)
			{
				MonoSingleton<AvatarManager>.Instance.SetPriority(component.velocity.magnitude < 10f);
			}
			currentPosition = containerRectTransform.anchoredPosition.y;
			currentHeight = containerRectTransform.sizeDelta.y;
			float num = parentHeight;
			if (parentContainerRectTransform.rect.height != parentHeight)
			{
				parentHeight = parentContainerRectTransform.rect.height;
				if (bottomAlign)
				{
					float num2 = num - parentHeight;
					containerRectTransform.anchoredPosition = new Vector2(containerRectTransform.anchoredPosition.x, containerRectTransform.anchoredPosition.y + num2);
					base.gameObject.GetComponent<ScrollRect>().StopMovement();
					if (IsAtBottom())
					{
						SetScrollToBottom();
					}
				}
				if (currentHeight < parentHeight)
				{
					NeedsReposition = true;
				}
			}
			foreach (ScrollItem item in items)
			{
				if (item.ItemGameObject != null)
				{
					float height = item.ItemGameObject.GetComponent<RectTransform>().rect.height;
					if (item.Height != height)
					{
						NeedsReposition = true;
						break;
					}
				}
			}
			if (NeedsReposition)
			{
				Reposition();
				NeedsReposition = false;
			}
			if (autoScrollInProgress && currentPosition != desiredPosition)
			{
				if (Mathf.Abs(currentPosition - desiredPosition) < 1f)
				{
					currentPosition = desiredPosition;
					autoScrollInProgress = false;
					base.gameObject.GetComponent<ScrollRect>().vertical = true;
				}
				else
				{
					currentPosition += (desiredPosition - currentPosition) / 4f;
				}
				containerRectTransform.anchoredPosition = new Vector2(containerRectTransform.anchoredPosition.x, currentPosition);
			}
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (OnPointerDownDelegates != null)
			{
				OnPointerDownDelegates(eventData);
			}
			if (autoScrollInProgress)
			{
				autoScrollInProgress = false;
				base.gameObject.GetComponent<ScrollRect>().vertical = true;
			}
		}

		public void OnScroll(PointerEventData eventData)
		{
			if (eventData.scrollDelta == Vector2.zero)
			{
				return;
			}

			autoScrollInProgress = false;
			ScrollRect scrollRect = base.gameObject.GetComponent<ScrollRect>();
			scrollRect.vertical = true;
			scrollRect.OnScroll(eventData);
			eventData.scrollDelta = Vector2.zero;
		}

		public void SetStaticHeight(IScrollItem aGenerator)
		{
			ScrollItem scrollItem = new ScrollItem(aGenerator, 0L);
			scrollItem.Generate(true);
			scrollItem.ItemGameObject.transform.SetParent(container, false);
			scrollItem.ItemGameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(scrollItem.ItemGameObject.GetComponent<RectTransform>().anchoredPosition.x, 0f);
			scrollItem.Update();
			staticHeight = scrollItem.Height;
			scrollItem.ItemGameObject.SetActive(false);
			scrollItem.Destroy();
			UnityEngine.Object.DestroyImmediate(scrollItem.ItemGameObject);
			scrollItem.ItemGameObject = null;
		}

		public void SetScrollToTop()
		{
			base.gameObject.GetComponent<ScrollRect>().verticalNormalizedPosition = 1f;
			Canvas.ForceUpdateCanvases();
			currentPosition = containerRectTransform.anchoredPosition.y;
			currentHeight = containerRectTransform.sizeDelta.y;
		}

		public void SetScrollToBottom()
		{
			base.gameObject.GetComponent<ScrollRect>().verticalNormalizedPosition = 0f;
			Canvas.ForceUpdateCanvases();
			currentPosition = containerRectTransform.anchoredPosition.y;
			currentHeight = containerRectTransform.sizeDelta.y;
		}

		public void ScrollToBottom()
		{
			desiredPosition = currentHeight - parentHeight;
			autoScrollInProgress = true;
			base.gameObject.GetComponent<ScrollRect>().vertical = false;
		}

		public void ModifyScrollPosition(float aChangeInHeight)
		{
			base.gameObject.GetComponent<ScrollRect>().StopMovement();
			containerRectTransform.anchoredPosition = new Vector2(containerRectTransform.anchoredPosition.x, containerRectTransform.anchoredPosition.y + aChangeInHeight);
		}

		public long ScrollNewItemInFromBottom(long id)
		{
			if (currentHeight <= parentHeight)
			{
				ScrollItem scrollItem = GetScrollItem(id);
				float num = scrollItem.Height + (float)itemPadding;
				containerRectTransform.anchoredPosition = new Vector2(containerRectTransform.anchoredPosition.x, 0f - num);
				desiredPosition = 0f;
			}
			else
			{
				desiredPosition = currentHeight - parentHeight;
			}
			autoScrollInProgress = true;
			base.gameObject.GetComponent<ScrollRect>().vertical = false;
			return id;
		}

		public void Remove(long aId)
		{
			foreach (ScrollItem item in items)
			{
				if (item.Id == aId)
				{
					items.Remove(item);
					UnityEngine.Object.Destroy(item.ItemGameObject);
					item.Destroy();
					Reposition();
					break;
				}
			}
		}

		public void Remove(IScrollItem aItem)
		{
			foreach (ScrollItem item in items)
			{
				if (item.Generator == aItem)
				{
					items.Remove(item);
					UnityEngine.Object.Destroy(item.ItemGameObject);
					item.Destroy();
					Reposition();
					break;
				}
			}
		}

		public void Remove(GameObject inst)
		{
			foreach (ScrollItem item in items)
			{
				if (item.ItemGameObject == inst)
				{
					items.Remove(item);
					item.Destroy();
					UnityEngine.Object.Destroy(item.ItemGameObject);
					Reposition();
					break;
				}
			}
		}

		public long Add(IScrollItem aGenerator, bool aAddToFront, bool aGenerateForHeightOnly = true)
		{
			return AddAt(null, (!aAddToFront) ? items.Count : 0, aGenerator, aAddToFront, aGenerateForHeightOnly).Id;
		}

		public long AddAfter(IScrollItem aItem, IScrollItem aGenerator, bool aGenerateForHeightOnly = false)
		{
			return AddAfter(GetId(aItem), aGenerator, aGenerateForHeightOnly);
		}

		public long AddAfter(long aId, IScrollItem aGenerator, bool aGenerateForHeightOnly = false)
		{
			ScrollItem scrollItem = AddAt(null, -1, aGenerator, false, aGenerateForHeightOnly);
			LinkedListNode<ScrollItem> linkedListNode = null;
			for (LinkedListNode<ScrollItem> linkedListNode2 = items.First; linkedListNode2 != null; linkedListNode2 = linkedListNode2.Next)
			{
				if (linkedListNode2.Value.Id == aId)
				{
					linkedListNode = linkedListNode2;
					break;
				}
			}
			if (linkedListNode != null)
			{
				items.AddAfter(linkedListNode, scrollItem);
				Reposition();
				return scrollItem.Id;
			}
			return -1L;
		}

		public long AddBefore(IScrollItem aItem, IScrollItem aGenerator, bool aGenerateForHeightOnly = false)
		{
			return AddBefore(GetId(aItem), aGenerator, aGenerateForHeightOnly);
		}

		public long AddBefore(long aId, IScrollItem aGenerator, bool aGenerateForHeightOnly = false)
		{
			ScrollItem scrollItem = AddAt(null, -1, aGenerator, false, aGenerateForHeightOnly);
			LinkedListNode<ScrollItem> linkedListNode = null;
			for (LinkedListNode<ScrollItem> linkedListNode2 = items.First; linkedListNode2 != null; linkedListNode2 = linkedListNode2.Next)
			{
				if (linkedListNode2.Value.Id == aId)
				{
					linkedListNode = linkedListNode2;
					break;
				}
			}
			if (linkedListNode != null)
			{
				items.AddBefore(linkedListNode, scrollItem);
				Reposition();
				return scrollItem.Id;
			}
			return -1L;
		}

		public IScrollItem Get(long aId)
		{
			foreach (ScrollItem item in items)
			{
				if (item.Id == aId)
				{
					return item.Generator;
				}
			}
			return null;
		}

		public IScrollItem Get(IScrollItem scrollItem)
		{
			foreach (ScrollItem item in items)
			{
				if (item.Generator == scrollItem)
				{
					return item.Generator;
				}
			}
			return null;
		}

		public long GetId(IScrollItem scrollItem)
		{
			foreach (ScrollItem item in items)
			{
				if (item.Generator == scrollItem)
				{
					return item.Id;
				}
			}
			return -1L;
		}

		public void Clear()
		{
			foreach (ScrollItem item in items)
			{
				item.Destroy();
				UnityEngine.Object.Destroy(item.ItemGameObject);
			}
			items.Clear();
			Reposition();
		}

		public void Reposition(bool aCheckForBottom = false)
		{
			bool flag = false;
			int num = 0;
			float num2 = 0f;
			float num3 = 0f;
			if (bottomAlign)
			{
				flag = IsAtBottom();
				foreach (ScrollItem item in items)
				{
					if (item.ItemGameObject != null)
					{
						item.Update();
					}
					num2 += item.Height + (float)itemPadding;
				}
				num2 += (float)bottomOffset;
				if (num2 < parentHeight)
				{
					num3 = parentHeight - num2;
				}
			}
			num2 = 0f;
			foreach (ScrollItem item2 in items)
			{
				if (item2.ItemGameObject != null)
				{
					item2.ItemGameObject.name = "item_" + num.ToString("0000");
					if (item2.ItemGameObject.GetComponent<RectTransform>() != null)
					{
						item2.ItemGameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(item2.ItemGameObject.GetComponent<RectTransform>().anchoredPosition.x, -1f * num2 - num3);
						item2.Update();
					}
				}
				else
				{
					item2.Position = Mathf.Abs(-1f * num2 - num3);
				}
				num2 += item2.Height + (float)itemPadding;
				num++;
			}
			num2 += num3 + (float)bottomOffset;
			if (num2 < parentHeight)
			{
				num2 = parentHeight;
			}
			currentHeight = num2;
			if (container != null && containerRectTransform != null)
			{
				containerRectTransform.sizeDelta = new Vector2(containerRectTransform.sizeDelta.x, num2);
			}
			if (flag && aCheckForBottom)
			{
				ScrollToBottom();
			}
		}

		private IEnumerator UpdateList()
		{
			while (true)
			{
				UpdateListNow();
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();
			}
		}

		public void UpdateListNow()
		{
			GetItemIndex(out lastStartIndex, out lastEndIndex, out lastStartVisibleIndex, out lastEndVisibleIndex);
			if (lastStartIndex > lastEndIndex)
			{
			}
			int num = 0;
			foreach (ScrollItem item in items)
			{
				bool flag = num < lastStartIndex || num > lastEndIndex;
				bool flag2 = num >= lastStartVisibleIndex && num <= lastEndVisibleIndex;
				if (item.ItemGameObject != null && flag)
				{
					item.Destroy();
					UnityEngine.Object.DestroyImmediate(item.ItemGameObject);
					item.ItemGameObject = null;
				}
				else if (item.ItemGameObject == null && !flag)
				{
					AddAt(item, num, item.Generator, false, false);
				}
				if (item.ItemGameObject != null && flag2 && !item.ItemGameObject.activeSelf)
				{
					item.ItemGameObject.SetActive(true);
				}
				else if (item.ItemGameObject != null && !flag2 && item.ItemGameObject.activeSelf)
				{
					item.ItemGameObject.SetActive(false);
				}
				num++;
			}
		}

		private void GetItemIndex(out int aStartIndex, out int aEndIndex, out int aStartVisibleIndex, out int aEndVisibleIndex)
		{
			int num = -1;
			int num2 = -1;
			int num3 = -1;
			int num4 = -1;
			float num5 = currentPosition - (float)offsetBuffer;
			float num6 = currentPosition + (float)offsetBuffer + parentHeight;
			float num7 = currentPosition - (float)visibleOffsetBuffer;
			float num8 = currentPosition + (float)visibleOffsetBuffer + parentHeight;
			int num9 = 0;
			foreach (ScrollItem item in items)
			{
				float num10 = ((item.Position != 0f) ? item.Position : (item.Height * (float)num9));
				if (num < 0 && num10 + item.Height > num5)
				{
					num = num9;
				}
				if (num2 < 0 && num10 > num6)
				{
					num2 = num9;
				}
				if (num3 < 0 && num10 + item.Height > num7)
				{
					num3 = num9;
				}
				if (num4 < 0 && num10 > num8)
				{
					num4 = num9;
				}
				if (num2 >= 0 && num >= 0 && num3 >= 0 && num4 >= 0)
				{
					break;
				}
				num9++;
			}
			aStartIndex = ((num >= 0) ? num : 0);
			aEndIndex = ((num2 >= 0) ? num2 : ((items.Count != 0) ? (items.Count - 1) : 0));
			aStartVisibleIndex = ((num3 >= 0) ? num3 : 0);
			aEndVisibleIndex = ((num4 >= 0) ? num4 : ((items.Count != 0) ? (items.Count - 1) : 0));
		}

		private ScrollItem GetScrollItem(long aId)
		{
			foreach (ScrollItem item in items)
			{
				if (item.Id == aId)
				{
					return item;
				}
			}
			return null;
		}

		private ScrollItem AddAt(ScrollItem aItem, int aIndex, IScrollItem aGenerator, bool aAddToFront, bool aGenerateForHeightOnly = true)
		{
			long aId = ((aItem != null) ? aItem.Id : (++startId));
			ScrollItem scrollItem = ((aItem != null) ? aItem : new ScrollItem(aGenerator, aId));
			long num = ((aIndex >= 0) ? aIndex : scrollItem.Id);
			float num2 = ((!(scrollItem.Generator is IScrollItemHelper)) ? (-1f) : ((IScrollItemHelper)scrollItem.Generator).GetGameObjectHeight());
			if (aGenerateForHeightOnly && num2 > 0f)
			{
				scrollItem.Height = num2;
				if (aItem == null && aIndex >= 0)
				{
					if (aAddToFront)
					{
						items.AddFirst(scrollItem);
					}
					else
					{
						items.AddLast(scrollItem);
					}
				}
			}
			else if (aGenerateForHeightOnly && Math.Abs(staticHeight) >= 1f)
			{
				scrollItem.Height = staticHeight;
				if (aItem == null && aIndex >= 0)
				{
					if (aAddToFront)
					{
						items.AddFirst(scrollItem);
					}
					else
					{
						items.AddLast(scrollItem);
					}
				}
			}
			else
			{
				scrollItem.Generate(aGenerateForHeightOnly);
				scrollItem.ItemGameObject.name = "item_" + num.ToString("0000");
				scrollItem.ItemGameObject.transform.SetParent(container, false);
				if (scrollItem.Generator is MixOAPhotoItem || scrollItem.Generator is MixOAVideoItem)
				{
					LayoutRebuilder.ForceRebuildLayoutImmediate(scrollItem.ItemGameObject.GetComponent<RectTransform>());
					scrollItem.Update();
				}
				scrollItem.ItemGameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(scrollItem.ItemGameObject.GetComponent<RectTransform>().anchoredPosition.x, (float)(-1 * num) * (scrollItem.Height + (float)itemPadding));
				scrollItem.Update();
				scrollItem.ItemGameObject.SetActive(false);
				if (!aGenerateForHeightOnly)
				{
					scrollItem.ItemGameObject.SetActive(true);
				}
				if (aItem == null && aIndex >= 0)
				{
					if (aAddToFront)
					{
						items.AddFirst(scrollItem);
					}
					else
					{
						items.AddLast(scrollItem);
					}
				}
				if (aGenerateForHeightOnly)
				{
					scrollItem.Destroy();
					UnityEngine.Object.DestroyImmediate(scrollItem.ItemGameObject);
					scrollItem.ItemGameObject = null;
				}
			}
			Reposition();
			return scrollItem;
		}

		public void Hide()
		{
			if (base.gameObject.activeSelf)
			{
				base.gameObject.SetActive(false);
			}
			if (updateLoop != null)
			{
				StopCoroutine(updateLoop);
			}
		}

		public void Show()
		{
			if (!base.gameObject.activeSelf)
			{
				base.gameObject.SetActive(true);
			}
			if (updateLoop != null)
			{
				StopCoroutine(updateLoop);
			}
			updateLoop = StartCoroutine(UpdateList());
		}

		public bool IsAtBottom()
		{
			float num = ((!autoScrollInProgress) ? currentPosition : desiredPosition);
			if (currentHeight <= parentHeight)
			{
				return true;
			}
			float num2 = MixConstants.CANVAS_HEIGHT * 0.1f;
			return num > currentHeight - parentHeight - num2;
		}

		public void Sort(Comparison<ScrollItem> comparer)
		{
			List<ScrollItem> list = items.ToList();
			items.Clear();
			list.Sort(comparer);
			items = new LinkedList<ScrollItem>(list);
			Reposition();
		}

		public void ResortItemInList(long aId, Comparison<IScrollItem> comparer)
		{
			ScrollItem scrollItem = GetScrollItem(aId);
			items.Remove(scrollItem);
			LinkedListNode<ScrollItem> linkedListNode;
			for (linkedListNode = items.Last; linkedListNode != null; linkedListNode = linkedListNode.Previous)
			{
				if (comparer(scrollItem.Generator, linkedListNode.Value.Generator) >= 0)
				{
					items.AddAfter(linkedListNode, scrollItem);
					break;
				}
			}
			if (linkedListNode == null)
			{
				items.AddFirst(scrollItem);
			}
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			checkForUserScrollToTop = true;
		}

		public void OnDrag(PointerEventData eventData)
		{
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (!checkForUserScrollToTop)
			{
				return;
			}
			ScrollRect component = base.gameObject.GetComponent<ScrollRect>();
			if (component.verticalNormalizedPosition >= 1f)
			{
				if (OnUserScrolledToTopDelegates != null)
				{
					OnUserScrolledToTopDelegates();
				}
				checkForUserScrollToTop = false;
			}
		}
	}
}
