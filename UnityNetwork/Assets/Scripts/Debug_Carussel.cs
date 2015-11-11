using UnityEngine;
using System.Collections;

public class Debug_Carussel : MonoBehaviour {

	void Update ()
	{
		transform.Rotate( new Vector3(0, 20*Time.deltaTime, 0) );
	}

}
