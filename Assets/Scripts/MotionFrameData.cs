using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MotionFrameData
{
    //RootMotion Velocity
    public float Velocity;
    public MotionBoneData[] motionBoneDataList;
    public MotionTrajectoryData[] motionTrajectoryDataList;
}
