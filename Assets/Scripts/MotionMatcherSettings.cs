using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MotionMatcherSettings", menuName = "MotionMatcher/MotionMatcherSettings")]
public class MotionMatcherSettings : ScriptableObject
{
    [Tooltip("PredictionTime")]
    public float[] predictionTrajectoryTimeList = { 0, 0.1f, 0.3f, 0.5f, 0.7f, 1.0f };

    [Tooltip("CaptureBone")]
    public string[] captureBoneList = { "EthanHips",
                                        "EthanLeftUpLeg", "EthanLeftLeg", "EthanLeftFoot",
                                        "EthanRightUpLeg", "EthanRightLeg", "EthanRightFoot" ,
                                        "EthanRightArm", "EthanRightForeArm", "EthanRightHand",
                                        "EthanLeftArm", "EthanLeftForeArm", "EthanLeftHand",
                                        "EthanNeck", "EthanHead" };
}
