using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MotionMatcher))]
public class MotionMatchingBakeDebugEditor : Editor
{
    private MotionsData currentMotionsData;
    public string defaultMotionName = "HumanoidIdle";

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    protected virtual void OnSceneGUI()
    {
        float size = 0.1f;
        MotionMatcher motionMatcher = (MotionMatcher)target;
        currentMotionsData = motionMatcher.motionsData;
        if (currentMotionsData)
        {
            for (int i = 0; i < currentMotionsData.motionDataList.Length; i++)
            {
                MotionData motionData = currentMotionsData.motionDataList[i];
                if (motionData.motionName == motionMatcher.bestMotionName || 
                    (string.IsNullOrEmpty(motionMatcher.bestMotionName) && motionData.motionName == defaultMotionName))
                {
                    for (int j = 0; j < motionData.motionFrameDataList.Length; j++)
                    {
                        MotionFrameData motionFrameData = motionData.motionFrameDataList[j];

                        for (int k = 0; k < motionFrameData.motionBoneDataList.Length; k++)
                        {
                            MotionBoneData motionBoneData = motionFrameData.motionBoneDataList[k];
                            Handles.color = Color.green;
                            Handles.SphereHandleCap(0, motionBoneData.position, motionBoneData.rotation * Quaternion.LookRotation(Vector3.right), size, EventType.Repaint);
                        }

                        for (int l = 0; l < motionFrameData.motionTrajectoryDataList.Length; l++)
                        {
                            MotionTrajectoryData motionTrajectoryData = motionFrameData.motionTrajectoryDataList[l];
                            Handles.color = Color.red;
                            Handles.SphereHandleCap(0, motionTrajectoryData.position, Quaternion.identity, size, EventType.Repaint);
                        }
                    }
                }         
            }
        }
    }
}