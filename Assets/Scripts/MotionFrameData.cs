using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MotionFrameData
{
    //RootMotion Velocity
    public int Velocity;
    public MotionBoneData[] motionBoneDataList;
    public MotionTrajectoryData[] motionTrajectoryDataList;
}
