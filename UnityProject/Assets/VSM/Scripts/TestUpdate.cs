using System.Linq;
using Assets.VSM.Scripts;
using DG.Tweening;
using UnityEngine;

namespace Revenga.VSM
{
    public class TestUpdate : MonoBehaviour
    {
        private static Component victim;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("0");
                UIReflectionSystem.TestStateController.SwitchIntoState(UIReflectionSystem.TestStateController.VsmList.ViewStateManagers.First().ManagerName, "T0", 3, Ease.InExpo);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("1");
                UIReflectionSystem.TestStateController.SwitchIntoState(UIReflectionSystem.TestStateController.VsmList.ViewStateManagers.First().ManagerName, "T1", 3, Ease.InExpo);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Debug.Log("2");
                UIReflectionSystem.TestStateController.SwitchIntoState(UIReflectionSystem.TestStateController.VsmList.ViewStateManagers.First().ManagerName, "T2", 3, Ease.InExpo);
            }
        }
    }
}