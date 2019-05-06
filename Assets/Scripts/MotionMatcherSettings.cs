using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MotionMatcherSettings", menuName = "MotionMatcher/MotionMatcherSettings")]
public class MotionMatcherSettings : ScriptableObject
{
    [Tooltip("PredictionTime")]
    public float ComputeMotionsBestCostGap = 0.1f;

    [Tooltip("EnableDebugText")]
    public bool EnableDebugText = false;

    [Tooltip("PredictionTime")]
    public float[] predictionTrajectoryTimeList = { 0, 0.1f, 0.3f, 0.5f, 0.7f, 1.0f };

    [Tooltip("CaptureBone")]
    public string[] captureBoneList = { "EthanSkeleton/EthanHips",
                                        "EthanSkeleton/EthanHips/EthanSpine/EthanLeftUpLeg", "EthanSkeleton/EthanHips/EthanSpine/EthanLeftUpLeg/EthanLeftLeg", "EthanSkeleton/EthanHips/EthanSpine/EthanLeftUpLeg/EthanLeftLeg/EthanLeftFoot",
                                        "EthanSkeleton/EthanHips/EthanSpine/EthanRightUpLeg", "EthanSkeleton/EthanHips/EthanSpine/EthanRightUpLeg/EthanRightLeg", "EthanSkeleton/EthanHips/EthanSpine/EthanRightUpLeg/EthanRightLeg/EthanRightFoot" ,

                                        "EthanSkeleton/EthanHips/EthanSpine/EthanSpine1/EthanSpine2/EthanNeck/EthanRightShoulder/EthanRightArm",
                                        "EthanSkeleton/EthanHips/EthanSpine/EthanSpine1/EthanSpine2/EthanNeck/EthanRightShoulder/EthanRightArm/EthanRightForeArm",
                                        "EthanSkeleton/EthanHips/EthanSpine/EthanSpine1/EthanSpine2/EthanNeck/EthanRightShoulder/EthanRightArm/EthanRightForeArm/EthanRightHand",

                                        "EthanSkeleton/EthanHips/EthanSpine/EthanSpine1/EthanSpine2/EthanNeck/EthanLeftShoulder/EthanLeftArm",
                                        "EthanSkeleton/EthanHips/EthanSpine/EthanSpine1/EthanSpine2/EthanNeck/EthanLeftShoulder/EthanLeftArm/EthanLeftForeArm",
                                        "EthanSkeleton/EthanHips/EthanSpine/EthanSpine1/EthanSpine2/EthanNeck/EthanLeftShoulder/EthanLeftArm/EthanLeftForeArm/EthanLeftHand",
                                        "EthanSkeleton/EthanHips/EthanSpine/EthanSpine1/EthanSpine2/EthanNeck",
                                        "EthanSkeleton/EthanHips/EthanSpine/EthanSpine1/EthanSpine2/EthanNeck/EthanHead" };

    [Tooltip("CrouchTag")]
    public string CrouchTag = "HumanoidCrouchIdle";

    [Tooltip("StandTag")]
    public string StandTag = "HumanoidIdle";
}
