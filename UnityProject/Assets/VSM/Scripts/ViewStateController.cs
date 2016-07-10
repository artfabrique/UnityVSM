using System;
using System.Collections.Generic;
using System.Linq;
using Assets.VSM.Scripts;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

namespace Revenga.VSM
{
    [Serializable]
    public class ViewStateController : MonoBehaviour
    {
        public const string VsmAnimatorSpeedParamName = "VSM_FrameJumper";

        [SerializeField] public VSMData VsmData;
        [NonSerialized] public VSMList VsmList;
        
        public DelayedSwitchData _delayedData;
        private bool _initialized;
        private Animator _animatorRef;
        private bool _wasEnabled;

        private readonly Dictionary<string, Component> Components = new Dictionary<string, Component>(); 

        public void SwitchTo(string managerName, string stateName, bool forceUpdate = true)
        {
            // Check for errors

            if (!_initialized)
            {
                Debug.LogError(string.Format(VSMError.VSMNotInitialized, managerName, stateName));
                return;
            }

            if (VsmList.ViewStateManagers.All(x => x.ManagerName != managerName))
            {
                Debug.LogError(string.Format(VSMError.StateManagerNotFound, managerName, stateName));
                return;
            }

            VSMManager vsmManager = VsmList.ViewStateManagers.FirstOrDefault(x => x.ManagerName == managerName);
            if (vsmManager != null)
            {
                VSMState targetState = vsmManager.States.FirstOrDefault(x => x.StateName == stateName);
                if (targetState == null)
                {
                    Debug.LogError(string.Format(VSMError.StateNotFound, managerName, stateName));
                    return;
                }

                if (!_animatorRef)
                {
                    Debug.LogError(string.Format(VSMError.CanNotFindAnimator, managerName, stateName));
                    return;
                }

                //Cache call data if GameObject is not active or was not enabled yet.

                if (!gameObject.activeInHierarchy || !gameObject.activeSelf || !_wasEnabled)
                {
                    Debug.LogWarning(string.Format(VSMError.AnimatiorDisabled, managerName, stateName));
                    if (_delayedData == null) _delayedData = new DelayedSwitchData();
                    _delayedData.Manager = managerName;
                    _delayedData.State = stateName;
                    _delayedData.ForceUpdate = forceUpdate;
                }
                else
                {
                    _delayedData = null;

                    if (vsmManager.CurrentStateName == stateName && !forceUpdate) return;

                    if (_animatorRef.parameters.All(x => x.name != "VSM_" + managerName))
                    {
                        Debug.LogError(string.Format(VSMError.CanNotFindSpeedParameter, managerName, stateName));
                        return;
                    }

                    _animatorRef.SetFloat("VSM_" + managerName, 1f);
                    _animatorRef.PlayInFixedTime(stateName, vsmManager.LayerIndex, targetState.Time);
                    _animatorRef.Update(Time.time);
                    _animatorRef.SetFloat("VSM_" + managerName, 0f);

                    vsmManager.CurrentStateName = stateName;

                    // Uncoment if you use NGUI
                    /*
                    if (gameObject.GetComponent<UIWidget>() != null)
                    {
                        gameObject.GetComponent<UIWidget>().SetDirty();
                    }
                    */

                    VsmList.ViewStateManagers[VsmList.ViewStateManagers.FindIndex(x => x.ManagerName == managerName)] =
                        vsmManager;
                }
            }
        }

        public void ParseData()
        {
            if (VsmData != null && !string.IsNullOrEmpty(VsmData.Data))
            {
                VsmList = JsonUtility.FromJson<VSMList>(VsmData.Data);

                /*
                if (VsmList!=null && VsmList.ViewStateManagers!=null)
                    Debug.Log(String.Format(VSMMessage.DataParsedSuccessfully, gameObject.name,VsmList.ViewStateManagers.Count));
                */
            }
            else
            {
                Debug.LogWarning(string.Format(VSMError.DataIsEmpty, gameObject.name));
            }
        }

