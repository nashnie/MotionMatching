using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;

/// <summary>
/// Nash
/// </summary>
public class MotionMatchingBakeEditor : EditorWindow
{
    [MenuItem("Tools/BakeMotion")]
    static void MakeWindow()
    {
        window = GetWindow(typeof(MotionMatchingBakeEditor)) as MotionMatchingBakeEditor;
        window.oColor = GUI.contentColor;
    }

    private static MotionMatchingBakeEditor window;
    private Color oColor;
    private Vector2 scrollpos;
    private Dictionary<string, int> frameSkips = new Dictionary<string, int>();
    private Dictionary<string, bool> bakeAnims = new Dictionary<string, bool>();

    Dictionary<string, int> bonesMap = new Dictionary<string, int>();
    Transform[] joints = null;

    public static string RootBoneName = "EthanSkeleton";
    const string MotionMatcherSettingsPath = "Assets/Resources/MotionMatcherSettings.asset";

    [SerializeField]
    private Vector2 scroll;
    [SerializeField]
    private int fps = 30;
    [SerializeField]
    private int previousGlobalBake = 1;
    [SerializeField]
    private int globalBake = 1;
    [SerializeField]
    private GameObject prefab;
    [SerializeField]
    private List<AnimationClip> customClips = new List<AnimationClip>();
    [SerializeField]
    private List<MeshFilter> meshFilters = new List<MeshFilter>();
    [SerializeField]
    private List<SkinnedMeshRenderer> skinnedRenderers = new List<SkinnedMeshRenderer>();
    [SerializeField]
    private GameObject previousPrefab;
    [SerializeField]
    private bool customCompression = false;
    [SerializeField]
    private GameObject spawnedAsset;
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private RuntimeAnimatorController animController;
    [SerializeField]
    private Avatar animAvatar;
    [SerializeField]
    private bool requiresAnimator;

    public float[] predictionTrajectoryTimeList;
    public string[] captureBoneList;
    public string crouchTag = "CrouchIdle";
    public string standTag = "Idle";

    private void OnEnable()
    {
        if (prefab == null && Selection.activeGameObject)
        {
            prefab = Selection.activeGameObject;
            OnPrefabChanged();
        }
    }

    private void OnDisable()
    {
        if (spawnedAsset)
        {
            DestroyImmediate(spawnedAsset.gameObject);
        }
    }

