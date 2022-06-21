using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BBG.BlockEscape
{
	public class LevelCreatorSettings : SingletonScriptableObject<LevelCreatorSettings>
	{
		#region Classes
		
		[System.Serializable]
		public class GenItem
		{
			public bool		isActive = true;
			public int		minMoves;
			public int		maxMoves;
			public string	subFolder;
			public string	filenamePrefix;
			public int		numLevels;

			public bool expanded;
		}
		
		#endregion // Classes

		#region Member Variables
		
		public string			outputFolderPath;
		public bool				overwriteFiles;
		public List<GenItem>	genItems;
		
		#endregion // Member Variables
	}
}
