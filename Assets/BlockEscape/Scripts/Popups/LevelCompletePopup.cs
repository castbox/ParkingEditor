using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBG.BlockEscape
{
	public class LevelCompletePopup : Popup
	{
		#region Inspector Variables

		[Space]
		[SerializeField] private GameObject					coinRewardObject		= null;
		[SerializeField] private GameObject					nextLevelButton			= null;
		[SerializeField] private GameObject					backToMenuButton		= null;
		[Space]
		[SerializeField] private Image						starImage				= null;
		[SerializeField] private GameObject					starMessageContainer	= null;
		[SerializeField] private Text						starMessageText			= null;
		[SerializeField] private AnimationCurve				starEarnedAnimCurve		= null;
		[Space]
		[SerializeField] private ProgressBar				rewardProgressBar		= null;
		[SerializeField] private Text						rewardProgressText		= null;
		[SerializeField] private CanvasGroup				rewardCoinContainer		= null;
		[SerializeField] private Text						rewardCoinAmountText	= null;
		[SerializeField] private CoinAnimationController	coinAnimationController	= null;

		#endregion

		#region Member Variables

		private const float StarEarnedAnimDuration		= 0.75f;
		private const float RewardProgressAnimDuration	= 0.5f;

		private string originalMessageText;

		#endregion

		#region Public Methods

		public override void Initialize()
		{
			base.Initialize();

			originalMessageText = starMessageText.text;
		}

		public override void OnShowing(object[] inData)
		{
			base.OnShowing(inData);

			int index = 0;

			bool	firstTimeCompleting	= (bool)inData[index++];
			bool	starAlreadyEarned	= (bool)inData[index++];
			bool	awardStar			= (bool)inData[index++];
			bool	isLastLevel			= (bool)inData[index++];
			int		fromRewardProgress	= (int)inData[index++];
			int		toRewardProgress	= (int)inData[index++];
			int		numLevelsForReward	= (int)inData[index++];
			int		numCoinsRewarded	= (int)inData[index++];
			int		numMovesForStar		= (int)inData[index++];

			coinAnimationController.ResetUI();
			coinRewardObject.SetActive(true);
			rewardCoinContainer.alpha = 1f;

			nextLevelButton.SetActive(!isLastLevel);
			backToMenuButton.SetActive(isLastLevel);

			rewardCoinAmountText.text	= "x" + numCoinsRewarded;
			rewardProgressText.text		= string.Format("{0} / {1}", toRewardProgress, numLevelsForReward);

			starMessageContainer.SetActive(!awardStar && !starAlreadyEarned);
			starImage.enabled = awardStar || starAlreadyEarned;

			float startDelay = animDuration + 0.25f;

			// If the star was awarded animat the star in
			if (awardStar)
			{
				PlayStarEarnedAnimation(startDelay);

				startDelay += StarEarnedAnimDuration + 0.25f;
			}
			// If the star was already earned, show the star
			else if (starAlreadyEarned)
			{
				starImage.color					= new Color(starImage.color.r, starImage.color.g, starImage.color.b, 1f);
				starImage.transform.localScale	= Vector3.one;

				rewardProgressBar.SetProgress((float)fromRewardProgress / (float)numLevelsForReward);
			}
			// Else show the play what they need to do to earn the star
			else
			{
				starMessageText.text = string.Format(originalMessageText, numMovesForStar);
			}

			if (firstTimeCompleting)
			{
				float fromProgress	= (float)fromRewardProgress / (float)numLevelsForReward;
				float toProgress	= (float)toRewardProgress / (float)numLevelsForReward;

				rewardProgressBar.SetProgressAnimated(fromProgress, toProgress, RewardProgressAnimDuration, startDelay);

				if (toRewardProgress == numLevelsForReward)
				{
					// Don't allow the player to exit the popup until the coin reward animation has finished
					SetPopupInteractable(false);

					startDelay += RewardProgressAnimDuration + 0.25f;
					
					// Play the coin animations after the progress bar has finished aniamting
					StartCoroutine(PlayCoinsAwardedAnimation(startDelay, numCoinsRewarded));
				}
			}
		}

		public override void OnHiding(bool cancelled)
		{
			base.OnHiding(cancelled);

			coinAnimationController.StopAllCoroutines();

			StopAllCoroutines();
		}

		#endregion

		#region Private Methods

		private void PlayStarEarnedAnimation(float startDelay)
		{
			UIAnimation anim;

			anim					= UIAnimation.ScaleX(starImage.transform as RectTransform, 2f, 1f, StarEarnedAnimDuration);
			anim.style				= UIAnimation.Style.Custom;
			anim.startOnFirstFrame	= true;
			anim.startDelay			= startDelay;
			anim.animationCurve		= starEarnedAnimCurve;
			anim.Play();

			anim					= UIAnimation.ScaleY(starImage.transform as RectTransform, 2f, 1f, StarEarnedAnimDuration);
			anim.style				= UIAnimation.Style.Custom;
			anim.startOnFirstFrame	= true;
			anim.startDelay			= startDelay;
			anim.animationCurve		= starEarnedAnimCurve;
			anim.Play();

			Color fromColor	= new Color(starImage.color.r, starImage.color.g, starImage.color.b, 0f);
			Color toColor	= new Color(starImage.color.r, starImage.color.g, starImage.color.b, 1f);

			anim					= UIAnimation.Color(starImage, fromColor, toColor, StarEarnedAnimDuration);
			anim.startOnFirstFrame	= true;
			anim.startDelay			= startDelay;
			anim.style				= UIAnimation.Style.EaseIn;
			anim.Play();
		}

		private IEnumerator PlayCoinsAwardedAnimation(float startDelay, int amountOfCoins)
		{
			// Wait before starting the coin animations
			yield return new WaitForSeconds(startDelay);

			UIAnimation.Alpha(rewardCoinContainer, 0f, 1f).Play();

			coinRewardObject.SetActive(false);

			coinAnimationController.Play(coinRewardObject, amountOfCoins, (int coin, int numCoins) => 
			{
				if (coin == numCoins)
				{
					SetPopupInteractable(true);
				}
			});
		}

		private void SetPopupInteractable(bool interactable)
		{
			CG.interactable		= interactable;
			CG.blocksRaycasts	= interactable;
		}

		#endregion
	}
}
