using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

namespace BBG.BlockEscape
{
	public class Board
	{
		#region Classes
		
		public class Cell
		{
			public Pos		cellPos;
			public string	blockId;
			public bool		isBlockVertical;
			public int		vMarker;
			public int		hMarker;

			public Cell Copy()
			{
				Cell cell = new Cell();

				cell.cellPos			= cellPos;
				cell.blockId			= blockId;
				cell.isBlockVertical	= isBlockVertical;

				return cell;
			}
		}

		public class Block
		{
			public string		blockId;
			public int			blockSize;
			public bool			isVertical;

			public List<Pos>	cellPositions		= new List<Pos>();

			public Block Copy()
			{
				Block block = new Block();

				block.blockId		= blockId;
				block.blockSize		= blockSize;
				block.isVertical	= isVertical;

				for (int i = 0; i < cellPositions.Count; i++)
				{
					block.cellPositions.Add(cellPositions[i]);
				}

				return block;
			}
		}

		public class Move
		{
			public string	blockId;
			public Pos		pos;

			public Move(string blockId, Pos pos)
			{
				this.blockId	= blockId;
				this.pos		= pos;
			}

			public Move Copy()
			{
				return new Move(blockId, pos);
			}
		}
		
		#endregion // Classes

		#region Member Variables
		
		public int				boardSize;
		public List<List<Cell>>	cells			= new List<List<Cell>>();
		public List<Block>		blocks			= new List<Block>();
		public List<Move>		movesMade		= new List<Move>();

		private bool			isInitialized;
		private int				globalBlockId;
		private StringBuilder 	state;
		public int				cellMarker;
		
		#endregion // Member Variables

		#region Public Methods

		public void Initialize(int size)
		{
			if (!isInitialized)
			{
				boardSize	= size;
				state		= new StringBuilder();

				for (int y = 0; y < boardSize; y++)
				{
					cells.Add(new List<Cell>());

					for (int x = 0; x < boardSize; x++)
					{
						Cell cell = new Cell();

						cell.cellPos	= new Pos(x, y);
						cell.blockId	= "";

						state.Append("_");

						cells[y].Add(cell);
					}
				}
				
				isInitialized = true;
			}
		}

		/// <summary>
		/// Adds a block to the board at the given start position, assumes a block can be placed at this position and there is no other blocks in the way.
		/// </summary>
		public void AddBlock(int x, int y, int blockSize, bool isVertical)
		{
			Debug.Log("star-----> addBlock");
			int		xInc	= isVertical ? 0 : 1;
			int		yInc	= isVertical ? 1 : 0;
			string	blockId	= (globalBlockId++).ToString();

			Block block = new Block();

			block.blockId		= blockId;
			block.blockSize		= blockSize;
			block.isVertical	= isVertical;

			// Check to make sure the block can fit on the board at the given position
			if (x + (blockSize - 1) * xInc >= boardSize || y + (blockSize - 1) * yInc >= boardSize)
			{
				throw new System.Exception("Cannot add block, block wont fit on the board.");
			}

			for (int i = 0; i < blockSize; i++)
			{
				int xPos = x + i * xInc;
				int yPos = y + i * yInc;

				Cell cell = cells[yPos][xPos];

				// Check that there is not already a block on the board where the new block needs to go
				if (!string.IsNullOrEmpty(cell.blockId))
				{
					throw new System.Exception("Cannot add block, block already exists on the board.");
				}

				cell.blockId			= blockId;
				cell.isBlockVertical	= isVertical;

				block.cellPositions.Add(new Pos(xPos, yPos));
			}

			blocks.Add(block);
		}

		public void RemoveBlock(int blockIndex)
		{
			Block block = blocks[blockIndex];

			for (int i = 0; i < block.blockSize; i++)
			{
				Pos		fromCellPos	= block.cellPositions[i];
				Cell	fromCell	= cells[fromCellPos.y][fromCellPos.x];

				fromCell.blockId = "";
			}

			blocks.RemoveAt(blockIndex);
		}

