using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBG.BlockEscape
{
	public class LevelData
	{
		#region Classes

		public class Move
		{
			public int blockIndex;
			public Pos pos;

			public Move(int blockIndex, Pos pos)
			{
				this.blockIndex	= blockIndex;
				this.pos		= pos;
			}
		}

		public class Block
		{
			public Pos	pos;
			public int	size;
			public bool	isVertical;

			public Block(Pos pos, int size, bool isVertical)
			{
				this.pos		= pos;
				this.size		= size;
				this.isVertical	= isVertical;
			}
		}

		#endregion

		#region Member Variables

		private TextAsset	levelFile;
		private string		levelFileText;
		private bool		isLevelFileParsed;

		// Values parsed from level file
		private string		timestamp;
		private int			gridSize;
		private List<Block>	blocks;
		private List<Move>	movesToComplete;

		#endregion

		#region Properties

		public string		Id				{ get; private set; }
		public string		PackId			{ get; private set; }
		public int			LevelIndex		{ get; private set; }

		public string		Timestamp		{ get { if (!isLevelFileParsed) ParseLevelFile(); return timestamp; } }
		public int			GridSize		{ get { if (!isLevelFileParsed) ParseLevelFile(); return gridSize; } }
		public List<Block>	Blocks			{ get { if (!isLevelFileParsed) ParseLevelFile(); return blocks; } }
		public List<Move>	MovesToComplete	{ get { if (!isLevelFileParsed) ParseLevelFile(); return movesToComplete; } }

		private string LevelFileText
		{
			get
			{
				if (string.IsNullOrEmpty(levelFileText) && levelFile != null)
				{
					levelFileText	= levelFile.text;
					levelFile		= null;
				}

				return levelFileText;
			}
		}

		#endregion

		#region Constructor

		public LevelData(TextAsset levelFile, string packId, int levelIndex)
		{
			this.levelFile	= levelFile;
			
			Init(packId, levelIndex);
		}

		public LevelData(string levelFileText, string packId, int levelIndex)
		{
			this.levelFileText	= levelFileText;
			
			Init(packId, levelIndex);
		}

		#endregion

		#region Private Methods

		private void Init(string packId, int levelIndex)
		{
			PackId				= packId;
			LevelIndex			= levelIndex;
			Id					= string.Format("{0}_{1}", packId, levelIndex);

			blocks				= new List<Block>();
			movesToComplete		= new List<Move>();
		}

		/// <summary>
		/// Parse the json in the level file
		/// </summary>
		private void ParseLevelFile()
		{
			if (isLevelFileParsed) return;

			string levelFileContents = LevelFileText;

			int			index	= 0;
			string[]	values	= levelFileContents.Split(';');

			timestamp	= values[index++];
			gridSize	= ParseInt(values, index++);

			// Parse the starting block positions
			int numBlocks = ParseInt(values, index++);

			for (int i = 0; i < numBlocks; i++)
			{
				int		x			= ParseInt(values, index++);
				int		y			= ParseInt(values, index++);
				int		size		= ParseInt(values, index++);
				bool	isVertical	= ParseBool(values, index++);

				blocks.Add(new Block(new Pos(x, y), size, isVertical));
			}

			// Parse the moves needed to complete the level
			int numMoves = ParseInt(values, index++);

			for (int i = 0; i < numMoves; i++)
			{
				int blockIndex	= ParseInt(values, index++);
				int x			= ParseInt(values, index++);
				int y			= ParseInt(values, index++);

				movesToComplete.Add(new Move(blockIndex, new Pos(x, y)));
			}

			isLevelFileParsed = true;
		}

		private int ParseInt(string[] values, int index)
		{
			return int.Parse(values[index]);
		}

		private bool ParseBool(string[] values, int index)
		{
			return bool.Parse(values[index]);
		}

		#endregion
	}
}