    private string GetAssetPath(string s)
    {
        string path = s;
        string[] split = path.Split('\\');
        path = string.Empty;
        int startIndex = 0;
        for (int i = 0; i < split.Length; i++)
        {
            if (split[i] == "Assets")
                break;
            startIndex++;
        }
        for (int i = startIndex; i < split.Length; i++)
            path += split[i] + "\\";
        path = path.TrimEnd("\\".ToCharArray());
        path = path.Replace("\\", "/");
        return path;
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Batch Bake Selected Objects"))
        {
            previousPrefab = null;
            foreach (var obj in Selection.gameObjects)
            {
                try
                {
                    prefab = obj;
                    OnPrefabChanged();
                    var toBakeClips = GetClips();
                    foreach (var clip in toBakeClips)
                    {
                        frameSkips[clip.name] = 1;
                    }
                    CreateSnapshots();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        GUI.skin.label.richText = true;
        scroll = GUILayout.BeginScrollView(scroll);
        {
            EditorGUI.BeginChangeCheck();
            prefab = EditorGUILayout.ObjectField("Asset to Bake", prefab, typeof(GameObject), true) as GameObject;
            if (prefab)
            {
                if (string.IsNullOrEmpty(GetPrefabPath()))
                {
                    DrawText("Cannot find asset path, are you sure this object is a prefab ? ", Color.red + Color.white * 0.5f);
                    return;
                }
                if (previousPrefab != prefab)
                {
                    OnPrefabChanged();
                }
                if (spawnedAsset == null)
                {
                    OnPrefabChanged();
                }
                animController = EditorGUILayout.ObjectField("Animation Controller", animController, typeof(RuntimeAnimatorController), true) as RuntimeAnimatorController;
                if (animController == null)
                {
                    GUI.skin.label.richText = true;
                    GUILayout.Label("<b>Specify a Animation Controller to auto-populate animation clips</b>");
                }
                if (requiresAnimator)
                {
                    if (animAvatar == null)
                    {
                        GetAvatar();
                    }       
                    animAvatar = EditorGUILayout.ObjectField("Avatar", animAvatar, typeof(Avatar), true) as Avatar;
                    if (animAvatar == null)
                    {
                        GUI.color = Color.red;
                        GUI.skin.label.richText = true;
                        GUILayout.Label("<color=red>For humanoid and optimized rigs, you must specify an Avatar</color>");
                        GUI.color = Color.white;
                    }
                }

                fps = EditorGUILayout.IntSlider("Bake FPS", fps, 1, 500);

                globalBake = EditorGUILayout.IntSlider("Global Frame Skip", globalBake, 1, fps);
                bool bChange = globalBake != previousGlobalBake;
                previousGlobalBake = globalBake;

                EditorGUILayout.LabelField("Custom Clips");
                for (int i = 0; i < customClips.Count; i++)
                {
                    GUILayout.BeginHorizontal();
                    {
                        customClips[i] = (AnimationClip)EditorGUILayout.ObjectField(customClips[i], typeof(AnimationClip), false);
                        if (GUILayout.Button("X", GUILayout.Width(32)))
                        {
                            customClips.RemoveAt(i);
                            GUILayout.EndHorizontal();
                            break;
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Add Custom Animation Clip"))
                {
                    customClips.Add(null);
                }
                if (GUILayout.Button("Add Selected Animation Clips"))
                {
                    foreach (var o in Selection.objects)
                    {
                        string p = AssetDatabase.GetAssetPath(o);
                        if (string.IsNullOrEmpty(p) == false)
                        {
                            AnimationClip[] clipsToAdd = AssetDatabase.LoadAllAssetRepresentationsAtPath(p).Where(q => q is AnimationClip).Cast<AnimationClip>().ToArray();
                            customClips.AddRange(clipsToAdd);
                        }
                    }
                }
                var clips = GetClips();
                string[] clipNames = bakeAnims.Keys.ToArray();

                bool modified = false;
                scrollpos = GUILayout.BeginScrollView(scrollpos, GUILayout.MinHeight(100), GUILayout.MaxHeight(1000));
                try
                {
                    EditorGUI.indentLevel++;
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Select All", GUILayout.Width(100)))
                        {
                            foreach (var clipName in clipNames)
                                bakeAnims[clipName] = true;
                        }
                        if (GUILayout.Button("Deselect All", GUILayout.Width(100)))
                        {
                            foreach (var clipName in clipNames)
                                bakeAnims[clipName] = false;
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Bake Animation");
                        GUILayout.Label("Frame Skip");
                    }
                    GUILayout.EndHorizontal();
                    foreach (var clipName in clipNames)
                    {
                        if (frameSkips.ContainsKey(clipName) == false)
                        {
                            frameSkips.Add(clipName, globalBake);
                        }

                        AnimationClip clip = clips.Find(q => q.name == clipName);
                        int framesToBake = clip ? (int)(clip.length * fps / frameSkips[clipName]) : 0;
                        GUILayout.BeginHorizontal();
                        {
                            bakeAnims[clipName] = EditorGUILayout.Toggle(string.Format("{0} ({1} frames)", clipName, framesToBake), bakeAnims[clipName]);
                            GUI.enabled = bakeAnims[clipName];
                            frameSkips[clipName] = Mathf.Clamp(EditorGUILayout.IntField(frameSkips[clipName]), 1, fps);
                            GUI.enabled = true;
                        }
                        GUILayout.EndHorizontal();
                        if (framesToBake > 500)
                        {
                            GUI.skin.label.richText = true;
                            EditorGUILayout.LabelField("<color=red>Long animations degrade performance, consider using a higher frame skip value.</color>", GUI.skin.label);
                        }
                        if (bChange)
                        {
                            frameSkips[clipName] = globalBake;
                        }
                        if (frameSkips[clipName] != 1)
                        {
                            modified = true;
                        }
                    }
                    EditorGUI.indentLevel--;
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                }
                GUILayout.EndScrollView();
                if (modified)
                {
                    DrawText("Skipping more frames during baking will result in a smaller asset size, but potentially degrade animation quality.", Color.yellow);
                }

                GUILayout.Space(10);
                int bakeCount = bakeAnims.Count(q => q.Value);
                GUI.enabled = bakeCount > 0;
                if (GUILayout.Button(string.Format("Generate Snapshots for {0} animation{1}", bakeCount, bakeCount > 1 ? "s" : string.Empty)))
                {
                    CreateSnapshots();
                }
                GUI.enabled = true;
                //SavePreferencesForAsset();
            }
            else // end if valid prefab
            {
                DrawText("Specify a asset to bake.", Color.red + Color.white * 0.5f);
            }
            EditorGUI.EndChangeCheck();
            if (GUI.changed)
            {
                Repaint();
            }
        }
        GUILayout.EndScrollView();
    }

    private void InitSettings()
    {
        MotionMatcherSettings motionMatcherSettings = AssetDatabase.LoadAssetAtPath(MotionMatcherSettingsPath, typeof(MotionMatcherSettings)) as MotionMatcherSettings;
        if (motionMatcherSettings)
        {
            predictionTrajectoryTimeList = motionMatcherSettings.predictionTrajectoryTimeList;
            captureBoneList = motionMatcherSettings.captureBoneList;
            crouchTag = motionMatcherSettings.CrouchTag;
            standTag = motionMatcherSettings.StandTag;
        }
    }

    private void InitAnimationBones(GameObject animationTarget)
    {
        bonesMap.Clear();
        Transform child = animationTarget.transform.Find(RootBoneName);
        joints = child.GetComponentsInChildren<Transform>();
        List<Transform> jointList = joints.ToList();
        for (int i = 0; i < captureBoneList.Length; i++)
        {
            string boneName = captureBoneList[i];
            Transform bone = animationTarget.transform.Find(boneName);
            int index = jointList.IndexOf(bone);
            bonesMap.Add(bone.name, index);
        }
    }

    private float GetAnimationClipCurve(AnimationClip clip, string path, string propertyName, float delta)
    {
        EditorCurveBinding curveBinding = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName);
        AnimationCurve animationCurve = AnimationUtility.GetEditorCurve(clip, curveBinding);
        return animationCurve.Evaluate(delta);
    }

    private void CreateSnapshots()
    {
        UnityEditor.Animations.AnimatorController bakeController = null;

        string assetPath = GetPrefabPath();
        if (string.IsNullOrEmpty(assetPath))
        {
            EditorUtility.DisplayDialog("Mesh Animator", "Unable to locate the asset path for prefab: " + prefab.name, "OK");
            return;
        }

        HashSet<string> allAssets = new HashSet<string>();

        List<AnimationClip> clips = GetClips();
        foreach (var clip in clips)
        {
            allAssets.Add(AssetDatabase.GetAssetPath(clip));
        }

        string[] split = assetPath.Split("/".ToCharArray());

        string assetFolder = string.Empty;
        for (int s = 0; s < split.Length - 1; s++)
        {
            assetFolder += split[s] + "/";
        }

        var sampleGO = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;
        sampleGO.name = prefab.name;

        animator = sampleGO.GetComponent<Animator>();
        if (animator == null)
        {
            animator = sampleGO.GetComponentInChildren<Animator>();
        }
        InitAnimationBones(sampleGO);
        if (requiresAnimator)
        {
            bakeController = CreateBakeController();
            if (animator == null)
            {
                animator = sampleGO.AddComponent<Animator>();
                animator.runtimeAnimatorController = bakeController;
                animator.avatar = GetAvatar();
            }
            else
            {
                animator.runtimeAnimatorController = bakeController;
                animator.avatar = GetAvatar();
            }
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = true;
        }

        MotionsData meshAnimationList = ScriptableObject.CreateInstance<MotionsData>();
        meshAnimationList.motionDataList = new MotionData[clips.Count];

        Transform rootMotionBaker = new GameObject().transform;

        for (int x = 0; x < clips.Count; x++)
        {
            AnimationClip animClip = clips[x];

            if (bakeAnims.ContainsKey(animClip.name) && bakeAnims[animClip.name] == false)
            {
                continue;
            }
            if (frameSkips.ContainsKey(animClip.name) == false)
            {
                Debug.LogWarningFormat("No animation with name {0} in frame skips", animClip.name);
                continue;
            }
            string meshAnimationPath = string.Format("{0}{1}.asset", assetFolder, FormatClipName(animClip.name));

            int bakeFrames = Mathf.CeilToInt(animClip.length * fps);
            meshAnimationList.motionDataList[x] = new MotionData();
            MotionData motionData = meshAnimationList.motionDataList[x];
            motionData.motionName = animClip.name;

            int totalFrame = 0;
            for (int i = 0; i < bakeFrames; i += frameSkips[animClip.name])
            {
                totalFrame++;
            }
            motionData.motionFrameDataList = new MotionFrameData[totalFrame];

            int frame = 0;
            for (int i = 0; i < bakeFrames; i += frameSkips[animClip.name])
            {
                motionData.motionFrameDataList[frame] = new MotionFrameData();
                motionData.motionFrameDataList[frame].motionBoneDataList = new MotionBoneData[bonesMap.Count];
                motionData.motionFrameDataList[frame].motionTrajectoryDataList = new MotionTrajectoryData[predictionTrajectoryTimeList.Length];
                for (int i1 = 0; i1 < bonesMap.Count; i1++)
                {
                    motionData.motionFrameDataList[frame].motionBoneDataList[i1] = new MotionBoneData();
                }
                for (int i2 = 0; i2 < predictionTrajectoryTimeList.Length; i2++)
                {
                    motionData.motionFrameDataList[frame].motionTrajectoryDataList[i2] = new MotionTrajectoryData();
                }
                frame++;
            }
        }

        int animCount = 0;
        for (int j = 0; j < clips.Count; j++)
        {
            //Vector3.zero, Quaternion.identity
            sampleGO.transform.position = Vector3.zero;
            sampleGO.transform.rotation = Quaternion.identity;
            AnimationClip animClip = clips[j];
            MotionData motionData = meshAnimationList.motionDataList[j];
            int bakeFrames = Mathf.CeilToInt(animClip.length * fps);

            int frame = 0;
            for (int i = 0; i < bakeFrames; i += frameSkips[animClip.name])
            {
                float bakeDelta = Mathf.Clamp01(((float)i / bakeFrames));
                EditorUtility.DisplayProgressBar("Baking Animation", string.Format("Processing: {0} Frame: {1}", animClip.name, i), bakeDelta);
                float animationTime = bakeDelta * animClip.length;
                MotionFrameData motionFrameData = motionData.motionFrameDataList[frame];
                MotionFrameData lastMotionFrameData = frame > 0 ? motionData.motionFrameDataList[frame - 1] : null;

                CaptureBoneSnapShot(animClip, motionFrameData, lastMotionFrameData, sampleGO, bakeFrames, i);
                CaptureTrajectorySnapShot(animClip, motionFrameData, lastMotionFrameData, sampleGO, bakeFrames, i);

                frame++;
            }
            animCount++;
        }

        //string TargetPath = Application.dataPath + "/Resources/" + sampleGO.name + "_MotionField1.asset";
        //AssetDatabase.DeleteAsset(TargetPath);
        //AssetDatabase.CreateAsset(meshAnimationList, TargetPath);

        AssetDatabase.CreateAsset(meshAnimationList, assetFolder + sampleGO.name + "_MotionField.asset");

        GameObject.DestroyImmediate(sampleGO);
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("MotionField", string.Format("Baked {0} motionField {1} successfully!", clips.Count, clips.Count > 1 ? "s" : string.Empty), "OK");

        try
        {
            //Wait
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Bake Error", string.Format("There was a problem baking the animations.", e), "OK");
        }
        finally
        {
            if (bakeController)
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(bakeController));
            }
        }
    }

    private Avatar GetAvatar()
    {
        if (animAvatar)
        {
            return animAvatar;
        }
        var objs = EditorUtility.CollectDependencies(new Object[] { prefab }).ToList();
        foreach (var obj in objs.ToArray())
        {
            objs.AddRange(AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(obj)));
        }
        objs.RemoveAll(q => q is Avatar == false || q == null);
        if (objs.Count > 0)
        {
            animAvatar = objs[0] as Avatar;
        }
        return animAvatar;
    }

    private List<AnimationClip> GetClips()
    {
        var clips = EditorUtility.CollectDependencies(new Object[] { prefab }).ToList();
        foreach (var obj in clips.ToArray())
        {
            clips.AddRange(AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(obj)));
        }
        clips.AddRange(customClips.Select(q => (Object)q));
        clips.RemoveAll(q => q is AnimationClip == false || q == null);
        foreach (AnimationClip clip in clips)
        {
            if (bakeAnims.ContainsKey(clip.name) == false)
            {
                bakeAnims.Add(clip.name, true);
            }   
        }
        clips.RemoveAll(q => bakeAnims.ContainsKey(q.name) == false);
        clips.RemoveAll(q => bakeAnims[q.name] == false);

        var distinctClips = clips.Select(q => (AnimationClip)q).Distinct().ToList();
        requiresAnimator = false;
        var humanoidCheck = new List<AnimationClip>(distinctClips);
        if (animController)
        {
            humanoidCheck.AddRange(animController.animationClips);
            distinctClips.AddRange(animController.animationClips);
            distinctClips = distinctClips.Distinct().ToList();
        }
        foreach (var c in humanoidCheck)
        {
            if (c && c.isHumanMotion)
            {
                requiresAnimator = true;
            }
        }

        try
        {
            if (requiresAnimator == false)
            {
                var importer = GetImporter(GetPrefabPath());
                if (importer && importer.animationType == ModelImporterAnimationType.Human)
                {
                    requiresAnimator = true;
                }
            }
        }
        catch { }

        try
        {
            if (requiresAnimator == false && IsOptimizedAnimator())
            {
                requiresAnimator = true;
            }    
        }
        catch { }

        for (int i = 0; i < distinctClips.Count; i++)
        {
            if (bakeAnims.ContainsKey(distinctClips[i].name) == false)
            {
                bakeAnims.Add(distinctClips[i].name, true);
            }
        }
        return distinctClips;
    }

    private void DrawText(string text, Color color)
    {
        GUI.contentColor = color;
        GUILayout.TextArea(text);
        GUI.contentColor = oColor;
    }

    private string GetPrefabPath()
    {
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(assetPath))
        {
            Object parentObject = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
            assetPath = AssetDatabase.GetAssetPath(parentObject);
        }
        return assetPath;
    }

