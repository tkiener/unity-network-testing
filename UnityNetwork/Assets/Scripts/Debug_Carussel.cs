using UnityEngine;
using System.Collections;

public class Debug_Carussel : MonoBehaviour {

	[SerializeField] float rotationSpeed = 20f;

	void Update ()
	{
		transform.Rotate( new Vector3(0, rotationSpeed*Time.deltaTime, 0) );
	}

}
