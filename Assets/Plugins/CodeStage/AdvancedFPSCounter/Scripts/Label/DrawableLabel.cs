namespace CodeStage.AdvancedFPSCounter.Labels
{
	using System.Text;
	using UnityEngine;
	using UnityEngine.UI;

	internal class DrawableLabel
	{
		// ----------------------------------------------------------------------------
		// internal fields
		// ----------------------------------------------------------------------------

		internal GameObject container;
		internal LabelAnchor anchor;
		internal StringBuilder newText;
		internal bool dirty;

		// ----------------------------------------------------------------------------
		// private fields
		// ----------------------------------------------------------------------------

		private GameObject labelGameObject;
		private RectTransform labelTransform;
		private ContentSizeFitter labelFitter;
		private HorizontalLayoutGroup labelGroup;

        private GameObject uiTextGameObject;
        private Text uiText;

        private Font font;
		private int fontSize;
		private float lineSpacing;
		private Vector2 pixelOffset;

		private readonly LabelEffect background;
	    private Image backgroundImage;

		private readonly LabelEffect shadow;
		private Shadow shadowComponent;

		private readonly LabelEffect outline;
		private Outline outlineComponent;

		// ----------------------------------------------------------------------------
		// constructor
		// ----------------------------------------------------------------------------

		internal DrawableLabel(GameObject container, LabelAnchor anchor, LabelEffect background, LabelEffect shadow, LabelEffect outline, Font font, int fontSize, float lineSpacing, Vector2 pixelOffset)
		{
			this.container = container;
			this.anchor = anchor;

			this.background = background;
			this.shadow = shadow;
			this.outline = outline;
			this.font = font;
			this.fontSize = fontSize;
			this.lineSpacing = lineSpacing;
			this.pixelOffset = pixelOffset;

			NormalizeOffset();

			newText = new StringBuilder(1000);
		}

		// ----------------------------------------------------------------------------
		// internal methods
		// ----------------------------------------------------------------------------

		internal void CheckAndUpdate()
		{
			if (newText.Length > 0)
			{
				if (uiText == null)
				{
					/* create label game object and configure it */
					labelGameObject = new GameObject(anchor.ToString(), typeof(RectTransform));
				    labelTransform = labelGameObject.GetComponent<RectTransform>();
				    labelFitter = labelGameObject.AddComponent<ContentSizeFitter>();

                    labelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    labelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                    labelGroup = labelGameObject.AddComponent<HorizontalLayoutGroup>();

                    labelGameObject.layer = container.layer;
					labelGameObject.tag = container.tag;
					labelGameObject.transform.SetParent(container.transform, false);

                    uiTextGameObject = new GameObject("Text", typeof(Text));
                    uiTextGameObject.transform.SetParent(labelGameObject.transform, false);

                    /* create UI Text component and apply settings */
                    uiText = uiTextGameObject.GetComponent<Text>();

					uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
					uiText.verticalOverflow = VerticalWrapMode.Overflow;

					ApplyShadow();
					ApplyOutline();
					ApplyFont();
                    uiText.fontSize = fontSize;
					uiText.lineSpacing = lineSpacing;

					UpdateTextPosition();
                    ApplyBackground();
                }

				if (dirty)
				{
					uiText.text = newText.ToString();
                    ApplyBackground();
                    dirty = false;
				}
				newText.Length = 0;
			}
			else if (uiText != null)
			{
                Clear();
			}
		}

		internal void Clear()
		{
			newText.Length = 0;
			if (labelGameObject != null)
			{
				Object.Destroy(labelGameObject);
                labelGameObject = null;
                labelTransform = null;
				uiText = null;
			}

		    if (backgroundImage != null)
		    {
                Object.Destroy(backgroundImage);
		        backgroundImage = null;
		    }
		}

		internal void Destroy()
		{
			Clear();
			newText = null;
		}

		internal void ChangeFont(Font labelsFont)
		{
			font = labelsFont;
			ApplyFont();

            ApplyBackground();
        }

		internal void ChangeFontSize(int newSize)
		{
			fontSize = newSize;
			if (uiText != null) uiText.fontSize = fontSize;

            ApplyBackground();
        }

		internal void ChangeOffset(Vector2 newPixelOffset)
		{
			pixelOffset = newPixelOffset;
			NormalizeOffset();

			if (labelTransform != null)
			{
				labelTransform.anchoredPosition = pixelOffset;
			}

            ApplyBackground();
        }

		internal void ChangeLineSpacing(float newValueLineSpacing)
		{
			lineSpacing = newValueLineSpacing;

			if (uiText != null)
			{
				uiText.lineSpacing = newValueLineSpacing;
			}

            ApplyBackground();
        }

		internal void ChangeBackground(bool enabled)
		{
			background.enabled = enabled;
			ApplyBackground();
		}

		internal void ChangeBackgroundColor(Color color)
		{
			background.color = color;
			ApplyBackground();
		}

        public void ChangeBackgroundPadding(int backgroundPadding)
        {
            background.padding = backgroundPadding;
            ApplyBackground();
        }

        internal void ChangeShadow(bool enabled)
		{
			shadow.enabled = enabled;
			ApplyShadow();
		}

		internal void ChangeShadowColor(Color color)
		{
			shadow.color = color;
			ApplyShadow();
		}

		internal void ChangeShadowDistance(Vector2 distance)
		{
			shadow.distance = distance;
			ApplyShadow();
		}

		internal void ChangeOutline(bool enabled)
		{
			outline.enabled = enabled;
			ApplyOutline();
		}

		internal void ChangeOutlineColor(Color color)
		{
			outline.color = color;
			ApplyOutline();
		}

		internal void ChangeOutlineDistance(Vector2 distance)
		{
			outline.distance = distance;
			ApplyOutline();
		}

		// ----------------------------------------------------------------------------
		// private methods
		// ----------------------------------------------------------------------------

		private void UpdateTextPosition()
		{
            labelTransform.localRotation = Quaternion.identity;
            labelTransform.sizeDelta = Vector2.zero;
            labelTransform.anchoredPosition = pixelOffset;

			switch (anchor)
			{
				case LabelAnchor.UpperLeft:
					uiText.alignment = TextAnchor.UpperLeft;
					labelTransform.anchorMin = Vector2.up;
                    labelTransform.anchorMax = Vector2.up;
                    labelTransform.pivot = new Vector2(0,1);
                    break;
				case LabelAnchor.UpperRight:
					uiText.alignment = TextAnchor.UpperRight;
					labelTransform.anchorMin = Vector2.one;
                    labelTransform.anchorMax = Vector2.one;
                    labelTransform.pivot = new Vector2(1, 1);
                    break;
				case LabelAnchor.LowerLeft:
					uiText.alignment = TextAnchor.LowerLeft;
					labelTransform.anchorMin = Vector2.zero;
                    labelTransform.anchorMax = Vector2.zero;
                    labelTransform.pivot = new Vector2(0, 0);
                    break;
				case LabelAnchor.LowerRight:
					uiText.alignment = TextAnchor.LowerRight;
					labelTransform.anchorMin = Vector2.right;
                    labelTransform.anchorMax = Vector2.right;
                    labelTransform.pivot = new Vector2(1, 0);
                    break;
				case LabelAnchor.UpperCenter:
					uiText.alignment = TextAnchor.UpperCenter;
					labelTransform.anchorMin = new Vector2(0.5f, 1f);
                    labelTransform.anchorMax = new Vector2(0.5f, 1f);
                    labelTransform.pivot = new Vector2(0.5f, 1);
                    break;
				case LabelAnchor.LowerCenter:
					uiText.alignment = TextAnchor.LowerCenter;
					labelTransform.anchorMin = new Vector2(0.5f, 0f);
                    labelTransform.anchorMax = new Vector2(0.5f, 0f);
                    labelTransform.pivot = new Vector2(0.5f, 0);
                    break;
				default:
					Debug.LogWarning(AFPSCounter.LogPrefix + "Unknown label anchor!", uiText);
					uiText.alignment = TextAnchor.UpperLeft;
					labelTransform.anchorMin = Vector2.up;
                    labelTransform.anchorMax = Vector2.up;
					break;
			}
		}

		private void NormalizeOffset()
		{
			switch (anchor)
			{
				case LabelAnchor.UpperLeft:
					pixelOffset.y = -pixelOffset.y;
					break;
				case LabelAnchor.UpperRight:
					pixelOffset.x = -pixelOffset.x;
					pixelOffset.y = -pixelOffset.y;
					break;
				case LabelAnchor.LowerLeft:
					// it's fine
					break;
				case LabelAnchor.LowerRight:
					pixelOffset.x = -pixelOffset.x;
					break;
				case LabelAnchor.UpperCenter:
					pixelOffset.y = -pixelOffset.y;
					pixelOffset.x = 0;
					break;
				case LabelAnchor.LowerCenter:
					pixelOffset.x = 0;
					break;
				default:
					pixelOffset.y = -pixelOffset.y;
					break;
			}
		}

		private void ApplyBackground()
		{
		    if (uiText == null) return;

		    if (background.enabled && !backgroundImage)
		    {
                backgroundImage = labelGameObject.AddComponent<Image>();
            }

            if (!background.enabled && backgroundImage)
            {
                Object.Destroy(backgroundImage);
                backgroundImage = null;
            }

            if (backgroundImage != null)
            {
                if (backgroundImage.color != background.color)
                {
                    backgroundImage.color = background.color;
                }

                if (labelGroup.padding.bottom != background.padding)
                {
                    labelGroup.padding.top = background.padding;
                    labelGroup.padding.left = background.padding;
                    labelGroup.padding.right = background.padding;
                    labelGroup.padding.bottom = background.padding;

                    labelGroup.SetLayoutHorizontal();
                }
            }
        }

		private void ApplyShadow()
		{
			if (uiText == null) return;

			if (shadow.enabled && !shadowComponent)
			{
				shadowComponent = uiTextGameObject.AddComponent<Shadow>();
			}

			if (!shadow.enabled && shadowComponent)
			{
				Object.Destroy(shadowComponent);
			}

			if (shadowComponent != null)
			{
				shadowComponent.effectColor = shadow.color;
				shadowComponent.effectDistance = shadow.distance;
			}
		}

		private void ApplyOutline()
		{
			if (uiText == null) return;

			if (outline.enabled && !outlineComponent)
			{
				outlineComponent = uiTextGameObject.AddComponent<Outline>();
			}

			if (!outline.enabled && outlineComponent)
			{
				Object.Destroy(outlineComponent);
			}

			if (outlineComponent != null)
			{
				outlineComponent.effectColor = outline.color;
				outlineComponent.effectDistance = outline.distance;
			}
		}

		private void ApplyFont()
		{
			if (uiText == null) return;

			if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			uiText.font = font;
		}
	}
}