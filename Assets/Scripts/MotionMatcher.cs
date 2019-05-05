using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nash
/// TODO 多线程
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

    private PlayerInput playerInput;
    private bool bCrouch = false;
    private List<MotionDebugData> motionDebugDataList;

    void Start()
    {
        currentMotionFrameData = new MotionFrameData();
        motionOwner = this.transform;
        currentComputeTime = motionMatcherSettings.ComputeMotionsBestCostGap;
        motionDebugDataList = new List<MotionDebugData>();
    }

    private void Update()
    {
        currentComputeTime += Time.deltaTime;
    }

    public string AcquireMatchedMotion(PlayerInput playerInput)
    {
        if (playerInput.crouch)
        {

        }
        else
        {
        }
        return bestMotionName;
    }

    public string AcquireMatchedMotion(string motionName, float velocity, Vector3 direction, float acceleration, float brake, float normalizedTime, bool crouch)
    {
        //velocity = .3f;
        //direction = Vector3.forward;
        //crouch = true;
        if (currentComputeTime >= motionMatcherSettings.ComputeMotionsBestCostGap)
        {
            currentComputeTime = 0;
            motionDebugDataList.Clear();
            MotionMainEntryType motionMainEntryType = MotionMainEntryType.none;
            if (bCrouch != crouch)
            {
                bCrouch = crouch;
                motionMainEntryType = crouch ? MotionMainEntryType.crouch : MotionMainEntryType.stand;
            }
            CapturePlayingMotionSnapShot(motionName, velocity, direction, acceleration, brake, normalizedTime, motionMainEntryType);
            AddDebugContent("Cost", true);
            ComputeMotionsBestCost();
            costText.text = textContent;
            //Debug.LogFormat("AcquireMatchedMotion velocity {0} bestMotionName {1} direction {2}", velocity, bestMotionName, direction);
        }

        return bestMotionName;
    }

    private float AcquireBakedMotionVelocity(string motionName, float normalizedTime, MotionMainEntryType motionMainEntryType)
    {
        MotionFrameData motionFrameData = AcquireBakedMotionFrameData(motionName, normalizedTime, motionMainEntryType);
        if (motionFrameData != null)
        {
            return motionFrameData.velocity;
        }
        return 0;
    }

    private MotionFrameData AcquireBakedMotionFrameData(string motionName, float normalizedTime, MotionMainEntryType motionMainEntryType)
    {
        MotionFrameData motionFrameData = null;
        for (int i = 0; i < motionsData.motionDataList.Length; i++)
        {
            MotionData motionData = motionsData.motionDataList[i];
            if (motionMainEntryType != MotionMainEntryType.none)
            {
                if (motionData.motionName.IndexOf(motionMatcherSettings.StandTag) >= 0 && 
                    motionMainEntryType == MotionMainEntryType.stand)
                {
                    motionFrameData = motionData.motionFrameDataList[0];
                    break;
                }
                if (motionData.motionName.IndexOf(motionMatcherSettings.CrouchTag) >= 0 &&
                    motionMainEntryType == MotionMainEntryType.crouch)
                {
                    motionFrameData = motionData.motionFrameDataList[0];
                    break;
                }
            }
            else if (motionData.motionName == motionName)
            {
                int frame = Mathf.FloorToInt(motionData.motionFrameDataList.Length * normalizedTime);
                motionFrameData = motionData.motionFrameDataList[frame];
            }
        }
        return motionFrameData;
    }

    private void CapturePlayingMotionSnapShot(string motionName, float velocity, Vector3 direction, float acceleration, float brake, float normalizedTime, MotionMainEntryType motionMainEntryType)
    {
        MotionFrameData bakedMotionFrameData = AcquireBakedMotionFrameData(motionName, normalizedTime, motionMainEntryType);
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
            bool bAcquireBestMotion = false;
            MotionDebugData motionDebugData = new MotionDebugData();
            for (int j = 0; j < motionData.motionFrameDataList.Length; j++)
            {
                float motionCost = 0f;
                float bonesCost = 0f;
                float bonePosCost = 0f;
                float boneRotCost = 0f;

                float trajectoryPosCost = 0f;
                float trajectoryVelCost = 0f;
                float trajectoryDirCost = 0f;
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
                    bonePosCost += BonePosCost * motionCostFactorSettings.bonePosFactor;
                    boneRotCost += BoneRotCost* motionCostFactorSettings.boneRotFactor/* + BoneVelocityCost * motionCostFactorSettings.boneVelFactor*/;
                    //AddDebugContent("BonePosCost: " + BonePosCost);
                    //AddDebugContent("BoneRotCost: " + BoneRotCost);
                    //AddDebugContent("BoneVelocityCost: " + BoneVelocityCost);
                }

                bonesCost = bonePosCost + boneRotCost;
                motionDebugData.bonePosCost = bonePosCost;
                motionDebugData.boneRotCost = boneRotCost;
                motionDebugData.bonePosCost = bonesCost;

                AddDebugContent("bonesTotalCost: " + bonesCost);

                for (int l = 0; l < motionFrameData.motionTrajectoryDataList.Length; l++)
                {
                    MotionTrajectoryData motionTrajectoryData = motionFrameData.motionTrajectoryDataList[l];
                    MotionTrajectoryData currentMotionTrajectoryData = currentMotionFrameData.motionTrajectoryDataList[l];

                    trajectoryPosCost += Vector3.SqrMagnitude(motionTrajectoryData.localPosition - currentMotionTrajectoryData.localPosition) * motionCostFactorSettings.predictionTrajectoryPosFactor;
                    trajectoryVelCost += Vector3.SqrMagnitude(motionTrajectoryData.velocity - currentMotionTrajectoryData.velocity) * motionCostFactorSettings.predictionTrajectoryRotFactor;
                    trajectoryDirCost += Vector3.Dot(motionTrajectoryData.direction, currentMotionTrajectoryData.direction) * motionCostFactorSettings.predictionTrajectoryDirFactor;
                    //AddDebugContent("trajectoryPosCost: " + trajectoryPosCost);
                    //AddDebugContent("trajectoryVelCost: " + trajectoryVelCost);
                    //AddDebugContent("trajectoryDirCost: " + trajectoryDirCost);
                }

                trajectorysCost = trajectoryPosCost + trajectoryVelCost + trajectoryDirCost;
                motionDebugData.trajectoryPosCost = trajectoryPosCost;
                motionDebugData.trajectoryVelCost = trajectoryVelCost;
                motionDebugData.trajectoryDirCost = trajectoryDirCost;
                motionDebugData.trajectorysCost = trajectorysCost;

                AddDebugContent("trajectorysToatalCost: " + trajectorysCost);

                rootMotionCost = (motionFrameData.velocity - currentMotionFrameData.velocity) * motionCostFactorSettings.rootMotionVelFactor;
                motionDebugData.rootMotionCost = rootMotionCost;
                AddDebugContent("rootMotionCost: " + rootMotionCost);
                motionCost = bonesCost + trajectorysCost + rootMotionCost;
                motionDebugData.motionCost = motionCost;
                AddDebugContent("motionTotalCost: " + motionCost);

                //Debug.LogFormat("ComputeMotionsBestCost motionName {0} motionCost {1} ", motionData.motionName, motionCost);

                if (bestMotionCost > motionCost)
                {
                    bestMotionCost = motionCost;
                    bestMotionFrameIndex = j;
                    bestMotionName = motionData.motionName;

                    motionDebugData.bestMotionName = bestMotionName;
                    motionDebugData.bestMotionFrameIndex = bestMotionFrameIndex;

                    bAcquireBestMotion = true;
                }
            }
            if (bAcquireBestMotion)
            {
                motionDebugDataList.Add(motionDebugData);
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