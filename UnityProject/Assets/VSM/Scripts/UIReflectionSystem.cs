using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using UnityEngine;

namespace Assets.VSM.Scripts
{
    public static class UIReflectionSystem
    {
        private static readonly Dictionary<string, Methods> Metods = new Dictionary<string, Methods>();

        private static Methods CreateMethod(Component component, string property)
        {
            var type = component.GetType();

            var propertyInfo = type.GetProperty(property);

            var getMethod = propertyInfo.GetGetMethod(true);
            var setMethod = propertyInfo.GetSetMethod(true);

            if (getMethod == null || setMethod == null) return null;

            var methods = new Methods(getMethod, setMethod, type.ToString());

            Metods.Add(property, methods);

            return methods;
        }

        public static void Set(Component component, string property, Vector4 value, float tweenTime)
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
                            newValue => methods.Set.Invoke(component, new[] { (object)newValue }),
                            value.x,
                            tweenTime)
                        .SetEase(Ease.OutExpo)
                        .SetAutoKill();
                    break;

                case "double":
                    DOTween
                        .To(() => Getter<double>(component, methods),
                            newValue => methods.Set.Invoke(component, new[] {(object) newValue}),
                            value.x,
                            tweenTime)
                        .SetEase(Ease.OutExpo)
                        .SetAutoKill();
                    break;

                case "int":
                    DOTween
                        .To(() => Getter<int>(component, methods),
                            newValue => methods.Set.Invoke(component, new[] {(object) newValue}),
                            (int)value.x,
                            tweenTime)
                        .SetEase(Ease.OutExpo)
                        .SetAutoKill();
                    break;

                case "uint":
                    DOTween
                        .To(() => Getter<uint>(component, methods),
                            newValue => methods.Set.Invoke(component, new[] {(object) newValue}),
                            (uint)value.x,
                            tweenTime)
                        .SetEase(Ease.OutExpo)
                        .SetAutoKill();
                    break;

                case "long":
                    DOTween
                        .To(() => Getter<long>(component, methods),
                            newValue => methods.Set.Invoke(component, new[] {(object) newValue}),
                            (long)value.x,
                            tweenTime)
                        .SetEase(Ease.OutExpo)
                        .SetAutoKill();
                    break;

                case "ulong":
                    DOTween
                        .To(() => Getter<ulong>(component, methods),
                            newValue => methods.Set.Invoke(component, new[] {(object) newValue}),
                            (ulong)value.x,
                            tweenTime)
                        .SetEase(Ease.OutExpo)
                        .SetAutoKill();
                    break;

                case "Vector2":
                    DOTween
                        .To(() => Getter<Vector2>(component, methods),
                            newValue => methods.Set.Invoke(component, new[] {(object) newValue}),
                            value,
                            tweenTime)
                        .SetEase(Ease.OutExpo)
                        .SetAutoKill();
                    break;

                case "Vector3":
                    DOTween
                        .To(() => Getter<Vector3>(component, methods),
                            newValue => methods.Set.Invoke(component, new[] {(object) newValue}),
                            value,
                            tweenTime)
                        .SetEase(Ease.OutExpo)
                        .SetAutoKill();
                    break;

                case "Vector4":
                    DOTween
                        .To(() => Getter<Vector4>(component, methods),
                            newValue => methods.Set.Invoke(component, new[] {(object) newValue}),
                            value,
                            tweenTime)
                        .SetEase(Ease.OutExpo)
                        .SetAutoKill();
                    break;

                case "Rect":
                    DOTween
                        .To(() => Getter<Rect>(component, methods),
                            newValue => methods.Set.Invoke(component, new[] {(object) newValue}),
                            new Rect(value.x, value.y, value.z, value.w),
                            tweenTime)
                        .SetEase(Ease.OutExpo)
                        .SetAutoKill();
                    break;

                case "RectOffset":
                    DOTween
                        .To(() => Getter<RectOffset>(component, methods),
                            newValue => methods.Set.Invoke(component, new[] { (object)newValue }),
                            new RectOffset((int)value.x, (int)value.y, (int)value.z, (int)value.w),
                            tweenTime)
                        .SetEase(Ease.OutExpo)
                        .SetAutoKill();
                    break;

                case "Color":
                    DOTween
                        .To(() => Getter<Color>(component, methods),
                            newValue => methods.Set.Invoke(component, new[] { (object)newValue }),
                            value,
                            tweenTime)
                        .SetEase(Ease.OutExpo)
                        .SetAutoKill();
                    break;

                default:
                    Debug.LogError(methods.Type + " type is not suported.");
                    return;

            }
        }

        private static T Getter<T>(Component component, Methods methods)
        {
            return (T) methods.Get.Invoke(component, null);
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
