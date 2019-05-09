using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MotionFrameData
{
    //RootMotion Velocity
    public float velocity;
    public float angularVelocity;
    public MotionBoneData[] motionBoneDataList;
    public MotionTrajectoryData[] motionTrajectoryDataList;

    public void Clear()
    {
        velocity = 0;
        motionBoneDataList = null;
        motionTrajectoryDataList = null;
    }
}
