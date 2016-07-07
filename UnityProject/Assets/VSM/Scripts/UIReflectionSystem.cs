using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Component = UnityEngine.Component;

namespace Assets.VSM.Scripts
{
    public static class UIReflectionSystem
    {
        private static readonly Dictionary<string, MethodInfo>  Metods = new Dictionary<string, MethodInfo>();

        private static MethodInfo CreateMethod(Component component, string property)
        {
            if (property == "color") return null;

            var type = component.GetType();

            var propertyInfo = type.GetProperty(property);

            var method = propertyInfo.GetSetMethod(true);

            Metods.Add(property, method);

            return method;
        }

        public static void Set(Component component, string property, object value)
        {
            MethodInfo method;
            Metods.TryGetValue(property, out method);
            if (method == null) method = CreateMethod(component, property);

            if (method == null || property != "localPosition")
            {
                // throw
                return;
            }

            method.Invoke(component, new[] { value });
        }
    }
}
