using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using Revenga.VSM;
using UnityEngine;

namespace Revenga.VSM
{
    public static class UIReflectionSystem
    {
        public static readonly Dictionary<string, Methods> MetodInfos = new Dictionary<string, Methods>();

        public static Methods CreateMethod(Component component, string property)
        {
            var type = component.GetType();
            var propertyInfo = type.GetProperty(property);
            if (propertyInfo == null)
            {
                Debug.LogError(string.Format("VSM: Error: Can not find property: {0}",property));
                return null;
            }

            var getMethod = propertyInfo.GetGetMethod(true);
            var setMethod = propertyInfo.GetSetMethod(true);

            if (getMethod == null || setMethod == null) return null;

            var methods = new Methods(getMethod, setMethod, propertyInfo.PropertyType.Name);

            MetodInfos.Add(property, methods);

            return methods;
        }

        public static T Getter<T>(Component component, Methods methods)
        {
            return (T) methods.Get.Invoke(component, null);
        }

        public static void Setter<T>(Component component, Methods methods, T newValue)
        {
            methods.Set.Invoke(component, new[] {(object)newValue});
        }

        public class Methods
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
