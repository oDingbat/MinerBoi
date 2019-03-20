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

	[Header("LayerMasks")] [Space(10)]
	public LayerMask environmentMask;

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
	int chunkCount = 80000;

	// Constants
	int chunkSize = 16;
	List<Chunk> chunksUnloaded = new List<Chunk>();
	List<Coordinates> chunkCoordsQueued = new List<Coordinates>();
	public Dictionary<Coordinates, Chunk> chunksLoaded = new Dictionary<Coordinates, Chunk>();

	SortedList<float, Coordinates> viewDistanceCoordinates = new SortedList<float, Coordinates>();

	public Coordinates playerCoordinates;

	public GameObject prefab_Chunk;

	public float averageTimeTotal = 0;
	public float averageTimeTerrainA = 0;
	public float averageTimeTerrainB = 0;
	public float averageTimeOres = 0;
	public float averageTimeBlockChange = 0;
	public float averageTimeMesh = 0;
	public int averageTimeCount = 0;

	public float chunkTimeTotal = 0;
	public float chunkTimeCount = 0;
	public int chunksSkipped = 0;

	public float findChunkTimeTotal = 0;
	public float findChunkPreUnload = 0;
	public float findChunkTimeQueueLoadUnload = 0;
	public float findChunkUnload = 0;
	public float findChunkTimeCount = 0;

	private void Start() {
		InitializeChunks();
		InitializeViewDistanceCoordinates();

		StartCoroutine(ChunkLoadCoroutine());

		LoadChunksNearCoordinates(PositionToCoordinates(player.transform.position));
	}

	IEnumerator ChunkLoadCoroutine () {
		while (true) {
			if (chunkCoordsQueued.Count > 0) {
				Stopwatch sw = new Stopwatch();
				sw.Start();

				Chunk chunkCurrent = chunksUnloaded[0];
				chunkCurrent.coordinates = chunkCoordsQueued[0];

				bool chunkNotNeeded = true;

				RaycastHit environmentHit;
				Vector3 chunkMidpoint = new Vector3(chunkCurrent.coordinates.x, chunkCurrent.coordinates.y, chunkCurrent.coordinates.z) * chunkSize + new Vector3(8, 8, 8);
				if (Vector3.Distance(chunkMidpoint, player.transform.position) < 32 ||
					Physics.Linecast(player.transform.position, chunkMidpoint, out environmentHit, environmentMask) == false ||
					Physics.Linecast(player.transform.position, chunkMidpoint + new Vector3(chunkSize / 2, chunkSize / 2, chunkSize / 2), out environmentHit, environmentMask) == false ||
					Physics.Linecast(player.transform.position, chunkMidpoint + new Vector3(-chunkSize / 2, chunkSize / 2, chunkSize / 2), out environmentHit, environmentMask) == false ||
					Physics.Linecast(player.transform.position, chunkMidpoint + new Vector3(chunkSize / 2, -chunkSize / 2, chunkSize / 2), out environmentHit, environmentMask) == false ||
					Physics.Linecast(player.transform.position, chunkMidpoint + new Vector3(-chunkSize / 2, -chunkSize / 2, chunkSize / 2), out environmentHit, environmentMask) == false ||
					Physics.Linecast(player.transform.position, chunkMidpoint + new Vector3(chunkSize / 2, chunkSize / 2, -chunkSize / 2), out environmentHit, environmentMask) == false ||
					Physics.Linecast(player.transform.position, chunkMidpoint + new Vector3(-chunkSize / 2, chunkSize / 2, -chunkSize / 2), out environmentHit, environmentMask) == false ||
					Physics.Linecast(player.transform.position, chunkMidpoint + new Vector3(chunkSize / 2, -chunkSize / 2, -chunkSize / 2), out environmentHit, environmentMask) == false ||
					Physics.Linecast(player.transform.position, chunkMidpoint + new Vector3(-chunkSize / 2, -chunkSize / 2, -chunkSize / 2), out environmentHit, environmentMask) == false) {
					chunkNotNeeded = false;
				} else {
					chunkCoordsQueued.RemoveAt(0);
					chunkNotNeeded = true;
				}

				if (chunkNotNeeded == false) {
					// Chunks new neighbors (if they exist)
					Chunk chunkTop = chunksLoaded.ContainsKey(chunkCurrent.coordinates + new Coordinates(0, 1, 0)) ? chunksLoaded[chunkCurrent.coordinates + new Coordinates(0, 1, 0)] : null;
					Chunk chunkBottom = chunksLoaded.ContainsKey(chunkCurrent.coordinates + new Coordinates(0, -1, 0)) ? chunksLoaded[chunkCurrent.coordinates + new Coordinates(0, -1, 0)] : null;
					Chunk chunkLeft = chunksLoaded.ContainsKey(chunkCurrent.coordinates + new Coordinates(-1, 0, 0)) ? chunksLoaded[chunkCurrent.coordinates + new Coordinates(-1, 0, 0)] : null;
					Chunk chunkRight = chunksLoaded.ContainsKey(chunkCurrent.coordinates + new Coordinates(1, 0, 0)) ? chunksLoaded[chunkCurrent.coordinates + new Coordinates(1, 0, 0)] : null;
					Chunk chunkFront = chunksLoaded.ContainsKey(chunkCurrent.coordinates + new Coordinates(0, 0, 1)) ? chunksLoaded[chunkCurrent.coordinates + new Coordinates(0, 0, 1)] : null;
					Chunk chunkBack = chunksLoaded.ContainsKey(chunkCurrent.coordinates + new Coordinates(0, 0, -1)) ? chunksLoaded[chunkCurrent.coordinates + new Coordinates(0, 0, -1)] : null;

					chunkCurrent.SetChunkNeighbors(chunkTop, chunkBottom, chunkLeft, chunkRight, chunkFront, chunkBack);
					chunkCurrent.LoadChunk(seed);

					// Set Neighboring chunks new neighbor chunkCurrent
					if (chunkTop) { chunkTop.chunkBottom = chunkCurrent; chunkTop.GenerateMesh(); }
					if (chunkBottom) { chunkBottom.chunkTop = chunkCurrent; chunkBottom.GenerateMesh(); }
					if (chunkLeft) { chunkLeft.chunkRight = chunkCurrent; chunkLeft.GenerateMesh(); }
					if (chunkRight) { chunkRight.chunkLeft = chunkCurrent; chunkRight.GenerateMesh(); }
					if (chunkFront) { chunkFront.chunkBack = chunkCurrent; chunkFront.GenerateMesh(); }
					if (chunkBack) { chunkBack.chunkFront = chunkCurrent; chunkBack.GenerateMesh(); }

					//UnityEngine.Debug.Log("(" + chunkCoordsQueued[0].x + ", " + chunkCoordsQueued[0].y + ", " + chunkCoordsQueued[0].z + ") - " + chunkCoordsQueued.Count + " - " + chunksLoaded.Count);

					chunksLoaded.Add(chunkCoordsQueued[0], chunksUnloaded[0]);
					chunksUnloaded.RemoveAt(0);
					chunkCoordsQueued.RemoveAt(0);

					sw.Stop();
					
					chunkTimeTotal *= chunkTimeCount;
					chunkTimeTotal += sw.ElapsedMilliseconds;
					chunkTimeCount++;
					chunkTimeTotal /= chunkTimeCount;

					//UnityEngine.Debug.Log("(Total: " + sw.ElapsedMilliseconds + ") (Skipped: " + chunksSkipped + " ) (Average: " + chunkTimeTotal + ")");
				} else {
					chunksSkipped++;
				}
			}

			yield return new WaitForSeconds(0.0001f);
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
			newChunk.chunkSize = chunkSize;
			newChunk.chunkSizeP1 = chunkSize + 1;
			newChunk.chunkSizeM1 = chunkSize - 1;
			chunksUnloaded.Add(newChunk);
		}
	}

	private void InitializeViewDistanceCoordinates () {
		SortedList<float, Coordinates> coordinatesByDistance = new SortedList<float, Coordinates>();
		int radius = 10;
		float i = 0f;

		for (int x = -radius; x <= radius; x++) {
			for (int y = -radius; y <= radius; y++) {
				for (int z = -radius; z <= radius; z++) {
					Vector3 positionCurrent = new Vector3(x, y, z);
					float distanceCurrent = Vector3.Distance(positionCurrent, Vector3.zero);
					if (distanceCurrent <= radius + i) {
						i += 0.0012345678f;
						coordinatesByDistance.Add(distanceCurrent + i, new Coordinates(positionCurrent));
					}
				}
			}
		}

		viewDistanceCoordinates = coordinatesByDistance;
	}

	private void LoadChunksNearCoordinates (Coordinates coordinatesCenter) {
		Stopwatch stopwatchTotal = new Stopwatch();
		stopwatchTotal.Start();

		Dictionary<Coordinates, Chunk> chunksLoadedKeeping = new Dictionary<Coordinates, Chunk>();

		Stopwatch stopwatchPreUnload = new Stopwatch();
		stopwatchPreUnload.Start();
		foreach (KeyValuePair<Coordinates, Chunk> chunkAndCoords in chunksLoaded) {
			chunkAndCoords.Value.queuedForUnload = true;
		}
		stopwatchPreUnload.Stop();

		// Clear queued chunk coords
		chunkCoordsQueued.Clear();

		Stopwatch stopwatchQueueLoadUnload = new Stopwatch();
		stopwatchQueueLoadUnload.Start();
		foreach (KeyValuePair<float, Coordinates> viewCoords in viewDistanceCoordinates) {
			Coordinates chunkCoordinates = coordinatesCenter + viewCoords.Value;

			Chunk chunkCurrent = null;

			if (chunksLoaded.TryGetValue(chunkCoordinates, out chunkCurrent)) {
				chunksLoadedKeeping.Add(chunkCoordinates, chunkCurrent);
				chunkCurrent.queuedForUnload = false;
			} else {
				chunkCoordsQueued.Add(chunkCoordinates);
			}
		}
		stopwatchQueueLoadUnload.Stop();

		Stopwatch stopwatchUnload = new Stopwatch();
		stopwatchUnload.Start();
		// Unload Chunks outside of render distance
		foreach (KeyValuePair<Coordinates, Chunk> chunkAndCoords in chunksLoaded) {
			if (chunkAndCoords.Value.queuedForUnload == true) {
				chunkAndCoords.Value.UnloadChunk();
				chunksUnloaded.Add(chunkAndCoords.Value);
			}
		}
		stopwatchUnload.Stop();

		// Override chunksLoaded dictionary
		chunksLoaded = chunksLoadedKeeping;

		stopwatchTotal.Stop();

		findChunkTimeTotal *= findChunkTimeCount;
		findChunkTimeTotal += stopwatchTotal.ElapsedMilliseconds;
		findChunkPreUnload *= findChunkTimeCount;
		findChunkPreUnload += stopwatchPreUnload.ElapsedMilliseconds;
		findChunkTimeQueueLoadUnload *= findChunkTimeCount;
		findChunkTimeQueueLoadUnload += stopwatchQueueLoadUnload.ElapsedMilliseconds;
		findChunkUnload *= findChunkTimeCount;
		findChunkUnload += stopwatchUnload.ElapsedMilliseconds;

		findChunkTimeCount++;

		findChunkTimeTotal /= findChunkTimeCount;
		findChunkPreUnload /= findChunkTimeCount;
		findChunkTimeQueueLoadUnload /= findChunkTimeCount;
		findChunkUnload /= findChunkTimeCount;

		UnityEngine.Debug.Log("(Total: " + stopwatchTotal.ElapsedMilliseconds + ") (Average: " + findChunkTimeTotal + ")" + ") (PreUnload: " + findChunkPreUnload + ")" + ") (QueueLoadUnload: " + findChunkTimeQueueLoadUnload + ")" + ") (Unload: " + findChunkUnload + ")");
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
