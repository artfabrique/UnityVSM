using System.Collections.Generic;
using System.Linq;
using Assets.VSM.Scripts;
using DG.Tweening;
using UnityEngine;

namespace Revenga.VSM
{
    public class TestUpdate : MonoBehaviour
    {
        public List<ViewStateController> TestStateController;


        private void Update()
        {
            if (TestStateController.Count == 0)
            {
                TestStateController = FindObjectsOfType<ViewStateController>().ToList();
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                //Debug.Log("0");
                foreach (var controller in TestStateController)
                {
                    controller.SwitchIntoState(VSM_Directional_Light.Managers.VSM_Directional_Light_ColorPos.ToString(), VSM_Directional_Light.VSM_Directional_Light_ColorPos.Bl.ToString(), 10, Ease.OutExpo);
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                //Debug.Log("1");
                foreach (var controller in TestStateController)
                {
                    controller.SwitchIntoState(VSM_Directional_Light.Managers.VSM_Directional_Light_ColorPos.ToString(), VSM_Directional_Light.VSM_Directional_Light_ColorPos.G.ToString(), 10, Ease.OutExpo);
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                //Debug.Log("2");
                foreach (var controller in TestStateController)
                {
                    controller.SwitchIntoState(VSM_Directional_Light.Managers.VSM_Directional_Light_ColorPos.ToString(), VSM_Directional_Light.VSM_Directional_Light_ColorPos.Red.ToString(), 10, Ease.OutExpo);
                }
            }
        }
    }
}