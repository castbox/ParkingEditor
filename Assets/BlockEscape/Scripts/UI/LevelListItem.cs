using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBG.BlockEscape
{
	public class LevelListItem : RecyclableListItem<LevelData>
	{
		#region Inspector Variables

		[SerializeField] private string	levelNumberPrefix	= "LVL ";
		[SerializeField] private Text	levelNumberText		= null;
		[SerializeField] private Image	starBlankImage			= null;
		[SerializeField] private Image	starImage		= null;
		[SerializeField] private Image	lockedIcon			= null;

		#endregion

		#region Public Methods

		public override void Initialize(LevelData levelData)
		{
		}

		public override void Setup(LevelData levelData)
		{
			levelNumberText.text = levelNumberPrefix + (levelData.LevelIndex + 1).ToString();

			if (GameManager.Instance.IsLevelCompleted(levelData))
			{
				SetCompleted(GameManager.Instance.HasEarnedStar(levelData));
			}
			else if (GameManager.Instance.IsLevelLocked(levelData))
			{
				SetLocked();
			}
			else
			{
				SetPlayable();
			}
		}

		public override void Removed()
		{
		}

		#endregion

		#region Private Methods

		private void SetCompleted(bool starEarned)
		{
			starBlankImage.enabled	= !starEarned;
			starImage.enabled		= starEarned;
			lockedIcon.enabled		= false;
		}

		private void SetLocked()
		{
			starBlankImage.enabled	= false;
			starImage.enabled		= false;
			lockedIcon.enabled		= true;
		}

		private void SetPlayable()
		{
			starBlankImage.enabled	= true;
			starImage.enabled		= false;
			lockedIcon.enabled		= false;
		}

		#endregion
	}
}
