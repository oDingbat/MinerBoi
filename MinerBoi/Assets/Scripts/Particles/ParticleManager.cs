using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour {

	[Header("Components")][Space(10)]
	public List<BlockDebris> blockDebris = new List<BlockDebris>();
	int blockDebrisIndex = 0;

	[Header("Prefabs")][Space(10)]
	public GameObject prefab_BlockDebris;

	[Header("Settings")][Space(10)]
	public int maxDebris = 256;

	private void Start () {
		InitializeDebris();
		StartCoroutine(DebrisLifespanCoroutine());
	}

	private void InitializeDebris () {
		for (int i = 0; i < maxDebris; i++) {
			BlockDebris newBlockDebris = Instantiate(prefab_BlockDebris, Vector3.zero, Quaternion.identity).GetComponent<BlockDebris>();
			newBlockDebris.Initialize(this);

			blockDebris.Add(newBlockDebris);

			newBlockDebris.gameObject.SetActive(false);
		}
	}

	public void SpawnDebris (Vector3 pos, Vector3 vel, Vector2 uvPos, int blockType) {

		float increment = 0.25f;
		Vector3 origin = pos - new Vector3(0.375f, 0.375f, 0.375f);

		for (int x = 1; x < 3; x++) {
			for (int y = 1; y < 3; y++) {
				for (int z = 1; z < 3; z++) {
					BlockDebris blockDebrisCurrent = blockDebris[blockDebrisIndex];

					Vector3 randomVelocity = Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)) * new Vector3(0, 7.5f, 0);

					blockDebrisCurrent.gameObject.SetActive(true);
					blockDebrisCurrent.Spawn(origin + new Vector3(x * increment, y * increment, z * increment), (vel * 2.5f) + randomVelocity);
					blockDebrisCurrent.SetupUVs(uvPos, blockType);

					blockDebrisIndex = (blockDebrisIndex == maxDebris - 1 ? 0 : blockDebrisIndex + 1);
				}
			}
		}

	}

	private IEnumerator DebrisLifespanCoroutine () {
		while (true) {
			yield return new WaitForSeconds(1);

			foreach (BlockDebris debris in blockDebris) {
				if (debris.gameObject.activeSelf == true && debris.spawnTime + 10f < Time.time) {
					debris.Despawn();
				}
			}

		}
	}

}
