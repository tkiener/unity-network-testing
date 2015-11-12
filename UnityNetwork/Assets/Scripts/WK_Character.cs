using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SyncData {
	public SyncData(Vector3 position)
	{
		this.position = position;
	}

	public Vector3 position;
}

public class WK_Character : MonoBehaviour {

	public Vector3 position {
		get{
			return tr.position;
		}
		set{
			tr.position = value;
		}
	}

	bool isLocalPlayer;

	Transform tr;
	Rigidbody rb;

	void Awake ()
	{
		tr = transform;
		rb = GetComponent<Rigidbody>();
	}

	public void Init (bool isLocalPlayer)
	{
//		GetComponent<Rigidbody>().isKinematic = !isLocalPlayer;
		rb.isKinematic = false;
		rb.useGravity = isLocalPlayer;

		this.isLocalPlayer = isLocalPlayer;
		if (isLocalPlayer)
		{
			Debug.Log("Init on client");
//			gameObject.AddComponent<Debug_AutoMover>();
			tr.parent = GameObject.Find ("Karussel").transform;
			tr.localPosition = Vector3.right * 5;
		}
	}


	void FixedUpdate ()
	{
		if (!isLocalPlayer)
			UpdateSyncPos();
	}



	Queue<SyncData> syncData = new Queue<SyncData>();
	int lastID = -1;
	float timeSinceLastDataSync = 0;
	float estimatedTimeUntilNextSync = 999;
	float timePerSyncPoint = 999;
	public void SetSyncData (Vector3[] data, float syncTime, int id)
	{
		if (data.Length == 0)
			return;

		foreach (var d in data)
			syncData.Enqueue(new SyncData(d));


		timeSinceLastDataSync = 0;
		estimatedTimeUntilNextSync = syncTime * 1.2f;
		timePerSyncPoint = estimatedTimeUntilNextSync / (syncData.Count + 1 - currentSyncLerpAlpha);


		Debug.Log("Received message ID: " + id + " with syncTime: " + syncTime);
		if (id <= lastID)
			Debug.LogError("Data out of order!");
		lastID = id;
	}



	SyncData lastSyncData;
	float currentSyncLerpAlpha = 0;
	Vector3 targetSyncPosition;
	void UpdateSyncPos()
	{
		if (lastSyncData == null)
		{
			if (syncData.Count == 0)
				return;
			lastSyncData = syncData.Dequeue();
		}

		timeSinceLastDataSync += Time.deltaTime;

		currentSyncLerpAlpha += 1/timePerSyncPoint * Time.deltaTime;
		while (currentSyncLerpAlpha > 1 && syncData.Count > 0)
		{
			currentSyncLerpAlpha -= 1;
			lastSyncData = syncData.Dequeue();
		}

		if (syncData.Count == 0)
		{
			currentSyncLerpAlpha = 0;
			tr.position = lastSyncData.position;
			rb.velocity = Vector3.zero;
			return;
		}

//		tr.position = Vector3.Lerp(lastSyncData.position, syncData.Peek().position, currentSyncLerpAlpha);

		targetSyncPosition = Vector3.Lerp(lastSyncData.position, syncData.Peek().position, currentSyncLerpAlpha);
		rb.velocity = (targetSyncPosition - tr.position) / Time.deltaTime;
	}

	void OnDrawGizmos ()
	{
		foreach (var sD in syncData)
			Gizmos.DrawSphere(sD.position, .3f);
	}
}
