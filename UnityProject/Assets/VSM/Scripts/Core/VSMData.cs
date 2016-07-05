using System;
using System.Collections.Generic;
using UnityEngine;

namespace Revenga.VSM
{
    [Serializable]
    public class VSMData : ScriptableObject
    {
        [SerializeField]
        public string ListName;
        [SerializeField]
        public string Data; 
    }
}