		/// <summary>
		/// Moves the block to the given position
		/// </summary>
		public void MoveBlockTo(int blockIndex, int x, int y)
		{
			if (blockIndex < 0 || blockIndex >= blocks.Count)
			{
				throw new System.Exception(string.Format("MoveBlock CellPos | blockIndex ({0}) out of bounds.", blockIndex));
			}

			Block block = blocks[blockIndex];

			int xInc = block.isVertical ? 0 : 1;
			int yInc = block.isVertical ? 1 : 0;

			// First remove the block
			for (int i = 0; i < block.blockSize; i++)
			{
				Pos		fromCellPos	= block.cellPositions[i];
				Cell	fromCell	= cells[fromCellPos.y][fromCellPos.x];

				fromCell.blockId = "";
			}

			// Add the block at the new location
			for (int i = 0; i < block.blockSize; i++)
			{
				// Get the new cell position
				int xPos = x + i * xInc;
				int yPos = y + i * yInc;

				Cell toCell = cells[yPos][xPos];

				// Check that there is not already a block on the board where the new block needs to go
				if (!string.IsNullOrEmpty(toCell.blockId) && toCell.blockId != block.blockId)
				{
					throw new System.Exception("MoveBlock CellPos | Cannot move block, block already exists.");
				}

				// Place a piece of the block at on toCell
				toCell.blockId			= block.blockId;
				toCell.isBlockVertical	= block.isVertical;

				// Update the position
				block.cellPositions[i] = new Pos(xPos, yPos);
			}
		}
		
		/// <summary>
		/// Moves the block to the given position
		/// </summary>
		public void MoveBlockTo(string blockId, int x, int y)
		{
			for (int i = 0; i < blocks.Count; i++)
			{
				Block block = blocks[i];

				if (blockId == block.blockId)
				{
					MoveBlockTo(i, x, y);
					break;
				}
			}
		}

		/// <summary>
		/// Moves the block in the given direction the amount of cells
		/// </summary>
		public void MoveBlock(int blockIndex, int dir, int amount)
		{
			if (blockIndex < 0 || blockIndex >= blocks.Count)
			{
				throw new System.Exception(string.Format("MoveBlock Direction | blockIndex ({0}) out of bounds.", blockIndex));
			}

			Block	block		= blocks[blockIndex];
			Pos 	fromCell	= block.cellPositions[0];
			int		xInc		= block.isVertical ? 0 : dir;
			int		yInc		= block.isVertical ? dir : 0;
			int		toX			= fromCell.x + xInc * amount;
			int		toY			= fromCell.y + yInc * amount;

			MoveBlockTo(blockIndex, toX, toY);
		}
		
