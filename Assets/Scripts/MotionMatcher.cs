using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;

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

    public MotionFrameData currentMotionFrameData;
    private Transform motionOwner;

    public int bestMotionFrameIndex = -1;
    public string bestMotionName = "HumanoidIdle";
    public MotionDebugData bestMotionDebugData = null;

    private bool bComputeMotionSnapShot = false;
    private float currentComputeTime;

    private string textContent = "";

    private PlayerInput playerInput;
    private bool bCrouch = false;
    private float lastVelocity = 0f;
    public List<MotionDebugData> motionDebugDataList;

    private Thread motionThread;

    void Start()
    {
        currentMotionFrameData = new MotionFrameData();
        motionOwner = this.transform;
        currentComputeTime = motionMatcherSettings.ComputeMotionsBestCostGap;
        motionDebugDataList = new List<MotionDebugData>();

        //HumanoidCrouchIdle 动画有问题，只能通过这种方式临时处理一下
        for (int i = 0; i < motionsData.motionDataList.Length; i++)
        {
            MotionData motionData = motionsData.motionDataList[i];
            if (motionData.motionName.Contains("HumanoidCrouchIdle"))
            {
                for (int j = 0; j < motionData.motionFrameDataList.Length; j++)
                {
                    MotionFrameData motionFrameData = motionData.motionFrameDataList[j];
                    for (int k = 0; k < motionFrameData.motionTrajectoryDataList.Length; k++)
                    {
                        MotionTrajectoryData motionTrajectoryData = motionFrameData.motionTrajectoryDataList[k];
                        motionTrajectoryData.localPosition = Vector3.zero;
                    }
                }
            }
        }
    }

    private void Update()
    {
        currentComputeTime += Time.deltaTime;
    }

    public string AcquireMatchedMotion(PlayerInput playerInput, string motionName, float normalizedTime)
    {
        //playerInput.crouch = true;
        //playerInput.direction = Vector3.forward;
        //playerInput.velocity = 0.65f;

        float velocity = playerInput.velocity;
        Vector3 direction = playerInput.direction;
        float acceleration = playerInput.acceleration;
        float brake = playerInput.brake;
        bool crouch = playerInput.crouch;

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
            lastVelocity = velocity;
            CapturePlayingMotionSnapShot(playerInput, motionName, normalizedTime, motionMainEntryType);
            if (motionMatcherSettings.EnableDebugText)
            {
                AddDebugContent("Cost", true);
            }
            ComputeMotionsBestCost();
            costText.text = textContent;
            //Debug.LogFormat("AcquireMatchedMotion velocity {0} MotionName {1} direction {2}", velocity, MotionName, direction);
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

    private void CapturePlayingMotionSnapShot(PlayerInput playerInput, string motionName, float normalizedTime, MotionMainEntryType motionMainEntryType)
    {
        float velocity = playerInput.velocity;
        float angularVelocity = playerInput.angularVelocity;
        Vector3 direction = playerInput.direction;
        float acceleration = playerInput.acceleration;
        float brake = playerInput.brake;
  
        MotionFrameData bakedMotionFrameData = AcquireBakedMotionFrameData(motionName, normalizedTime, motionMainEntryType);
        currentMotionFrameData.velocity = velocity;
        currentMotionFrameData.motionBoneDataList = bakedMotionFrameData.motionBoneDataList;
        currentMotionFrameData.motionTrajectoryDataList = new MotionTrajectoryData[motionMatcherSettings.predictionTrajectoryTimeList.Length];

        for (int i = 0; i < motionMatcherSettings.predictionTrajectoryTimeList.Length; i++)
        {
            float predictionTrajectoryTime = motionMatcherSettings.predictionTrajectoryTimeList[i];
            currentMotionFrameData.motionTrajectoryDataList[i] = new MotionTrajectoryData();
            MotionTrajectoryData motionTrajectoryData = currentMotionFrameData.motionTrajectoryDataList[i];
            motionTrajectoryData.localPosition = velocity * direction * predictionTrajectoryTime;
            motionTrajectoryData.velocity = velocity * direction;
            if (Mathf.Abs(playerInput.angularVelocity) > 0)
            {
                motionTrajectoryData.direction = Quaternion.Euler(0, playerInput.angularVelocity * predictionTrajectoryTime, 0) * Vector3.forward;
            }
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
            if (motionMatcherSettings.EnableDebugText)
            {
                AddDebugContent("motion: " + motionData.motionName);
            }

            for (int j = 0; j < motionData.motionFrameDataList.Length; j++)
            {
                MotionDebugData motionDebugData = new MotionDebugData();
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
                    float BonePosCost = Vector3.SqrMagnitude(motionBoneData.localPosition - currentMotionBoneData.localPosition);
                    Quaternion BonePosError = Quaternion.Inverse(motionBoneData.localRotation) * currentMotionBoneData.localRotation;
                    float BoneRotCost = Mathf.Abs(BonePosError.x) + Mathf.Abs(BonePosError.y) + Mathf.Abs(BonePosError.z) + (1 - Mathf.Abs(BonePosError.w));
                    //float BoneVelocityCost = Vector3.SqrMagnitude(motionBoneData.velocity - currentMotionBoneData.velocity);
                    bonePosCost += BonePosCost * motionCostFactorSettings.bonePosFactor;
                    boneRotCost += BoneRotCost * motionCostFactorSettings.boneRotFactor/* + BoneVelocityCost * motionCostFactorSettings.boneVelFactor*/;
                    //AddDebugContent("BonePosCost: " + BonePosCost);
                    //AddDebugContent("BoneRotCost: " + BoneRotCost);
                    //AddDebugContent("BoneVelocityCost: " + BoneVelocityCost);
                }

                bonesCost = bonePosCost + boneRotCost;
                motionDebugData.bonePosCost = bonePosCost;
                motionDebugData.boneRotCost = boneRotCost;
                motionDebugData.bonesCost = bonesCost;

                if (motionMatcherSettings.EnableDebugText)
                {
                    AddDebugContent("bonesTotalCost: " + bonesCost);
                }

                for (int l = 0; l < motionFrameData.motionTrajectoryDataList.Length; l++)
                {
                    MotionTrajectoryData motionTrajectoryData = motionFrameData.motionTrajectoryDataList[l];
                    MotionTrajectoryData currentMotionTrajectoryData = currentMotionFrameData.motionTrajectoryDataList[l];

                    trajectoryPosCost += Vector3.SqrMagnitude(motionTrajectoryData.localPosition - currentMotionTrajectoryData.localPosition) * motionCostFactorSettings.predictionTrajectoryPosFactor;
                    //trajectoryVelCost += Vector3.SqrMagnitude(motionTrajectoryData.velocity - currentMotionTrajectoryData.velocity) * motionCostFactorSettings.predictionTrajectoryVelFactor;
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

                if (motionMatcherSettings.EnableDebugText)
                {
                    AddDebugContent("trajectorysToatalCost: " + trajectorysCost);
                }

                rootMotionCost = Mathf.Abs(motionFrameData.velocity - currentMotionFrameData.velocity) * motionCostFactorSettings.rootMotionVelFactor;
                motionDebugData.rootMotionCost = rootMotionCost;
                if (motionMatcherSettings.EnableDebugText)
                {
                    AddDebugContent("rootMotionCost: " + rootMotionCost);
                }

                motionCost = bonesCost + trajectorysCost + rootMotionCost;
                motionDebugData.motionCost = motionCost;

                if (motionMatcherSettings.EnableDebugText)
                {
                    AddDebugContent("motionTotalCost: " + motionCost);
                }

                //Debug.LogFormat("ComputeMotionsBestCost motionName {0} motionCost {1} ", motionData.motionName, motionCost);

                if (bestMotionCost > motionCost)
                {
                    bestMotionCost = motionCost;
                    bestMotionFrameIndex = j;
                    bestMotionName = motionData.motionName;
                    bestMotionDebugData = motionDebugData;

                    motionDebugData.motionName = motionData.motionName;
                    motionDebugData.motionFrameIndex = j;

                    motionDebugDataList.Add(motionDebugData);
                }
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