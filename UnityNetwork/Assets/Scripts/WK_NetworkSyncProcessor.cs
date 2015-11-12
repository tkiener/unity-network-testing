using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Syncs a given Body in Network.
/// ATTENTION: SYNC_CHANNEL has to be defined as needed in current project!
/// </summary>
public class WK_NetworkSyncProcessor : NetworkBehaviour {
	const int SYNC_CHANNEL = 0; //should be set to Channel with QoS-Type "Unreliable Sequenced"

	[SerializeField] float positionsPerSecond = 10;
	[SerializeField] float syncsPerSecond = 10;
	[SerializeField, Range(0f, 1f)] float dataLossPercent = 0;
	[SerializeField] int savedSyncTimes = 20;
	

	List<Vector3> positionData = new List<Vector3>();
	List<Quaternion> rotationData = new List<Quaternion>();
	List<float> timeData = new List<float>();
	int id = 0;

	float lastSyncTime;
	Queue<float> syncTimes = new Queue<float>();
	float largestSyncTime = 0;

	WK_NetworkSyncBody body;

	/// <summary>
	/// Start Syncing.
	/// </summary>
	/// <param name="body">Body that should be synced. if left empty, the script is added to this gameobject.</param>
	public void StartSyncing (WK_NetworkSyncBody body = null)
	{
		if (body != null)
			this.body = body;
		else if (this.body == null)
			this.body = gameObject.AddComponent<WK_NetworkSyncBody>();


		if (isLocalPlayer)
		{
			StartCoroutine(SavePositionDataCo());
			StartCoroutine(SendPositionDataCo());
		}
	}

	public void StopSyncing ()
	{
		positionData.Clear();
		rotationData.Clear();
		timeData.Clear();
		id = 0;
		
		syncTimes.Clear();
		lastSyncTime = Time.timeSinceLevelLoad;
		largestSyncTime = 0;

		StopAllCoroutines();
	}

	//Saves Positions in interval
	IEnumerator SavePositionDataCo ()
	{
		while (true)
		{
			yield return new WaitForSeconds(1f/positionsPerSecond);
			positionData.Add(body.position);
			rotationData.Add(body.rotation);
			timeData.Add(Time.timeSinceLevelLoad);
		}
	}

	//Sends position in interval, if allready saved
	IEnumerator SendPositionDataCo ()
	{
		while (true)
		{
			yield return new WaitForSeconds(1f/syncsPerSecond);
			
			if (positionData.Count == 0)
				continue;
			
			if (dataLossPercent == 0 || Random.Range(0f, 1f) >= dataLossPercent)
				SyncPositionData(positionData.ToArray(), rotationData.ToArray(), timeData.ToArray(), id++);
			
			positionData.Clear();
			rotationData.Clear();
			timeData.Clear();
		}
	}
	
	//Syncs Data over network
	void SyncPositionData (Vector3[] pos, Quaternion[] rot, float[] time, int id)
	{
		Cmd_SyncPositionData(pos, rot, time, id);
	}
	
	[Command(channel = SYNC_CHANNEL)]
	void Cmd_SyncPositionData (Vector3[] pos, Quaternion[] rot, float[] time, int id)
	{
		Rpc_SnycPositionData(pos, rot, time, id);
	}
	
	[ClientRpc(channel = SYNC_CHANNEL)]
	void Rpc_SnycPositionData (Vector3[] pos, Quaternion[] rot, float[] time, int id)
	{
		if (isLocalPlayer)
			return;
		
		body.SetSyncData(pos, rot, time, CalculateAverageTimeBetweenSnycs(), id);
	}
	
	
	//Calculates expected time between syncs
	float CalculateAverageTimeBetweenSnycs ()
	{
		float snycTime = Time.timeSinceLevelLoad - lastSyncTime;
		
		if (lastSyncTime != 0)
		{
			syncTimes.Enqueue(snycTime);
			if (syncTimes.Count > savedSyncTimes)
				syncTimes.Dequeue();
			
			largestSyncTime = 0;
			foreach(var t in syncTimes)
				if (t > largestSyncTime)
					largestSyncTime = t;
		}
		
		lastSyncTime = Time.timeSinceLevelLoad;
		
		return largestSyncTime;
	}
}
