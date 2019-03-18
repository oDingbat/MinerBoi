using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Linq;

public class WorldGenerator : MonoBehaviour {

	[Header("References")] [Space(10)]
	public ParticleManager particleManager;
	public Transform chunkContainer;
	public Player player;

	[Header("Block Infos")] [Space(10)]
	public BlockInfo[] blockInfos;

	[Header("Generation Settings")] [Space(10)]
	public int seed;
	public AnimationCurve curve_Elevation;
	public int elevationBottom = -96;
	public int elevationHeight = 256;
	public NoiseSettings noiseSettings_Mountains;
	public NoiseSettings noiseSettings_Elevation;
	public NoiseSettings noiseSettings_Gravel;
	public NoiseSettings noiseSettings_Caves;

	[Header("Ores")] [Space(10)]
	public NoiseSettings noiseSettings_Coal;
	public NoiseSettings noiseSettings_Iron;

	[Header("Chunk Settings")] [Space(10)]
	int chunkCount = 10000;

	// Constants
	int chunkSize = 16;
	List<Chunk> chunksUnloaded = new List<Chunk>();
	List<Coordinates> chunkCoordsQueued = new List<Coordinates>();
	public Dictionary<Coordinates, Chunk> chunksLoaded = new Dictionary<Coordinates, Chunk>();

	public Coordinates playerCoordinates;

	public GameObject prefab_Chunk;

	private void Start() {
		InitializeChunks();

		StartCoroutine(ChunkLoadCoroutine());

		LoadChunksNearCoordinates(PositionToCoordinates(player.transform.position));
	}

	IEnumerator ChunkLoadCoroutine () {
		while (true) {
			if (chunkCoordsQueued.Count > 0) {
				Chunk chunkCurrent = chunksUnloaded[0];
				chunkCurrent.coordinates = chunkCoordsQueued[0];

				// Chunks new neighbors (if they exist)
				Chunk chunkTop = chunksLoaded.ContainsKey(chunkCurrent.coordinates + new Coordinates(0, 1, 0)) ? chunksLoaded[chunkCurrent.coordinates + new Coordinates(0, 1, 0)] : null;
				Chunk chunkBottom = chunksLoaded.ContainsKey(chunkCurrent.coordinates + new Coordinates(0, -1, 0)) ? chunksLoaded[chunkCurrent.coordinates + new Coordinates(0, -1, 0)] : null;
				Chunk chunkLeft = chunksLoaded.ContainsKey(chunkCurrent.coordinates + new Coordinates(-1, 0, 0)) ? chunksLoaded[chunkCurrent.coordinates + new Coordinates(-1, 0, 0)] : null;
				Chunk chunkRight = chunksLoaded.ContainsKey(chunkCurrent.coordinates + new Coordinates(1, 0, 0)) ? chunksLoaded[chunkCurrent.coordinates + new Coordinates(1, 0, 0)] : null;
				Chunk chunkFront = chunksLoaded.ContainsKey(chunkCurrent.coordinates + new Coordinates(0, 0, 1)) ? chunksLoaded[chunkCurrent.coordinates + new Coordinates(0, 0, 1)] : null;
				Chunk chunkBack = chunksLoaded.ContainsKey(chunkCurrent.coordinates + new Coordinates(0, 0, -1)) ? chunksLoaded[chunkCurrent.coordinates + new Coordinates(0, 0, -1)] : null;

				chunkCurrent.SetChunkNeighbors(chunkTop, chunkBottom, chunkLeft, chunkRight, chunkFront, chunkBack);
				chunkCurrent.LoadChunk(chunkSize, seed);

				// Set Neighboring chunks new neighbor chunkCurrent
				if (chunkTop) {		chunkTop.chunkBottom = chunkCurrent; chunkTop.GenerateMesh(); }
				if (chunkBottom) {	chunkBottom.chunkTop = chunkCurrent; chunkBottom.GenerateMesh(); }
				if (chunkLeft) {	chunkLeft.chunkRight = chunkCurrent; chunkLeft.GenerateMesh(); }
				if (chunkRight) {	chunkRight.chunkLeft = chunkCurrent; chunkRight.GenerateMesh(); }
				if (chunkFront) {	chunkFront.chunkBack = chunkCurrent; chunkFront.GenerateMesh(); }
				if (chunkBack) {	chunkBack.chunkFront = chunkCurrent; chunkBack.GenerateMesh(); }

				//UnityEngine.Debug.Log("(" + chunkCoordsQueued[0].x + ", " + chunkCoordsQueued[0].y + ", " + chunkCoordsQueued[0].z + ") - " + chunkCoordsQueued.Count + " - " + chunksLoaded.Count);

				chunksLoaded.Add(chunkCoordsQueued[0], chunksUnloaded[0]);
				chunksUnloaded.RemoveAt(0);
				chunkCoordsQueued.RemoveAt(0);
			}

			yield return new WaitForSeconds(0.00125f);
		}
		
	}

