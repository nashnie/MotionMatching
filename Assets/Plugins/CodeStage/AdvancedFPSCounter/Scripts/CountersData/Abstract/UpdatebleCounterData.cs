namespace CodeStage.AdvancedFPSCounter.CountersData
{
	using System.Collections;
	using UnityEngine;

	/// <summary>
	/// Base class for the counters which should repeatedly update at runtime.
	/// </summary>
	public abstract class UpdatableCounterData : BaseCounterData
	{
		// ----------------------------------------------------------------------------
		// protected fields
		// ----------------------------------------------------------------------------

		protected Coroutine updateCoroutine;

		// ----------------------------------------------------------------------------
		// properties exposed to the inspector
		// ----------------------------------------------------------------------------

		#region UpdateInterval
		[Tooltip("Update interval in seconds.")]
		[Range(0.1f, 10f)]
		[SerializeField]
		protected float updateInterval = 0.5f;

		/// <summary>
		/// Update interval in seconds.
		/// </summary>
		public float UpdateInterval
		{
			get { return updateInterval; }
			set
			{
				if (System.Math.Abs(updateInterval - value) < 0.001f || !Application.isPlaying) return;

				updateInterval = value;
			}
		}
		#endregion

		// ----------------------------------------------------------------------------
		// protected methods
		// ----------------------------------------------------------------------------

		protected override void PerformInitActions()
		{
			base.PerformInitActions();

			StartUpdateCoroutine();
		}

		protected override void PerformDeActivationActions()
		{
			base.PerformDeActivationActions();

			StopUpdateCoroutine();
		}

		protected abstract IEnumerator UpdateCounter();

		// ----------------------------------------------------------------------------
		// private methods
		// ----------------------------------------------------------------------------

		private void StartUpdateCoroutine()
		{
			updateCoroutine = main.StartCoroutine(UpdateCounter());
		}

		private void StopUpdateCoroutine()
		{
			main.StopCoroutine(updateCoroutine);
		}
	}
}