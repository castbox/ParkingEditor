using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBG.BlockEscape
{
	public class LevelGenerator : Worker
	{
		#region Member Variables

		private const int GridSize			= 6;
		private const int TargetBlockRow	= 3;
		private const int TargetBlockSize	= 2;
		private const int MinBlockSize		= 2;
		private const int MaxBlockSize		= 3;
		
		private System.Random					random;
		private LevelCreatorSettings.GenItem	genItem;

		private int								targetMoves;
		private int								iterations;

		private Board							bestBoard;
		private Dictionary<string, float>		cachedBoardEnergies;

		private readonly object lockObj		= new object();
		private int				curMoves	= 0;

		#endregion // Member Variables

		#region Properties

		public int CurMoves
		{
			get { lock(lockObj) return curMoves; }
			set { lock(lockObj) curMoves = value; }
		}
		
		#endregion // Properties

		#region Public Methods
		
		public LevelGenerator(LevelCreatorSettings.GenItem genItem, System.Random random, int targetMoves, int iterations)
		{
			this.genItem		= genItem;
			this.random			= random;
			this.targetMoves	= targetMoves;
			this.iterations		= iterations;
		}
		
		public override void Begin()
		{
			cachedBoardEnergies = new Dictionary<string, float>();
		}

		public override void DoWork()
		{
			GenerateBoard();

			Stop();
		}

		public Board GetBestBoard()
		{
			return bestBoard;
		}
		
		#endregion // Public Methods

		#region Private Methods

		private void GenerateBoard()
		{
			float	maxTemp		= 20f;

			Board board				= CreateStartingBoard();
			float energy			= GetEnergy(board);
			
			bestBoard = board.Copy();

			CurMoves = board.movesMade.Count;

			for (int step = 1; step <= iterations; step++)
			{
				if (Stopping) break;

				float t		= (float)step / (float)iterations;
				float temp	= Mathf.Lerp(maxTemp, 0.1f, t);

				Progress = t;

				Board mutatedBoard	= MutateBoard(board);
				float mutatedEnergy = 0;

				string mutatedBoardState = mutatedBoard.GetBoardState();

				if (cachedBoardEnergies.ContainsKey(mutatedBoardState))
				{
					mutatedEnergy = cachedBoardEnergies[mutatedBoardState];
				}
				else
				{
					mutatedEnergy = GetEnergy(mutatedBoard);
					cachedBoardEnergies.Add(mutatedBoardState, mutatedEnergy);
				}

				int movesMade = mutatedBoard.movesMade.Count;

				if (movesMade > bestBoard.movesMade.Count)
				{
					bestBoard = mutatedBoard.Copy();

					CurMoves = bestBoard.movesMade.Count;

					// If the best board has the target moves then break now
					if (movesMade == targetMoves)
					{
						break;
					}

					// If the best board has more than the target moves then we simply need to solve the board until there is only target moves left
					if (movesMade > targetMoves)
					{
						int makeMoves = movesMade - targetMoves;

						for (int i = 0; i < makeMoves; i++)
						{
							Board.Move move = bestBoard.movesMade[0];
							bestBoard.movesMade.RemoveAt(0);
							
							bestBoard.MoveBlockTo(move.blockId, move.pos.x, move.pos.y);
						}

						break;
					}
				}

				float change	= mutatedEnergy - energy;
				float rand		= (float)random.Next(0, 10001) / 10000f;
				float p 		= Mathf.Exp(-change / temp);

				// mutatedBoard.Print(string.Format("Mutated | # moves: {0}, energy: {1}, change: {2}, rand: {3}, p: {4}\nBoard | # moves: {5}, energy: {6}", movesMade, mutatedEnergy, change, rand, p, board.movesMade.Count, energy));

				if (!Mathf.Approximately(p, 0) && p >= rand)
				{
					board	= mutatedBoard;
					energy	= mutatedEnergy;

					// Debug.Log("ACCEPTED");
				}
			}
		}

		private Board CreateStartingBoard()
		{
			Board board = new Board();

			board.Initialize(GridSize);
			board.AddBlock(0, TargetBlockRow, TargetBlockSize, false);

			return board;
		}

		private float GetEnergy(Board board)
		{
			LevelSolver levelSolver = new LevelSolver(board, Mathf.RoundToInt(targetMoves * 1.25f));

			levelSolver.StartWorker();

			// Wait for the solver to finish
			while (!levelSolver.Stopping)
			{
				// LevelGenerator is stopping so return now
				if (Stopping)
				{
					levelSolver.Stop();

					return 100000000f;
				}
			}

			// Debug.Log("Finished solving");

			// Board is not solvable
			if (board.movesMade.Count == 0)
			{
				return 100000000f;
			}

			// Get the energy which is made up of the number of moves it takes to solver the board and the number of steps needed
			int		movesCount	= board.movesMade.Count;
			float	solveTime	= (float)levelSolver.solvedTime;

			int		movesDiff			= Mathf.Abs(targetMoves * 2 - movesCount);
			float	iterationsWeight	= (movesDiff + 1) / (targetMoves * 2);

			return Mathf.Pow(movesDiff, 2) + (solveTime / 100f) * iterationsWeight;
		}

		private Board MutateBoard(Board board)
		{
			Board mutatedBoard = board.Copy();

			bool	done	= false;
			int		s		= 0;
			int		n		= 4;

			while (!done)
			{
				done = true;

				switch (random.Next(s, s + n) % 4)
				{
					case 0:	// Move a random block
					{
						// Debug.Log("Move");
						if (!MoveRandomBlock(mutatedBoard))
						{
							// No blocks could be moved on the board, set s and n so this case won't be picked again
							done	= false;
							s		= 1;
							n		= 3;	
						}
						break;
					}
					case 1:	// Remove and add a random block
					{
						// Debug.Log("Remove + Add");
						if (mutatedBoard.blocks.Count > 1)
						{
							// Remove a random block
							mutatedBoard.RemoveBlock(random.Next(1, mutatedBoard.blocks.Count));
						}

						AddRandomBlock(mutatedBoard);

						break;
					}
					case 2:	// Remove a random block
					{
						// Debug.Log("Remove");
						if (mutatedBoard.blocks.Count <= 1)
						{
							// Cannot remove any more blocks, set s and n so this case won't be picked again
							done	= false;
							s		= 3;
							n		= 3;	
						}
						else
						{
							// Remove a random block (Not the first one)
							mutatedBoard.RemoveBlock(random.Next(1, mutatedBoard.blocks.Count));
						}
						break;
					}
					case 3: // Add a random block
					{
						// Debug.Log("Add");
						if (!AddRandomBlock(mutatedBoard))
						{
							// There where no valid spots to add a block, set s and n so this case won't be picked again
							done	= false;
							s		= 0;
							n		= 3;	
						}
						
						break;
					}
				}
			}

			return mutatedBoard;
		}

		private bool MoveRandomBlock(Board board)
		{
			int numBlocks	= board.blocks.Count;
			int iStart		= random.Next(0, numBlocks);
			int i			= iStart;

			while (true)
			{
				Board.Block	block		= board.blocks[i];
				int			firstDir	= (random.Next(0, 2) == 1) ? 1 : -1;

				for (int j = -1; j <= 1; j += 2)
				{
					int dir = j * firstDir;

					int moveAmount;

					// Try and move the block
					if (board.CanMoveBlock(i, dir, out moveAmount))
					{
						// We don't want to move the target block to far right
						if (i == 0)
						{
							if (dir == -1 || (dir == 1 && block.cellPositions[0].x == 0))
							{
								// Move the target block 1 space
								board.MoveBlock(i, dir, 1);
							}
						}
						else
						{
							// Move the block
							board.MoveBlock(i, dir, random.Next(1, moveAmount + 1));
						}

						return true;
					}
				}

				i = (i + 1) % numBlocks;

				// Check if we are back at the start, if so then we cannot move any blocks
				if (i == iStart)
				{
					return false;
				}
			}
		}

		private bool AddRandomBlock(Board board)
		{
			// Pick a random cell to start
			int xStart = random.Next(0, board.boardSize);
			int yStart = random.Next(0, board.boardSize);

			int x = xStart;
			int y = yStart;

			int targetY = board.blocks[0].cellPositions[0].y;

			while (true)
			{
				// Check if the cell is empty
				if (string.IsNullOrEmpty(board.cells[y][x].blockId))
				{
					bool isVertical;
					int dirsToTry;

					if (y == targetY)
					{
						// We don't want to place any horizontal blocks in the target block row
						isVertical	= true;
						dirsToTry	= 1;
					}
					else
					{
						// Pick a random direction to try first
						isVertical	= (random.Next(0, 2) == 1) ? true : false;
						dirsToTry	= 2;
					}

					// Need to try placing a vertical and horizontal block at x/y
					for (int i = 0; i < dirsToTry; i++)
					{
						int xDir = isVertical ? 0 : 1;
						int yDir = isVertical ? 1 : 0;

						int emptyCells = Mathf.Min(MaxBlockSize, CountEmptyCells(board, x, y, xDir, yDir));

						if (emptyCells >= MinBlockSize)
						{
							// Add a block with random valid size
							board.AddBlock(x, y, random.Next(MinBlockSize, emptyCells + 1), isVertical);

							return true;
						}

						isVertical = !isVertical;
					}
				}

				x++;

				if (x >= board.boardSize)
				{
					x = 0;
					y = (y + 1) % board.boardSize;
				}

				// Check if we are back at the start, if so then we cannot add any more blocks
				if (x == xStart && y == yStart)
				{
					return false;
				}
			}
		}

		private int CountEmptyCells(Board board, int fromX, int fromY, int xDir, int yDir)
		{
			int count = 0;

			for (int x = fromX, y = fromY; board.IsCellEmpty(x, y); x += xDir, y += yDir, count++) {}

			return count;
		}

		#endregion // Private Methods
	}
}