	private void Update () {
		Coordinates newPlayerCoordinates = PositionToCoordinates(player.transform.position);

		if (playerCoordinates != newPlayerCoordinates) {
			LoadChunksNearCoordinates(newPlayerCoordinates);
		}

		playerCoordinates = newPlayerCoordinates;
	}

	private void LateUpdate () {
		foreach (KeyValuePair<Coordinates, Chunk> chunkAndCoords in chunksLoaded) {
			if (chunkAndCoords.Value.meshUpdatePending == true) {
				chunkAndCoords.Value.GenerateMesh();
			}
		}
	}
	
	private void InitializeChunks () {
		// Creates all of the chunks that can be present at any given time; populates the chunks array

		for (int i = 0; i < chunkCount; i++) {
			Chunk newChunk = Instantiate(prefab_Chunk, Vector3.zero, Quaternion.identity, chunkContainer).GetComponent<Chunk>();
			newChunk.worldGenerator = this;
			chunksUnloaded.Add(newChunk);
		}
	}

	private void LoadChunksNearCoordinates (Coordinates coordinatesCenter) {
		Stopwatch sw = new Stopwatch();

		sw.Start();

		Dictionary<Coordinates, Chunk> chunksLoadedKeeping = new Dictionary<Coordinates, Chunk>();

		foreach (KeyValuePair<Coordinates, Chunk> chunkAndCoords in chunksLoaded) {
			chunkAndCoords.Value.queuedForUnload = true;
		}

		// Clear queued chunk coords
		chunkCoordsQueued.Clear();

		int radius = 3;

		Stopwatch swA = new Stopwatch();
		swA.Start();

		for (int x = -radius; x <= radius; x++) {
			for (int y = -radius; y <= radius; y++) {
				for (int z = -radius; z <= radius; z++) {
					Coordinates chunkCoordinates = coordinatesCenter + new Coordinates(x, y, z);

					Chunk chunkCurrent = null;

					if (chunksLoaded.TryGetValue(chunkCoordinates, out chunkCurrent)) {
						chunksLoadedKeeping.Add(chunkCoordinates, chunkCurrent);
						chunkCurrent.queuedForUnload = false;
					} else {
						chunkCoordsQueued.Add(chunkCoordinates);
					}
				}
			}
		}

		swA.Stop();

		// Unload Chunks outside of render distance
		foreach (KeyValuePair<Coordinates, Chunk> chunkAndCoords in chunksLoaded) {
			if (chunkAndCoords.Value.queuedForUnload == true) {
				chunkAndCoords.Value.UnloadChunk();
				chunksUnloaded.Add(chunkAndCoords.Value);
			}
		}

		// Override chunksLoaded dictionary
		chunksLoaded = chunksLoadedKeeping;

		sw.Stop();
		//UnityEngine.Debug.Log("(Totol: " + sw.ElapsedMilliseconds + ") (A: " + swA.ElapsedMilliseconds + ")");
	}

	private Coordinates PositionToCoordinates(Vector3 pos) {
		return PositionToCoordinates(pos.x, pos.y, pos.z);
	}

	private Coordinates PositionToCoordinates (float x, float y, float z) {
		int chunkSizeHalf = chunkSize / 2;
		Coordinates coordinates = new Coordinates(Mathf.Round((x - chunkSizeHalf) / chunkSize), Mathf.Round((y - chunkSizeHalf) / chunkSize), Mathf.Round((z - chunkSizeHalf) / chunkSize));

		return coordinates;
	}	

	public void UpdateChunk (Coordinates coordinates) {
		if (chunksLoaded.ContainsKey(coordinates)) {
			chunksLoaded[coordinates].UpdateChunk();
		}
	}

}
