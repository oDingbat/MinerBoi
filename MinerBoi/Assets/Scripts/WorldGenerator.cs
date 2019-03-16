using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Linq;

public class WorldGenerator : MonoBehaviour {

	[Header("References")] [Space(10)]
	public Transform chunkContainer;
	public Player player;

	[Header("Block Infos")] [Space(10)]
	public BlockInfo[] blockInfos;

	[Header("Generation Settings")] [Space(10)]
	public int seed;
	public Vector3 offset;
	public int octaves = 4;
	public float scale = 50.1f;
	public float persistance = 0.5f;        // Controls decrease in amplitude between octaves
	public float lacunarity = 2f;           // The increase in frequency between octaves

	[Header("Chunk Settings")] [Space(10)]
	int chunkCount = 10000;

	// Constants
	int chunkSize = 16;
	List<Chunk> chunksUnloaded = new List<Chunk>();
	Dictionary<Vector3, Chunk> chunksLoaded = new Dictionary<Vector3, Chunk>();
	List<Chunk> chunksLoadPending = new List<Chunk>();

	public Vector3 playerCoordinates;

	public GameObject prefab_Chunk;

	private void Start() {
		InitializeChunks();

		StartCoroutine(ChunkLoadCoroutine());

		LoadChunksNearCoordinates(PositionToCoordinates(player.transform.position));
	}

	IEnumerator ChunkLoadCoroutine () {
		while (true) {
			if (chunksLoadPending.Count > 0) {
				chunksLoadPending[0].LoadChunk(chunkSize, scale, seed, octaves, persistance, lacunarity, offset);

				chunksLoaded.Add(chunksLoadPending[0].coordinates, chunksLoadPending[0]);

				chunksLoadPending.RemoveAt(0);
			}

			yield return new WaitForSeconds(0.005f);
		}
	}

	private void Update () {
		Vector3 newPlayerCoordinates = PositionToCoordinates(player.transform.position);

		if (playerCoordinates != newPlayerCoordinates) {
			LoadChunksNearCoordinates(newPlayerCoordinates);
		}

		playerCoordinates = newPlayerCoordinates;
	}

	private void InitializeChunks () {
		// Creates all of the chunks that can be present at any given time; populates the chunks array

		for (int i = 0; i < chunkCount; i++) {
			Chunk newChunk = Instantiate(prefab_Chunk, Vector3.zero, Quaternion.identity, chunkContainer).GetComponent<Chunk>();
			newChunk.worldGenerator = this;
			chunksUnloaded.Add(newChunk);
		}
	}

	private void LoadChunksNearCoordinates (Vector3 coordinatesCenter) {

		Vector3 coordCurrent = Vector3.zero;

		// Find coordinates of all nearby chunks we need loaded
		List<Vector3> coordinatesNearby = new List<Vector3>();
		for (int x = (int)coordinatesCenter.x - 6; x <= coordinatesCenter.x + 6; x++) {
			for (int y = (int)coordinatesCenter.y - 6; y <= coordinatesCenter.y + 6; y++) {
				for (int z = (int)coordinatesCenter.z - 6; z <= coordinatesCenter.z + 6; z++) {
					coordCurrent = new Vector3(x, y, z);
					coordinatesNearby.Add(coordCurrent);
				}
			}
		}

		// Find Chunks To Unload
		List<Vector3> chunkCoordsToUnload = new List<Vector3>();
		foreach (KeyValuePair<Vector3, Chunk> chunkAndCoords in chunksLoaded) {
			if (coordinatesNearby.Contains(chunkAndCoords.Key) == false) {
				chunkCoordsToUnload.Add(chunkAndCoords.Key);
			} else {
				coordinatesNearby.Remove(chunkAndCoords.Key);       // Remove the key from nearbyCoordinates if this chunk is already loaded
			}
		}
		foreach (Chunk chunkCurrent in chunksLoadPending) {
			if (coordinatesNearby.Contains(chunkCurrent.coordinates) == false) {
				chunkCoordsToUnload.Add(chunkCurrent.coordinates);
			} else {
				coordinatesNearby.Remove(chunkCurrent.coordinates);       // Remove the key from nearbyCoordinates if this chunk is already being loaded
			}
		}

		// Unload chunks out of range
		foreach (Vector3 coords in chunkCoordsToUnload) {
			if (chunksLoaded.ContainsKey(coords)) {
				// Move chunk to chunks unloaded
				chunksUnloaded.Add(chunksLoaded[coords]);

				// Unload chunk and remove from chunksLoaded
				chunksLoaded[coords].UnloadChunk();
				chunksLoaded.Remove(coords);
			} else if (chunksLoadPending.Exists(c => c.coordinates == coords)) {
				Chunk chunkToUnload = chunksLoadPending.Single(c => c.coordinates == coords);
				// Move chunk to chunks unloaded
				chunksUnloaded.Add(chunkToUnload);

				// Unload chunk and remove from chunksLoaded
				chunkToUnload.UnloadChunk();
				chunksLoadPending.Remove(chunkToUnload);
			}
		}

		// Load Chunks
		if (chunksUnloaded.Count > 0) {
			foreach (Vector3 coordOfNewChunk in coordinatesNearby) {
				// Add chunk into chunksLoaded Dictionary
				chunksLoadPending.Add(chunksUnloaded[0]);
				chunksUnloaded[0].SetChunkCoordinates(coordOfNewChunk);

				// Remove chunk from chunksUnloaded
				chunksUnloaded.RemoveAt(0);
			}
		}
	}

	private Vector3 PositionToCoordinates (Vector3 position) {
		int chunkSizeHalf = chunkSize / 2;
		Vector3 coordinates = new Vector3(Mathf.Round((position.x - chunkSizeHalf) / chunkSize), Mathf.Round((position.y - chunkSizeHalf) / chunkSize), Mathf.Round((position.z - chunkSizeHalf) / chunkSize));

		return coordinates;
	}	

	public void UpdateChunk (Vector3 coordinates) {
		if (chunksLoaded.ContainsKey(coordinates)) {
			chunksLoaded[coordinates].UpdateChunk();
		}
	}

}
