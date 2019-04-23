namespace CodeStage.AdvancedFPSCounter
{
	/// <summary>
	/// Enumeration of possible AFPSCounter.OperationMode values.
	/// </summary>
	public enum OperationMode: byte
	{
		/// <summary>
		/// Removes all counters and stops all internal processes except the global hotkey listening.
		/// </summary>
		Disabled,

		/// <summary>
		/// Allows to read all enabled counters data keeping them hidden and avoiding any additional 
		/// resources usage for assembling and showing counters values on screen.
		/// </summary>
		Background,

		/// <summary>
		/// Allows to see counters on screen and runs all internal processes as intended.
		/// </summary>
		Normal
	}
}