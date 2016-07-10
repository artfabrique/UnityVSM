using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using Revenga.VSM;
using UnityEngine;

namespace Assets.VSM.Scripts
{
    public static class UIReflectionSystem
    {
        private static readonly Dictionary<string, Methods> Metods = new Dictionary<string, Methods>();
        public static ViewStateController TestStateController;

        public static void Tween(Component component, string property, Vector4 value, float tweenTime, Ease ease)
        {
            Methods methods;
            Metods.TryGetValue(property, out methods);
            if (methods == null) methods = CreateMethod(component, property);

            if (methods == null)
            {
                Debug.LogError("No setter or getter found for " + property + " of the " + component.gameObject.name + ".");
                return;
            }

            switch (methods.Type)
            {
                case "float":
                    DOTween
                        .To(() => Getter<float>(component, methods),
                            newValue => Setter(component, methods, newValue),
                            value.x,
                            tweenTime)
                        .SetEase(ease)
                        .SetAutoKill();
                    break;

                case "double":
                    DOTween
                        .To(() => Getter<double>(component, methods),
                            newValue => Setter(component, methods, newValue),
                            value.x,
                            tweenTime)
                        .SetEase(ease)
                        .SetAutoKill();
                    break;

                case "int":
                    DOTween
                        .To(() => Getter<int>(component, methods),
                            newValue => Setter(component, methods, newValue),
                            (int)value.x,
                            tweenTime)
                        .SetEase(ease)
                        .SetAutoKill();
                    break;

                case "uint":
                    DOTween
                        .To(() => Getter<uint>(component, methods),
                            newValue => Setter(component, methods, newValue),
                            (uint)value.x,
                            tweenTime)
                        .SetEase(ease)
                        .SetAutoKill();
                    break;

                case "long":
                    DOTween
                        .To(() => Getter<long>(component, methods),
                            newValue => Setter(component, methods, newValue),
                            (long)value.x,
                            tweenTime)
                        .SetEase(ease)
                        .SetAutoKill();
                    break;

                case "ulong":
                    DOTween
                        .To(() => Getter<ulong>(component, methods),
                            newValue => Setter(component, methods, newValue),
                            (ulong)value.x,
                            tweenTime)
                        .SetEase(ease)
                        .SetAutoKill();
                    break;

                case "Vector2":
                    DOTween
                        .To(() => Getter<Vector2>(component, methods),
                            newValue => Setter<Vector2>(component, methods, newValue),    // Explicit generic cast is important!
                            value,
                            tweenTime)
                        .SetEase(ease)
                        .SetAutoKill();
                    break;

                case "Vector3":
                    DOTween
                        .To(() => Getter<Vector3>(component, methods),
                            newValue => Setter<Vector3>(component, methods, newValue),    // Explicit generic cast is important!
                            value,
                            tweenTime)
                        .SetEase(ease)
                        .SetAutoKill();
                    break;

                case "Vector4":
                    DOTween
                        .To(() => Getter<Vector4>(component, methods),
                            newValue => Setter<Vector4>(component, methods, newValue),    // Explicit generic cast is important!
                            value,
                            tweenTime)
                        .SetEase(ease)
                        .SetAutoKill();
                    break;

                case "Rect":
                    DOTween
                        .To(() => Getter<Rect>(component, methods),
                            newValue => Setter(component, methods, newValue),
                            new Rect(value.x, value.y, value.z, value.w),
                            tweenTime)
                        .SetEase(ease)
                        .SetAutoKill();
                    break;

                case "RectOffset":
                    DOTween
                        .To(() => Getter<RectOffset>(component, methods),
                            newValue => Setter(component, methods, newValue),
                            new RectOffset((int)value.x, (int)value.y, (int)value.z, (int)value.w),
                            tweenTime)
                        .SetEase(ease)
                        .SetAutoKill();
                    break;

                case "Color":
                    DOTween
                        .To(() => Getter<Color>(component, methods),
                            newValue => Setter<Color>(component, methods, newValue),    // Explicit generic cast is important!
                            value,
                            tweenTime)
                        .SetEase(ease)
                        .SetAutoKill();
                    break;

                default:
                    Debug.LogError(methods.Type + " type is not suported.");
                    return;

            }
        }

        private static Methods CreateMethod(Component component, string property)
        {
            var type = component.GetType();

            var propertyInfo = type.GetProperty(property);

            var getMethod = propertyInfo.GetGetMethod(true);
            var setMethod = propertyInfo.GetSetMethod(true);

            if (getMethod == null || setMethod == null) return null;

            var methods = new Methods(getMethod, setMethod, propertyInfo.PropertyType.Name);

            Metods.Add(property, methods);

            return methods;
        }

        private static T Getter<T>(Component component, Methods methods)
        {
            return (T) methods.Get.Invoke(component, null);
        }

        private static void Setter<T>(Component component, Methods methods, T newValue)
        {
            methods.Set.Invoke(component, new[] {(object)newValue});
        }

        private class Methods
        {
            public readonly MethodInfo Get;
            public readonly MethodInfo Set;

            public readonly string Type;

            public Methods(MethodInfo get, MethodInfo set, string type)
            {
                Get = get;
                Set = set;
                Type = type;
            }
        }
    }
}
