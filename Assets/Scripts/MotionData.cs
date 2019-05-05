using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MotionData
{
    public MotionMainEntryType motionMainEntryType;
    public string motionName;
    public MotionFrameData[] motionFrameDataList;

    public void Clear()
    {
        motionName = "";
        motionMainEntryType = MotionMainEntryType.none;
        motionFrameDataList = null;
    }
}