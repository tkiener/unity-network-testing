using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gets Information from WK_NetworkSyncProcessor and syncs rigidbody.
/// Should be destroyed when gameround ended
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class WK_NetworkSyncBody : MonoBehaviour {

	public struct SyncData {
		public Vector3 position;
		public Quaternion rotation;
		public float time;
	}

	Queue<SyncData> syncData = new Queue<SyncData>();
	int lastID = -1;
	float timeSinceLastDataSync = 0;
	float estimatedTimeUntilNextSync = 999;
	float timePerSyncPoint = 999;
	float originalNeededTime = 0;

	Transform tr;
	public Vector3 position {
		get{
			return tr.position;
		}
	}
	
	Rigidbody rb;
	public Quaternion rotation {
		get {
			return tr.rotation;
		}
	}

	void Awake ()
	{
		tr = transform;
		rb = GetComponent<Rigidbody>();
		lastSyncData = new SyncData(){time = -1};

		this.enabled = false;
		
	}

	void FixedUpdate ()
	{
		UpdateSyncPos();
	}
	

	public void SetSyncData (Vector3[] data, Quaternion[] rotation, float[] time, float syncTime, int id)
	{
		if (data.Length == 0)
			return;
		
		for (int i = 0; i < data.Length; i++)
			syncData.Enqueue(new SyncData(){
				position = data[i],
				rotation = rotation[i],
				time = time[i]
			});
		
		if (lastSyncData.time == -1)
		{
			lastSyncData = syncData.Dequeue();

			this.enabled = true;
			
			rb.isKinematic = false;
			rb.useGravity = false;
			
			rb.constraints = RigidbodyConstraints.FreezeRotation;

			if (syncData.Count == 0)
				return;
		}
		
		timeSinceLastDataSync = 0;
		estimatedTimeUntilNextSync = syncTime * 1.2f;
		
		originalNeededTime = time[time.Length-1] - Mathf.Lerp(lastSyncData.time, syncData.Peek().time, currentSyncLerpAlpha);
		timePerSyncPoint = estimatedTimeUntilNextSync / originalNeededTime * (syncData.Peek().time - lastSyncData.time);
		
		if (id <= lastID)
			Debug.LogError("Data out of order!");
		lastID = id;
	}
	
	
	
	SyncData lastSyncData;
	float currentSyncLerpAlpha = 0;
	Vector3 targetSyncPosition;
	void UpdateSyncPos()
	{
		if (lastSyncData.time == -1)
			return;
		
		timeSinceLastDataSync += Time.deltaTime;
		
		currentSyncLerpAlpha += 1/timePerSyncPoint * Time.deltaTime;
		while (currentSyncLerpAlpha > 1 && syncData.Count > 0)
		{
			currentSyncLerpAlpha -= 1;
			lastSyncData = syncData.Dequeue();
			if (syncData.Count > 0)
			{
				float oldTimePerSyncPoint = timePerSyncPoint;
				timePerSyncPoint = estimatedTimeUntilNextSync / originalNeededTime * (syncData.Peek().time - lastSyncData.time);
				currentSyncLerpAlpha *= oldTimePerSyncPoint / timePerSyncPoint;
			}
			else
				timePerSyncPoint = 999;
		}
		
		if (syncData.Count == 0)
		{
			Debug.LogWarning("Reached final point: timeSinceLastDataSync " + timeSinceLastDataSync + " / estimatedTimeUntilNextSync " + estimatedTimeUntilNextSync);
			currentSyncLerpAlpha = 0;
			tr.position = lastSyncData.position;
			tr.rotation = lastSyncData.rotation;
			rb.velocity = Vector3.zero;
			return;
		}
		
		//		tr.position = Vector3.Lerp(lastSyncData.position, syncData.Peek().position, currentSyncLerpAlpha);
		
		rb.MoveRotation( Quaternion.Slerp(lastSyncData.rotation, syncData.Peek().rotation, currentSyncLerpAlpha) );
		
		targetSyncPosition = Vector3.Lerp(lastSyncData.position, syncData.Peek().position, currentSyncLerpAlpha);
		rb.velocity = (targetSyncPosition - tr.position) / Time.deltaTime;
	}

#if UNITY_EDITOR
	void OnDrawGizmos ()
	{
		foreach (var sD in syncData)
			Gizmos.DrawSphere(sD.position, .3f);
	}
#endif
}
