using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BBG.BlockEscape
{
	public class Block : MonoBehaviour
	{
		#region Inspector Variables
		
		[SerializeField] private Image		bkgImage		= null;
		[SerializeField] private Transform	hintDirArrow	= null;
		
		#endregion // Inspector Variables

		#region Properties
		
		public RectTransform RectT { get { return transform as RectTransform; } }
		
		#endregion // Properties

		#region Public Methods
		
		public void Setup(Color color, bool flipBkg)
		{
			bkgImage.color = color;

			float width		= RectT.rect.width;
			float height	= RectT.rect.height;

			bkgImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			bkgImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

			if (flipBkg)
			{
				bkgImage.rectTransform.sizeDelta	= new Vector2(height, width);
				bkgImage.transform.localEulerAngles	= new Vector3(0, 0, 90);
			}
			else
			{
				bkgImage.rectTransform.sizeDelta	= new Vector2(width, height);
				bkgImage.transform.localEulerAngles	= new Vector3(0, 0, 0);
			}

			if (hintDirArrow != null)
			{
				hintDirArrow.gameObject.SetActive(false);
			}

			HideHintArrow();
		}

		public void ShowHintArrow(int arrowDirection)
		{
			hintDirArrow.gameObject.SetActive(true);
			hintDirArrow.localEulerAngles = new Vector3(0, 0, arrowDirection * 90);
		}

		public void HideHintArrow()
		{
			if (hintDirArrow != null)
			{
				hintDirArrow.gameObject.SetActive(false);
			}
		}
		
		#endregion // Public Methods
	}
}
