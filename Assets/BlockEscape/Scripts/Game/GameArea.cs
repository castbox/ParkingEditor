using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BBG.BlockEscape
{
	[ExecuteInEditMode]
	public class GameArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
	{
		#region Classes

		private class BlockObject
		{
			public int				index		= 0;
			public LevelData.Block	blockData	= null;
			public Block			block		= null;
		}

		#endregion

		#region Inspector Variables

		[SerializeField] private Block		 		blockPrefab				= null;
		[SerializeField] private Block		 		hintBlockPrefab			= null;
		[SerializeField] private RectTransform		boardContainer			= null;
		[SerializeField] private float				spacing					= 10f;
		[SerializeField] private Color				blockColor				= Color.white;
		[SerializeField] private Color				targetBlockColor		= Color.white;
		[Space]
		[SerializeField] private Text				movesMadeAmountText		= null;
		[Space]
		[SerializeField] private bool				showGridLines 			= true;
		[SerializeField] private Color				gridLineColor 			= Color.white;
		[SerializeField] private float				gridLineThickness 		= 4;
		[Space]
		[SerializeField] private bool				showBorder 				= true;
		[SerializeField] private Color				borderColor 			= Color.white;
		[SerializeField] private float				borderThickness	 		= 5;
		[SerializeField] private int				borderCornerRoundness	= 5;

		#endregion

		#region Member Variables

		private GameObjectPool			blockPool;
		private GridImage				gridImage;
		private UILine					borderUILine;

		private LevelData				activeLevelData;
		private LevelSaveData			activeLevelSaveData;
		private Vector2					gridCellSize;
		private List<BlockObject>		blockObjects;
		private Block					hintBlock;

		private bool					isPointerActive;
		private int						activePointerId;
		private BlockObject				activeBlockObject;
		private Vector2					dragOffset;

		#endregion

		#region Properties

		public System.Action OnLevelCompleted { get; set; }

		private RectTransform RectT { get { return transform as RectTransform; } }

		#endregion

		#region Unity Methods

		#endregion

		#region Public Variables

		public void Initialize()
		{
			blockPool = new GameObjectPool(blockPrefab.gameObject, 2, GameObjectPool.CreatePoolContainer(transform, "block_pool"));

			// Create the hint Block that will be used when displaying hints
			hintBlock = Instantiate(hintBlockPrefab, boardContainer, false);
			hintBlock.gameObject.SetActive(false);

			// Create a GridImage if we are to show grid lines
			if (showGridLines)
			{
				GameObject gridLineContainer = CreateBoardContainer("grid_lines").gameObject;

				gridImage			= gridLineContainer.AddComponent<GridImage>();
				gridImage.color		= gridLineColor;
				gridImage.Thickness	= gridLineThickness;
				gridImage.Padding	= spacing;
			}

			// Create the border UI line
			if (showBorder)
			{
				RectTransform borderContainer = CreateBoardContainer("border");

				borderContainer.pivot = Vector2.zero;

				borderUILine				= borderContainer.gameObject.AddComponent<UILine>();
				borderUILine.color			= borderColor;
				borderUILine.Thickness		= borderThickness;
				borderUILine.LineRoundness	= borderCornerRoundness;
			}
		}

		public void SetupLevel(LevelData levelData, LevelSaveData levelSaveData)
		{
			activeLevelData		= levelData;
			activeLevelSaveData	= levelSaveData;

			// Get the size of a cell on the board/grid
			float gridCellWidth		= (float)(boardContainer.rect.width - (levelData.GridSize + 1) * spacing) / levelData.GridSize;
			float gridCellHeight	= (float)(boardContainer.rect.height - (levelData.GridSize + 1) * spacing) / levelData.GridSize;

			gridCellSize = new Vector2(gridCellWidth, gridCellHeight);

			// Clear the UI from the previous game
			Clear();

			SetMovesMadeText();

			// Setup the grid lines
			if (gridImage != null)
			{
				gridImage.GridSize = levelData.GridSize;
			}

			// Setup the border
			if (borderUILine != null)
			{
				SetupBorder(levelData);
			}

			// Create all the blocks and place them in the proper positions on the board
			blockObjects = CreateBlocks(levelData.Blocks, levelSaveData.blockPositions);

			// Check if hints are active
			if (activeLevelSaveData.hintUsed)
			{
				SetupHint();
			}
			else
			{
				// Make sure the hint block is hidden
				hintBlock.gameObject.SetActive(false);
			}
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			// If there is already an active pointer or the active level data is now null then ignore this event
			if (isPointerActive || activeLevelData == null)
			{
				return;
			}

			isPointerActive = true;
			activePointerId = eventData.pointerId;

			Vector2 boardPosition = GetBoardPosition(eventData.position);

			// Check if the mouse is in the board container, if so we need to check if the player clicked on a block on the board and start dragging it
			if (RectTransformUtility.RectangleContainsScreenPoint(boardContainer, eventData.position))
			{
				if (TryStartDraggingBlockOnBoard(boardPosition))
				{
					// Set the drag offset so the block doesn't "jump" to the mouse position
					dragOffset = activeBlockObject.block.RectT.anchoredPosition - boardPosition;
				}
			}

			// If activeBlockObject is not null then a block was selected
			if (activeBlockObject != null)
			{
				UpdateActiveBlockPosition(boardPosition);
			}
		}

		public void OnDrag(PointerEventData eventData)
		{
			// If the event is not for the active down pointer then ignore this event
			if (!isPointerActive || eventData.pointerId != activePointerId || activeBlockObject == null)
			{
				return;
			}

			UpdateActiveBlockPosition(GetBoardPosition(eventData.position));
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			// If the event is not for the active down pointer then ignore this event
			if (!isPointerActive || eventData.pointerId != activePointerId || activeLevelSaveData == null)
			{
				return;
			}

			if (activeBlockObject != null)
			{
				SoundManager.Instance.Play("block-moved");

				Pos gridPos;

				MoveActiveBlockToClosestCell(out gridPos);

				// Check if the block moved
				if (activeLevelSaveData.MoveBlock(activeBlockObject.index, gridPos))
				{
					SetMovesMadeText();

					// Check if hints are active and the block moved to the correct position
					if (activeLevelSaveData.hintUsed && gridPos.IsEqual(activeLevelData.MovesToComplete[activeLevelSaveData.nextHintMoveIndex].pos))
					{
						// Show the next move that needs to be made
						ShowNextHint();
					}

					// Check if the target block moved and it moved to the edge of the board
					if (activeBlockObject.index == 0 && gridPos.x == activeLevelData.GridSize - activeLevelData.Blocks[0].size)
					{
						// Level is complete, move the block out of the board
						PositionBlock(activeBlockObject.block.RectT, new Pos(activeLevelData.GridSize + 1, activeLevelData.Blocks[0].pos.y), true, 0.35f);
				
						// Set the active level data to null so mouse events will be ignored until the next level starts
						activeLevelData		= null;
						activeLevelSaveData	= null;

						if (OnLevelCompleted != null)
						{
							OnLevelCompleted();
						}
					}
				}
			}

			isPointerActive		= false;
			activeBlockObject	= null;
		}

		/// <summary>
		/// Resets the game
		/// </summary>
		public void ResetActiveGame()
		{
			if (activeLevelData != null && activeLevelSaveData != null)
			{
				activeLevelSaveData.Reset(activeLevelData);

				SetupLevel(activeLevelData, activeLevelSaveData);
			}
		}

		/// <summary>
		/// Reverts the last move that was made
		/// </summary>
		public void UndoLastMove()
		{
			if (activeLevelSaveData != null && !activeLevelSaveData.hintUsed)
			{
				int blockIndex;
				Pos blockPosition;

				if (activeLevelSaveData.UndoMove(out blockIndex, out blockPosition))
				{
					PositionBlock(blockObjects[blockIndex].block.RectT, blockPosition, true);
					SetMovesMadeText();
				}
			}
		}

		/// <summary>
		/// Displays the moves required to beat the level
		/// </summary>
		public void DisplayHint()
		{
			ResetActiveGame();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Creates a GameObject container
		/// </summary>
		private RectTransform CreateBoardContainer(string name)
		{
			GameObject		container		= new GameObject(name, typeof(CanvasRenderer));
			RectTransform	containerRectT	= container.AddComponent<RectTransform>();

			containerRectT.SetParent(boardContainer, false);

			// Set anchors to expand to fill
			containerRectT.anchorMin = Vector2.zero;
			containerRectT.anchorMax = Vector2.one;
			containerRectT.offsetMin = Vector2.zero;
			containerRectT.offsetMax = Vector2.zero;

			return containerRectT;
		}

		/// <summary>
		/// Updates the moves made amount text
		/// </summary>
		private void SetMovesMadeText()
		{
			if (activeLevelSaveData != null)
			{
				movesMadeAmountText.text = string.Format("{0} / {1}", activeLevelSaveData.movesMade, activeLevelData.MovesToComplete.Count);
			}
		}

		/// <summary>
		/// Removes all blocks from the game
		/// </summary>
		public void Clear()
		{
			blockPool.ReturnAllObjectsToPool();
		}

		private void SetupBorder(LevelData levelData)
		{
			List<Vector2> linePoints = new List<Vector2>();

			linePoints.Add(new Vector2(boardContainer.rect.width, spacing + (levelData.Blocks[0].pos.y + 1) * (gridCellSize.y + spacing)));
			linePoints.Add(new Vector2(boardContainer.rect.width, boardContainer.rect.height));
			linePoints.Add(new Vector2(0, boardContainer.rect.height));
			linePoints.Add(new Vector2(0, 0));
			linePoints.Add(new Vector2(boardContainer.rect.width, 0));
			linePoints.Add(new Vector2(boardContainer.rect.width, levelData.Blocks[0].pos.y * (gridCellSize.y + spacing)));

			borderUILine.SetPoints(linePoints);
		}

		/// <summary>
		/// Creates the blocks and positions them on the board at the given positions
		/// </summary>
		private List<BlockObject> CreateBlocks(List<LevelData.Block> blocks, List<Pos> blockPositions)
		{
			List<BlockObject> blockObjects = new List<BlockObject>();

			for (int i = 0; i < blocks.Count; i++)
			{
				LevelData.Block	blockData		= blocks[i];
				Pos				blockPosition	= blockPositions[i];
				Block			block			= blockPool.GetObject<Block>(boardContainer);

				BlockObject blockObject	= new BlockObject();
				blockObject.index		= i;
				blockObject.blockData	= blockData;
				blockObject.block		= block;

				SetupBlock(block.RectT, blockData, blockPosition);

				block.Setup(i == 0 ? targetBlockColor : blockColor, !blockData.isVertical);

				blockObjects.Add(blockObject);
			}

			return blockObjects;
		}

		/// <summary>
		/// Sets the blocks position and size on the board
		/// </summary>
		private void SetupBlock(RectTransform blockRectT, LevelData.Block blockData, Pos gridPosition)
		{
			// Set the anchors/pivot so they are in the bottom left corner
			blockRectT.anchorMin	= Vector2.zero;
			blockRectT.anchorMax	= Vector2.zero;
			blockRectT.pivot		= Vector2.zero;

			// Set the size of the block
			float xBlocks = (blockData.isVertical ? 1 : blockData.size);
			float yBlocks = (blockData.isVertical ? blockData.size : 1);

			float blockWidth	= gridCellSize.x * xBlocks + (xBlocks - 1) * spacing;
			float blockHeight	= gridCellSize.y * yBlocks + (yBlocks - 1) * spacing;

			blockRectT.sizeDelta = new Vector2(blockWidth, blockHeight);

			// Set the position of the block
			PositionBlock(blockRectT, gridPosition);
		}

		private void PositionBlock(RectTransform blockRectT, Pos gridPosition, bool animate = false, float animDuration = 0.15f)
		{
			float xPos = spacing + gridPosition.x * (gridCellSize.x + spacing);
			float yPos = spacing + gridPosition.y * (gridCellSize.y + spacing);

			if (!animate)
			{
				blockRectT.anchoredPosition = new Vector2(xPos, yPos);
			}
			else
			{
				UIAnimation anim;

				anim		= UIAnimation.PositionX(blockRectT, xPos, animDuration);
				anim.style	= UIAnimation.Style.EaseOut;
				anim.Play();

				anim		= UIAnimation.PositionY(blockRectT, yPos, animDuration);
				anim.style	= UIAnimation.Style.EaseOut;
				anim.Play();
			}
		}

		/// <summary>
		/// Gets the board position relative to the bottom left corner of the board
		/// </summary>
		private Vector2 GetBoardPosition(Vector2 screenPosition)
		{
			Vector2 boardPosition;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(boardContainer, screenPosition, null, out boardPosition);

			boardPosition.x += boardContainer.rect.width / 2f;
			boardPosition.y += boardContainer.rect.height / 2f;

			return boardPosition;
		}

		/// <summary>
		/// Attempts to start dragging a block from the board at the given screen position
		/// </summary>
		private bool TryStartDraggingBlockOnBoard(Vector2 boardPosition)
		{
			// Get the block that was selected
			BlockObject blockObject = GetBlockObjectAt(boardPosition);

			if (blockObject != null && (!activeLevelSaveData.hintUsed || IsHintBlock(blockObject.index)))
			{
				activeBlockObject = blockObject;

				UIAnimation.DestroyAllAnimations(activeBlockObject.block.RectT.gameObject);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns true if hints are active and the block is the next block that needs to move for hints
		/// </summary>
		private bool IsHintBlock(int blockIndex)
		{
			return blockIndex == activeLevelData.MovesToComplete[activeLevelSaveData.nextHintMoveIndex].blockIndex;
		}

		/// <summary>
		/// Gets the block that is at the given board position
		/// </summary>
		private BlockObject GetBlockObjectAt(Vector2 boardPosition)
		{
			for (int i = 0; i < blockObjects.Count; i++)
			{
				BlockObject	blockObject	= blockObjects[i];
				Rect		blockRect	= new Rect(blockObject.block.RectT.anchoredPosition, blockObject.block.RectT.rect.size);

				if (blockRect.Contains(boardPosition))
				{
					return blockObject;
				}
			}

			return null;
		}

		/// <summary>
		/// Sets the active blocks position to the given screen position inside GameAreas RectTransform
		/// </summary>
		private void UpdateActiveBlockPosition(Vector2 boardPosition)
		{
			// Get the board position we want to move the block to
			Vector2 moveToPosition = GetMoveToPosition(boardPosition);

			if (activeBlockObject.block.RectT.anchoredPosition == moveToPosition)
			{
				return;
			}

			int moveDir;
			Pos lastEmptyPos;

			// Get a list of grid positions the block would have to move through to get to the moveToPosition
			List<Pos> gridPositions = GetGridPositionsBetween(activeBlockObject, moveToPosition, out moveDir);

			// Check for any other blocks occupying those grid positions
			if (DoesBlockOccupyCell(gridPositions, activeBlockObject.index, out lastEmptyPos))
			{
				if (lastEmptyPos.x == -1 || lastEmptyPos.y == -1)
				{
					Debug.LogError("[GameArea] UpdateActiveBlockPosition | lastEmptyPos is negative");

					return;
				}

				if (moveDir == 1)
				{
					lastEmptyPos.x -= activeBlockObject.blockData.isVertical ? 0 : activeBlockObject.blockData.size - 1;
					lastEmptyPos.y -= activeBlockObject.blockData.isVertical ? activeBlockObject.blockData.size - 1 : 0;
				}

				PositionBlock(activeBlockObject.block.RectT, lastEmptyPos);
			}
			else
			{
				activeBlockObject.block.RectT.anchoredPosition = moveToPosition;
			}

			// If hints are active make sure the hint arrow is pointing in the correct direction
			if (activeLevelSaveData.hintUsed)
			{
				UpdateHintArrow();
			}
		}

		/// <summary>
		/// Gets the position that the blocks RectTransform needs to be set to
		/// </summary>
		private Vector2 GetMoveToPosition(Vector2 boardPosition)
		{
			Vector2 dragPosition	= boardPosition + dragOffset;
			Vector2 blockPosition	= activeBlockObject.block.RectT.anchoredPosition;

			// Set the x/y based on if its a vertical block
			if (activeBlockObject.blockData.isVertical)
			{
				blockPosition.y = dragPosition.y;

				// Make sure the block does not move off the board
				float bottom	= blockPosition.y;
				float top		= bottom + activeBlockObject.block.RectT.rect.height;

				if (bottom < spacing)
				{
					blockPosition.y += spacing - bottom;
				}
				else if (top > boardContainer.rect.height - spacing)
				{
					blockPosition.y -= top - (boardContainer.rect.height - spacing);
				}
			}
			else
			{
				blockPosition.x = dragPosition.x;

				// Make sure the block does not move off the board
				float left	= blockPosition.x;
				float right	= left + activeBlockObject.block.RectT.rect.width;

				if (left < spacing)
				{
					blockPosition.x += spacing - left;
				}
				else if (right > boardContainer.rect.width - spacing)
				{
					blockPosition.x -= right - (boardContainer.rect.width - spacing);
				}
			}

			return blockPosition;
		}

		/// <summary>
		/// Gets all grid positions between
		/// </summary>
		private List<Pos> GetGridPositionsBetween(BlockObject blockObject, Vector2 toPosition, out int dir)
		{
			Vector2 fromPosition = blockObject.block.RectT.anchoredPosition;

			Vector2 fromPos	= fromPosition;
			Vector2 toPos	= toPosition;

			dir = -1;

			if (blockObject.blockData.isVertical)
			{
				if (fromPosition.y < toPosition.y)
				{
					dir = 1;
					toPosition.y += blockObject.block.RectT.rect.height;
				}

				fromPos.y	= Mathf.Min(fromPosition.y, toPosition.y);
				toPos.y		= Mathf.Max(fromPosition.y, toPosition.y);
			}
			else if (!blockObject.blockData.isVertical)
			{
				if (fromPosition.x < toPosition.x)
				{
					dir = 1;
					toPosition.x += blockObject.block.RectT.rect.width;
				}

				fromPos.x	= Mathf.Min(fromPosition.x, toPosition.x);
				toPos.x		= Mathf.Max(fromPosition.x, toPosition.x);
			}

			fromPos -= new Vector2(spacing, spacing);

			Pos fromGridPos	= new Pos(0, 0);
			Pos toGridPos	= new Pos(0, 0);

			fromGridPos.x = Mathf.FloorToInt(fromPos.x / (gridCellSize.x + spacing));
			fromGridPos.y = Mathf.FloorToInt(fromPos.y / (gridCellSize.y + spacing));

			toGridPos.x = Mathf.FloorToInt(toPos.x / (gridCellSize.x + spacing));
			toGridPos.y = Mathf.FloorToInt(toPos.y / (gridCellSize.y + spacing));

			List<Pos> gridPositions = new List<Pos>();

			Pos pos = fromGridPos;

			while (gridPositions.Count < activeLevelData.GridSize)
			{
				gridPositions.Add(pos);

				if (pos.IsEqual(toGridPos))
				{
					break;
				}

				if (blockObject.blockData.isVertical)
				{
					pos.y++;
				}
				else
				{
					pos.x++;
				}
			}

			if (dir == -1)
			{
				gridPositions.Reverse();
			}

			return gridPositions;
		}

		private bool DoesBlockOccupyCell(List<Pos> gridPositions, int ignoreBlockIndex, out Pos lastEmptyPos)
		{
			lastEmptyPos = new Pos(-1, -1);

			for (int i = 0; i < gridPositions.Count; i++)
			{
				Pos pos = gridPositions[i];

				for (int j = 0; j < activeLevelSaveData.blockPositions.Count; j++)
				{
					if (j == ignoreBlockIndex) continue;

					LevelData.Block	block			= activeLevelData.Blocks[j];
					Pos				blockFromPos	= activeLevelSaveData.blockPositions[j];

					int toX			= blockFromPos.x + (block.isVertical ? 0 : block.size - 1);
					int toY			= blockFromPos.y + (block.isVertical ? block.size - 1 : 0);
					Pos blockToPos	= new Pos(toX, toY);

					if (pos.x >= blockFromPos.x && pos.x <= blockToPos.x && pos.y >= blockFromPos.y && pos.y <= blockToPos.y)
					{
						return true;
					}
				}

				lastEmptyPos = pos;
			}

			return false;
		}

		private void MoveActiveBlockToClosestCell(out Pos gridPos)
		{
			gridPos = activeBlockObject.blockData.pos;

			if (activeBlockObject.blockData.isVertical)
			{
				// Get the closest grid y position for the active block to move into
				float y = activeBlockObject.block.RectT.anchoredPosition.y - spacing / 2f + gridCellSize.y / 2f;

				gridPos.y = Mathf.FloorToInt(y / (gridCellSize.y + spacing));
			}
			else
			{
				// Get the closest grid x position for the active block to move into
				float x = activeBlockObject.block.RectT.anchoredPosition.x - spacing / 2f + gridCellSize.x / 2f;

				gridPos.x = Mathf.FloorToInt(x / (gridCellSize.x + spacing));
			}

			PositionBlock(activeBlockObject.block.RectT, gridPos, true);
		}

		/// <summary>
		/// Increments the hint move index and shows it
		/// </summary>
		private void ShowNextHint()
		{
			activeLevelSaveData.nextHintMoveIndex++;

			HideLastHintArrow();

			// Check if there are still moves to be made
			if (activeLevelSaveData.nextHintMoveIndex < activeLevelData.MovesToComplete.Count)
			{
				// Setup the next hint
				SetupHint();
			}
			else
			{
				hintBlock.gameObject.SetActive(false);
			}
		}

		/// <summary>
		/// Sets up the board to display the current hint move
		/// </summary>
		private void SetupHint()
		{
			// Size and position the hint block
			ShowHintBlock();

			// Update the arrow
			UpdateHintArrow();
		}

		/// <summary>
		/// Shows and positions the hint block to the grid position where the next block needs to move
		/// </summary>
		private void ShowHintBlock()
		{
			LevelData.Move	move		= activeLevelData.MovesToComplete[activeLevelSaveData.nextHintMoveIndex];
			BlockObject		blockObject	= blockObjects[move.blockIndex];

			hintBlock.gameObject.SetActive(true);

			// Setup the hint block so it appears in the position that the block needst ot move to
			SetupBlock(hintBlock.RectT, blockObject.blockData, move.pos);

			// Setup the look of the hint block to be the same as the block that is moving
			hintBlock.Setup(move.blockIndex == 0 ? targetBlockColor : blockColor, !blockObject.blockData.isVertical);
		}

		/// <summary>
		/// Shows the hint arrow pointing in the direction the block needs to move for the next block that needs to move
		/// </summary>
		private void UpdateHintArrow()
		{
			LevelData.Move	move		= activeLevelData.MovesToComplete[activeLevelSaveData.nextHintMoveIndex];
			BlockObject		blockObject	= blockObjects[move.blockIndex];

			Vector2 blockPos		= blockObject.block.RectT.anchoredPosition;
			Vector2 hintBlockPos	= hintBlock.RectT.anchoredPosition;

			int arrowDirection = 0;

			// Get the direction the arrow should be pointing
			if (blockObject.blockData.isVertical && blockPos.y <= hintBlockPos.y)
			{
				// 1 == Up
				arrowDirection = 1;
			}
			else if (blockObject.blockData.isVertical && blockPos.y > hintBlockPos.y)
			{
				// 3 == Down
				arrowDirection = 3;
			}
			else if (!blockObject.blockData.isVertical && blockPos.x <= hintBlockPos.x)
			{
				// 0 == Right
				arrowDirection = 0;
			}
			else if (!blockObject.blockData.isVertical && blockPos.x > hintBlockPos.x)
			{
				// 2 == Left
				arrowDirection = 2;
			}

			blockObject.block.ShowHintArrow(arrowDirection);
		}

		/// <summary>
		/// Hides the hint arrow for the last block
		/// </summary>
		private void HideLastHintArrow()
		{
			int lastIndex = activeLevelSaveData.nextHintMoveIndex - 1;

			if (lastIndex != -1)
			{
				LevelData.Move lastMove = activeLevelData.MovesToComplete[lastIndex];

				// Hint the block that is displsying the current hint
				blockObjects[lastMove.blockIndex].block.HideHintArrow();
			}
		}

		#endregion
	}
}
