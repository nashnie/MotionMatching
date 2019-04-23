namespace CodeStage.AdvancedFPSCounter
{
	/// <summary>
	/// Enumeration of possible \link CodeStage.AdvancedFPSCounter.CountersData.FPSCounterData.CurrentFpsLevel FPSCounterData.CurrentFpsLevel\endlink values.
	/// </summary>
	public enum FPSLevel: byte
	{
		/// <summary>
		/// FPS value is normal.
		/// </summary>
		Normal = 0,

		/// <summary>
		/// FPS value below \link CodeStage.AdvancedFPSCounter.CountersData.FPSCounterData.warningLevelValue FPSCounterData.warningLevelValue\endlink.
		/// </summary>
		Warning = 1,

		/// <summary>
		/// FPS value below or equal to \link CodeStage.AdvancedFPSCounter.CountersData.FPSCounterData.criticalLevelValue FPSCounterData.criticalLevelValue\endlink.
		/// </summary>
		Critical = 2
	}
}