#if UNITY_EDITOR
namespace CodeStage.AdvancedFPSCounter
{
	using Editor.UI;
	using Utils;

	using UnityEditor;
	using UnityEngine;

	[CustomEditor(typeof(AFPSRenderRecorder))]
	[CanEditMultipleObjects()]
	public class AFPSRenderRecorderEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			EditorUIUtils.SetupStyles();
			GUILayout.Label("This component is used by <b>Advanced FPS Counter</b> to measure camera <b>Render Time</b>.", EditorUIUtils.richMiniLabel);
		}
	}
}
#endif