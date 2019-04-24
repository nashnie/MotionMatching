using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MotionMatcherSettings", menuName = "MotionMatcher/MotionMatcherSettings")]
public class MotionMatcherSettings : ScriptableObject
{
    [Tooltip("PredictionTime")]
    public float[] PredictionTrajectoryTime = { 0, 0.1f, 0.3f, 0.5f, 0.7f, 1.0f };
}
