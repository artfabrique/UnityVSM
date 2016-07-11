using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace Revenga.VSM
{
    public class TestUpdate : MonoBehaviour
    {
        public List<ViewStateController> TestStateController;


        void OnEnable()
        {
            if (TestStateController==null || TestStateController.Count == 0)
            {
                TestStateController = new List<ViewStateController>();
                TestStateController = FindObjectsOfType<ViewStateController>().ToList();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                //Debug.Log("0");
                foreach (var controller in TestStateController)
                {
                    controller.SwitchIntoState(VSM_ExampleCube.Managers.VSM_ExampleCube_Test.ToString(), VSM_ExampleCube.VSM_ExampleCube_Test.Red.ToString(), 10, Ease.OutExpo);
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                //Debug.Log("1");
                foreach (var controller in TestStateController)
                {
                    controller.SwitchIntoState(VSM_ExampleCube.Managers.VSM_ExampleCube_Test.ToString(), VSM_ExampleCube.VSM_ExampleCube_Test.Green.ToString(), 10, Ease.OutExpo);
                }
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                //Debug.Log("2");
                foreach (var controller in TestStateController)
                {
                    controller.SwitchIntoState(VSM_ExampleCube.Managers.VSM_ExampleCube_Test.ToString(), VSM_ExampleCube.VSM_ExampleCube_Test.Blue.ToString(), 10, Ease.OutExpo);
                }
            }
        }
    }
}