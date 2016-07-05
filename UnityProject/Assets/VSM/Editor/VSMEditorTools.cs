using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace Revenga.VSM
{
    public class VSMEditorTools
    {
        private static bool _isInHorisontalBlock = false;

        public static float SpaceH = 4f;

        static public bool DrawGroupHeader(string label, string id)
        {
            bool state = EditorPrefs.GetBool(id, true);

            GUILayout.Space(3f);
            if (!state) GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
            GUILayout.BeginHorizontal();
            GUI.changed = false;

            label = "<b><size=11>" + label + "</size></b>";

            //Triangles

            if (state) label = "\u25BC " + label;
            else label = "\u25BA " + label;

            if (!GUILayout.Toggle(true, label, "dragtab", GUILayout.MinWidth(20f)) ) state = !state;
            
            GUILayout.Space(2f);
            GUILayout.EndHorizontal();

            //GUI.backgroundColor = Color.white;

            if (!state) GUILayout.Space(3f);

            if (GUI.changed) EditorPrefs.SetBool(id, state);

            GUI.backgroundColor = Color.white;
            return state;
        }

        static public void BeginGroupContents()
        {
            _isInHorisontalBlock = true;
            GUILayout.BeginHorizontal();
            EditorGUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(10f));
            
            GUILayout.BeginVertical();
            GUILayout.Space(2f); 
        }

        static public void EndGroupContents()
        {
            GUILayout.Space(3f);
            GUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (_isInHorisontalBlock)
            {
                GUILayout.Space(3f);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(3f);
        }

        static public void SerializedListAdd(SerializedObject so, string propertyName, UnityEngine.Object obj)
        {
            if (so == null || string.IsNullOrEmpty(propertyName)) return;

            SerializedProperty sp = so.FindProperty(propertyName);
            if (sp == null) return;
            if(!sp.isArray) return;
            sp.InsertArrayElementAtIndex(sp.arraySize);
            sp.arraySize++;
            sp.GetArrayElementAtIndex(sp.arraySize-1).objectReferenceValue = obj;
            so.ApplyModifiedProperties();
            Debug.Log(so.FindProperty(propertyName).GetArrayElementAtIndex(0).objectReferenceValue);
        }

        static public void DrawPadding()
        {
            GUILayout.Space(18f);
        }
        

        static public bool NearlyEqual(double a, double b, double epsilon)
        {
            double absA = Math.Abs(a);
            double absB = Math.Abs(b);
            double diff = Math.Abs(a - b);

            if (a == b)
            { // shortcut, handles infinities
                return true;
            }
            else if (a == 0 || b == 0 || diff < Double.Epsilon)
            {
                // a or b is zero or both are extremely close to it
                // relative error is less meaningful here
                return diff < epsilon;
            }
            else
            { // use relative error
                return diff / (absA + absB) < epsilon;
            }
        }


        [AddComponentMenu("Reset Component")]
        static public void ResetComponent()
        {
            Debug.Log("Reset Component");
        }
    }

    // Utility Window

    public class PromptForStringInput : EditorWindow
    {
        public string Title;
        public string Text;
        public string OkButtonLabel;
        public string CancelButtonLabel;
        public string DefaultValue;
        public string Value;

        public delegate void OnWindowResult(string result);
        public event OnWindowResult OkEvent;
        public event OnWindowResult CancelEvent;

        public static PromptForStringInput Show(string title, string text, string okButtonLabel, string cancelButtonLabel, string defaultValue)
        {
            PromptForStringInput window = ScriptableObject.CreateInstance<PromptForStringInput>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 150);

            window.Title = title;
            window.Text = text;
            window.OkButtonLabel = (string.IsNullOrEmpty(okButtonLabel))?"Ok":okButtonLabel;
            window.CancelButtonLabel = (string.IsNullOrEmpty(cancelButtonLabel)) ? "Cancel" : cancelButtonLabel;
            window.DefaultValue = defaultValue;
            window.Value = defaultValue;

            window.ShowUtility();
            return window;
        }

        protected void OnGUI()
        {
            EditorGUILayout.LabelField(Text, EditorStyles.wordWrappedLabel);
            GUILayout.Space(10f);
            Value = EditorGUILayout.TextField(Value);
            GUILayout.Space(30f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(CancelButtonLabel)) this.OnCancel();
            if (GUILayout.Button(OkButtonLabel)) this.OnOk();
            EditorGUILayout.EndHorizontal();
        }

        private void OnCancel()
        {
            OnCancelEvent();
            this.Close();
        }

        private void OnOk()
        {
            OnOkEvent();
            this.Close();
        }


        // Events

        protected virtual void OnOkEvent()
        {
            var handler = OkEvent;
            if (handler != null) handler(Value);
        }

        protected virtual void OnCancelEvent()
        {
            var handler = CancelEvent;
            if (handler != null) handler(Value);
        }
    }
    public static class CurveExtension
    {

        public static void UpdateAllLinearTangents(this AnimationCurve curve)
        {
            for (int i = 0; i < curve.keys.Length; i++)
            {
                UpdateTangentsFromMode(curve, i);
            }
        }

        // UnityEditor.CurveUtility.cs (c) Unity Technologies
        public static void UpdateTangentsFromMode(AnimationCurve curve, int index)
        {
            if (index < 0 || index >= curve.length)
                return;
            Keyframe key = curve[index];
            if (KeyframeUtil.GetKeyTangentMode(key, 0) == TangentMode.Linear && index >= 1)
            {
                key.inTangent = CalculateLinearTangent(curve, index, index - 1);
                curve.MoveKey(index, key);
            }
            if (KeyframeUtil.GetKeyTangentMode(key, 1) == TangentMode.Linear && index + 1 < curve.length)
            {
                key.outTangent = CalculateLinearTangent(curve, index, index + 1);
                curve.MoveKey(index, key);
            }
            if (KeyframeUtil.GetKeyTangentMode(key, 0) != TangentMode.Smooth && KeyframeUtil.GetKeyTangentMode(key, 1) != TangentMode.Smooth)
                return;
            curve.SmoothTangents(index, 0.0f);
        }

        // UnityEditor.CurveUtility.cs (c) Unity Technologies
        private static float CalculateLinearTangent(AnimationCurve curve, int index, int toIndex)
        {
            return (float)(((double)curve[index].value - (double)curve[toIndex].value) / ((double)curve[index].time - (double)curve[toIndex].time));
        }

    }

    public enum TangentMode
    {
        Editable = 0,
        Smooth = 1,
        Linear = 2,
        Stepped = Linear | Smooth,
    }

    public enum TangentDirection
    {
        Left,
        Right
    }

    public class KeyframeUtil
    {

        public static Keyframe GetNew(float time, float value, TangentMode leftAndRight)
        {
            return GetNew(time, value, leftAndRight, leftAndRight);
        }

        public static Keyframe GetNew(float time, float value, TangentMode left, TangentMode right)
        {
            object boxed = new Keyframe(time, value); // cant use struct in reflection			

            SetKeyBroken(boxed, true);
            SetKeyTangentMode(boxed, 0, left);
            SetKeyTangentMode(boxed, 1, right);

            Keyframe keyframe = (Keyframe)boxed;
            if (left == TangentMode.Stepped)
                keyframe.inTangent = float.PositiveInfinity;
            if (right == TangentMode.Stepped)
                keyframe.outTangent = float.PositiveInfinity;

            return keyframe;
        }


        // UnityEditor.CurveUtility.cs (c) Unity Technologies
        public static void SetKeyTangentMode(object keyframe, int leftRight, TangentMode mode)
        {

            Type t = typeof(UnityEngine.Keyframe);
            FieldInfo field = t.GetField("m_TangentMode", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            int tangentMode = (int)field.GetValue(keyframe);

            if (leftRight == 0)
            {
                tangentMode &= -7;
                tangentMode |= (int)mode << 1;
            }
            else
            {
                tangentMode &= -25;
                tangentMode |= (int)mode << 3;
            }

            field.SetValue(keyframe, tangentMode);
            if (GetKeyTangentMode(tangentMode, leftRight) == mode)
                return;
            Debug.Log("bug");
        }

        // UnityEditor.CurveUtility.cs (c) Unity Technologies
        public static TangentMode GetKeyTangentMode(int tangentMode, int leftRight)
        {
            if (leftRight == 0)
                return (TangentMode)((tangentMode & 6) >> 1);
            else
                return (TangentMode)((tangentMode & 24) >> 3);
        }

        // UnityEditor.CurveUtility.cs (c) Unity Technologies
        public static TangentMode GetKeyTangentMode(Keyframe keyframe, int leftRight)
        {
            Type t = typeof(UnityEngine.Keyframe);
            FieldInfo field = t.GetField("m_TangentMode", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            int tangentMode = (int)field.GetValue(keyframe);
            if (leftRight == 0)
                return (TangentMode)((tangentMode & 6) >> 1);
            else
                return (TangentMode)((tangentMode & 24) >> 3);
        }


        // UnityEditor.CurveUtility.cs (c) Unity Technologies
        public static void SetKeyBroken(object keyframe, bool broken)
        {
            Type t = typeof(UnityEngine.Keyframe);
            FieldInfo field = t.GetField("m_TangentMode", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            int tangentMode = (int)field.GetValue(keyframe);

            if (broken)
                tangentMode |= 1;
            else
                tangentMode &= -2;
            field.SetValue(keyframe, tangentMode);
        }

    }

}