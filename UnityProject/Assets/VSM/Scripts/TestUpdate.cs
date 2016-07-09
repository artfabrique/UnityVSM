using Assets.VSM.Scripts;
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
                Debug.Log("1");
                //UIReflectionSystem.Set(victim, "localPosition", Vector3.zero, 5);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Debug.Log("2");
                //UIReflectionSystem.Set(victim, "localPosition", Vector3.one, 5);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Debug.Log("3");
                //UIReflectionSystem.Set(victim, "localPosition", Vector3.up, 5);
            }
        }
    }
}