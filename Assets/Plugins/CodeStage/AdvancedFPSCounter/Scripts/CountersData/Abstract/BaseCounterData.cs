namespace CodeStage.AdvancedFPSCounter.CountersData
{
	using System.Text;

	using Labels;

	using UnityEngine;

	/// <summary>
	/// Base class for all counters.
	/// </summary>
	[AddComponentMenu("")]
	[System.Serializable]
	public abstract class BaseCounterData
	{
        protected const string BoldStart = "<b>";
        protected const string BoldEnd = "</b>";
        protected const string ItalicStart = "<i>";
        protected const string ItalicEnd = "</i>";

		// ----------------------------------------------------------------------------
		// internal fields
		// ----------------------------------------------------------------------------

		internal StringBuilder text;
		internal bool dirty;

		// ----------------------------------------------------------------------------
		// protected fields
		// ----------------------------------------------------------------------------

		[System.NonSerialized]
		protected AFPSCounter main;
		protected string colorCached;
		protected bool inited;

		// ----------------------------------------------------------------------------
		// properties
		// ----------------------------------------------------------------------------

		#region Enabled
		[SerializeField]
		protected bool enabled = true;

		/// <summary>
		/// Enables or disables counter with immediate label refresh.
		/// </summary>
		public bool Enabled
		{
			get { return enabled; }
			set
			{
				if (enabled == value || !Application.isPlaying) return;

				enabled = value;

				if (enabled)
				{
					Activate();
				}
				else
				{
					Deactivate();
				}
				main.UpdateTexts();
			}
		}
		#endregion

		#region Anchor
		[Tooltip("Current anchoring label for the counter output.\n" +
		         "Refreshes both previous and specified label when switching anchor.")]
		[SerializeField]
		protected LabelAnchor anchor = LabelAnchor.UpperLeft;

		/// <summary>
		/// Current anchoring label for the counter output. Refreshes both previous and specified label when switching anchor.
		/// </summary>
		public LabelAnchor Anchor
		{
			get
			{
				return anchor;
			}
			set
			{
				if (anchor == value || !Application.isPlaying) return;
				var previousAnchor = anchor;
				anchor = value;
				if (!enabled) return;

				dirty = true;
				main.MakeDrawableLabelDirty(previousAnchor);
				main.UpdateTexts();
			}
		}
		#endregion

		#region Color
		[Tooltip("Regular color of the counter output.")]
		[SerializeField]
		protected Color color;

		/// <summary>
		/// Regular color of the counter output.
		/// </summary>
		public Color Color
		{
			get { return color; }
			set
			{
				if (color == value || !Application.isPlaying) return;
				color = value;
				if (!enabled) return;

				CacheCurrentColor();

				Refresh();
			}
		}
        #endregion

        #region Style
        [Tooltip("Controls text style.")]
        [SerializeField]
        protected FontStyle style = FontStyle.Normal;

        /// <summary>
        /// Controls bold text style.
        /// </summary>
        public FontStyle Style
        {
            get { return style; }
            set
            {
                if (style == value || !Application.isPlaying) return;
                style = value;
                if (!enabled) return;

                Refresh();
            }
        }
		#endregion

		#region ExtraText
		[Tooltip("Additional text to append to the end of the counter in normal Operation Mode.")]
		protected string extraText;

		/// <summary>
		/// Additional text to append to the end of the counter in normal Operation Mode.
		/// <br/>Refresh() will be called on change.
		/// <br/>Set to null to remove extra text.
		/// </summary>
		public string ExtraText
		{
			get { return extraText; }
			set
			{
				if (extraText == value || !Application.isPlaying) return;
				extraText = value;
				if (!enabled) return;
				Refresh();
			}
		}
		#endregion

		// ----------------------------------------------------------------------------
		// public methods
		// ----------------------------------------------------------------------------

		/// <summary>
		/// Updates counter's value and forces current label refresh.
		/// </summary>
		public void Refresh()
		{
			if (!enabled || !Application.isPlaying) return;
			UpdateValue(true);
			main.UpdateTexts();
		}

		// ----------------------------------------------------------------------------
		// internal methods
		// ----------------------------------------------------------------------------

		internal virtual void UpdateValue()
		{
			UpdateValue(false);
		}

		internal abstract void UpdateValue(bool force);

		internal void Init(AFPSCounter reference)
		{
			main = reference;
		}

		internal void Destroy()
		{
			main = null;

			if (text != null)
			{
				text.Remove(0, text.Length);
				text = null;
			}
		}

		internal virtual void Activate()
		{
			if (!enabled) return;
			if (main.OperationMode == OperationMode.Disabled) return;
			if (!HasData()) return;

			if (text == null)
			{
				text = new StringBuilder(500);
			}
			else
			{
				text.Length = 0;
			}

			if (main.OperationMode == OperationMode.Normal)
			{
				if (colorCached == null)
				{
					CacheCurrentColor();
				}
			}

			PerformActivationActions();

			if (!inited)
			{
				PerformInitActions();
				inited = true;
			}

			UpdateValue();
		}

		internal virtual void Deactivate()
		{
			if (!inited) return;

			if (text != null)
			{
				text.Remove(0, text.Length);
			}
			main.MakeDrawableLabelDirty(anchor);

			PerformDeActivationActions();

			inited = false;
		}

		// ----------------------------------------------------------------------------
		// protected methods
		// ----------------------------------------------------------------------------

		protected virtual void PerformInitActions() { }
		protected virtual void PerformActivationActions() {}
		protected virtual void PerformDeActivationActions() {}

		protected abstract bool HasData();
		// we have to cache color HTML tag to avoid extra allocations
		protected abstract void CacheCurrentColor();

        protected void ApplyTextStyles()
        {
            if (text.Length > 0)
            {
                switch (style)
                {
                    case FontStyle.Normal:
                        break;
                    case FontStyle.Bold:
                        text.Insert(0, BoldStart);
                        text.Append(BoldEnd);
                        break;
                    case FontStyle.Italic:
                        text.Insert(0, ItalicStart);
                        text.Append(ItalicEnd);
                        break;
                    case FontStyle.BoldAndItalic:
						text.Insert(0, BoldStart);
						text.Append(BoldEnd);
						text.Insert(0, ItalicStart);
						text.Append(ItalicEnd);
						break;
                    default:
                        throw new System.ArgumentOutOfRangeException();
                }
            }

			if (!string.IsNullOrEmpty(extraText))
			{
				text.Append(AFPSCounter.NewLine).Append(extraText);
			}
		}
    }
}