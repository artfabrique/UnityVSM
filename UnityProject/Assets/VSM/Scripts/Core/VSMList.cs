using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Revenga.VSM
{
    [System.Serializable]
    public class VSMList
    {
        [SerializeField]
        public List<VSMManager> ViewStateManagers;
    }
}