        public void RebindStates()
        {
            if (VsmList == null || VsmList.ViewStateManagers == null) return;
            foreach (VSMManager manager in VsmList.ViewStateManagers)
            {
                foreach (VSMState state in manager.States)
                {
                    foreach (VSMStateProperty property in state.Properties)
                    {
                        var path = property.P + property.C;

                        Component tmpC;
                        Components.TryGetValue(path, out tmpC);
                        if(tmpC != null) continue;

                        var tmpTr = gameObject.transform.FindChild(property.P);
                        if (tmpTr == null)
                        {
                            Debug.LogWarning(property.P + " not found ");
                            continue;
                        }

                        var tmpType = AssemblyUtils.FindTypeFromLoadedAssemblies(property.C);
                        if (tmpType == null)
                        {
                            Debug.LogWarning("Component " + property.C + " not found on " + property.P);
                            continue;
                        }

                        tmpC = tmpTr.gameObject.GetComponent(tmpType);
                        if (tmpC == null)
                        {
                            Debug.LogWarning("Component " + property.C + " not found on " + property.P);
                            continue;
                        }

                        Components.Add(path, tmpC);
                    }
                }
            }
        }

        public void SwitchIntoState(string managerName, string stateName, float time, Ease ease)
        {
            if (!_initialized)
            {
                Debug.LogError(string.Format(VSMError.VSMNotInitialized, managerName, stateName));
                return;
            }

            if (VsmList.ViewStateManagers.All(x => x.ManagerName != managerName))
            {
                Debug.LogError(string.Format(VSMError.StateManagerNotFound, managerName, stateName));
                return;
            }

            VSMManager vsmManager = VsmList.ViewStateManagers.FirstOrDefault(x => x.ManagerName == managerName);
            if (vsmManager != null)
            {
                VSMState targetState = vsmManager.States.FirstOrDefault(x => x.StateName == stateName);
                if (targetState == null)
                {
                    Debug.LogError(string.Format(VSMError.StateNotFound, managerName, stateName));
                    return;
                }

                if (!_animatorRef)
                {
                    Debug.LogError(string.Format(VSMError.CanNotFindAnimator, managerName, stateName));
                    return;
                }

                foreach (VSMStateProperty property in targetState.Properties)
                {
                    var path = property.P + property.C;

                    Component tmpC;
                    Components.TryGetValue(path, out tmpC);
                    if (tmpC == null)
                    {
                        var tmpTr = gameObject.transform.FindChild(property.P);
                        if (tmpTr == null)
                        {
                            Debug.LogWarning(property.P + " not found ");
                            continue;
                        }

                        var tmpType = AssemblyUtils.FindTypeFromLoadedAssemblies(property.C);
                        if (tmpType == null)
                        {
                            Debug.LogWarning("Component " + property.C + " not found on " + property.P);
                            continue;
                        }

                        tmpC = tmpTr.gameObject.GetComponent(tmpType);
                        if (tmpC == null)
                        {
                            Debug.LogWarning("Component " + property.C + " not found on " + property.P);
                            continue;
                        }
                    }

                    UIReflectionSystem.Tween(tmpC, property.N, property.O, time, ease);
                }
            }
        }

        public void Init()
        {
            if (_initialized) return;
            _animatorRef = GetComponent<Animator>();
            _animatorRef.logWarnings = true;
            _animatorRef.enabled = false;

            _initialized = true;

            ParseData();
            RebindStates();
        }

        protected void Awake()
        {
            Init();
        }

        protected void OnEnable()
        {
            _wasEnabled = true;
            if (_delayedData != null)
            {
                SwitchTo(_delayedData.Manager, _delayedData.State, _delayedData.ForceUpdate);
            }
        }

        protected void OnDisable()
        {
            _wasEnabled = false;
            _delayedData = null;
        }

        public class DelayedSwitchData
        {
            public string Manager;
            public string State;
            public bool ForceUpdate;
        }

        public void StateTag(string stateName)
        {
            // Just dummy foo for events tagging.
        }
    }

    public class VSMError
    {
        public const string NoStateManagers = "There are no any View State Managers...";

        public const string StateManagerNotFound =
            "Sate Manager with name {0} was not found while switching to state {1}";

        public const string StateNotFound = "Sate with name {1} was not found in {0} state manager";

        public const string AnimatiorDisabled =
            "{0}:{1} | Animator component is disabled while trying switch state. This call was cached.";

        public const string CanNotFindAnimator =
            "{0}:{1} | Animator reference is null. Be sure to init VSM before calling it's methods!";

        public const string VSMNotInitialized = "{0}:{1} | VSM was not initialized yet!";
        public const string CanNotFindSpeedParameter = "{0}:{1} | Can not find VSM_{0} parameter!";
        public const string DataIsEmpty = "VSM Data is empty in {0}...";
    }

    public class VSMMessage
    {
        public const string DataParsedSuccessfully =
            "VSM: Data has been successfully parsed! \nName: {0} \nTotal managers: {1}";
    }
}
