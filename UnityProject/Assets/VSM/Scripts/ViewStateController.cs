using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace Revenga.VSM
{
    [Serializable]
    public class ViewStateController : MonoBehaviour
    {
        public const string VsmAnimatorSpeedParamName = "VSM_FrameJumper";

        [SerializeField] public VSMData VsmData;
        [NonSerialized] public VSMList VsmList;
        
        public DelayedSwitchData DelayedData;
        private bool _initialized;
        private Animator _animatorRef;
        private bool _wasEnabled;
        private readonly Dictionary<string, Component> _components = new Dictionary<string, Component>(); 
        private readonly Dictionary<string, GameObject> _gameObjects = new Dictionary<string, GameObject>(); 

        public void ParseData()
        {
            if (VsmData != null && !string.IsNullOrEmpty(VsmData.Data))
            {
                VsmList = JsonUtility.FromJson<VSMList>(VsmData.Data);
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
                        var tweenTargetType = TryToSearchAndSaveTweenTarget(property.P, property.C);
                        if (tweenTargetType == TweenTargetType.None)
                        {
                            Debug.LogError("Neither GameObject or Component found with path " + property.P + " and name " + property.C);
                        }
                    }
                }
            }
        }

        private TweenTargetType TryToSearchAndSaveTweenTarget(string path, string componentName)
        {
            var pathFull = path + componentName;

            Component tmpC;
            _components.TryGetValue(pathFull, out tmpC);
            if (tmpC != null) return TweenTargetType.Component;

            GameObject go;
            _gameObjects.TryGetValue(pathFull, out go);
            if (go != null) return TweenTargetType.GameObject;

            var tmpTr = gameObject.transform.FindChild(path);
            if (tmpTr == null)
            {
                Debug.LogWarning(path + " not found ");
                return TweenTargetType.None;
            }

            var tmpType = AssemblyUtils.FindTypeFromLoadedAssemblies(componentName);
            if (tmpType == null)
            {
                Debug.LogWarning("Component " + componentName + " not found on " + path);
                return TweenTargetType.None;
            }

            if (tmpType == typeof(GameObject))
            {
                _gameObjects.Add(pathFull, tmpTr.gameObject);
                return TweenTargetType.GameObject;
            }

            tmpC = tmpTr.gameObject.GetComponent(tmpType);
            if (tmpC == null)
            {
                Debug.LogWarning("Component " + componentName + " not found on " + path);
                return TweenTargetType.None;
            }

            _components.Add(pathFull, tmpC);
            return TweenTargetType.Component;
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
            if (!gameObject.activeInHierarchy || !gameObject.activeSelf || !_wasEnabled)
            {
                Debug.LogWarning(string.Format(VSMError.AnimatiorDisabled, managerName, stateName));
                if (DelayedData == null) DelayedData = new DelayedSwitchData();
                DelayedData.Manager = managerName;
                DelayedData.State = stateName;
                DelayedData.Time = time;
                DelayedData.EaseType = ease;
            }
            else
            {
                DelayedData = null;

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
                        
                        var tweenTargetType = TryToSearchAndSaveTweenTarget(property.P, property.C);
                        if (tweenTargetType == TweenTargetType.None)
                        {
                            Debug.LogError("Neither GameObject or Component found with path " + property.P + " and name " + property.C);
                            continue;
                        }

                        switch (tweenTargetType)
                        {
                            case TweenTargetType.None:
                                Debug.LogError("Neither GameObject or Component found with path " + property.P + " and name " + property.C);
                                continue;

                            case TweenTargetType.Component:
                                Tween(_components[path], property.N, property.O, time, ease);
                                break;

                            case TweenTargetType.GameObject:
                                Tween(_gameObjects[path], property.N, property.O, time, ease);
                                break;
                        }
                    }
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
            if (DelayedData != null)
            {
                SwitchIntoState(DelayedData.Manager, DelayedData.State, DelayedData.Time, DelayedData.EaseType);
            }
        }

        protected void OnDestroy() { }

        public void Tween(Component component, string property, Vector4 value, float tweenTime, Ease ease)
        {
            
            UIReflectionSystem.Methods methods;
            UIReflectionSystem.MetodInfos.TryGetValue(property, out methods);
            if (methods == null) methods = UIReflectionSystem.CreateMethod(component, property);

            if (methods == null)
            {
                Debug.LogError("No setter or getter found for " + property + " of the " + component.gameObject.name + ".");
                return;
            }

            //List<object> Tweens = new List<object>();
            var tmpTweenId = string.Concat(gameObject.GetInstanceID(), component.GetInstanceID(), property);

            if (DOTween.IsTweening(tmpTweenId))
                DOTween.Kill(tmpTweenId);

            switch (methods.Type)
            {
                case "Boolean":
                    UIReflectionSystem.Setter(component, methods, Convert.ToBoolean(value.x));
                    break;

                case "Single":
                    DOTween
                        .To(() => UIReflectionSystem.Getter<float>(component, methods),
                            newValue => UIReflectionSystem.Setter(component, methods, newValue),
                            value.x,
                            tweenTime)
                        .TweenParams(ease, tmpTweenId);
                    break;

                case "double":
                    DOTween
                        .To(() => UIReflectionSystem.Getter<double>(component, methods),
                            newValue => UIReflectionSystem.Setter(component, methods, newValue),
                            value.x,
                            tweenTime)
                        .TweenParams(ease, tmpTweenId);
                    break;

                case "int":
                    DOTween
                        .To(() => UIReflectionSystem.Getter<int>(component, methods),
                            newValue => UIReflectionSystem.Setter(component, methods, newValue),
                            (int)value.x,
                            tweenTime)
                        .TweenParams(ease, tmpTweenId);
                    break;

                case "uint":
                    DOTween
                        .To(() => UIReflectionSystem.Getter<uint>(component, methods),
                            newValue => UIReflectionSystem.Setter(component, methods, newValue),
                            (uint)value.x,
                            tweenTime)
                        .TweenParams(ease, tmpTweenId);
                    break;

                case "long":
                    DOTween
                        .To(() => UIReflectionSystem.Getter<long>(component, methods),
                            newValue => UIReflectionSystem.Setter(component, methods, newValue),
                            (long)value.x,
                            tweenTime)
                        .TweenParams(ease, tmpTweenId);
                    break;

                case "ulong":
                    DOTween
                        .To(() => UIReflectionSystem.Getter<ulong>(component, methods),
                            newValue => UIReflectionSystem.Setter(component, methods, newValue),
                            (ulong)value.x,
                            tweenTime)
                        .TweenParams(ease, tmpTweenId);
                    break;

                case "Vector2":
                    DOTween
                        .To(() => UIReflectionSystem.Getter<Vector2>(component, methods),
                            newValue => UIReflectionSystem.Setter<Vector2>(component, methods, newValue),    // Explicit generic cast is important!
                            value,
                            tweenTime)
                        .TweenParams(ease, tmpTweenId);
                    break;

                case "Vector3":
                    DOTween
                        .To(() => UIReflectionSystem.Getter<Vector3>(component, methods),
                            newValue => UIReflectionSystem.Setter<Vector3>(component, methods, newValue),    // Explicit generic cast is important!
                            value,
                            tweenTime)
                        .TweenParams(ease, tmpTweenId);
                    break;

                case "Vector4":
                    DOTween
                        .To(() => UIReflectionSystem.Getter<Vector4>(component, methods),
                            newValue => UIReflectionSystem.Setter<Vector4>(component, methods, newValue),    // Explicit generic cast is important!
                            value,
                            tweenTime)
                        .TweenParams(ease, tmpTweenId);
                    break;

                case "Rect":
                    DOTween
                        .To(() => UIReflectionSystem.Getter<Rect>(component, methods),
                            newValue => UIReflectionSystem.Setter(component, methods, newValue),
                            new Rect(value.x, value.y, value.z, value.w),
                            tweenTime)
                        .TweenParams(ease, tmpTweenId);
                    break;

                case "RectOffset":
                    DOTween
                        .To(() => UIReflectionSystem.Getter<RectOffset>(component, methods),
                            newValue => UIReflectionSystem.Setter(component, methods, newValue),
                            new RectOffset((int)value.x, (int)value.y, (int)value.z, (int)value.w),
                            tweenTime)
                        .TweenParams(ease, tmpTweenId);
                    break;

                case "Color":
                    DOTween
                        .To(() => UIReflectionSystem.Getter<Color>(component, methods),
                            newValue => UIReflectionSystem.Setter<Color>(component, methods, newValue),    // Explicit generic cast is important!
                            value,
                            tweenTime)
                        .TweenParams(ease, tmpTweenId);
                    break;

                default:
                    Debug.LogError(methods.Type + " type is not suported.");
                    return;
            }
        }

        public void Tween(GameObject go, string property, Vector4 value, float tweenTime, Ease ease)
        {
            switch (property)
            {
                case "isActive":
                    go.SetActive(Convert.ToBoolean(value.x));
                    break;
            }
        }

        protected void OnDisable()
        {
            _wasEnabled = false;
            DelayedData = null;
        }

        public class DelayedSwitchData
        {
            public string Manager;
            public string State;
            public float Time;
            public Ease EaseType;
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

    public static class ViewStateControllerHelper
    {
        public static void TweenParams(this Tween tween, Ease ease, string tmpTweenId)
        {
            tween
                .SetEase(ease)
                .SetAutoKill(false)
                .SetId(tmpTweenId)
                .SetRecyclable();
        }
    }

    public enum TweenTargetType { None = 0, Component, GameObject }
}
