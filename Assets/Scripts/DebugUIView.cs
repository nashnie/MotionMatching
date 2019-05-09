using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

public class DebugUIView : MonoBehaviour
{
    public MotionMatcher playerMotionMatcher;
    public Dropdown motionDropdown;

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

            //for (int j = 0; j < motionData.motionFrameDataList.Length; j++)
            //{
            //    MotionFrameData motionFrameData = motionData.motionFrameDataList[j];
            //    Dropdown.OptionData optionData = new Dropdown.OptionData();
            //    optionData.text = motionData.motionName + "_" + (j + 1);
            //    options.Add(optionData);
            //}
        }
        motionDropdown.AddOptions(options);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
