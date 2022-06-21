using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBG.BlockEscape
{
	public class GameScreen : Screen
	{
		#region Inspector Variables

		[Space]

		[SerializeField] private GameArea	gameArea		= null;
		[SerializeField] private Text		hintCostText	= null;

		#endregion // Inspector Variables

		#region Public Methods
		
		public override void Initialize()
		{
			base.Initialize();

			hintCostText.text = GameManager.Instance.HintCoinCost.ToString();

			gameArea.Initialize();
			gameArea.OnLevelCompleted += OnLevelCompleted;

			GameEventManager.Instance.RegisterEventHandler(GameEventManager.LevelStartedEventId, OnLevelStarted);
		}

		/// <summary>
		/// Invoked when the Hint button on the GameScreen is clicked
		/// </summary>
		public void OnHintClicked()
		{
			// Try and spend a hint/coins for the hint
			if (GameManager.Instance.TryUseHint())
			{
				// Currency has been spend, display the hint
				gameArea.DisplayHint();
			}
		}
		
		#endregion // Public Methods

		#region Private Methods
		
		private void OnLevelStarted(string eventId, object[] data)
		{
			SetupGameArea();
		}

		private void OnLevelCompleted()
		{
			GameEventManager.Instance.SendEvent(GameEventManager.ActiveLevelCompletedEventId);
		}

		private void SetupGameArea()
		{
			LevelData		activeLevelData		= GameManager.Instance.ActiveLevelData;
			LevelSaveData	activeLevelSaveData	= GameManager.Instance.ActiveLevelSaveData;

			if (activeLevelData != null && activeLevelSaveData != null)
			{
				gameArea.SetupLevel(activeLevelData, activeLevelSaveData);
			}
		}
		
		#endregion // Private Methods
	}
}
