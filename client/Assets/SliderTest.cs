using System;
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class SliderTest : MonoBehaviour
{

    [Range(0, 1)] public float Val;
    public UISprite Area;
    public float swingDelta = 40f;
	
	// Update is called once per frame
	void Update ()
	{
        if(Area==null) return;
	    var xpos = swingDelta*Mathf.Sin((Val* 180) * (Mathf.PI / 180));
	    var cached = transform.position;
        transform.localPosition = new Vector3(xpos, Area.height * Val);
        Debug.DrawLine(cached,transform.position, Color.red, 3,false);
	}
}
