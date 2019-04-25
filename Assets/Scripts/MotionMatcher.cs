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

    private int bestMotionFrameIndex = -1;
    private string bestMotionName = "";

    void Start()
    {
        currentMotionFrameData = new MotionFrameData();
        motionOwner = this.transform;
    }

    public string AcquireMatchedMotion(float velocity, Vector3 direction, float acceleration, float brake)
    {
        CapturePlayingMotionSnapShot(velocity, direction, acceleration, brake);
        ComputeMotionsBestCost();
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
            LastPredictionTrajectoryTime = predictionTrajectoryTime;
            float deltaTime = predictionTrajectoryTime - LastPredictionTrajectoryTime;
            MotionTrajectoryData motionTrajectoryData = currentMotionFrameData.motionTrajectoryDataList[i];
            motionTrajectoryData.position = velocity * direction * deltaTime;
            motionTrajectoryData.velocity = velocity * direction;
            motionTrajectoryData.direction = direction;
        }

        currentMotionFrameData.motionBoneDataList = new MotionBoneData[motionMatcherSettings.captureBoneList.Length];
        for (int j = 0; j < motionMatcherSettings.captureBoneList.Length; j++)
        {
            string captureBone = motionMatcherSettings.captureBoneList[j];
            Transform bone = motionOwner.Find(captureBone);
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
