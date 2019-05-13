using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;

public class DebugUIView : MonoBehaviour
{
    public MotionMatcher playerMotionMatcher;
    public Dropdown motionDropdown;
    public GameObject costTextContainer;
    public Text leftCostText;
    public Text rightCostText;
    public Button hideCostTextContainerBtn;

    private MotionFrameData currentMotionFrameData;
    private MotionFrameData targetMotionFrameData;


    // Start is called before the first frame update
    void Start()
    {
        Assert.IsNotNull(motionDropdown);
        Assert.IsNotNull(playerMotionMatcher);

        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
        MotionData[] motionDataList = playerMotionMatcher.motionsData.motionDataList;
        for (int i = 0; i < motionDataList.Length; i++)
        {
            MotionData motionData = motionDataList[i];

            Dropdown.OptionData optionData = new Dropdown.OptionData();
            optionData.text = motionData.motionName + "_" + 1;
            options.Add(optionData);
        }
        motionDropdown.AddOptions(options);
        motionDropdown.onValueChanged.AddListener(onDropdownChanged);
        hideCostTextContainerBtn.onClick.AddListener(onHideCostTextContainer);
        costTextContainer.SetActive(false);
        hideCostTextContainerBtn.gameObject.SetActive(false);
    }

    private void onHideCostTextContainer()
    {
        hideCostTextContainerBtn.gameObject.SetActive(false);
        costTextContainer.SetActive(false);
        rightCostText.text = "";
    }

    private void onDropdownChanged(int arg0)
    {
        costTextContainer.SetActive(true);
        hideCostTextContainerBtn.gameObject.SetActive(true);

        MotionData[] motionDataList = playerMotionMatcher.motionsData.motionDataList;
        MotionData motionData = motionDataList[arg0];
        targetMotionFrameData = motionData.motionFrameDataList[0];
        currentMotionFrameData = playerMotionMatcher.currentMotionFrameData;

        MotionDebugData motionDebugData = new MotionDebugData();

        AddDebugContent("----------------------" + motionData.motionName);

        float motionCost = 0f;
        float bonesCost = 0f;
        float bonePosCost = 0f;
        float boneRotCost = 0f;

        float trajectoryPosCost = 0f;
        float trajectoryVelCost = 0f;
        float trajectoryDirCost = 0f;
        float trajectorysCost = 0f;

        float rootMotionCost = 0f;

        for (int k = 0; k < targetMotionFrameData.motionBoneDataList.Length; k++)
        {
            MotionBoneData motionBoneData = targetMotionFrameData.motionBoneDataList[k];
            MotionBoneData currentMotionBoneData = currentMotionFrameData.motionBoneDataList[k];
            float BonePosCost = Vector3.SqrMagnitude(motionBoneData.localPosition - currentMotionBoneData.localPosition);
            Quaternion BonePosError = Quaternion.Inverse(motionBoneData.localRotation) * currentMotionBoneData.localRotation;
            float BoneRotCost = Mathf.Abs(BonePosError.x) + Mathf.Abs(BonePosError.y) + Mathf.Abs(BonePosError.z) + (1 - Mathf.Abs(BonePosError.w));
            //float BoneVelocityCost = Vector3.SqrMagnitude(motionBoneData.velocity - currentMotionBoneData.velocity);
            bonePosCost += BonePosCost * playerMotionMatcher.motionCostFactorSettings.bonePosFactor;
            boneRotCost += BoneRotCost * playerMotionMatcher.motionCostFactorSettings.boneRotFactor/* + BoneVelocityCost * motionCostFactorSettings.boneVelFactor*/;
            //AddDebugContent("BonePosCost: " + BonePosCost);
            //AddDebugContent("BoneRotCost: " + BoneRotCost);
            //AddDebugContent("BoneVelocityCost: " + BoneVelocityCost);
        }

        bonesCost = bonePosCost + boneRotCost;
        motionDebugData.bonePosCost = bonePosCost;
        motionDebugData.boneRotCost = boneRotCost;
        motionDebugData.bonesCost = bonesCost;

        AddDebugContent("bonesTotalCost: " + bonesCost);

        for (int l = 0; l < targetMotionFrameData.motionTrajectoryDataList.Length; l++)
        {
            MotionTrajectoryData motionTrajectoryData = targetMotionFrameData.motionTrajectoryDataList[l];
            MotionTrajectoryData currentMotionTrajectoryData = currentMotionFrameData.motionTrajectoryDataList[l];

            trajectoryPosCost += Vector3.SqrMagnitude(motionTrajectoryData.localPosition - currentMotionTrajectoryData.localPosition) * playerMotionMatcher.motionCostFactorSettings.predictionTrajectoryPosFactor;
            //trajectoryVelCost += Vector3.SqrMagnitude(motionTrajectoryData.velocity - currentMotionTrajectoryData.velocity) * motionCostFactorSettings.predictionTrajectoryVelFactor;
            trajectoryDirCost += Vector3.Dot(motionTrajectoryData.direction, currentMotionTrajectoryData.direction) * playerMotionMatcher.motionCostFactorSettings.predictionTrajectoryDirFactor;
            //AddDebugContent("trajectoryPosCost: " + trajectoryPosCost);
            //AddDebugContent("trajectoryVelCost: " + trajectoryVelCost);
            //AddDebugContent("trajectoryDirCost: " + trajectoryDirCost);
        }

        trajectorysCost = trajectoryPosCost + trajectoryVelCost + trajectoryDirCost;
        motionDebugData.trajectoryPosCost = trajectoryPosCost;
        motionDebugData.trajectoryVelCost = trajectoryVelCost;
        motionDebugData.trajectoryDirCost = trajectoryDirCost;
        motionDebugData.trajectorysCost = trajectorysCost;

        AddDebugContent("trajectoryPosCost: " + trajectoryPosCost);
        AddDebugContent("trajectoryVelCost: " + trajectoryVelCost);
        AddDebugContent("trajectoryDirCost: " + trajectoryDirCost);
        AddDebugContent("trajectorysToatalCost: " + trajectorysCost);

        rootMotionCost = Mathf.Abs(targetMotionFrameData.velocity - currentMotionFrameData.velocity) * playerMotionMatcher.motionCostFactorSettings.rootMotionVelFactor;
        motionDebugData.rootMotionCost = rootMotionCost;
        AddDebugContent("rootMotionCost: " + rootMotionCost);

        motionCost = bonesCost + trajectorysCost + rootMotionCost;
        motionDebugData.motionCost = motionCost;

        AddDebugContent("motionTotalCost: " + motionCost);
    }


    private void AddDebugContent(string content)
    {
        rightCostText.text += content;
        rightCostText.text += "\n";
    }

    private void OnDrawGizmos()
    {
        Vector3 baseLocation = playerMotionMatcher.transform.position;
        if (currentMotionFrameData != null)
        {
            Gizmos.color = Color.yellow;       
            for (int i = 0; i < currentMotionFrameData.motionTrajectoryDataList.Length; i++)
            {
                MotionTrajectoryData motionTrajectoryData = currentMotionFrameData.motionTrajectoryDataList[i];
                Gizmos.DrawSphere(motionTrajectoryData.localPosition + baseLocation, 0.05f);
            }
        }

        if (targetMotionFrameData != null)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < targetMotionFrameData.motionTrajectoryDataList.Length; i++)
            {
                MotionTrajectoryData motionTrajectoryData = targetMotionFrameData.motionTrajectoryDataList[i];
                Gizmos.DrawSphere(motionTrajectoryData.localPosition + baseLocation, 0.05f);
            }
        }
    }
}
