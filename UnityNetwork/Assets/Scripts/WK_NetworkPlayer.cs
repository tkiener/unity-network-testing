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
	int id = 0;

	[SerializeField] float positionsPerSecond = 10;
	IEnumerator SavePositionDataCo ()
	{
		while (true)
		{
			yield return new WaitForSeconds(1f/positionsPerSecond);
			positionData.Add(character.position);
		}
	}

	IEnumerator SendPositionDataCo ()
	{
		while (true)
		{
			yield return new WaitForSeconds(2f);

			if (positionData.Count == 0)
				continue;

			SyncPositionData(positionData.ToArray(), id++);
			positionData.Clear();
		}
	}


	void SyncPositionData (Vector3[] pos, int id)
	{
//		if (!isServer)
			Cmd_SyncPositionData(pos, id);
//		else
//			Rpc_SnycPositionData(pos, id);
	}

	[Command(channel = 2)]
	void Cmd_SyncPositionData (Vector3[] pos, int id)
	{
		Rpc_SnycPositionData(pos, id);
	}

	[ClientRpc(channel = 2)]
	void Rpc_SnycPositionData (Vector3[] pos, int id)
	{
		if (isLocalPlayer)
			return;

		character.SetSyncData(pos, CalculateAverageTimeBetweenSnycs(), id);
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
				averageSyncTime += t;
			averageSyncTime /= syncTimes.Count;
		}

		lastSyncTime = Time.timeSinceLevelLoad;

		return averageSyncTime;
	}

}
