namespace CodeStage.AdvancedFPSCounter.Utils
{
	using UnityEngine;

	/// <summary>
	/// This is a helper class for the %AFPSCounter Render Time feature.
	/// </summary>
	/// It should be attached to the Camera to measure the approximate render time and report it to the current %AFPSCounter instance.
	/// <br/>You may use \link CodeStage.AdvancedFPSCounter.CountersData.FPSCounterData::RenderAutoAdd FPSCounterData.RenderAutoAdd \endlink 
	/// property to let %AFPSCounter add it automatically to the Camera with Main Camera tag.
	/// <br/>You also may add it by hand to all cameras you wish to measure.
	/// <br/><strong>\htmlonly<font color="7030A0">NOTE:</font>\endhtmlonly It doesn't take into account Image Effects and IMGUI!</strong>
	/// \sa \link CodeStage.AdvancedFPSCounter.CountersData.FPSCounterData::Render FPSCounterData.Render \endlink
	/// \sa \link CodeStage.AdvancedFPSCounter.CountersData.FPSCounterData::RenderAutoAdd FPSCounterData.RenderAutoAdd \endlink
	[DisallowMultipleComponent]
	public class AFPSRenderRecorder : MonoBehaviour
	{
		private bool recording;
		private float renderTime;

#if UNITY_EDITOR
		private bool editorWarningFired = false;

		private void OnValidate()
		{
			var cam = GetComponent<Camera>();
			if (cam == null)
			{
				Debug.LogError(AFPSCounter.LogPrefix + "Look like this AFPSRenderRecorder instance is added to the Game Object without Camera! It will not work.", this);
			}
		}
#endif

		private void OnPreCull()
		{
			if (recording || AFPSCounter.Instance == null) return;

#if UNITY_EDITOR
			if (!editorWarningFired && !AFPSCounter.Instance.fpsCounter.Render)
			{
				Debug.LogWarning(AFPSCounter.LogPrefix + "You have this AFPSRenderRecorder instance running, " +
				                 "but Render is disabled at the AFPSCounter.\n" +
				                 "It's a waste of resources. " +
				                 "Consider removing this instance or enable Render counter at the AFPSCounter.", this);
				editorWarningFired = true;
			}
#endif

			recording = true;
			renderTime = Time.realtimeSinceStartup;
		}

		private void OnPostRender()
		{
			if (!recording || AFPSCounter.Instance == null) return;

			recording = false;
			renderTime = Time.realtimeSinceStartup - renderTime;

			AFPSCounter.Instance.fpsCounter.AddRenderTime(renderTime * 1000f);
		}
	}
}