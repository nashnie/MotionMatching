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
            Transform child = motionMatcher.transform.Find(MotionMatchingBakeEditor.RootBoneName);
            Transform[] joints = child.GetComponentsInChildren<Transform>();

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
                            Transform bone = joints[motionBoneData.boneIndex];
                            if (bone)
                            {
                                Vector3 bonePosition = motionBoneData.position;
                                Handles.color = Color.green;
                                Handles.SphereHandleCap(0, bonePosition, Quaternion.identity, size, EventType.Repaint);
                            }
                        }

                        for (int l = 0; l < motionFrameData.motionTrajectoryDataList.Length; l++)
                        {
                            MotionTrajectoryData motionTrajectoryData = motionFrameData.motionTrajectoryDataList[l];
                            Handles.color = Color.yellow;
                            Handles.SphereHandleCap(0, motionTrajectoryData.position, Quaternion.identity, size, EventType.Repaint);
                        }
                    }
                }         
            }
        }
    }
}