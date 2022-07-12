using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BBG.BlockEscape
{
	public class LevelCreatorEditor : CustomEditorWindow
	{
		#region Member Variables
		
		private SerializedObject		settingsSerializedObject;
		private Object					outputFolder;

		private bool					isGenerating;
		private LevelGenerator			levelGenerator;
		private System.Random			random;
		private int						randomSeed;
		private int						genItemIndex;
		private int						numLevelsGenerated;
		private int						numLevelsToGenerate;
		private int						targetMoves;
		private int						iterations;

		private double startTime;


		private static LevelCreatorEditor _instance;
		public static LevelCreatorEditor Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = CreateWindow<LevelCreatorEditor>();
				}

				return _instance;
			}
		}

		#endregion // Member Variables

		#region Properties
		
		private SerializedObject SettingsSerializedObject
		{
			get
			{
				if (settingsSerializedObject == null)
				{
					settingsSerializedObject = new SerializedObject(LevelCreatorSettings.Instance);
				}

				return settingsSerializedObject;
			}
		}

		private string OutputFolderAssetPath
		{
			get { return SettingsSerializedObject.FindProperty("outputFolderPath").stringValue; }
			set { SettingsSerializedObject.FindProperty("outputFolderPath").stringValue = value; }
		}
		
		#endregion // Properties

		#region Unity Methods

		private void OnEnable()
		{
			SetOutputFolderReference();
		}
		
		private void Update()
		{
			if (isGenerating)
			{
				if (levelGenerator.Stopped)
				{
					LevelFinishedGenerating(levelGenerator);

					if (!isGenerating)
					{
						return;
					}
				}

				List<LevelCreatorSettings.GenItem> genItems = LevelCreatorSettings.Instance.genItems;

				string	title		= string.Format("Gen Item {0} of {1}", genItemIndex + 1, genItems.Count);
				string	message		= string.Format("Generating level {0} of {1}. Board moves: {2}, target: {3}", numLevelsGenerated + 1, numLevelsToGenerate, levelGenerator.CurMoves, targetMoves);

				float t = (float)numLevelsGenerated / numLevelsToGenerate;

				bool cancelled = EditorUtility.DisplayCancelableProgressBar(title, message, t);

				if (cancelled)
				{
					StopGenerating();

					Debug.Log("Cancelled");
				}
			}
		}
		
		#endregion // Unity Methods

		#region Public Methods

		[MenuItem("Tools/Bizzy Bee Games/Level Creator Window")]
		public static void OpenWindow()
		{
			EditorWindow.GetWindow<LevelCreatorEditor>("Level Creator").Show();
		}

		public override void DoGUI()
		{
			SettingsSerializedObject.Update();

			if (levelGenerator != null)
			{
				GUI.enabled = false;
			}

			DrawLevelCreatorSettings();

			GUI.enabled = true;

			SettingsSerializedObject.ApplyModifiedProperties();
		}
		
		#endregion // Public Methods

		#region Private Methods

		private void DrawLevelCreatorSettings()
		{
			BeginBox("Level Creator Settings");

			DrawOuputFolder();

			EditorGUILayout.PropertyField(SettingsSerializedObject.FindProperty("overwriteFiles"));

			EditorGUILayout.Space();

			DrawBoldLabel("Generation Items");

			SerializedProperty genItemsProp = SettingsSerializedObject.FindProperty("genItems");

			if (GUILayout.Button("Add Generation Item") || genItemsProp.arraySize == 0)
			{
				genItemsProp.InsertArrayElementAtIndex(genItemsProp.arraySize);
			}

			int deletedIndex = -1;

			for (int i = 0; i < genItemsProp.arraySize; i++)
			{
				SerializedProperty genItem				= genItemsProp.GetArrayElementAtIndex(i);
				SerializedProperty genItemExpandedProp	= genItem.FindPropertyRelative("expanded");
				SerializedProperty getItemIsActiveProp	= genItem.FindPropertyRelative("isActive");

				EditorGUILayout.BeginHorizontal();

				genItemExpandedProp.boolValue = DrawFoldout(genItemExpandedProp.boolValue, "Gen Item " + (i + 1));

				getItemIsActiveProp.boolValue = EditorGUILayout.Toggle(getItemIsActiveProp.boolValue, GUILayout.Width(16));
				EditorGUILayout.LabelField("Active", GUILayout.Width(50));

				if (GUILayout.Button("Delete Gen Item " + (i + 1), GUILayout.Width(125)))
				{
					deletedIndex = i;
				}

				EditorGUILayout.EndHorizontal();

				if (genItemExpandedProp.boolValue)
				{
					EditorGUI.indentLevel++;

					if (!getItemIsActiveProp.boolValue)
					{
						EditorGUILayout.HelpBox("This generation item is not active, it will not run when \"Generate Levels\" is clicked.", MessageType.Warning);
					}

					DrawGenItem(genItem);

					EditorGUI.indentLevel--;
				}
			}

			if (deletedIndex != -1)
			{
				genItemsProp.DeleteArrayElementAtIndex(deletedIndex);
			}

			EditorGUILayout.Space();

			if (GUILayout.Button("Generate Levels"))
			{
				Debug.Log("star-----> click Generate Levels");
				StartGeneratingLevels();
			}

			EndBox();
		}

		private void DrawOuputFolder()
		{
			Object setOutputFolder = EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(Object), false);

			if (setOutputFolder != outputFolder)
			{
				outputFolder = setOutputFolder;

				SetOutputFolderAssetPath();
			}
		}

		private void DrawGenItem(SerializedProperty genItemProp)
		{
			SerializedProperty minMovesProp			= genItemProp.FindPropertyRelative("minMoves");
			SerializedProperty maxMovesProp			= genItemProp.FindPropertyRelative("maxMoves");
			SerializedProperty numLevelsProp		= genItemProp.FindPropertyRelative("numLevels");

			EditorGUILayout.PropertyField(minMovesProp);
			EditorGUILayout.PropertyField(maxMovesProp);
			EditorGUILayout.PropertyField(numLevelsProp);
			EditorGUILayout.PropertyField(genItemProp.FindPropertyRelative("subFolder"));
			EditorGUILayout.PropertyField(genItemProp.FindPropertyRelative("filenamePrefix"));

			int minMoves			= minMovesProp.intValue;
			int maxMoves			= maxMovesProp.intValue;
			int numLevels			= numLevelsProp.intValue;

			minMoves				= Mathf.Max(minMoves, 1);
			maxMoves				= Mathf.Max(maxMoves, minMoves);
			numLevels				= Mathf.Max(numLevels, 1);

			minMovesProp.intValue	= minMoves;
			maxMovesProp.intValue	= maxMoves;
			numLevelsProp.intValue	= numLevels;
		}

		private void SetOutputFolderReference()
		{
			// Set the reference to the output folder
			if (!string.IsNullOrEmpty(OutputFolderAssetPath))
			{
				outputFolder = AssetDatabase.LoadAssetAtPath<Object>(OutputFolderAssetPath);

				// Check if the folder still exists
				if (outputFolder == null)
				{
					OutputFolderAssetPath = null;
				}
			}
		}

		private void SetOutputFolderAssetPath()
		{
			if (outputFolder == null)
			{
				OutputFolderAssetPath = null;
			}
			else
			{
				OutputFolderAssetPath = AssetDatabase.GetAssetPath(outputFolder);
			}
		}

		private void StartGeneratingLevels()
		{
			startTime = Utilities.SystemTimeInMilliseconds;

			isGenerating		= true;
			numLevelsGenerated	= 0;
			numLevelsToGenerate = 0;
			genItemIndex		= -1;
			iterations			= 1000;

			GenerateNextLevel();
		}

		private void StopGenerating()
		{
			Debug.LogFormat("Finished, took {0} seconds", (Utilities.SystemTimeInMilliseconds - startTime) / 1000.0);

			isGenerating = false;

			if (levelGenerator != null)
			{
				levelGenerator.Stop();
				levelGenerator = null;
			}

			EditorUtility.ClearProgressBar();

			AssetDatabase.Refresh();
		}

		private int GetNextActiveGenItemIndex(int fromIndex)
		{
			List<LevelCreatorSettings.GenItem> getItems = LevelCreatorSettings.Instance.genItems;

			for (int i = fromIndex; i < getItems.Count; i++)
			{
				LevelCreatorSettings.GenItem genItem = getItems[i];

				if (genItem.isActive)
				{
					return i;
				}
			}

			return -1;
		}

		private void GenerateNextLevel()
		{
			while (numLevelsGenerated == numLevelsToGenerate)
			{
				List<LevelCreatorSettings.GenItem> getItems = LevelCreatorSettings.Instance.genItems;

				genItemIndex = GetNextActiveGenItemIndex(genItemIndex + 1);

				if (genItemIndex >= getItems.Count || genItemIndex == -1)
				{
					StopGenerating();

					return;
				}

				LevelCreatorSettings.GenItem genItem = getItems[genItemIndex];

				numLevelsGenerated	= GetNumLevelsGenerated(genItem);
				numLevelsToGenerate	= genItem.numLevels;
			}

			GenerateLevel(genItemIndex);
		}

		private int GetNumLevelsGenerated(LevelCreatorSettings.GenItem genItem)
		{
			if (!LevelCreatorSettings.Instance.overwriteFiles)
			{
				string outputFolder = Application.dataPath + OutputFolderAssetPath.Remove(0, "Assets".Length);
				outputFolder += string.IsNullOrEmpty(genItem.subFolder) ? "" : "/" + genItem.subFolder;

				if (System.IO.Directory.Exists(outputFolder))
				{
					return System.IO.Directory.GetFiles(outputFolder, "*.txt").Length;
				}
			}

			return 0;
		}

		private void GenerateLevel(int index)
		{
			randomSeed	= Random.Range(0, int.MaxValue);
			random		= new System.Random(randomSeed);

			// Debug.Log("randomSeed: " + randomSeed);

			LevelCreatorSettings.GenItem genItem = LevelCreatorSettings.Instance.genItems[index];

			targetMoves = Mathf.RoundToInt(Mathf.Lerp(genItem.minMoves, genItem.maxMoves, (float)numLevelsGenerated / (genItem.numLevels - 1)));

			levelGenerator = new LevelGenerator(LevelCreatorSettings.Instance.genItems[index], random, targetMoves, iterations);
			levelGenerator.StartWorker();
		}

		private void LevelFinishedGenerating(LevelGenerator levelGenerator)
		{
			Board board = levelGenerator.GetBestBoard();

			if (!string.IsNullOrEmpty(levelGenerator.error))
			{
				Debug.LogError("An error or exception has occured, stopping level generations:\n" + levelGenerator.error);
				StopGenerating();
				return;
			}

			if (board.movesMade.Count >= LevelCreatorSettings.Instance.genItems[genItemIndex].minMoves)
			{
				WriteBoardToLevelFile(board);

				numLevelsGenerated++;
			}
			else
			{
				iterations += 10000;
				Debug.LogWarningFormat("No viable boards generated, trying again with {0} iterations", iterations);
			}

			GenerateNextLevel();
		}

		private void WriteBoardToLevelFile(Board board)
		{
			string contents = "";

			contents += Utilities.SystemTimeInMilliseconds;
			contents += ";" + board.boardSize.ToString();

			Dictionary<string, int> blockIdToIndex = new Dictionary<string, int>();

			contents += ";" + board.blocks.Count;

			for (int i = 0; i < board.blocks.Count; i++)
			{
				Board.Block block = board.blocks[i];

				blockIdToIndex[block.blockId] = i;

				contents += string.Format(";{0};{1};{2};{3}", block.cellPositions[0].x, block.cellPositions[0].y, block.blockSize, block.isVertical);
			}

			contents += ";" + board.movesMade.Count;

			for (int i = 0; i < board.movesMade.Count; i++)
			{
				Board.Move move = board.movesMade[i];

				contents += string.Format(";{0};{1};{2}", blockIdToIndex[move.blockId], move.pos.x, move.pos.y);
			}

			LevelCreatorSettings.GenItem genItem = LevelCreatorSettings.Instance.genItems[genItemIndex];

			string outputFolder = Application.dataPath + OutputFolderAssetPath.Remove(0, "Assets".Length);

			outputFolder += string.IsNullOrEmpty(genItem.subFolder) ? "" : "/" + genItem.subFolder;

			if (!System.IO.Directory.Exists(outputFolder))
			{
				System.IO.Directory.CreateDirectory(outputFolder);
			}

			string path = GetFilePath(outputFolder, genItem.filenamePrefix);

			System.IO.File.WriteAllText(path, contents);
		}

		private string GetFilePath(string folderPath, string prefix)
		{
			if (string.IsNullOrEmpty(prefix))
			{
				prefix = "level";
			}

			int num = 0;

			while (true)
			{
				string numStr	= (num == 0) ? "" : "_" + num;
				string path		= string.Format("{0}/{1}{2}.txt", folderPath, prefix, numStr);

				if (LevelCreatorSettings.Instance.overwriteFiles || !System.IO.File.Exists(path))
				{
					return path;
				}

				num++;
			}
		}
		
		#endregion // Private Methods

	}
}
