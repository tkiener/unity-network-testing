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

		GetComponent<WK_NetworkSyncProcessor>().StartSyncing(character.GetComponent<WK_NetworkSyncBody>());
	}

	public override void OnStartLocalPlayer ()
	{
		base.OnStartLocalPlayer ();

		GetComponent<WK_NetworkSyncProcessor>().StartSyncing();
	}

}
