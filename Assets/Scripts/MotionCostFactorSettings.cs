using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MotionCostFactorSettings", menuName = "MotionMatcher/MotionCostFactorSettings")]
public class MotionCostFactorSettings : ScriptableObject
{
    [Tooltip("")]
    public float bonePosFactor = 1.0f;

    [Tooltip("")]
    public float boneRotFactor = 1.0f;

    [Tooltip("")]
    public float boneVelFactor = 1.0f;

    [Tooltip("")]
    public float rootMotionVelFactor = 1.5f;

    [Tooltip("")]
    public float predictionTrajectoryPosFactor = 2.0f;

    [Tooltip("")]
    public float predictionTrajectoryVelFactor = 2.0f;

    [Tooltip("")]
    public float predictionTrajectoryDirFactor = 1.0f;
}
