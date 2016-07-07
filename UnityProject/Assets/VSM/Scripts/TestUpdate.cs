using Assets.VSM.Scripts;
using UnityEngine;

namespace Revenga.VSM
{
    public class TestUpdate : MonoBehaviour
    {
        private static Component victim;
        private static int _i = 0;

        private void Update()
        {
            UIReflectionSystem.Set(victim, "localPosition", new Vector3(_i--, _i++, _i--));


            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Debug.Log("1");
                //UIReflectionSystem.SwitchToTest(Vector3.zero);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("2");
                //UIReflectionSystem.SwitchToTest(Vector3.one);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Debug.Log("3");
                //UIReflectionSystem.SwitchToTest(Vector3.up);
            }
        }
    }
}