using UnityEngine;
using System.Collections;

public class Debug_AutoMover : MonoBehaviour {

	Rigidbody rb;

	void Awake ()
	{
		rb = GetComponent<Rigidbody>();
	}

	void FixedUpdate () {
		rb.AddForce(Vector3.right * 3 * Time.deltaTime, ForceMode.VelocityChange);
	}
}
