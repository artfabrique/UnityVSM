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
                //Debug.Log("0");
                UIReflectionSystem.TestStateController.SwitchIntoState(VSM_Directional_Light.Managers.VSM_Directional_Light_ColorPos.ToString(), VSM_Directional_Light.VSM_Directional_Light_ColorPos.Bl.ToString(), 0.2f, Ease.OutExpo);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                //Debug.Log("1");
                UIReflectionSystem.TestStateController.SwitchIntoState(VSM_Directional_Light.Managers.VSM_Directional_Light_ColorPos.ToString(), VSM_Directional_Light.VSM_Directional_Light_ColorPos.G.ToString(),0.2f, Ease.OutExpo);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                //Debug.Log("2");
                UIReflectionSystem.TestStateController.SwitchIntoState(VSM_Directional_Light.Managers.VSM_Directional_Light_ColorPos.ToString(), VSM_Directional_Light.VSM_Directional_Light_ColorPos.Red.ToString(), 0.2f, Ease.OutExpo);
            }
        }
    }
}