using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if BBG_MT_ADS
using BBG.MobileTools;
#endif

namespace BBG.BlockEscape
{
	[RequireComponent(typeof(Button))]
	public class RewardAdButton : MonoBehaviour
	{
		#region Inspector Variables

		[SerializeField] private string		currencyId				= "";
		[SerializeField] private int		amountToReward			= 0;
		[SerializeField] private GameObject	uiContainer				= null;
		[SerializeField] private bool		testInEditor			= false;

		[Space]

		[SerializeField] private bool	showOnlyWhenCurrencyIsLow	= false;
		[SerializeField] private int	currencyShowTheshold		= 0;

		[Space]

		[SerializeField] private bool	showRewardGrantedPopup		= false;
		[SerializeField] private string	rewardGrantedPopupId		= "";

		#endregion

		#region Unity Methods

		private void Start()
		{
			uiContainer.SetActive(false);

			bool areRewardAdsEnabled = false;

			#if UNITY_EDITOR
			areRewardAdsEnabled = testInEditor;
			#endif

			#if BBG_MT_ADS
			areRewardAdsEnabled |= MobileAdsManager.Instance.AreRewardAdsEnabled;
			#endif

			if (areRewardAdsEnabled)
			{
				UpdateUI();

				#if BBG_MT_ADS
				MobileAdsManager.Instance.OnRewardAdLoaded	+= UpdateUI;
				MobileAdsManager.Instance.OnAdsRemoved		+= OnAdsRemoved;
				#endif

				CurrencyManager.Instance.OnCurrencyChanged	+= OnCurrencyChanged;

				gameObject.GetComponent<Button>().onClick.AddListener(OnClicked);
			}
		}

		#endregion

		#region Private Methods

		private void OnCurrencyChanged(string changedCurrencyId)
		{
			if (currencyId == changedCurrencyId)
			{
				UpdateUI();
			}
		}

		private void UpdateUI()
		{
			bool rewardAdLoaded = false;

			#if UNITY_EDITOR
			rewardAdLoaded = testInEditor;
			#endif

			#if BBG_MT_ADS
			rewardAdLoaded |= MobileAdsManager.Instance.RewardAdState == AdNetworkHandler.AdState.Loaded;
			#endif

			bool passShowThreshold = (!showOnlyWhenCurrencyIsLow || CurrencyManager.Instance.GetAmount(currencyId) <= currencyShowTheshold);

			uiContainer.SetActive(rewardAdLoaded && passShowThreshold);
		}

		private void OnClicked()
		{
			#if UNITY_EDITOR
			if (testInEditor)
			{
				OnRewardAdGranted();

				return;
			}
			#endif

			uiContainer.SetActive(false);

			#if BBG_MT_ADS
			MobileAdsManager.Instance.ShowRewardAd(null, OnRewardAdGranted);
			#endif
		}

		private void OnRewardAdGranted()
		{
			// Increment the currency right now
			CurrencyManager.Instance.Give(currencyId, amountToReward);

			if (showRewardGrantedPopup)
			{
				object[] popupData =
				{
					amountToReward
				};

				// Show a reward ad granted popup
				PopupManager.Instance.Show(rewardGrantedPopupId, popupData);
			}
			else
			{
				// If no reward ad granted popup will appear then update the currency text right away
				CurrencyManager.Instance.UpdateCurrencyText(currencyId);
			}
		}

		#if BBG_MT_ADS
		private void OnAdsRemoved()
		{
			MobileAdsManager.Instance.OnAdsRemoved -= OnAdsRemoved;

			// Check if we reward ads are still enabled
			if (!MobileAdsManager.Instance.AreRewardAdsEnabled)
			{
				MobileAdsManager.Instance.OnRewardAdLoaded	-= UpdateUI;
				CurrencyManager.Instance.OnCurrencyChanged	-= OnCurrencyChanged;

				uiContainer.SetActive(false);
			}
		}
		#endif

		#endregion
	}
}