    private void OnPrefabChanged()
    {
        if (spawnedAsset)
        {
            GameObject.DestroyImmediate(spawnedAsset.gameObject);
        }
        if (Application.isPlaying)
        {
            return;
        }
        animator = null;
        animAvatar = null;
        if (prefab)
        {
            if (spawnedAsset == null)
            {
                spawnedAsset = GameObject.Instantiate(prefab) as GameObject;
                SetChildFlags(spawnedAsset.transform, HideFlags.HideAndDontSave);
            }
            bakeAnims.Clear();
            frameSkips.Clear();
            InitSettings();
            AutoPopulateFiltersAndRenderers();
            AutoPopulateAnimatorAndController();
        }
        previousPrefab = prefab;
    }

    private void AutoPopulateFiltersAndRenderers()
    {
        meshFilters.Clear();
        skinnedRenderers.Clear();
        MeshFilter[] filtersInPrefab = spawnedAsset.GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < filtersInPrefab.Length; i++)
        {
            if (meshFilters.Contains(filtersInPrefab[i]) == false)
            {
                meshFilters.Add(filtersInPrefab[i]);
            }  
            if (filtersInPrefab[i].GetComponent<MeshRenderer>())
            {
                filtersInPrefab[i].GetComponent<MeshRenderer>().enabled = false;
            }
        }
        SkinnedMeshRenderer[] renderers = spawnedAsset.GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (skinnedRenderers.Contains(renderers[i]) == false)
            {
                skinnedRenderers.Add(renderers[i]);
            }
            renderers[i].enabled = false;
        }
    }

    private void AutoPopulateAnimatorAndController()
    {
        animator = spawnedAsset.GetComponent<Animator>();
        if (animator == null)
        {
            animator = spawnedAsset.GetComponentInChildren<Animator>();
        }
        if (animator && animController == null)
        {
            animController = animator.runtimeAnimatorController;
        }
    }

    private bool IsOptimizedAnimator()
    {
        var i = GetAllImporters();
        if (i.Count > 0)
            return i.Any(q => q.optimizeGameObjects);
        return false;
    }

    private ModelImporter GetImporter(string p)
    {
        return ModelImporter.GetAtPath(p) as ModelImporter;
    }

    private List<ModelImporter> GetAllImporters()
    {
        List<ModelImporter> importers = new List<ModelImporter>();
        importers.Add(GetImporter(GetPrefabPath()));
        foreach (var mf in meshFilters)
        {
            if (mf && mf.sharedMesh)
            {
                importers.Add(GetImporter(AssetDatabase.GetAssetPath(mf.sharedMesh)));
            }
        }
        foreach (var sr in skinnedRenderers)
        {
            if (sr && sr.sharedMesh)
            {
                importers.Add(GetImporter(AssetDatabase.GetAssetPath(sr.sharedMesh)));
            }
        }
        importers.RemoveAll(q => q == null);
        importers = importers.Distinct().ToList();
        return importers;
    }

    private void SetChildFlags(Transform t, HideFlags flags)
    {
        Queue<Transform> q = new Queue<Transform>();
        q.Enqueue(t);
        for (int i = 0; i < t.childCount; i++)
        {
            Transform c = t.GetChild(i);
            q.Enqueue(c);
            SetChildFlags(c, flags);
        }
        while (q.Count > 0)
        {
            q.Dequeue().gameObject.hideFlags = flags;
        }
    }

    private UnityEditor.Animations.AnimatorController CreateBakeController()
    {
        // Creates the controller automatically containing all animation clips
        string tempPath = "Assets/TempBakeController.controller";
        var bakeName = AssetDatabase.GenerateUniqueAssetPath(tempPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(bakeName);
        var baseStateMachine = controller.layers[0].stateMachine;
        var clips = GetClips();
        foreach (var clip in clips)
        {
            var state = baseStateMachine.AddState(clip.name);
            state.motion = clip;
        }
        return controller;
    }

    private string FormatClipName(string name)
    {
        string badChars = "!@#$%%^&*()=+}{[]'\";:|";
        for (int i = 0; i < badChars.Length; i++)
        {
            name = name.Replace(badChars[i], '_');
        }
        return name;
    }

    private void CaptureBoneSnapShot(AnimationClip animClip, MotionFrameData motionFrameData, MotionFrameData lastMotionFrameData, GameObject sampleGO, float bakeFrames, float CurrentFrame)
    {
        float frameSkipsTimeStep = frameSkips[animClip.name] / (float)fps;
        float bakeDelta = CurrentFrame / bakeFrames;
        float animationTime = bakeDelta * animClip.length;
        if (requiresAnimator)
        {
            float normalizedTime = animationTime / animClip.length;
            animator.Play(animClip.name, 0, normalizedTime);
            animator.Update(frameSkipsTimeStep);
        }
        else
        {
            GameObject sampleObject = sampleGO;
            Animation legacyAnimation = sampleObject.GetComponentInChildren<Animation>();
            if (animator && animator.gameObject != sampleObject)
            {
                sampleObject = animator.gameObject;
            }
            else if (legacyAnimation && legacyAnimation.gameObject != sampleObject)
            {
                sampleObject = legacyAnimation.gameObject;
            }
            animClip.SampleAnimation(sampleObject, animationTime);
        }


        int index = 0;
        foreach (string boneName in bonesMap.Keys)
        {
            int boneIndex = bonesMap[boneName];
            Transform child = joints[boneIndex];
            MotionBoneData motionBoneData = motionFrameData.motionBoneDataList[index];
            motionBoneData.position = child.position;
            motionBoneData.localPosition = child.localPosition;
            motionBoneData.rotation = child.rotation;
            motionBoneData.localRotation = child.localRotation;
            motionBoneData.velocity = Vector3.zero;
            motionBoneData.boneName = child.name;
            motionBoneData.boneIndex = boneIndex;

            //calc velocity
            if (lastMotionFrameData != null)
            {
                MotionBoneData lastMotionBoneData = lastMotionFrameData.motionBoneDataList[index];      
                lastMotionBoneData.velocity = (motionBoneData.localPosition - lastMotionBoneData.localPosition) / frameSkipsTimeStep;
            }
            index++;
        }
    }

    private void CaptureTrajectorySnapShot(AnimationClip animClip, MotionFrameData motionFrameData, MotionFrameData lastMotionFrameData, GameObject sampleGO, float bakeFrames, float currentFrame)
    {
        float lastFrameTime = 0;
        for (int i = 0; i < predictionTrajectoryTimeList.Length; i++)
        {
            float PredictionTrajectoryTime = predictionTrajectoryTimeList[i];
            float bakeDelta = currentFrame / bakeFrames;
            EditorUtility.DisplayProgressBar("Baking Animation", string.Format("Processing: {0} Frame: {1}", animClip.name, i), bakeDelta);
            float animationTime = bakeDelta * animClip.length + (PredictionTrajectoryTime * fps / bakeFrames) * animClip.length;
            if (requiresAnimator)
            {
                float normalizedTime = animationTime / animClip.length;
                animator.Play(animClip.name, 0, normalizedTime);
                if (lastFrameTime == 0)
                {
                    float nextBakeDelta = Mathf.Clamp01(((float)(PredictionTrajectoryTime * fps / bakeFrames)));
                    float nextAnimationTime = nextBakeDelta * animClip.length;
                    lastFrameTime = animationTime - nextAnimationTime;
                }
                animator.Update(animationTime - lastFrameTime);
                lastFrameTime = animationTime;
            }
            else
            {
                GameObject sampleObject = sampleGO;
                Animation legacyAnimation = sampleObject.GetComponentInChildren<Animation>();
                if (animator && animator.gameObject != sampleObject)
                {
                    sampleObject = animator.gameObject;
                }
                else if (legacyAnimation && legacyAnimation.gameObject != sampleObject)
                {
                    sampleObject = legacyAnimation.gameObject;
                }
                animClip.SampleAnimation(sampleObject, animationTime);
            }
            MotionTrajectoryData motionTrajectoryData = motionFrameData.motionTrajectoryDataList[i];
            motionTrajectoryData.position = animator.transform.position;
            motionTrajectoryData.localPosition = motionTrajectoryData.position;
            motionTrajectoryData.direction = animator.transform.forward;
            //TODO
            motionTrajectoryData.velocity = Vector3.zero;
        }

        float currentClipTime = predictionTrajectoryTimeList[predictionTrajectoryTimeList.Length - 1];
        MotionTrajectoryData firstMotionTrajectoryData = motionFrameData.motionTrajectoryDataList[0];
        MotionTrajectoryData lastMotionTrajectoryData = motionFrameData.motionTrajectoryDataList[motionFrameData.motionTrajectoryDataList.Length - 1];   
        Vector3 offset = lastMotionTrajectoryData.position - firstMotionTrajectoryData.position;
        Vector3 velocity = offset / currentClipTime;
        motionFrameData.velocity = velocity.magnitude;
    }
}