		/// <summary>
		/// Checks if the block can move in the given direction, if true then amount will be the maximum number of cells it can move
		/// </summary>
		public bool CanMoveBlock(int blockIndex, int dir, out int amount)
		{
			if (blockIndex < 0 || blockIndex >= blocks.Count)
			{
				throw new System.Exception(string.Format("MoveBlock Direction | blockIndex ({0}) out of bounds.", blockIndex));
			}

			Block block = blocks[blockIndex];

			int xInc = block.isVertical ? 0 : dir;
			int yInc = block.isVertical ? dir : 0;

			// Find the last empty space on the board
			Pos fromCell = (dir > 0) ? block.cellPositions[block.blockSize - 1] : block.cellPositions[0];

			int x = fromCell.x;
			int y = fromCell.y;
			
			amount = 0;

			while (IsCellEmpty(x + xInc, y + yInc))
			{
				x += xInc;
				y += yInc;

				amount++;
			}

			// Check that the block actually moved
			if (amount == 0)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Adds the move to the list of moves
		/// </summary>
		public void AddMove(string blockId, Pos pos)
		{
			movesMade.Add(new Move(blockId, pos));
		}

		/// <summary>
		/// Checks if the given x/y position is on the board and is empty
		/// </summary>
		public bool IsCellEmpty(int xPos, int yPos)
		{
			if (!IsOnBoard(xPos, yPos))
			{
				return false;
			}

			return string.IsNullOrEmpty(cells[yPos][xPos].blockId);
		}

		public bool IsOnBoard(int xPos, int yPos)
		{
			return (xPos >= 0 && yPos >= 0 && xPos < boardSize && yPos < boardSize);
		}

		public string GetBoardState()
		{
			Dictionary<string, char>	mapping			= new Dictionary<string, char>();
			int							curInt			= 0;
			string						targetBlockId	= blocks[0].blockId;

			for (int y = 0; y < boardSize; y++)
			{
				List<Cell> row = cells[y];

				for (int x = 0; x < boardSize; x++)
				{
					int		index	= x + y * boardSize;
					string	blockId	= row[x].blockId;

					if (string.IsNullOrEmpty(blockId))
					{
						state[index] = '_';
					}
					else if (blockId == targetBlockId)
					{
						state[index] = '#';
					}
					else
					{
						if (!mapping.ContainsKey(blockId))
						{
							mapping.Add(blockId, (char)((int)'A' + curInt));
							curInt++;
						}

						state[index] = mapping[blockId];
					}
				}
			}

			return state.ToString();
		}

		public Board Copy()
		{
			Board boardCopy = new Board();

			boardCopy.isInitialized	= true;
			boardCopy.boardSize		= boardSize;
			boardCopy.globalBlockId = globalBlockId;
			boardCopy.state			= new StringBuilder();
			boardCopy.cellMarker	= cellMarker;

			// Copy each Cell
			for (int y = 0; y < boardSize; y++)
			{
				boardCopy.cells.Add(new List<Cell>());

				for (int x = 0; x < boardSize; x++)
				{
					boardCopy.cells[y].Add(cells[y][x].Copy());
					boardCopy.state.Append("_");
				}
			}

			// Copy each block
			for (int i = 0; i < blocks.Count; i++)
			{
				boardCopy.blocks.Add(blocks[i].Copy());
			}

			// Copy each move made
			for (int i = 0; i < movesMade.Count; i++)
			{
				boardCopy.movesMade.Add(movesMade[i].Copy());
			}

			return boardCopy;
		}

		/// <summary>
		/// Returns the board state which is a string the represents the blockindex in each cell
		/// </summary>
		public void Print(string header = "")
		{
			string str = header;

			Dictionary<int, int> blockIdMapping = new Dictionary<int, int>();
			int blockIdToUse = 0;

			string targetBlockId = blocks[0].blockId;

			for (int y = boardSize - 1; y >= 0; y--)
			{
				if (!string.IsNullOrEmpty(str))
				{
					str += "\n";
				}

				for (int x = 0; x < boardSize; x++)
				{
					Cell cell = cells[y][x];

					if (string.IsNullOrEmpty(cell.blockId))
					{
						str += "_";
					}
					else if (cell.blockId == targetBlockId)
					{
						str += "#";
					}
					else
					{
						int blockId = int.Parse(cell.blockId);

						if (!blockIdMapping.ContainsKey(blockId))
						{
							blockIdMapping.Add(blockId, blockIdToUse++);
						}

						int num = blockIdMapping[blockId];

						str += ((char)((int)'A' + num)).ToString();
					}
				}
			}

			Debug.Log(str);
		}

		/// <summary>
		/// Returns the board state which is a string the represents the blockindex in each cell
		/// </summary>
		public void PrintMarkedCells()
		{
			string str = "";

			for (int y = boardSize - 1; y >= 0; y--)
			{
				if (!string.IsNullOrEmpty(str))
				{
					str += "\n";
				}

				for (int x = 0; x < boardSize; x++)
				{
					Cell cell = cells[y][x];

					if (cell.vMarker == cellMarker || cell.hMarker == cellMarker)
					{
						str += "X";
					}
					else
					{
						str += "_";
					}
				}
			}

			Debug.Log(str);
		}

		#endregion // Public Methods

		#region Private Methods
		
		
		
		#endregion // Private Methods
	}
}
