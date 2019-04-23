namespace CodeStage.AdvancedFPSCounter.Labels
{
	/// <summary>
	/// Anchor, used to stick counters labels to the screen corners.
	/// </summary>
	public enum LabelAnchor: byte
	{
		/// <summary>
		/// Upper left screen corner. Text will be left-aligned.
		/// </summary>
		UpperLeft,

		/// <summary>
		/// Upper right screen corner. Text will be right-aligned.
		/// </summary>
		UpperRight,

		/// <summary>
		/// Lower left screen corner. Text will be left-aligned.
		/// </summary>
		LowerLeft,

		/// <summary>
		/// Lower right screen corner. Text will be right-aligned.
		/// </summary>
		LowerRight,

		/// <summary>
		/// Upper center of the screen. Text will be centered.
		/// </summary>
		UpperCenter,

		/// <summary>
		/// Lower center of the screen. Text will be centered.
		/// </summary>
		LowerCenter
	}
}