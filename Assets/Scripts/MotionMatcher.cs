using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Nash
/// </summary>
public class MotionMatcher : MonoBehaviour
{
    public MotionsData motionsData;
    public MotionMatcherSettings motionMatcherSettings;
    public MotionCostFactorSettings motionCostFactorSettings;

    private MotionFrameData currentMotionFrameData;
    private Transform motionOwner;

    public int bestMotionFrameIndex = -1;
    public string bestMotionName = "HumanoidIdle";

    private float currentComputeTime;

    void Start()
    {
        currentMotionFrameData = new MotionFrameData();
        motionOwner = this.transform;
        currentComputeTime = float.MaxValue;
    }

    private void Update()
    {
        currentComputeTime += Time.deltaTime;
    }

    public string AcquireMatchedMotion(float velocity, Vector3 direction, float acceleration, float brake)
    {
        return bestMotionName;

        if (currentComputeTime >= motionMatcherSettings.ComputeMotionsBestCostGap)
        {
            currentComputeTime = 0;
            CapturePlayingMotionSnapShot(velocity, direction, acceleration, brake);
            ComputeMotionsBestCost();
            Debug.LogFormat("AcquireMatchedMotion velocity {0} bestMotionName {1} ", velocity, bestMotionName);
        }

        return bestMotionName;
    }

    private void CapturePlayingMotionSnapShot(float velocity, Vector3 direction, float acceleration, float brake)
    {
        currentMotionFrameData.Clear();
        currentMotionFrameData.velocity = velocity;
        currentMotionFrameData.motionTrajectoryDataList = new MotionTrajectoryData[motionMatcherSettings.predictionTrajectoryTimeList.Length];
        float LastPredictionTrajectoryTime = 0;
        for (int i = 0; i < motionMatcherSettings.predictionTrajectoryTimeList.Length; i++)
        {
            float predictionTrajectoryTime = motionMatcherSettings.predictionTrajectoryTimeList[i];
            float deltaTime = predictionTrajectoryTime - LastPredictionTrajectoryTime;
            currentMotionFrameData.motionTrajectoryDataList[i] = new MotionTrajectoryData();
            MotionTrajectoryData motionTrajectoryData = currentMotionFrameData.motionTrajectoryDataList[i];
            motionTrajectoryData.position = velocity * direction * deltaTime;
            motionTrajectoryData.velocity = velocity * direction;
            motionTrajectoryData.direction = direction;
            LastPredictionTrajectoryTime = predictionTrajectoryTime;
        }

        currentMotionFrameData.motionBoneDataList = new MotionBoneData[motionMatcherSettings.captureBoneList.Length];
        for (int j = 0; j < motionMatcherSettings.captureBoneList.Length; j++)
        {
            string captureBone = motionMatcherSettings.captureBoneList[j];
            Transform bone = motionOwner.Find(captureBone);
            currentMotionFrameData.motionBoneDataList[j] = new MotionBoneData();
            MotionBoneData motionBoneData = currentMotionFrameData.motionBoneDataList[j];
            motionBoneData.position = bone.position;
            motionBoneData.rotation = bone.rotation;
            //motionBoneData.velocity = velocity;
        }
    }

    private void ComputeMotionsBestCost()
    {
        float bestMotionCost = float.MaxValue;
        bestMotionName = "";
        bestMotionFrameIndex = 0;
        for (int i = 0; i < motionsData.motionDataList.Length; i++)
        {
            MotionData motionData = motionsData.motionDataList[i];
            for (int j = 0; j < motionData.motionFrameDataList.Length; j++)
            {
                float motionCost = 0f;
                float bonesCost = 0f;
                float trajectorysCost = 0f;
                float rootMotionCost = 0f;

                MotionFrameData motionFrameData = motionData.motionFrameDataList[j];
                for (int k = 0; k < motionFrameData.motionBoneDataList.Length; k++)
                {
                    MotionBoneData motionBoneData = motionFrameData.motionBoneDataList[k];
                    MotionBoneData currentMotionBoneData = currentMotionFrameData.motionBoneDataList[k];
                    float BonePosCost = Vector3.SqrMagnitude(motionBoneData.position - currentMotionBoneData.position);
                    Quaternion BonePosError = Quaternion.Inverse(motionBoneData.rotation) * currentMotionBoneData.rotation;
                    float BoneRotCost = Mathf.Abs(BonePosError.x) + Mathf.Abs(BonePosError.y) + Mathf.Abs(BonePosError.z) + (1 - Mathf.Abs(BonePosError.w));
                    float BoneVelocityCost = Vector3.SqrMagnitude(motionBoneData.velocity - currentMotionBoneData.velocity);
                    bonesCost += BonePosCost * motionCostFactorSettings.bonePosFactor + BoneRotCost * motionCostFactorSettings.boneRotFactor + BoneVelocityCost * motionCostFactorSettings.boneVelFactor;
                }
                for (int l = 0; l < motionFrameData.motionTrajectoryDataList.Length; l++)
                {
                    MotionTrajectoryData motionTrajectoryData = motionFrameData.motionTrajectoryDataList[l];
                    MotionTrajectoryData currentMotionTrajectoryData = currentMotionFrameData.motionTrajectoryDataList[l];

                    float trajectoryPosCost = Vector3.SqrMagnitude(motionTrajectoryData.position - currentMotionTrajectoryData.position);
                    float trajectoryVelCost = Vector3.SqrMagnitude(motionTrajectoryData.velocity - currentMotionTrajectoryData.velocity);
                    float trajectoryDirCost = Vector3.Dot(motionTrajectoryData.direction, currentMotionTrajectoryData.direction);
                    trajectorysCost += trajectoryPosCost * motionCostFactorSettings.predictionTrajectoryPosFactor + trajectoryVelCost * motionCostFactorSettings.predictionTrajectoryRotFactor + trajectoryDirCost * motionCostFactorSettings.predictionTrajectoryDirFactor;
                }

                rootMotionCost = (motionFrameData.velocity - currentMotionFrameData.velocity) * motionCostFactorSettings.rootMotionVelFactor;
                motionCost = bonesCost + trajectorysCost + rootMotionCost;

                Debug.LogFormat("ComputeMotionsBestCost motionName {0} motionCost {1} ", motionData.motionName, motionCost);

                if (bestMotionCost > motionCost)
                {
                    bestMotionCost = motionCost;
                    bestMotionFrameIndex = j;
                    bestMotionName = motionData.motionName;
                }
            }
        }
    }
}
