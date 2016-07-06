using System;
using System.Collections.Generic;
using UnityEngine;

namespace Revenga.VSM
{
    [Serializable]
    public class VSMList
    {
        [SerializeField] public List<VSMManager> ViewStateManagers;
    }
}
