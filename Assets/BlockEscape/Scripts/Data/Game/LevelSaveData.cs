using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBG.BlockEscape
{
	public class LevelSaveData
	{
		#region Classes
		
		public class Undo
		{
			public int blockIndex;
			public Pos blockPosition;
			public int movesMade;
			public int lastMovedBlockIndex;
		}
		
		#endregion // Classes

		#region Member Variables

		public string		timestamp;
		public List<Pos>	blockPositions;
		public bool			hintUsed;
		public int			nextHintMoveIndex;
		public int			movesMade;
		public int			lastMovedBlockIndex;
		public List<Undo>	undos;

		#endregion

		#region Public Methods

		public LevelSaveData(LevelData levelData)
		{
			blockPositions	= new List<Pos>();
			undos			= new List<Undo>();
			timestamp		= levelData.Timestamp;
			
			Reset(levelData);
		}

		public LevelSaveData(JSONNode saveData)
		{
			blockPositions	= new List<Pos>();
			undos			= new List<Undo>();

			LoadSave(saveData);
		}

		public void Reset(LevelData levelData)
		{
			movesMade			= 0;
			lastMovedBlockIndex	= -1;
			nextHintMoveIndex	= 0;

			blockPositions.Clear();
			undos.Clear();

			for (int i = 0; i < levelData.Blocks.Count; i++)
			{
				blockPositions.Add(levelData.Blocks[i].pos);
			}
		}

		public bool MoveBlock(int blockIndex, Pos pos)
		{
			Pos curPos = blockPositions[blockIndex];

			if (pos.IsEqual(curPos))
			{
				return false;
			}

			// Add an undo move
			Undo undo = new Undo();

			undo.blockIndex				= blockIndex;
			undo.blockPosition			= curPos;
			undo.movesMade				= movesMade;
			undo.lastMovedBlockIndex	= lastMovedBlockIndex;

			undos.Add(undo);

			// Update the save data
			blockPositions[blockIndex] = pos;

			if (lastMovedBlockIndex != blockIndex)
			{
				movesMade++;
			}

			lastMovedBlockIndex = blockIndex;

			return true;
		}

		public bool UndoMove(out int blockIndex, out Pos blockPosition)
		{
			if (undos.Count > 0)
			{
				Undo undo = undos[undos.Count - 1];
				undos.RemoveAt(undos.Count - 1);

				blockIndex		= undo.blockIndex;
				blockPosition	= undo.blockPosition;

				blockPositions[blockIndex]	= blockPosition;
				movesMade					= undo.movesMade;
				lastMovedBlockIndex			= undo.lastMovedBlockIndex;

				return true;
			}

			blockIndex		= -1;
			blockPosition	= new Pos();

			return false;
		}

		public Dictionary<string, object> Save()
		{
			List<object> savedBlockPositions = new List<object>();

			for (int i = 0; i < blockPositions.Count; i++)
			{
				Pos							blockPosition		= blockPositions[i];
				Dictionary<string, object>	savedBlockPosition	= new Dictionary<string, object>();

				savedBlockPosition["x"]		= blockPosition.x;
				savedBlockPosition["y"]		= blockPosition.y;

				savedBlockPositions.Add(savedBlockPosition);
			}

			List<object> savedUndoes = new List<object>();

			for (int i = 0; i < undos.Count; i++)
			{
				Undo						undo		= undos[i];
				Dictionary<string, object>	savedUndo	= new Dictionary<string, object>();

				savedUndo["i"]			= undo.blockIndex;
				savedUndo["x"]			= undo.blockPosition.x;
				savedUndo["y"]			= undo.blockPosition.y;
				savedUndo["moves_made"]	= undo.movesMade;
				savedUndo["last_index"]	= undo.lastMovedBlockIndex;

				savedUndoes.Add(savedUndo);
			}

			Dictionary<string, object>	saveData = new Dictionary<string, object>();

			saveData["timestamp"]		= timestamp;
			saveData["block_positions"]	= savedBlockPositions;
			saveData["hint_used"]		= hintUsed;
			saveData["hint_index"]		= nextHintMoveIndex;
			saveData["moves_made"]		= movesMade;
			saveData["last_index"]		= lastMovedBlockIndex;
			saveData["undos"]			= savedUndoes;

			return saveData;
		}

		public void LoadSave(JSONNode saveData)
		{
			timestamp 			= saveData["timestamp"].Value;
			hintUsed			= saveData["hint_used"].AsBool;
			nextHintMoveIndex	= saveData["hint_index"].AsInt;
			movesMade			= saveData["moves_made"].AsInt;
			lastMovedBlockIndex	= saveData["last_index"].AsInt;

			JSONArray savedBlockPositions = saveData["block_positions"].AsArray;

			foreach (JSONNode savedBlockPosition in savedBlockPositions)
			{
				int x	= savedBlockPosition["x"].AsInt;
				int y	= savedBlockPosition["y"].AsInt;

				blockPositions.Add(new Pos(x, y));
			}

			JSONArray undosSaveData = saveData["undos"].AsArray;

			foreach (JSONNode undoSaveData in undosSaveData)
			{
				Undo undo = new Undo();

				undo.blockIndex				= undoSaveData["i"].AsInt;
				undo.blockPosition			= new Pos(undoSaveData["x"].AsInt, undoSaveData["y"].AsInt);
				undo.movesMade				= undoSaveData["moves_made"].AsInt;
				undo.lastMovedBlockIndex	= undoSaveData["last_index"].AsInt;

				undos.Add(undo);
			}
		}

		#endregion
	}
}
