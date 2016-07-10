﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CodeGeneration;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Revenga.VSM
{
    [CustomEditor(typeof (ViewStateController), true)]
    public class ViewStateControllerInspector : Editor
    {
        private const string DirectorySeparatorChar = "/";

        public static ViewStateControllerInspector Instance;

        private ViewStateController _controller;
        private AnimatorController _animatorController;
        private Animator _animator;
        private VSMList _cachedVsmList;

        //Constants
        private const string ManagersListNamePrefix = "VSM_";
        private const string ManagerNamePrefix = "VSM_";

        private const string TagFunctionName = "StateTag";

        private string _rssFolder; // = "Assets"+ DirectorySeparatorChar + "Resources"+ DirectorySeparatorChar;
        private string _trashFolder; // = "Deleted" + DirectorySeparatorChar;
        private const string GeneratedFolder = "Generated" + DirectorySeparatorChar;
        private const string DataFolder = "VSM" + DirectorySeparatorChar;

        private string _dataAssetPath = "";

        protected void OnEnable()
        {
            Instance = this;

            if (!SetupNeeded())
            {
                _rssFolder = EditorPrefs.GetString("VSMCONFIG_RssFolder") + DirectorySeparatorChar;
                _trashFolder = EditorPrefs.GetString("VSMCONFIG_TrashFolder") + DirectorySeparatorChar;
            }

            _controller = target as ViewStateController;
            if (_controller == null)
            {
                Debug.Log("VSM: No ViewStateController component. Aborting");
                return;
            }
            _animator = _controller.GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = _controller.gameObject.AddComponent<Animator>();
                Debug.Log("VSM: No Animator component. Adding one...");
            }
            _animatorController = _animator.runtimeAnimatorController as AnimatorController;
            _controller.ParseData();

            if (_controller.VsmData != null)
            {
                _dataAssetPath = _rssFolder + DataFolder + _controller.VsmData.ListName + ".asset";
                
            }
            SetAllKeyframesToConstant();
            BakeAllStates();
            RefreshCachedData();
        }

        protected void OnDestroy()
        {
            Instance = null;
        }

        private void Initialize()
        {

            _controller.VsmList = new VSMList {ViewStateManagers = new List<VSMManager>()};

            CreateAnimatorControllerAsset();
            CreateDataAsset();
        }

        //================================
        // INSPECTOR GUI
        //================================

        public override void OnInspectorGUI()
        {
            //DrawDefaultInspector();
            GUILayout.Space(10f);
            if (SetupNeeded())
            {
                GUILayout.Label("You need to setup data paths first!");
                if(GUILayout.Button("Setup...")) VSMPreferencesWindow.Init();
                return;
            }

            if (_controller.VsmData != null)
            {

                if (!DrawViewStateManagers())
                {
                    GUILayout.Label("There are no any Managers for this Game Object. Click «Create New» or «Import»!");
                }
                else
                {
                    GUILayout.Space(10f);
                    if(GUILayout.Button("Regenerate Code")) RegenerateCode();
                }
                GUILayout.Space(10f);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create New")) CreateManager();
                //if (GUILayout.Button("Clone...")) CloneManagerFromFile();
                if (GUILayout.Button("Create from *.anim")) ImportManager();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("The system has not been initialized yet...");
                if (GUILayout.Button("Initialize")) Initialize();
            }
        }

        private bool SetupNeeded()
        {
            bool needSetup = !EditorPrefs.HasKey("VSMCONFIG_RssFolder") || !EditorPrefs.HasKey("VSMCONFIG_TrashFolder");

            return needSetup;
        }

        public bool DrawViewStateManagers()
        {
            if(_cachedVsmList == null || _cachedVsmList.ViewStateManagers.Count == 0) return false;
            
            for (int i = 0; i < _cachedVsmList.ViewStateManagers.Count; i++)
            {
                VSMManager currentManager = _cachedVsmList.ViewStateManagers[i];
                VSMManager originalManager = GetOriginalManager(currentManager);

                if (VSMEditorTools.DrawGroupHeader(originalManager.ManagerName, "VSMGroup_" + originalManager.ManagerName))
                {
                    VSMEditorTools.BeginGroupContents();
                    
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(VSMEditorTools.SpaceH);

                    GUILayout.Label("Manager:");
                    currentManager.ManagerName = EditorGUILayout.TextField(currentManager.ManagerName);

                    
                    if (currentManager.ManagerName != originalManager.ManagerName)
                    {
                        if (GUILayout.Button("Rename")) RenameManager(currentManager);
                    }

                    GUILayout.FlexibleSpace();

                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("X", GUILayout.Width(20f))) DeleteManager(currentManager);
                    GUI.backgroundColor = Color.white;

                    GUILayout.Space(VSMEditorTools.SpaceH);
                    EditorGUILayout.EndHorizontal();


                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(VSMEditorTools.SpaceH);
                    if (GUILayout.Button("Bake Manager SwitchIntoState")) BakeManagerStates(originalManager);
                    GUILayout.Space(VSMEditorTools.SpaceH);
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(VSMEditorTools.SpaceH*2);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(VSMEditorTools.SpaceH);
                    EditorGUILayout.BeginVertical();
                    DrawStates(currentManager,originalManager);
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(VSMEditorTools.SpaceH);
                    EditorGUILayout.EndHorizontal();

                    VSMEditorTools.EndGroupContents();
                    GUILayout.Space(VSMEditorTools.SpaceH*2);
                }
                
            }
            return true;
        }

        private void DrawStates(VSMManager currentManager, VSMManager originalManager)
        {
            if (currentManager.States == null) currentManager.States = new List<VSMState>();

            if (currentManager.States.Count > 0)
            {
                for (int i = 0; i < currentManager.States.Count; i++)
                {

                    VSMState currentState = currentManager.States[i];
                    VSMState originalState = originalManager.States.FirstOrDefault(x => x.Id == currentState.Id);

                    GUI.backgroundColor = Color.gray;
                    VSMEditorTools.BeginGroupContents();

                    EditorGUILayout.BeginHorizontal();

                    bool isDefaultState = originalState.StateName == originalManager.DefaultStateName;
                    bool makeDefault = GUILayout.Toggle(isDefaultState, "");
                    if (!isDefaultState && makeDefault)
                    {
                        originalManager.DefaultStateName = originalState.StateName;
                        UpdateDataAsset();
                    }
                    GUILayout.Space(VSMEditorTools.SpaceH*2);
                    
                    GUILayout.Label("State:");
                    currentState.StateName = EditorGUILayout.TextField(currentState.StateName, GUILayout.MinWidth(100f));

                    if (currentState.StateName != originalState.StateName)
                    {
                        GUI.backgroundColor = Color.white;
                        if (GUILayout.Button("Rename", GUILayout.MaxWidth(80f)))
                            RenameState(currentManager, originalManager, currentState);
                        GUI.backgroundColor = Color.gray;
                    }

                    GUILayout.FlexibleSpace();
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("X", GUILayout.Width(20f))) DeleteState(originalManager, originalManager.States[i]);
                    GUI.backgroundColor = Color.white;


                    EditorGUILayout.EndHorizontal();

                    VSMEditorTools.EndGroupContents();
                    GUI.backgroundColor = Color.white;
                    //GUILayout.Space(VSMEditorTools.SpaceH);
                }
            }
            else
            {
                GUILayout.Label("There is no states in this manager...");
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New State")) CreateNewState(currentManager, originalManager);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(VSMEditorTools.SpaceH);
        }

   

        //================================
        // ANIMATOR OPERATIONS
        //================================
        private void SetAllKeyframesToConstant()
        {
            foreach (var animationClip in AnimationUtility.GetAnimationClips(_controller.gameObject))
            {
                List<EditorCurveBinding> bindings = AnimationUtility.GetCurveBindings(animationClip).ToList();

                foreach (EditorCurveBinding binding in bindings)
                {
                    var curve = AnimationUtility.GetEditorCurve(animationClip, binding);
                    List<Keyframe> oldKeyframes = curve.keys.ToList();
                    List<Keyframe> newKeyframes = new List<Keyframe>();
                    foreach (Keyframe key in oldKeyframes)
                    {
                        var k = new Keyframe(key.time, key.value, float.PositiveInfinity, float.PositiveInfinity);
                        k.tangentMode = 0;
                        newKeyframes.Add(k);
                    }

                    curve.keys = newKeyframes.ToArray();
                    AnimationUtility.SetEditorCurve(animationClip, binding,curve);
                }
            }
        }
        private void CreateAnimatorControllerAsset()
        {
            // Create AnimationController if there is no any attached to Animator component
            if (_animatorController == null)
            {
                var animatorControllerName = ManagerNamePrefix+StripStringForFileName(_controller.gameObject.name);
                if (!Directory.Exists(_rssFolder + DataFolder))
                {
                    AssetDatabase.CreateFolder(_rssFolder.TrimEnd('/'), DataFolder.TrimEnd('/'));
                    AssetDatabase.SaveAssets();
                }
                _animatorController = AnimatorController.CreateAnimatorControllerAtPath(_rssFolder + DataFolder + animatorControllerName + ".controller");
                _animator.runtimeAnimatorController = _animatorController;
                Debug.Log("VSM: Created AnimatorController and added link to Animator component..");
            }

            // Create speed parameter if there is no any
            if (_animatorController.parameters.All(x => x.name != ViewStateController.VsmAnimatorSpeedParamName))
            {
                AnimatorControllerParameter acSpeedParam = new AnimatorControllerParameter();
                acSpeedParam.name = ViewStateController.VsmAnimatorSpeedParamName;
                acSpeedParam.defaultFloat = 0.0f;
                acSpeedParam.type = AnimatorControllerParameterType.Float;

                _animatorController.AddParameter(acSpeedParam);
                Debug.Log("VSM: Added Speed parameter for frame jumping..");
            }
        }
        private void CreateAnimatorLayer(VSMManager currentManager, AnimationClip clip = null)
        {
            // If there is no layer for that manager - create it.
            AnimatorControllerLayer al = new AnimatorControllerLayer();
            al.name = currentManager.ManagerName;
            al.stateMachine = new AnimatorStateMachine();
            al.stateMachine.name = al.stateMachine.MakeUniqueStateMachineName(currentManager.ManagerName);
            al.blendingMode = AnimatorLayerBlendingMode.Override;
            al.defaultWeight = 1f;
            _animatorController.AddLayer(al);
            currentManager.LayerIndex = _animatorController.layers.Length-1;
                
            // Create Animation clip for that manager or use one from params
            if (clip == null)
            {
                AnimationClip animationClip = new AnimationClip();
                animationClip.frameRate = 10f;
                animationClip.wrapMode = WrapMode.ClampForever;
                animationClip.name = currentManager.ManagerName;
                string animationClipAssetPath = _rssFolder + DataFolder + animationClip.name + ".anim";
                AssetDatabase.CreateAsset(animationClip, animationClipAssetPath);
                AssetDatabase.ImportAsset(animationClipAssetPath);
            }

            if (AssetDatabase.GetAssetPath(_animatorController) != "")
                AssetDatabase.AddObjectToAsset(al.stateMachine, AssetDatabase.GetAssetPath(_animatorController));
        }

        private AnimationClip LoadManagerAnimationClip(VSMManager currentManager)
        {
            AnimationClip ac =AssetDatabase.LoadAssetAtPath<AnimationClip>(_rssFolder + DataFolder + currentManager.ManagerName + ".anim");
            if (ac == null)
            {
                Debug.LogError(string.Format("VSM: Error: Can not load animation clip for manager '{0}' at path '{1}'",currentManager.ManagerName, _rssFolder + DataFolder + currentManager.ManagerName + ".anim"));
            }
            return ac;
        }

        private void CreateAnimatorState(VSMManager currentManager, VSMState currentState)
        {
            // Check if there is an animator layer fot that manager and the index is right..
            if (currentManager.LayerIndex < 0 || currentManager.LayerIndex >= _animatorController.layers.Length || _animatorController.layers[currentManager.LayerIndex].name != currentManager.ManagerName)
            {
                Debug.LogError(string.Format("VSM: Error: Can not find animator layer by stored index and name {0}.\n{1}\n{2}\n Trying to find by name...",currentManager.ManagerName, currentManager.LayerIndex, _animatorController.layers.Length));
            }

            // If layer on stored index has a different name try to find it by name in layers array
            AnimatorControllerLayer currentLayer = GetManagerAnimationLayer(currentManager, _animatorController.layers);

            // Create state if there is no any.
            AnimatorState animatorState = new AnimatorState();
            animatorState.name = currentState.StateName;
            animatorState.motion = LoadManagerAnimationClip(currentManager);
            animatorState.speed = 1f;
            animatorState.speedParameterActive = true;
            animatorState.speedParameter = ViewStateController.VsmAnimatorSpeedParamName;

            // Check if is there a state tag in the animetion clip
            AnimationEvent newTag = (animatorState.motion as AnimationClip).events.FirstOrDefault(x => x.functionName == TagFunctionName && x.stringParameter == currentState.StateName);

            if (newTag == null)
            {
                // Add clip tag for state if tereis no any.
                newTag = new AnimationEvent();
                newTag.functionName = TagFunctionName;
                newTag.time = GetNewTagTime(animatorState.motion as AnimationClip, true);
                newTag.stringParameter = currentState.StateName;
                //Animation
                var newEventsList = (animatorState.motion as AnimationClip).events.ToList();
                newEventsList.Add(newTag);
                AnimationUtility.SetAnimationEvents((animatorState.motion as AnimationClip), newEventsList.ToArray());
            }
            else
            {
                Debug.LogWarning(string.Format("VSM: Warning: Found clip tag with name '{0}' on time {1}. Updating VSMState {2}",newTag.stringParameter, newTag.time, currentState.StateName));
            }

            currentState.Time = newTag.time;

            currentLayer.stateMachine.AddState(animatorState, new Vector3(200, 70 * currentLayer.stateMachine.states.Length));
            var transition = currentLayer.stateMachine.AddAnyStateTransition(animatorState);
            transition.hasExitTime = true;
            transition.duration = 0;
            transition.exitTime = 0;

            
            if (AssetDatabase.GetAssetPath(animatorState) == "")
                AssetDatabase.AddObjectToAsset(animatorState,_animatorController);

            UpdateDataAsset();
        }
        private void DeleteAnimatorState(VSMManager currentManager, VSMState state)
        {
            AnimatorControllerLayer animationLayer = GetManagerAnimationLayer(currentManager, _animatorController.layers);

            AnimatorState animatorState = animationLayer.stateMachine.states.FirstOrDefault(x => x.state.name == state.StateName).state;
            if (animatorState == null)
            {
                return;
            }

            // Get curve bindings to objects and get curve keyframes
            AnimationClip ac = animatorState.motion as AnimationClip;

            // Delete state tag
            List<AnimationEvent> stateTags = ac.events.ToList();
            stateTags.Remove(stateTags.FirstOrDefault(x => x.functionName == TagFunctionName && x.stringParameter == state.StateName));

            //Set events array back to animation clip
            AnimationUtility.SetAnimationEvents(ac, stateTags.ToArray());

            // Delete state from Animator Controller asset
            animationLayer.stateMachine.RemoveState(animatorState);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_animatorController));
            
        }
        private void DeleteAnimatorLayer(VSMManager originalManager)
        {
            AnimatorControllerLayer[] animationLayers = _animatorController.layers;

            AnimatorControllerLayer animationLayer = GetManagerAnimationLayer(originalManager, animationLayers);

            // Delete states from Animator Controller asset
            foreach (var childAnimatorState in animationLayer.stateMachine.states)
            {
                AnimatorState state = childAnimatorState.state;
                //DestroyImmediate(state);
                animationLayer.stateMachine.RemoveState(state);
                //Destroy(state);
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_animatorController));
            }

            // Delete state machine from Animator Controller asset
            var sm = animationLayer.stateMachine;
            animationLayer.stateMachine = null;

            var animationLayersList = animationLayers.ToList();
            animationLayersList.Remove(animationLayer);
            _animatorController.layers = animationLayersList.ToArray();

            AssetDatabase.SaveAssets();

            DestroyImmediate(sm,true);

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_animatorController));
        }
        private void RenameAnimationLayer(VSMManager originalManager, VSMManager currentManager)
        {
            AnimatorControllerLayer[] animationLayers = _animatorController.layers;

            AnimatorControllerLayer animationLayer = GetManagerAnimationLayer(originalManager, animationLayers);

            // Rename state machine in Animator Controller asset
            AssetDatabase.ClearLabels(animationLayer.stateMachine);
            animationLayer.stateMachine.name = currentManager.ManagerName;
            AssetDatabase.SetLabels(animationLayer.stateMachine, new string[] { currentManager.ManagerName });
            
            animationLayer.name = currentManager.ManagerName;

            _animatorController.layers = animationLayers;

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_animatorController));
        }
        private void RenameAnimatorState(VSMManager currentManager, VSMManager originalManager, VSMState currentState)
        {
            AnimatorControllerLayer animationLayer = GetManagerAnimationLayer(originalManager, _animatorController.layers);

            VSMState originalState = originalManager.States.FirstOrDefault(x => x.Id == currentState.Id);
            if (originalState == null)
            {
                Debug.LogError(string.Format("VSM: Error! Can not find original state '{0}'(id={1}) in manager '{2}'","", currentState.Id, originalManager.ManagerName));
                return;
            }

            AnimatorState animatorState = animationLayer.stateMachine.states.FirstOrDefault(x => x.state.name == originalState.StateName).state;
            if (animatorState == null)
            {
                return;
            }
            
            // Get Animation clip for current manager
            AnimationClip ac = animatorState.motion as AnimationClip;

            // Rename state tag
            List<AnimationEvent> stateTags = ac.events.ToList();
            AnimationEvent e = stateTags.FirstOrDefault(x => x.functionName == TagFunctionName && x.stringParameter == originalState.StateName);
            if (e == null)
            {
                Debug.LogError(string.Format("VSM: Error! Can not find state tag '{0}' in animation '{1}'", "", currentState.StateName, ac.name));
                return;
            }
            e.stringParameter = currentState.StateName;

            //Set events array back to animation clip
            AnimationUtility.SetAnimationEvents(ac, stateTags.ToArray());

            // Rename state in Animator Controller asset
            animatorState.name = currentState.StateName;
            
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_animatorController));
        }
        private int GetStateKeyframeIndex(AnimationClip clip, VSMState state)
        {
            AnimationEvent e =clip.events.FirstOrDefault(x => x.stringParameter == state.StateName && x.functionName == TagFunctionName);
            if (e == null) return -1;
            return clip.events.ToList().IndexOf(e);
        }
        private float GetNewTagTime(AnimationClip animationClip, bool afterKeys)
        {
            if (afterKeys)
            {
                var stateTimeLength = 1 / animationClip.frameRate;
                var length = 0f;

                if (AnimationUtility.GetCurveBindings(animationClip).Length > 0)
                {
                    length = animationClip.length + stateTimeLength;
                }
                else if (animationClip.events.Length != 0)
                {
                    var tags = animationClip.events.Where(x => x.functionName == TagFunctionName).ToArray();
                    var lastEventTime = tags[tags.Length - 1].time;
                    length = lastEventTime + stateTimeLength;
                }

                return length;
            }
            else
            {
                if (animationClip.events.Length == 0) return 0f;
                var tags = animationClip.events.Where(x => x.functionName == TagFunctionName).ToArray();
                var lastEventTime = tags[tags.Length - 1].time;
                return lastEventTime + (1/animationClip.frameRate);
            }
        }
        private void ParseStateKeyframes(VSMManager currentManager, VSMState state)
        {
            AnimatorControllerLayer animationLayer = _animatorController.layers.FirstOrDefault(x => x.name == currentManager.ManagerName);
            if (animationLayer == null)
            {
                return;
            }

            AnimatorState animatorState = animationLayer.stateMachine.states.FirstOrDefault(x => x.state.name == state.StateName).state;
            if (animatorState == null)
            {
                return;
            }

            // Get curve bindings to objects and get curve keyframes
            AnimationClip ac = animatorState.motion as AnimationClip;
            List<EditorCurveBinding> floatCurveBindingsList = AnimationUtility.GetCurveBindings(ac).ToList();
            //List<EditorCurveBinding> objectCurveBindingsList = AnimationUtility.GetObjectReferenceCurveBindings(ac).ToList();

            //List<Keyframe> stateKeyframes = new List<Keyframe>();

            state.Properties = new List<VSMStateProperty>();

            for (int i = 0; i < floatCurveBindingsList.Count; i++)
            {
                EditorCurveBinding binding = floatCurveBindingsList[i];
                
                AnimationCurve curve = AnimationUtility.GetEditorCurve(ac, binding);
                
                if (curve.keys.Any(x=>Math.Abs(x.time - state.Time) < float.Epsilon))
                {
                    var propertyName = binding.propertyName.TrimStart('m', '_');
                    propertyName = propertyName.Substring(0, propertyName.IndexOf(".", StringComparison.Ordinal));

                    var property = state.Properties.FirstOrDefault(p => p.N == propertyName) ?? new VSMStateProperty {P = binding.path};

                    Keyframe stateKey = curve.keys.FirstOrDefault(x => Math.Abs(x.time - state.Time) < float.Epsilon);

                    //Check if property is a struct
                    if (binding.propertyName.Contains("."))
                    {
                        UnityEngine.Object targetObj = AnimationUtility.GetAnimatedObject(_controller.gameObject, binding);
                        
                        var fieldName = binding.propertyName.Substring(binding.propertyName.IndexOf(".", StringComparison.Ordinal) + 1);
                        var tmpName = char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
                        if (tmpName == "localEulerAnglesRaw") tmpName = "eulerAngles";
                        property.N = tmpName;
                        
                        var existingProperty = state.Properties.FirstOrDefault(x => x.N == property.N);
                        if (existingProperty != null) property = existingProperty;

                        Type componentType = targetObj.GetType();
                        MemberInfo member = componentType.GetMember(tmpName).FirstOrDefault();
                        if (member == null)
                        {
                            Debug.LogError("Member not found" + propertyName);
                            continue;
                        }

                        string typeName;
                        Type type;

                        if (member.MemberType == MemberTypes.Property)
                        {
                            type = ((PropertyInfo)member).PropertyType;
                            typeName = type.Name;
                        }
                        else
                        {
                            type = ((FieldInfo)member).FieldType;
                            typeName = type.Name;
                        }
                            
                        switch (typeName)
                        {
                            case "Vector2":
                                property.O = CreateInstanceByType<Vector2>(property.O, type, fieldName, stateKey.value);
                                break;

                            case "Vector3":
                                property.O = CreateInstanceByType<Vector3>(property.O, type, fieldName, stateKey.value);
                                break;

                            case "Vector4":
                                property.O = CreateInstanceByType<Vector4>(property.O, type, fieldName, stateKey.value);
                                break;

                            case "Rect":
                                property.O = CreateInstanceByType<Rect>(property.O, type, fieldName, stateKey.value);
                                break;

                            case "RectOffset":
                                property.O = CreateInstanceByType<Rect>(property.O, type, fieldName, stateKey.value);
                                break;

                            case "Color":
                                property.O = CreateInstanceByType<Color>(property.O, type, fieldName, stateKey.value);
                                break;

                            default:
                                Debug.LogError(typeName + " not suported. Skiping...");
                                continue;
                        }
                           
                    }
                    else
                    {
                        property.N = binding.propertyName;
                        property.O = new Vector4(stateKey.value, 0);
                    }
                    
                    property.C = binding.type.ToString();

                    if (state.Properties.All(p => p.N != property.N))
                    {
                        state.Properties.Add(property);
                    }
                }
            }
        }

        private static Vector4 CreateInstanceByType<T>(object obj, Type myType, string fieldName, float value)
        {
            if(obj == null) obj = (T)Activator.CreateInstance(myType);
            var info = myType.GetField(fieldName);
            info.SetValue(obj, value);

            if (typeof(T) == typeof(Rect))
            {
                var rect = (Rect) obj;
                return new Vector4(rect.x, rect.y, rect.width, rect.height);
            }

            if (typeof (T) == typeof (RectOffset))
            {
                var rectOffset = (RectOffset)obj;
                return new Vector4(rectOffset.left, rectOffset.right, rectOffset.top, rectOffset.bottom);
            }

            return (Vector4)obj;
        }

        private AnimatorControllerLayer GetManagerAnimationLayer(VSMManager manager, AnimatorControllerLayer[] animationLayers)
        {
            AnimatorControllerLayer animationLayer = animationLayers.FirstOrDefault(x => x.name == manager.ManagerName);
            if (animationLayer == null)
            {
                Debug.LogError("VSM: Can not find Animation layer with name: " + manager.ManagerName);
                return null;
            }
            return animationLayer;
        }

        private void UpdateAllStatemachineLayouts()
        {
            if(_controller.VsmList==null || _controller.VsmList.ViewStateManagers.Count==0) return;

            AnimatorControllerLayer[] layers = _animatorController.layers;

            for (int i = 0; i < layers.Length; i++)
            {
                AnimatorControllerLayer layer = layers[i];
                UnityEditor.Animations.AnimatorStateMachine sm = layer.stateMachine;
                ChildAnimatorState[] childStates = sm.states;
                for (int j = 0; j < childStates.Length; j++)
                {
                    ChildAnimatorState state = childStates[j];
                    childStates[j] = new ChildAnimatorState()
                    {
                        state = state.state,
                        position = new Vector3(400, 70*j)
                    };

                }
                sm.states = childStates;
                layer.stateMachine = sm;
                _animatorController.layers = layers;
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_animatorController));
            }
        }
        
        //==============================
        // MANAGER OPERATIONS
        //==============================

        private VSMManager CreateManager(AnimationClip clip = null)
        {
            if (_controller.VsmList == null)
            {
                _controller.VsmList = new VSMList();
                _controller.VsmList.ViewStateManagers = new List<VSMManager>();
            }
            VSMManager vsmManager = new VSMManager();

            if (string.IsNullOrEmpty(vsmManager.Id))
                vsmManager.Id = Guid.NewGuid().ToString();

            vsmManager.States = new List<VSMState>();
            vsmManager.ManagerName = ManagerNamePrefix + StripStringForFileName(_controller.gameObject.name) + "_" + GetTimestamp();
            _controller.VsmList.ViewStateManagers.Add(vsmManager);

            CreateAnimatorLayer(vsmManager,clip);
            UpdateDataAsset();
            return vsmManager;
        }

        private void DeleteManager(VSMManager currentManager)
        {
            if (
                !EditorUtility.DisplayDialog("VSM",
                    "Are you sure you want to delete this manager? Operation is NOT Undoable!", "Delete forever",
                    "NOOOO!")) return;

            if (_controller.VsmList == null || _controller.VsmList.ViewStateManagers.Count == 0)
            {
                Debug.LogError(string.Format("VSM: Error: Can not delete manager {0} with id {1} because VSMList in null or empty!", currentManager.ManagerName, currentManager.Id));
                return;
            }

            VSMManager originalManager =_controller.VsmList.ViewStateManagers.FirstOrDefault(x => x.Id == currentManager.Id);
            if (originalManager == null)
            {
                Debug.LogError(string.Format("VSM: Error: Can not find manager {0} with id {1} in VSMList!", currentManager.ManagerName, currentManager.Id));
                return;
            }

            DeleteAnimatorLayer(originalManager);

            if (!AssetDatabase.IsValidFolder(_trashFolder + DataFolder.TrimEnd('/')))
            {
                Debug.Log("VSM: Creating Trash folder: " + originalManager.ManagerName);
                AssetDatabase.CreateFolder(_trashFolder.TrimEnd('/'), DataFolder.TrimEnd('/'));
            }
            var resultMoveAsset = AssetDatabase.MoveAsset(_rssFolder + DataFolder + originalManager.ManagerName + ".anim", _trashFolder + DataFolder + originalManager.ManagerName + "_" + GetTimestamp() + ".anim");
            if (!string.IsNullOrEmpty(resultMoveAsset))
            {
                Debug.LogError("VSM: Error moving asset to trash:" + resultMoveAsset);
            }

            var wasDeleted = _controller.VsmList.ViewStateManagers.Remove(originalManager);
            if(!wasDeleted)
                Debug.LogError("VSM: Manager was not deleted from collection..");

            UpdateDataAsset();
        }
        private void RenameManager(VSMManager currentManager)
        {
            if (_controller.VsmList == null || _controller.VsmList.ViewStateManagers.Count == 0)
            {
                Debug.LogError(string.Format("VSM: Error: Can not rename manager {0} with id {1} because VSMList in null or empty!", currentManager.ManagerName, currentManager.Id));
                return;
            }

            VSMManager originalManager = _controller.VsmList.ViewStateManagers.FirstOrDefault(x => x.Id == currentManager.Id);

            if (originalManager == null)
            {
                Debug.LogError(string.Format("VSM: Error: Can not find manager {0} with id {1} in VSMList!", currentManager.ManagerName, currentManager.Id));
                return;
            }

            if (_controller.VsmList.ViewStateManagers.Any(x => x.ManagerName == currentManager.ManagerName))
            {
                EditorUtility.DisplayDialog("VSM", string.Format("Error! Manager with name '{0}' already exists!", currentManager.ManagerName), "Ok");
                currentManager.ManagerName = originalManager.ManagerName;
                return;
            }

            RenameAnimationLayer(originalManager, currentManager);
            AssetDatabase.RenameAsset(_rssFolder + DataFolder + originalManager.ManagerName + ".anim", currentManager.ManagerName);

            if (currentManager.ManagerName != originalManager.ManagerName)
            {
                originalManager.ManagerName = currentManager.ManagerName;
            }

            UpdateDataAsset();
        }

        private void ImportManager()
        {
            var animPath = EditorUtility.OpenFilePanel("Select Animation Clip...", "Assets", "anim");
            if (!string.IsNullOrEmpty(animPath))
            {
                if (animPath.StartsWith(Application.dataPath))
                {
                    animPath = "Assets" + animPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    Debug.LogError(string.Format("VSM: Error: Can not load asset at path {0}", animPath));
                    return;
                }

                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
                if (clip == null)
                {
                    Debug.LogError(string.Format("VSM: Error: Can not load asset at path {0}", animPath));
                    return;
                }
                VSMManager manager = CreateManager(clip);

                AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(clip), _rssFolder + DataFolder + manager.ManagerName + ".anim");
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(_rssFolder + DataFolder + manager.ManagerName + ".anim");
                
                // Create states from clip
                AnimationEvent[] stateTags = clip.events.Where(x=>x.functionName == TagFunctionName).ToArray();

                for (int i = 0; i < stateTags.Length; i++)
                {
                    VSMState vsmState = new VSMState();
                    if (string.IsNullOrEmpty(vsmState.Id))
                        vsmState.Id = Guid.NewGuid().ToString();
                    vsmState.StateName = stateTags[i].stringParameter;
                    manager.States.Add(vsmState);

                    CreateAnimatorState(manager, vsmState);
                }
                UpdateDataAsset();
            }
        }

        private void BakeAllStates()
        {
            if (_controller.VsmList == null) return;

            foreach (VSMManager manager in _controller.VsmList.ViewStateManagers)
            {
                BakeManagerStates(manager);
            }
        }

        private void BakeManagerStates(VSMManager curentManager)
        {
            foreach (VSMState state in curentManager.States)
            {
                ParseStateKeyframes(curentManager, state);
            }

            UpdateDataAsset();
        }

        private void UpdateAllStateTimings()
        {
            if (_controller.VsmList == null) return;
            
            foreach (VSMManager manager in _controller.VsmList.ViewStateManagers)
            {
                AnimatorControllerLayer animationLayer = GetManagerAnimationLayer(manager, _animatorController.layers);

                foreach (var state in manager.States)
                {
                    AnimatorState animatorState = animationLayer.stateMachine.states.FirstOrDefault(x => x.state.name == state.StateName).state;
                    if (animatorState == null)
                    {
                        return;
                    }

                    // Get Animation clip for current manager
                    AnimationClip ac = animatorState.motion as AnimationClip;

                    var e = ac.events.FirstOrDefault(x => x.stringParameter == state.StateName);
                    if(e==null) continue;

                    state.Time = e.time;
                }
            }
        }

        private VSMManager GetOriginalManager(VSMManager manager)
        {
            VSMManager originalManager = _controller.VsmList.ViewStateManagers.FirstOrDefault(x => x.Id == manager.Id);

            if (originalManager == null)
            {
                Debug.LogError(string.Format("VSM: Error: Can not find manager {0} with id {1} in VSMList!", manager.ManagerName, manager.Id));
                return null;
            }
            return originalManager;
        }

        //==============================
        // STATES OPERATIONS
        //==============================

        private void CreateNewState(VSMManager currentManager, VSMManager originalManager)
        {
            VSMState vsmState = new VSMState();
            if (string.IsNullOrEmpty(vsmState.Id))
                vsmState.Id = Guid.NewGuid().ToString();
            vsmState.StateName = "State_" + GetTimestamp();
            originalManager.States.Add(vsmState);

            CreateAnimatorState(originalManager, vsmState);
            UpdateDataAsset();
        }
        private void CloneState(VSMManager currentManager, VSMManager originalManager, int stateToCloneIndex)
        {
            throw new NotImplementedException();
        }
        private void DeleteState(VSMManager currentManager, VSMState vsmState)
        {
            DeleteAnimatorState(currentManager,vsmState);
            currentManager.States.Remove(vsmState);

            UpdateDataAsset();
        }
        private void RenameState(VSMManager currentManager, VSMManager originalManager, VSMState currentState)
        {
            var originalState = originalManager.States.FirstOrDefault(x => x.Id == currentState.Id);
            if (originalState == null)
            {
                Debug.LogError(string.Format("VSM: Error! Can not find state '{0}'(id={1}) in the original state manager '{2}'", currentState.StateName, currentState.Id, originalManager.ManagerName));
                return;
            }

            if (originalManager.States.Any(x => x.StateName == currentState.StateName))
            {
                EditorUtility.DisplayDialog("VSM",
                    string.Format("State '{0}' already exists in '{1}'!", currentState.StateName,
                        originalManager.ManagerName), "Ok");
                currentState.StateName = originalState.StateName;
                return;
            }

            RenameAnimatorState(currentManager, originalManager, currentState);

            

            originalState.StateName = currentState.StateName;

            UpdateDataAsset();
        }

        //==============================
        // DATA ASSET OPERATIONS
        //==============================

        private void CreateDataAsset()
        {
            Debug.Log("VSM: CreateDataAsset()");
            if (_controller.VsmData != null)
            {
                _dataAssetPath = _rssFolder + DataFolder + _controller.VsmData.ListName + ".asset";
                return;
            }

            _controller.VsmData = CreateInstance<VSMData>();
            _controller.VsmData.ListName = ManagersListNamePrefix + StripStringForFileName(_controller.gameObject.name);

            _dataAssetPath = _rssFolder + DataFolder + _controller.VsmData.ListName + ".asset";

            AssetDatabase.CreateAsset(_controller.VsmData, _dataAssetPath);
            AssetDatabase.ImportAsset(_dataAssetPath, ImportAssetOptions.ForceUncompressedImport);
            _controller.VsmData = AssetDatabase.LoadAssetAtPath<VSMData>(_dataAssetPath);

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();

            Debug.Log("VSM: Created VSMData asset with name " + ManagersListNamePrefix + StripStringForFileName(_controller.gameObject.name) + ".asset");

        }
        private void UpdateDataAsset()
        {
            if(_controller.VsmList==null) return;

            _controller.VsmData = AssetDatabase.LoadAssetAtPath<VSMData>(_dataAssetPath);
            if (_controller.VsmData == null)
            {
                Debug.LogError("VSM: Error loading data asset from:" + _dataAssetPath);
                return;
            }

            SetAllKeyframesToConstant();
            UpdateAllStateTimings();
            UpdateAllStatemachineLayouts();

            _controller.VsmData.Data = JsonUtility.ToJson(_controller.VsmList);
            
            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(_controller.VsmData);
            RefreshCachedData();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        protected void RefreshCachedData()
        {
            if (_controller.VsmData == null || _controller.VsmList == null)
            {
                Debug.LogWarning("VSM: Can't build Serialized object for inspector because VSMData or VSMList is null...");
                return;
            }

            _cachedVsmList = JsonUtility.FromJson<VSMList>(JsonUtility.ToJson(_controller.VsmList));

            Repaint();
        }

        //==============================
        // UTILS
        //==============================

        private int GetTimestamp()
        {
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (int)(DateTime.UtcNow - epochStart).TotalSeconds;
        }

        private string StripStringForFileName(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            return s.Replace(" ", "_").Replace(",", "_");
        }

        private void RegenerateCode()
        {
            if(SetupNeeded()) return;

            if (_controller.VsmData == null || _controller.VsmList == null || _controller.VsmList.ViewStateManagers.Count == 0) return;

            string localPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(_controller)))+DirectorySeparatorChar;

            if (!AssetDatabase.IsValidFolder(localPath + GeneratedFolder.TrimEnd('/')))
            {
                AssetDatabase.CreateFolder(localPath, GeneratedFolder.TrimEnd('/'));
            }
            
            GeneratorEnums generator = new GeneratorEnums();
            generator.Session = new Dictionary<string, object>();
            generator.Session["m_ClassName"] = _controller.VsmData.ListName;
            generator.Session["m_Enums"] =
                _controller.VsmList.ViewStateManagers.Where(x => x.States.Count > 0)
                    .ToDictionary(manager => manager.ManagerName,
                        x => x.States.Select(stete => stete.StateName).ToArray());

            generator.Initialize();

            string result = generator.TransformText();

            var path = Path.GetFullPath(localPath + GeneratedFolder + _controller.VsmData.ListName + ".cs");
            File.WriteAllText(path, result);

            AssetDatabase.ImportAsset(localPath + GeneratedFolder + _controller.VsmData.ListName + ".cs");
            AssetDatabase.Refresh();
        }
    }
}
