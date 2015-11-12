using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class WK_NetworkPlayer : NetworkBehaviour {

	[SerializeField] GameObject characterObjectPrefab;
	WK_Character character;

	public override void OnStartClient ()
	{
		base.OnStartClient ();

		character = Instantiate<GameObject>(characterObjectPrefab).GetComponent<WK_Character>();
		
		character.Init(isLocalPlayer);
	}

	public override void OnStartLocalPlayer ()
	{
		base.OnStartLocalPlayer ();

		character.Init(isLocalPlayer);
		StartCoroutine(SendPositionDataCo());
		StartCoroutine(SavePositionDataCo());
	}





	//_________POSITION SYNCING

	List<Vector3> positionData = new List<Vector3>();
	List<Quaternion> rotationData = new List<Quaternion>();
	List<float> timeData = new List<float>();
	int id = 0;

	[SerializeField] float positionsPerSecond = 10;
	[SerializeField] float syncsPerSecond = 10;
	[SerializeField, Range(0f, 1f)] float dataLossPercent = .2f;

	IEnumerator SavePositionDataCo ()
	{
		while (true)
		{
			yield return new WaitForSeconds(1f/positionsPerSecond);
			positionData.Add(character.position);
			rotationData.Add(character.rotation);
			timeData.Add(Time.timeSinceLevelLoad);
		}
	}

	IEnumerator SendPositionDataCo ()
	{
		while (true)
		{
			yield return new WaitForSeconds(1f/syncsPerSecond);

			if (positionData.Count == 0)
				continue;

			if (Random.Range(0f, 1f) >= dataLossPercent)
				SyncPositionData(positionData.ToArray(), rotationData.ToArray(), timeData.ToArray(), id++);

			positionData.Clear();
			rotationData.Clear();
			timeData.Clear();
		}
	}


	void SyncPositionData (Vector3[] pos, Quaternion[] rot, float[] time, int id)
	{
		Cmd_SyncPositionData(pos, rot, time, id);
	}

	[Command(channel = 2)]
	void Cmd_SyncPositionData (Vector3[] pos, Quaternion[] rot, float[] time, int id)
	{
		Rpc_SnycPositionData(pos, rot, time, id);
	}

	[ClientRpc(channel = 2)]
	void Rpc_SnycPositionData (Vector3[] pos, Quaternion[] rot, float[] time, int id)
	{
		if (isLocalPlayer)
			return;

		character.SetSyncData(pos, rot, time, CalculateAverageTimeBetweenSnycs(), id);
	}


	float lastSyncTime;
	Queue<float> syncTimes = new Queue<float>();
	int savedSyncTimes = 20;
	float averageSyncTime = 0;
	float CalculateAverageTimeBetweenSnycs ()
	{
		float snycTime = Time.timeSinceLevelLoad - lastSyncTime;

		if (lastSyncTime != 0)
		{
			syncTimes.Enqueue(snycTime);
			if (syncTimes.Count > savedSyncTimes)
				syncTimes.Dequeue();

			averageSyncTime = 0;
			foreach(var t in syncTimes)
				if (t > averageSyncTime)
					averageSyncTime = t;
//			averageSyncTime /= syncTimes.Count;
		}

		lastSyncTime = Time.timeSinceLevelLoad;

		return averageSyncTime;
	}

}
