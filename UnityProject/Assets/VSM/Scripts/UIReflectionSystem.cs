using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;

namespace Assets.VSM.Scripts
{
    public static class UIReflectionSystem
    {
        private static readonly Dictionary<string, Methods>  Metods = new Dictionary<string, Methods>();

        private static readonly Dictionary<Component, TweenStruct>  PropertiesInTween = new Dictionary<Component, TweenStruct>();

        private static bool _isCoroutineActive;

        private static MonoBehaviour _mb;

        private static Methods CreateMethod(Component component, string property)
        {
            if (property == "color") return null;

            var type = component.GetType();

            var propertyInfo = type.GetProperty(property);

            var getMethod = propertyInfo.GetGetMethod(true);
            var setMethod = propertyInfo.GetSetMethod(true);

            var methods = new Methods(getMethod, setMethod);

            Metods.Add(property, methods);

            return methods;
        }

        public static void Set(Component component, string property, object value)
        {
            Methods methods;
            Metods.TryGetValue(property, out methods);
            if (methods == null) methods = CreateMethod(component, property);

            if (methods == null || property != "localPosition")
            {
                // throw
                return;
            }

            /*if (!_isCoroutineActive)
            {
                _mb.StartCoroutine(TweenCoroutine());
            }

            var tween = new TweenStruct(methods, methods.Get.Invoke(component, null), value, 5);*/
            //PropertiesInTween.Add(component, tween);

            ;

            //Delegate k = Delegate.CreateDelegate(typeof(DOGetter<>).MakeGenericType(methods.Get.DeclaringType), methods.Get);
            //var s = Delegate.CreateDelegate(typeof(DOSetter<>).MakeGenericType(methods.Set.DeclaringType), methods.Set);

            DOGetter<Vector3> getter = () => (Vector3)methods.Get.Invoke(component, null);
            DOSetter<Vector3> setter = newValue => { methods.Set.Invoke(component, new[] {(object) newValue}); };
            DOTween.To(() => Vector3.zero, newValue => { ; }, Vector3.zero, 1);

            //(Vector3)methods.Set.Invoke(component, new []{ (object)x }), (Vector3)value, 5)
            DOTween
                .To(getter, setter, Vector3.zero, 4)
                .SetEase(Ease.OutExpo)
                .SetAutoKill();

        }

       /* private static IEnumerator TweenCoroutine()
        {
            _isCoroutineActive = true;

            while (PropertiesInTween.Count != 0)
            {
                //Tween
                foreach (var kvp in PropertiesInTween)
                {
                    var value = kvp.Value;

                    value.Methods.Set.Invoke(kvp.Key, new[] {value.NewValue});
                }

                yield return null;
            }

            _isCoroutineActive = false;
        }

        public class TweenStruct
        {
            public readonly Methods Methods;
            public readonly object OldValue;
            public readonly object NewValue;
            public float TimeToTween;

            public object CurrentValue;

            public TweenStruct(Methods methods, object oldValue, object newValue, float timeToTween)
            {
                Methods = methods;
                OldValue = oldValue;
                NewValue = newValue;
                TimeToTween = timeToTween;
            }
        }*/

        public class Methods
        {
            public readonly MethodInfo Get;
            public readonly MethodInfo Set;

            public Methods(MethodInfo get, MethodInfo set)
            {
                Get = get;
                Set = set;
            }
        }
    }
}
