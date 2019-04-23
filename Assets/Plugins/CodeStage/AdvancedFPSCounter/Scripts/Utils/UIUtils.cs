namespace CodeStage.AdvancedFPSCounter.Utils
{
	using UnityEngine;

	internal class UIUtils
	{
		internal static void ResetRectTransform(RectTransform rectTransform)
		{
			rectTransform.localRotation = Quaternion.identity;
			rectTransform.localScale = Vector3.one;
			rectTransform.pivot = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.anchoredPosition3D = Vector3.zero;
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;
		}
	}
}