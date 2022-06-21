using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBG.BlockEscape
{
	public class LevelSolver : Worker
	{
		#region Member Variables

		public double	solvedTime;
		
		private Board	originalBoard;
		private int		maxNumberOfMoves;
		private double	startTime;
		
		#endregion // Member Variables

		#region Public Methods
		
		public LevelSolver(Board board, int maxNumberOfMoves)
		{
			this.originalBoard		= board;
			this.maxNumberOfMoves	= maxNumberOfMoves;
		}
		
		#endregion // Public Methods

		#region Protected Methods
		
		public override void Begin()
		{
			startTime = Utilities.SystemTimeInMilliseconds;

			originalBoard.movesMade.Clear();

			if (!CheckSolvable(originalBoard))
			{
				Stop();
			}
		}

		public override void DoWork()
		{
			if (!TrySolveBoard(originalBoard))
			{
				originalBoard.movesMade.Clear();
			}

			solvedTime = Utilities.SystemTimeInMilliseconds - startTime;

			Stop();
		}

		#endregion // Protected Methods

		#region Private Methods

		private bool TrySolveBoard(Board board)
		{
			Dictionary<string, int> visitedBoardStates = new Dictionary<string, int>();

			int cuttoff			= board.boardSize - board.blocks[0].blockSize;
			int prevDicSize		= 0;
			int noChangeCount	= 0;

			for (int maxDepth = 0; maxDepth <= maxNumberOfMoves; maxDepth++)
			{
				if (Stopping) return false;

				if (DFS(board, -1, 0, maxDepth, visitedBoardStates))
				{
					return true;
				}

				if (prevDicSize == visitedBoardStates.Count)
				{
					noChangeCount++;
				}
				else
				{
					noChangeCount = 0;
				}

				if (noChangeCount > cuttoff)
				{
					return false;
				}

				prevDicSize = visitedBoardStates.Count;
			}

			return false;
		}

		private bool DFS(Board board, int lastMovedBlock, int depth, int maxDepth, Dictionary<string, int> visitedBoardStates)
		{
			if (Stopping) return false;
			
			string	boardState	= board.GetBoardState();
			int		height		= maxDepth - depth;

			if (visitedBoardStates.ContainsKey(boardState) && visitedBoardStates[boardState] >= height)
			{
				return false;
			}

			visitedBoardStates[boardState] = height;

			// Check if the target block cna escape the puzzle, ei the puzzle has been solved
			if (CheckEscape(board))
			{
				// Add the final move of the block out of the board
				Board.Block	targetBlock	= board.blocks[0];
				string		blockId		= targetBlock.blockId;
				Pos			pos			= new Pos(board.boardSize - targetBlock.blockSize, targetBlock.cellPositions[0].y);

				board.movesMade.Add(new Board.Move(blockId, pos));

				return true;
			}

			if (depth == maxDepth)
			{
				return false;
			}

			// Move each block on the board except for the last moved block
			for (int blockIndex = 0; blockIndex < board.blocks.Count; blockIndex++)
			{
				if (Stopping) return false;

				if (blockIndex == lastMovedBlock) continue;

				Board.Block	block = board.blocks[blockIndex];

				// Try moving the block in both directions (left/right or up/down)
				for (int dir = -1; dir <= 1; dir += 2)
				{
					if (Stopping) return false;

					int moveAmount;

					// Try and move the block
					if (board.CanMoveBlock(blockIndex, dir, out moveAmount))
					{
						for (int amount = 1; amount <= moveAmount; amount++)
						{
							if (Stopping) return false;

							// Get the current cell position of the block
							Pos cellPos = block.cellPositions[0];

							// Move the block on the new board
							board.MoveBlock(blockIndex, dir, amount);
							board.movesMade.Add(new Board.Move(block.blockId, board.blocks[blockIndex].cellPositions[0]));

							if (DFS(board, blockIndex, depth + 1, maxDepth, visitedBoardStates))
							{
								// Move the block back so the board will be returned to it original state when the algo started
								board.MoveBlockTo(blockIndex, cellPos.x, cellPos.y);
								return true;
							}

							// Move the block back and try another
							board.MoveBlockTo(blockIndex, cellPos.x, cellPos.y);
							board.movesMade.RemoveAt(board.movesMade.Count - 1);
						}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Checks if the target block can escape the board
		/// </summary>
		private bool CheckEscape(Board board)
		{
			Board.Block targetBlock = board.blocks[0];

			int startX	= targetBlock.cellPositions[targetBlock.blockSize - 1].x;
			int y		= targetBlock.cellPositions[0].y;

			for (int x = startX + 1; x < board.boardSize; x++)
			{
				if (!board.IsCellEmpty(x, y))
				{
					return false;
				}
			}

			return true;
		}
		
		public bool CheckSolvable(Board board)
		{
			board.cellMarker++;

			Board.Block targetBlock		= board.blocks[0];
			Pos			targetLastCell	= targetBlock.cellPositions[targetBlock.blockSize - 1];
			float		targetX			= targetLastCell.x;
			float		targetY			= targetLastCell.y;

			if (board.boardSize % 2 == 0)
			{
				int checkBlockSize	= board.boardSize / 2;
				int minCheckX		= board.boardSize - targetBlock.blockSize;

				for (int i = 0; i < board.blocks.Count; i++)
				{
					Board.Block block = board.blocks[i];

					if (block.isVertical && block.blockSize == checkBlockSize && block.cellPositions[0].x >= minCheckX)
					{
						for (int y = 0; y < block.blockSize; y++)
						{
							board.cells[y][block.cellPositions[0].x].vMarker = board.cellMarker;
						}
					}
				}
			}

			bool done = false;

			while (!done)
			{
				done = true;

				for (int i = 0; i < board.blocks.Count; i++)
				{
					Board.Block block = board.blocks[i];

					Pos firstCellPos	= block.cellPositions[0];
					Pos lastCellPos		= block.cellPositions[block.blockSize - 1];

					int xDir = block.isVertical ? 0 : 1;
					int yDir = block.isVertical ? 1 : 0;

					int pEmpty		= CountEmptyCells(board, lastCellPos.x + xDir, lastCellPos.y + yDir, xDir, yDir, block.isVertical);
					int nEmpty		= CountEmptyCells(board, firstCellPos.x - xDir, firstCellPos.y - yDir, -xDir, -yDir, block.isVertical);
					int totalEmpty	= pEmpty + nEmpty;

					int numPermaBlocked = block.blockSize - totalEmpty;

					if (numPermaBlocked > 0)
					{
						int offset	= totalEmpty - nEmpty;
						int x		= firstCellPos.x + xDir * offset;
						int y		= firstCellPos.y + yDir * offset;

						for (int j = 0; j < numPermaBlocked; j++)
						{
							int			cellX	= x + xDir * j;
							int			cellY	= y + yDir * j;
							Board.Cell	cell	= board.cells[cellY][cellX];

							bool marked = false;

							if (block.isVertical)
							{
								if (cell.vMarker != board.cellMarker)
								{
									cell.vMarker	= board.cellMarker;
									marked			= true;
								}
							}
							else
							{
								if (cell.hMarker != board.cellMarker)
								{
									cell.hMarker	= board.cellMarker;
									marked			= true;
								}
							}

							if (marked)
							{
								done = false;

								// Check if the marked cell is in the path of the target block, if so this board will be impossible to solve
								if (cellY == targetY && cellX > targetX)
								{
									return false;
								}
							}
						}
					}
				}
			}

			// board.Print("Check Solvable");
			// board.PrintMarkedCells();

			return true;
		}

		private int CountEmptyCells(Board board, int xStart, int yStart, int xInc, int yInc, bool isVertical)
		{
			int empty = 0;

			for (int x = xStart, y = yStart; board.IsOnBoard(x, y); x += xInc, y += yInc)
			{
				Board.Cell cell = board.cells[y][x];

				if ((isVertical && cell.hMarker == board.cellMarker) || (!isVertical && cell.vMarker == board.cellMarker))
				{
					break;
				}

				if (string.IsNullOrEmpty(cell.blockId) || cell.isBlockVertical != isVertical)
				{
					empty++;
				}
			}

			return empty;
		}
		
		#endregion // Private Methods
	}
}
