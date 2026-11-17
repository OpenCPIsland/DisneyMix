using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Mix.Games.Tray.Runner
{
	public class LevelBuilder : MonoBehaviour
	{
		private enum BuildDragAxis
		{
			BOTH = 0,
			HORIZONTAL = 1,
			VERTICAL = 2
		}

		[Header("External References")]
		public MainRunnerGame game;

		public BuildBackground builderBackground;

		[Range(0f, 1f)]
		[Header("Visual Properties")]
		public float builderChunkScale = 0.3f;

		public Vector3 offsetFromCamera;

		public Vector3 topChunkOffset;

		public Vector3 bottomChunkOffset;

		public Vector3 defaultChunkSize;

		[Space(10f)]
		[Range(0f, 5f)]
		public float extraSpaceBetweenChunks;

		[Header("Selected Chunk Wobble")]
		public float selectedChunkWobbleRotationFrequency = 1f;

		public Vector3 selectedChunkWobbleRotationAmount;

		public float selectedChunkWobblePositionFrequency = 1f;

		public Vector3 selectedChunkWobblePositionAmount;

		public float selectedChunkWobbleRampUpTime = 0.5f;

		[Header("Drag & Swipe Properties")]
		[Range(0f, 1f)]
		public float swipeThreshold;

		[Range(0f, 1f)]
		public float swipeAxisDetermineThreshold;

		[Range(1f, 50f)]
		public float dragSpeedModifier = 30f;

		[Header("Movement Properties")]
		[Range(0f, 1f)]
		public float selectChunkTransitionTime = 0.35f;

		public Ease selectChunkTransitionEase;

		[Space(10f)]
		[Range(0f, 1f)]
		public float swapChunkTransitionTime = 0.2f;

		public Ease swapChunkTransitionInEase;

		public Ease swapChunkTransitionOutEase;

		[Space(10f)]
		public float newChunkScaleInTime = 0.5f;

		public float newchunkScaleInDelay = 0.5f;

		public Ease newChunkScaleInEase;

		[Range(0f, 1f)]
		[Space(10f)]
		public float chunkReturnTime = 0.1f;

		public Ease chunkReturnTransitionEase;

		private GameObject mLevelChunksHolder;

		private GameObject mTemplateChunksHolder;

		private ChunkController mStartChunk;

		private ChunkController mEndChunk;

		private List<ChunkController> mLevelChunks;

		private List<ChunkController> mTemplateChunks;

		private int mCurrentLevelChunkIndex;

		private int mCurrentTemplateChunkIndex;

		private bool mIsSwiping;

		private Vector2 mSwipeStartPosition;

		private BuildDragAxis mCurrentBuildDragAxis;

		private float mSwipeAxisDetermineThresholdInPixels;

		private bool mIsBuilderInterpolatingHorizontal;

		private bool mIsBuilderInterpolatingVertical;

		private Vector3 mLevelChunksHolderDragOffset = Vector3.zero;

		private Vector3 mSelectedTemplateChunkDragOffset = Vector3.zero;

		private Vector3 mWobblePositionAmount = Vector3.zero;

		private Vector3 mWobbleRotationAmount = Vector3.zero;

		private Vector3 mWobblePositionRampVelocity = Vector3.zero;

		private Vector3 mRotationRampVelocity = Vector3.zero;

		public bool HasThePlayerSwappedAtLeastOneChunk { get; private set; }

		public int CurrentLevelChunkIndex
		{
			get
			{
				return mCurrentLevelChunkIndex;
			}
		}

		private Vector3 BuilderFocalPoint
		{
			get
			{
				return game.cameraController.transform.position + offsetFromCamera;
			}
		}

		public bool IsFirstChunkSelected
		{
			get
			{
				return mCurrentLevelChunkIndex == 0;
			}
		}

		public bool IsLastChunkSelected
		{
			get
			{
				return mCurrentLevelChunkIndex == mLevelChunks.Count - 1;
			}
		}

		private void Awake()
		{
			mLevelChunksHolder = new GameObject("Level Chunks Holder");
			mLevelChunksHolder.transform.SetParent(base.transform);
			mTemplateChunksHolder = new GameObject("Template Chunks Holder");
			mTemplateChunksHolder.transform.SetParent(mLevelChunksHolder.transform);
			SpawnTemplateChunks();
		}

		private void OnEnable()
		{
			builderBackground.gameObject.SetActive(true);
		}

		private void OnDisable()
		{
			builderBackground.gameObject.SetActive(false);
		}

		private void Start()
		{
			mLevelChunks = new List<ChunkController>();
			for (int i = 0; i < game.CHUNK_PIECES; i++)
			{
				mLevelChunks.Add(null);
			}
			SpawnAndAddStartChunk();
			SpawnAndAddEndChunk();
			mCurrentTemplateChunkIndex = 0;
			SelectChunk(0);
			HasThePlayerSwappedAtLeastOneChunk = false;
			mSwipeAxisDetermineThresholdInPixels = Mathf.Pow((float)Screen.width * swipeAxisDetermineThreshold, 2f);
		}

		private void Update()
		{
			UpdateInput();
			UpdateLevelHolderPosition();
			UpdateAllLevelChunkPositions();
			UpdateSelectedTemplateChunkPosition();
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube(game.cameraController.transform.position + offsetFromCamera, Vector3.one * 0.5f);
		}

		private void SpawnAndAddStartChunk()
		{
			mStartChunk = SpawnChunk(game.startChunk);
		}

		private void SpawnAndAddEndChunk()
		{
			mEndChunk = SpawnChunk(game.endChunk);
		}

		private void CopyChunkToLevel(int index, ChunkController chunkPrefab)
		{
			mLevelChunks[index] = SpawnChunk(chunkPrefab);
		}

		private ChunkController SpawnChunk(ChunkController chunkPrefab)
		{
			ChunkController chunkController = Object.Instantiate(chunkPrefab);
			chunkController.name = chunkPrefab.name;
			chunkController.transform.SetParent(mLevelChunksHolder.transform);
			chunkController.transform.localScale = Vector3.one * builderChunkScale;
			chunkController.centerChunk = true;
			return chunkController;
		}

		private void SpawnTemplateChunks()
		{
			mTemplateChunks = new List<ChunkController>();
			for (int i = 0; i < game.chunks.Count; i++)
			{
				ChunkController chunkController = Object.Instantiate(game.chunks[i]);
				chunkController.transform.SetParent(mTemplateChunksHolder.transform);
				chunkController.transform.localScale = Vector3.one * builderChunkScale;
				chunkController.name = game.chunks[i].name;
				chunkController.gameObject.SetActive(false);
				chunkController.centerChunk = true;
				chunkController.ChunkIndex = i;
				mTemplateChunks.Add(chunkController);
			}
		}

		public ChunkController GetCurrentLevelChunk()
		{
			return mTemplateChunks[mCurrentTemplateChunkIndex];
		}

		public void Finish()
		{
			MainRunnerGame.PlaySound("ButtonUI", game.SOUND_PREFIX);
			ReconcileSelectedTemplateWithLevelChunk(mCurrentLevelChunkIndex, mCurrentTemplateChunkIndex);
			game.SetLevelData(mLevelChunks);
			game.OnBuilderFinish();
		}

		private Vector3 GetLevelChunksHolderOffsetForChunkIndex(int levelChunkIndex)
		{
			Vector3 zero = Vector3.zero;
			zero -= mStartChunk.endLink * builderChunkScale;
			for (int i = 0; i < levelChunkIndex; i++)
			{
				if (mLevelChunks[i] != null)
				{
					zero -= mLevelChunks[i].endLink * builderChunkScale;
				}
				else
				{
					zero -= defaultChunkSize * builderChunkScale;
				}
			}
			return zero;
		}

		private bool SelectChunk(int index, bool isRelative = false)
		{
			int num = mCurrentLevelChunkIndex;
			if (isRelative)
			{
				mCurrentLevelChunkIndex = Mathf.Clamp(mCurrentLevelChunkIndex + index, 0, mLevelChunks.Count - 1);
			}
			else
			{
				mCurrentLevelChunkIndex = Mathf.Clamp(index, 0, mLevelChunks.Count - 1);
			}
			bool flag = false;
			if (mLevelChunks[mCurrentLevelChunkIndex] == null)
			{
				if (num >= 0)
				{
					SpawnRandomChunkAtLevelIndex(mCurrentLevelChunkIndex, mTemplateChunks[mCurrentTemplateChunkIndex]);
				}
				else
				{
					SpawnRandomChunkAtLevelIndex(mCurrentLevelChunkIndex);
				}
				flag = true;
			}
			if (num >= 0)
			{
				ReconcileSelectedTemplateWithLevelChunk(num, mCurrentTemplateChunkIndex);
				mLevelChunks[num].gameObject.SetActive(true);
				mTemplateChunks[mCurrentTemplateChunkIndex].gameObject.SetActive(false);
			}
			mCurrentTemplateChunkIndex = mLevelChunks[mCurrentLevelChunkIndex].ChunkIndex;
			mTemplateChunks[mCurrentTemplateChunkIndex].transform.position = mLevelChunks[mCurrentLevelChunkIndex].transform.position;
			mTemplateChunks[mCurrentTemplateChunkIndex].transform.localEulerAngles = Vector3.zero;
			if (flag)
			{
				Sequence s = DOTween.Sequence();
				mTemplateChunks[mCurrentTemplateChunkIndex].transform.localScale = Vector3.one * 0.01f;
				s.AppendInterval(newchunkScaleInDelay);
				s.AppendCallback(delegate
				{
					mTemplateChunks[mCurrentTemplateChunkIndex].gameObject.SetActive(true);
					MainRunnerGame.PlaySound("NextLevelChunk", game.SOUND_PREFIX);
				});
				s.Append(mTemplateChunks[mCurrentTemplateChunkIndex].transform.DOScale(Vector3.one * builderChunkScale, newChunkScaleInTime).SetEase(newChunkScaleInEase));
			}
			else
			{
				mTemplateChunks[mCurrentTemplateChunkIndex].gameObject.SetActive(true);
			}
			mLevelChunks[mCurrentLevelChunkIndex].gameObject.SetActive(false);
			mIsBuilderInterpolatingHorizontal = true;
			mLevelChunksHolderDragOffset = Vector3.zero;
			if (num != mCurrentLevelChunkIndex)
			{
				MainRunnerGame.PlaySound("BeginSwapLevelChunk", game.SOUND_PREFIX);
			}
			else
			{
				MainRunnerGame.PlaySound("EndSwapLevelChunk", game.SOUND_PREFIX);
			}
			mLevelChunksHolder.transform.DOMove(BuilderFocalPoint + GetLevelChunksHolderOffsetForChunkIndex(mCurrentLevelChunkIndex), selectChunkTransitionTime).SetEase(selectChunkTransitionEase).OnComplete(OnSelectChunkComplete);
			return num != mCurrentLevelChunkIndex;
		}

		private void SpawnRandomChunkAtLevelIndex(int index, ChunkController previousChunk = null)
		{
			if (previousChunk == null)
			{
				CopyChunkToLevel(index, game.chunks[Random.Range(0, game.chunks.Count)]);
				return;
			}
			ChunkController chunkByName = game.GetChunkByName(previousChunk.name);
			CopyChunkToLevel(index, game.chunks[game.GetRandomChunkIndexWithDifficulty(chunkByName.difficulty, chunkByName)]);
		}

		private void ReconcileSelectedTemplateWithLevelChunk(int levelChunkIndex, int templateChunkIndex)
		{
			if (mLevelChunks[levelChunkIndex].ChunkIndex != templateChunkIndex)
			{
				Object.Destroy(mLevelChunks[levelChunkIndex].gameObject);
				mLevelChunks[levelChunkIndex] = null;
				CopyChunkToLevel(levelChunkIndex, game.chunks[templateChunkIndex]);
			}
		}

		private void OnSelectChunkComplete()
		{
			mIsBuilderInterpolatingHorizontal = false;
		}

		private void SwapChunk(int direction)
		{
			if (!mIsBuilderInterpolatingVertical)
			{
				int oldTemplateChunkIndex = mCurrentTemplateChunkIndex;
				mCurrentTemplateChunkIndex = Loop(mCurrentTemplateChunkIndex + direction, mTemplateChunks.Count);
				Vector3 vector;
				Vector3 vector2;
				if (direction > 0)
				{
					vector = bottomChunkOffset;
					vector2 = topChunkOffset;
				}
				else
				{
					vector = topChunkOffset;
					vector2 = bottomChunkOffset;
				}
				mIsBuilderInterpolatingVertical = true;
				mSelectedTemplateChunkDragOffset = Vector3.zero;
				Tweener t = mTemplateChunks[oldTemplateChunkIndex].transform.DOMove(mLevelChunks[mCurrentLevelChunkIndex].transform.position + vector, swapChunkTransitionTime);
				t.SetEase(swapChunkTransitionOutEase);
				t.OnComplete(delegate
				{
					mTemplateChunks[oldTemplateChunkIndex].gameObject.SetActive(false);
				});
				mTemplateChunks[mCurrentTemplateChunkIndex].gameObject.SetActive(true);
				mTemplateChunks[mCurrentTemplateChunkIndex].transform.position = mLevelChunks[mCurrentLevelChunkIndex].transform.position + vector2;
				Tweener t2 = mTemplateChunks[mCurrentTemplateChunkIndex].transform.DOMove(mLevelChunks[mCurrentLevelChunkIndex].transform.position, swapChunkTransitionTime);
				t2.SetEase(swapChunkTransitionInEase);
				t2.OnComplete(delegate
				{
					mIsBuilderInterpolatingVertical = false;
				});
				HasThePlayerSwappedAtLeastOneChunk = true;
				MainRunnerGame.PlaySound("BeginSwapLevelChunk", game.SOUND_PREFIX);
			}
		}

		private int Loop(int value, int count)
		{
			for (value %= count; value < 0; value += count)
			{
			}
			return value;
		}

		private void UpdateInput()
		{
			if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.isPressed && game.IsInputInViewport() && !mIsBuilderInterpolatingHorizontal && !mIsBuilderInterpolatingVertical && !mIsSwiping)
			{
				mIsSwiping = true;
				mSwipeStartPosition = UnityEngine.InputSystem.Pointer.current != null ? UnityEngine.InputSystem.Pointer.current.position.ReadValue() : Vector2.zero;
				if (mCurrentBuildDragAxis == BuildDragAxis.BOTH)
				{
					if (HasThePlayerSwappedAtLeastOneChunk)
					{
						mCurrentBuildDragAxis = BuildDragAxis.BOTH;
					}
					else
					{
						mCurrentBuildDragAxis = BuildDragAxis.VERTICAL;
					}
				}
			}
			if (mIsSwiping && !mIsBuilderInterpolatingHorizontal && !mIsBuilderInterpolatingVertical)
			{
				Vector2 mousePosition = UnityEngine.InputSystem.Pointer.current != null ? UnityEngine.InputSystem.Pointer.current.position.ReadValue() : Vector2.zero;
				Vector2 vector = new Vector2(mSwipeStartPosition.x - mousePosition.x, mSwipeStartPosition.y - mousePosition.y);
				if (mCurrentBuildDragAxis == BuildDragAxis.BOTH)
				{
					if (vector.sqrMagnitude > mSwipeAxisDetermineThresholdInPixels)
					{
						if (Mathf.Abs(vector.x) > Mathf.Abs(vector.y))
						{
							mCurrentBuildDragAxis = BuildDragAxis.HORIZONTAL;
							vector.y = 0f;
						}
						else
						{
							mCurrentBuildDragAxis = BuildDragAxis.VERTICAL;
							vector.x = 0f;
						}
					}
					else
					{
						vector.y = 0f;
					}
				}
				else if (mCurrentBuildDragAxis == BuildDragAxis.VERTICAL)
				{
					vector.x = 0f;
				}
				else
				{
					vector.y = 0f;
				}
				mLevelChunksHolderDragOffset.x = vector.x / dragSpeedModifier;
				mSelectedTemplateChunkDragOffset.y = vector.y / dragSpeedModifier;
				if (Mathf.Abs(vector.x) > (float)Screen.width * swipeThreshold)
				{
					if (vector.x > 0f)
					{
						if (SelectChunk(mCurrentLevelChunkIndex + 1))
						{
							builderBackground.OnNextChunk();
						}
					}
					else if (SelectChunk(mCurrentLevelChunkIndex - 1))
					{
						builderBackground.OnPreviousChunk();
					}
					mIsSwiping = false;
					return;
				}
				if (Mathf.Abs(vector.y) > (float)Screen.width * swipeThreshold)
				{
					if (vector.y > 0f)
					{
						SwapChunk(1);
						builderBackground.OnChunkSwap(BuildBackground.SwapDirection.DOWN);
					}
					else if (vector.y < 0f)
					{
						SwapChunk(-1);
						builderBackground.OnChunkSwap(BuildBackground.SwapDirection.UP);
					}
					mIsSwiping = false;
					return;
				}
			}
			if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasReleasedThisFrame && mIsSwiping)
			{
				if (mCurrentBuildDragAxis == BuildDragAxis.HORIZONTAL)
				{
					ResetAfterDraggingHorizontal();
				}
				else if (mCurrentBuildDragAxis == BuildDragAxis.VERTICAL)
				{
					ResetAfterDraggingVertical();
				}
				mIsSwiping = false;
				mCurrentBuildDragAxis = BuildDragAxis.BOTH;
			}
		}

		private void UpdateLevelHolderPosition()
		{
			if (!mIsBuilderInterpolatingHorizontal)
			{
				mLevelChunksHolder.transform.position = BuilderFocalPoint + GetLevelChunksHolderOffsetForChunkIndex(mCurrentLevelChunkIndex) - mLevelChunksHolderDragOffset;
			}
		}

		private void ResetAfterDraggingHorizontal()
		{
			mIsBuilderInterpolatingHorizontal = true;
			mLevelChunksHolderDragOffset = Vector3.zero;
			mLevelChunksHolder.transform.DOMove(BuilderFocalPoint + GetLevelChunksHolderOffsetForChunkIndex(mCurrentLevelChunkIndex), chunkReturnTime).SetEase(chunkReturnTransitionEase).OnComplete(OnResetHorizontalAfterDraggingComplete);
		}

		private void ResetAfterDraggingVertical()
		{
			mIsBuilderInterpolatingVertical = true;
			mSelectedTemplateChunkDragOffset = Vector3.zero;
			mTemplateChunks[mCurrentTemplateChunkIndex].transform.DOMove(mLevelChunks[mCurrentLevelChunkIndex].transform.position, chunkReturnTime).SetEase(chunkReturnTransitionEase).OnComplete(OnResetVerticalAfterDraggingComplete);
		}

		private void OnResetHorizontalAfterDraggingComplete()
		{
			mIsBuilderInterpolatingHorizontal = false;
		}

		private void OnResetVerticalAfterDraggingComplete()
		{
			mIsBuilderInterpolatingVertical = false;
		}

		private void UpdateAllLevelChunkPositions()
		{
			Vector3 zero = Vector3.zero;
			mStartChunk.transform.localPosition = zero;
			zero += mStartChunk.endLink * builderChunkScale + Vector3.right * extraSpaceBetweenChunks;
			for (int i = 0; i < mLevelChunks.Count; i++)
			{
				if (mLevelChunks[i] != null)
				{
					mLevelChunks[i].transform.localPosition = zero;
					zero += mLevelChunks[i].endLink * builderChunkScale + Vector3.right * extraSpaceBetweenChunks;
				}
				else
				{
					zero += defaultChunkSize * builderChunkScale + Vector3.right * extraSpaceBetweenChunks;
				}
			}
			mEndChunk.transform.localPosition = zero;
			zero += mEndChunk.endLink * builderChunkScale + Vector3.right * extraSpaceBetweenChunks;
		}

		private void UpdateSelectedTemplateChunkPosition()
		{
			if (!mIsBuilderInterpolatingVertical)
			{
				mTemplateChunks[mCurrentTemplateChunkIndex].transform.position = mLevelChunks[mCurrentLevelChunkIndex].transform.position - mSelectedTemplateChunkDragOffset + Mathf.Sin(Time.time * selectedChunkWobblePositionFrequency) * mWobblePositionAmount;
				mTemplateChunks[mCurrentTemplateChunkIndex].transform.localEulerAngles = Mathf.Sin(Time.time * selectedChunkWobbleRotationFrequency) * mWobbleRotationAmount;
				mWobbleRotationAmount = Vector3.SmoothDamp(mWobbleRotationAmount, selectedChunkWobbleRotationAmount, ref mRotationRampVelocity, selectedChunkWobbleRampUpTime);
				mWobblePositionAmount = Vector3.SmoothDamp(mWobblePositionAmount, selectedChunkWobblePositionAmount, ref mWobblePositionRampVelocity, selectedChunkWobbleRampUpTime);
			}
			else
			{
				mWobblePositionAmount = Vector3.zero;
				mWobblePositionRampVelocity = Vector3.zero;
				mWobbleRotationAmount = Vector3.zero;
				mRotationRampVelocity = Vector3.zero;
			}
		}

		public bool AllChunksDefined()
		{
			bool flag = true;
			for (int i = 0; i < mLevelChunks.Count; i++)
			{
				flag = flag && mLevelChunks[i] != null;
			}
			return flag;
		}

		public void LeftArrowTapped()
		{
			SelectChunk(-1, true);
		}

		public void RightArrowTapped()
		{
			SelectChunk(1, true);
		}

		public void UpArrowTapped()
		{
			SwapChunk(1);
			builderBackground.OnChunkSwap(BuildBackground.SwapDirection.DOWN);
		}

		public void DownArrowTapped()
		{
			SwapChunk(-1);
			builderBackground.OnChunkSwap(BuildBackground.SwapDirection.UP);
		}
	}
}
