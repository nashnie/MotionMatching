using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotionDebugData
{
    public int bestMotionFrameIndex = -1;
    public string bestMotionName = "HumanoidIdle";

    public float bonePosCost;
    public float boneRotCost;
    public float bonesCost = 0f;

    public float trajectoryPosCost;
    public float trajectoryVelCost;
    public float trajectoryDirCost;
    public float trajectorysCost = 0f;

    public float rootMotionCost = 0f;

    public float motionCost = 0f;
}
