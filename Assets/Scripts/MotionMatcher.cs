using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nash
/// </summary>
public class MotionMatcher : MonoBehaviour
{
    public Text costText;
    public MotionsData motionsData;
    public MotionMatcherSettings motionMatcherSettings;
    public MotionCostFactorSettings motionCostFactorSettings;

    private MotionFrameData currentMotionFrameData;
    private Transform motionOwner;

    public int bestMotionFrameIndex = -1;
    public string bestMotionName = "HumanoidIdle";

    private bool bComputeMotionSnapShot = false;
    private float currentComputeTime;

    private string textContent = "";

    void Start()
    {
        currentMotionFrameData = new MotionFrameData();
        motionOwner = this.transform;
        currentComputeTime = motionMatcherSettings.ComputeMotionsBestCostGap;
    }

    private void Update()
    {
        currentComputeTime += Time.deltaTime;
    }

    public string AcquireMatchedMotion(PlayerInput playerInput)
    {
        return bestMotionName;
    }

    public string AcquireMatchedMotion(string motionName, float velocity, Vector3 direction, float acceleration, float brake, float normalizedTime)
    {
        //velocity = 1.0f;
        //direction = Vector3.forward;
        if (currentComputeTime >= motionMatcherSettings.ComputeMotionsBestCostGap)
        {
            currentComputeTime = 0;
            CapturePlayingMotionSnapShot(motionName, velocity, direction, acceleration, brake, normalizedTime);
            AddDebugContent("Cost", true);
            ComputeMotionsBestCost();
            costText.text = textContent;
            //Debug.LogFormat("AcquireMatchedMotion velocity {0} bestMotionName {1} direction {2}", velocity, bestMotionName, direction);
        }

        return bestMotionName;
    }

    private float AcquireBakedMotionVelocity(string motionName, float normalizedTime)
    {
        MotionFrameData motionFrameData = AcquireBakedMotionFrameData(motionName, normalizedTime);
        if (motionFrameData != null)
        {
            return motionFrameData.velocity;
        }
        return 0;
    }

    private MotionFrameData AcquireBakedMotionFrameData(string motionName, float normalizedTime)
    {
        for (int i = 0; i < motionsData.motionDataList.Length; i++)
        {
            MotionData motionData = motionsData.motionDataList[i];
            if (motionData.motionName == motionName)
            {
                int frame = Mathf.FloorToInt(motionData.motionFrameDataList.Length * normalizedTime);
                MotionFrameData motionFrameData = motionData.motionFrameDataList[frame];
                return motionFrameData;
            }
        }
        return null;
    }

    private void CapturePlayingMotionSnapShot(string motionName, float velocity, Vector3 direction, float acceleration, float brake, float normalizedTime)
    {
        MotionFrameData bakedMotionFrameData = AcquireBakedMotionFrameData(motionName, normalizedTime);
        currentMotionFrameData.velocity = velocity;
        currentMotionFrameData.motionBoneDataList = bakedMotionFrameData.motionBoneDataList;
        currentMotionFrameData.motionTrajectoryDataList = new MotionTrajectoryData[motionMatcherSettings.predictionTrajectoryTimeList.Length];
        float LastPredictionTrajectoryTime = 0;
        Vector3 LastPredictionPosition = Vector3.zero;
        for (int i = 0; i < motionMatcherSettings.predictionTrajectoryTimeList.Length; i++)
        {
            float predictionTrajectoryTime = motionMatcherSettings.predictionTrajectoryTimeList[i];
            float deltaTime = predictionTrajectoryTime - LastPredictionTrajectoryTime;
            currentMotionFrameData.motionTrajectoryDataList[i] = new MotionTrajectoryData();
            MotionTrajectoryData motionTrajectoryData = currentMotionFrameData.motionTrajectoryDataList[i];
            motionTrajectoryData.localPosition = LastPredictionPosition + velocity * direction * deltaTime;
            motionTrajectoryData.velocity = velocity * direction;
            motionTrajectoryData.direction = direction;
            LastPredictionTrajectoryTime = predictionTrajectoryTime;
            LastPredictionPosition = motionTrajectoryData.localPosition;
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
            AddDebugContent("motion: " + motionData.motionName);
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
                    //float BoneVelocityCost = Vector3.SqrMagnitude(motionBoneData.velocity - currentMotionBoneData.velocity);
                    bonesCost += BonePosCost * motionCostFactorSettings.bonePosFactor + BoneRotCost * motionCostFactorSettings.boneRotFactor/* + BoneVelocityCost * motionCostFactorSettings.boneVelFactor*/;
                    //AddDebugContent("BonePosCost: " + BonePosCost);
                    //AddDebugContent("BoneRotCost: " + BoneRotCost);
                    //AddDebugContent("BoneVelocityCost: " + BoneVelocityCost);
                }

                AddDebugContent("bonesTotalCost: " + bonesCost);

                for (int l = 0; l < motionFrameData.motionTrajectoryDataList.Length; l++)
                {
                    MotionTrajectoryData motionTrajectoryData = motionFrameData.motionTrajectoryDataList[l];
                    MotionTrajectoryData currentMotionTrajectoryData = currentMotionFrameData.motionTrajectoryDataList[l];

                    float trajectoryPosCost = Vector3.SqrMagnitude(motionTrajectoryData.localPosition - currentMotionTrajectoryData.localPosition);
                    float trajectoryVelCost = Vector3.SqrMagnitude(motionTrajectoryData.velocity - currentMotionTrajectoryData.velocity);
                    float trajectoryDirCost = Vector3.Dot(motionTrajectoryData.direction, currentMotionTrajectoryData.direction);
                    trajectorysCost += trajectoryPosCost * motionCostFactorSettings.predictionTrajectoryPosFactor + trajectoryVelCost * motionCostFactorSettings.predictionTrajectoryRotFactor + trajectoryDirCost * motionCostFactorSettings.predictionTrajectoryDirFactor;
                    //AddDebugContent("trajectoryPosCost: " + trajectoryPosCost);
                    //AddDebugContent("trajectoryVelCost: " + trajectoryVelCost);
                    //AddDebugContent("trajectoryDirCost: " + trajectoryDirCost);
                }

                AddDebugContent("trajectorysToatalCost: " + trajectorysCost);

                rootMotionCost = (motionFrameData.velocity - currentMotionFrameData.velocity) * motionCostFactorSettings.rootMotionVelFactor;
                AddDebugContent("rootMotionCost: " + rootMotionCost);
                motionCost = bonesCost + trajectorysCost + rootMotionCost;
                AddDebugContent("motionTotalCost: " + motionCost);

                //Debug.LogFormat("ComputeMotionsBestCost motionName {0} motionCost {1} ", motionData.motionName, motionCost);

                if (bestMotionCost > motionCost)
                {
                    bestMotionCost = motionCost;
                    bestMotionFrameIndex = j;
                    bestMotionName = motionData.motionName;
                }

                break;
            }
        }
    }

    private void AddDebugContent(string content, bool bClear = false)
    {
        if (bClear)
        {
            textContent = "";
        }
        textContent += content;
        textContent += "\n";
    }
}