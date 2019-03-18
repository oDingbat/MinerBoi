using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

[RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer)), RequireComponent (typeof(MeshCollider))]
public class Chunk : MonoBehaviour {
	
	[Header("Components")][Space(10)]
	public MeshFilter meshFilter;
	public MeshRenderer meshRenderer;
	public MeshCollider meshCollider;

	[Header("References")][Space(10)]
	public WorldGenerator worldGenerator;

	public int[,] blocksAbove;
	public int[,,] blocks;

	public enum ChunkDirection { Top, Bottom, Left, Right, Front, Back }

	public Chunk chunkTop;
	public Chunk chunkBottom;
	public Chunk chunkLeft;
	public Chunk chunkRight;
	public Chunk chunkFront;
	public Chunk chunkBack;

	public bool meshUpdatePending = false;

	public Coordinates coordinates;
	public bool queuedForUnload;

	const float blockAtlasSize = 4;
	float blockAtlasIncrement = 1f / blockAtlasSize;

	public void SetChunkNeighbors (Chunk chunkTopNew, Chunk chunkBottomNew, Chunk chunkLeftNew, Chunk chunkRightNew, Chunk chunkFrontNew, Chunk chunkBackNew) {
		chunkTop = chunkTopNew;
		chunkBottom = chunkBottomNew;
		chunkLeft = chunkLeftNew;
		chunkRight = chunkRightNew;
		chunkFront = chunkFrontNew;
		chunkBack = chunkBackNew;
	}

	public void LoadChunk (int chunkSize, int seed) {

		transform.position = new Vector3(coordinates.x, coordinates.y, coordinates.z) * chunkSize;
		Vector3 offset = new Vector3(coordinates.x, coordinates.y, coordinates.z) * chunkSize;

		blocks = new int[chunkSize, chunkSize, chunkSize];
		blocksAbove = new int[chunkSize, chunkSize];

		Vector3 chunkPosition = new Vector3(coordinates.x, coordinates.y, coordinates.z) * 16;

		float[,,] noiseMapMountains = Noise.GenerateNoiseMap3D(chunkSize, chunkSize + 1, chunkSize, seed, worldGenerator.noiseSettings_Mountains, offset);
		float[,,] noiseMapElevation = Noise.Convert2DTo3D(Noise.GenerateNoiseMap2D(chunkSize, chunkSize, seed, worldGenerator.noiseSettings_Elevation, new Vector3(offset.x, offset.z)), Mathf.Clamp((worldGenerator.elevationHeight - worldGenerator.elevationBottom) + 1, 0, 2048), worldGenerator.curve_Elevation);

		float[,,] noiseMapGravel = Noise.GenerateNoiseMap3D(chunkSize, chunkSize + 1, chunkSize, seed + 1, worldGenerator.noiseSettings_Gravel, offset);
		float[,,] noiseMapCoal = Noise.GenerateNoiseMap3D(chunkSize, chunkSize + 1, chunkSize, seed + 2, worldGenerator.noiseSettings_Coal, offset);
		float[,,] noiseMapIron = Noise.GenerateNoiseMap3D(chunkSize, chunkSize + 1, chunkSize, seed + 3, worldGenerator.noiseSettings_Iron, offset);

		float[,,] noiseMapElevationSlice = new float[chunkSize, chunkSize + 1, chunkSize];

		// Generate NoiseMapElevationSlice
		for (int x = 0; x < chunkSize; x++) {
			for (int z = 0; z < chunkSize; z++) {
				for (int y = 0; y < chunkSize + 1; y++) {
					if (chunkPosition.y < worldGenerator.elevationBottom) {
						noiseMapElevationSlice[x, y, z] = 1.25f;
					} else if (chunkPosition.y > worldGenerator.elevationBottom + worldGenerator.elevationHeight) {
						noiseMapElevationSlice[x, y, z] = -1.25f;
					} else {
						noiseMapElevationSlice[x, y, z] = noiseMapElevation[x, (int)(chunkPosition.y - worldGenerator.elevationBottom + y), z];
					}
				}
			}
		}

		// Combine NoiseMaps
		float[,,] noiseMapFinal = Noise.CombineNoiseMaps(noiseMapMountains, noiseMapElevationSlice);

		// Generate Blocks
		for (int x = 0; x < chunkSize; x++) {
			for (int z = 0; z < chunkSize; z++) {
				for (int y = 0; y < chunkSize + 1; y++) {
					if (y == chunkSize) {
						if (noiseMapFinal[x, y, z] >= 0f) {
							blocksAbove[x, z] = 1;
						} else {
							blocksAbove[x, z] = 0;
						}
					} else {
						if (noiseMapFinal[x, y, z] >= 0f) {
							blocks[x, y, z] = 1;
						} else {
							blocks[x, y, z] = 0;
						}
					}
				}
			}
		}

		// Generate Grass / Dirt
		for (int x = 0; x < chunkSize; x++) {
			for (int z = 0; z < chunkSize; z++) {
				for (int y = 0; y < chunkSize; y++) {
					if (blocks[x, y, z] == 1 && ((y < chunkSize - 1 && blocks[x, y + 1, z] == 0) || (y == chunkSize - 1 && blocksAbove[x, z] == 0))) {
						blocks[x, y, z] = 3;        // Set Grass
						if (y > 0 && blocks[x, y - 1, z] == 1) {
							blocks[x, y - 1, z] = 2;    // Set Dirt
							if (y > 2 && blocks[x, y - 2, z] == 1) {
								blocks[x, y - 2, z] = 2;    // Set Dirt
							}
						}

					}
				}
			}
		}


		// Generate Ores
		for (int x = 0; x < chunkSize; x++) {
			for (int z = 0; z < chunkSize; z++) {
				for (int y = 0; y < chunkSize; y++) {
					if (blocks[x, y, z] == 1) {
						if (noiseMapGravel[x, y, z] > 0.5f) {               // Gravel
							blocks[x, y, z] = 4;
						} else if (noiseMapCoal[x, y, z] > 1.5f) {          // Coal
							blocks[x, y, z] = 5;
						} else if (noiseMapIron[x, y, z] > 1.5f) {          // Iron
							blocks[x, y, z] = 6;
						}
					}
				}
			}
		}

		GenerateMesh();
	}
	
	public void UnloadChunk () {
		coordinates = new Coordinates(0, 0, 0);
		blocks = new int[0, 0, 0];
		transform.position = Vector3.zero;

		// Clear Chunk Neighbors
		if (chunkTop) { chunkTop.chunkBottom = null; }
		if (chunkBottom) { chunkBottom.chunkTop = null; }
		if (chunkLeft) { chunkLeft.chunkRight = null; }
		if (chunkRight) { chunkRight.chunkLeft = null; }
		if (chunkFront) { chunkFront.chunkBack = null; }
		if (chunkBack) { chunkBack.chunkFront = null; }

		chunkTop = null;
		chunkBottom = null;
		chunkLeft = null;
		chunkRight = null;
		chunkFront = null;
		chunkBack = null;

		ClearMesh();
	}

	public void GenerateMesh () {
		meshUpdatePending = false;

		int width = blocks.GetLength(0);
		int height = blocks.GetLength(1);
		int depth = blocks.GetLength(2);

		// Create Mesh Pieces
		Mesh newMesh = new Mesh();
		List<int> triangles = new List<int>();
		List<Vector3> vertices = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		int vertCount = 0;

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				for (int z = 0; z < depth; z++) {
					if (blocks[x, y, z] != 0) {		// Check if this block is NOT air
						// Top Face
						if (y < height - 1 && blocks[x, y + 1, z] == 0 || (y == height - 1 && chunkTop != null && chunkTop.blocks[x, 0, z] == 0)) {
							vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
							vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
							vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
							vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));

							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 1);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 3);

							Vector2 faceUVPos = worldGenerator.blockInfos[blocks[x, y, z]].uvPosTop * blockAtlasIncrement;
							uvs.Add(new Vector2(faceUVPos.x, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - (faceUVPos.y + blockAtlasIncrement)));
							uvs.Add(new Vector2(faceUVPos.x, 1 - (faceUVPos.y + blockAtlasIncrement)));

							vertCount += 4;
						}

						// Bottom Face
						if (y > 0 && blocks[x, y - 1, z] == 0 || (y == 0 && chunkBottom != null && chunkBottom.blocks[x, 15, z] == 0)) {
							vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
							vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));
							vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f));
							vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));

							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 1);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 3);

							Vector2 faceUVPos = worldGenerator.blockInfos[blocks[x, y, z]].uvPosBottom * blockAtlasIncrement;
							uvs.Add(new Vector2(faceUVPos.x, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - (faceUVPos.y + blockAtlasIncrement)));
							uvs.Add(new Vector2(faceUVPos.x, 1 - (faceUVPos.y + blockAtlasIncrement)));

							vertCount += 4;
						}

						// Left Face
						if (x > 0 && blocks[x - 1, y, z] == 0 || (x == 0 && chunkLeft != null && chunkLeft.blocks[15, y, z] == 0)) {
							vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
							vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));
							vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));
							vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));

							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 1);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 3);

							Vector2 faceUVPos = worldGenerator.blockInfos[blocks[x, y, z]].uvPosSide * blockAtlasIncrement;
							uvs.Add(new Vector2(faceUVPos.x, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - (faceUVPos.y + blockAtlasIncrement)));
							uvs.Add(new Vector2(faceUVPos.x, 1 - (faceUVPos.y + blockAtlasIncrement)));

							vertCount += 4;
						}

						// Right Face
						if (x < width - 1 && blocks[x + 1, y, z] == 0 || (x == width - 1 && chunkRight != null && chunkRight.blocks[0, y, z] == 0)) {
							vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
							vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
							vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f));
							vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));

							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 1);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 3);

							Vector2 faceUVPos = worldGenerator.blockInfos[blocks[x, y, z]].uvPosSide * blockAtlasIncrement;
							uvs.Add(new Vector2(faceUVPos.x, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - (faceUVPos.y + blockAtlasIncrement)));
							uvs.Add(new Vector2(faceUVPos.x, 1 - (faceUVPos.y + blockAtlasIncrement)));

							vertCount += 4;
						}

						// Front Face
						if (z > 0 && blocks[x, y, z - 1] == 0 || (z == 0 && chunkBack != null && chunkBack.blocks[x, y, 15] == 0)) {
							vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f));
							vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f));
							vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f));
							vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f));

							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 1);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 3);

							Vector2 faceUVPos = worldGenerator.blockInfos[blocks[x, y, z]].uvPosSide * blockAtlasIncrement;
							uvs.Add(new Vector2(faceUVPos.x, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - (faceUVPos.y + blockAtlasIncrement)));
							uvs.Add(new Vector2(faceUVPos.x, 1 - (faceUVPos.y + blockAtlasIncrement)));

							vertCount += 4;
						}

						// Back Face
						if (z < depth - 1 && blocks[x, y, z + 1] == 0 || (z == depth - 1 && chunkFront != null && chunkFront.blocks[x, y, 0] == 0)) {
							vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
							vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f));
							vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f));
							vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f));

							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 1);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 3);

							Vector2 faceUVPos = worldGenerator.blockInfos[blocks[x, y, z]].uvPosSide * blockAtlasIncrement;
							uvs.Add(new Vector2(faceUVPos.x, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - (faceUVPos.y + blockAtlasIncrement)));
							uvs.Add(new Vector2(faceUVPos.x, 1 - (faceUVPos.y + blockAtlasIncrement)));

							vertCount += 4;
						}
					}
				}
			}
		}

		// Set Mesh
		newMesh.name = "Chunk Mesh";
		newMesh.vertices = vertices.ToArray();
		newMesh.triangles = triangles.ToArray();
		newMesh.uv = uvs.ToArray();
		newMesh.RecalculateNormals();

		meshFilter.sharedMesh = newMesh;
		meshCollider.sharedMesh = newMesh;
	}	

	public void ClearMesh () {
		meshFilter.sharedMesh = null;
		meshCollider.sharedMesh = null;
	}

	public void SetChunkCoordinates (Coordinates newCoordinates) {
		coordinates = newCoordinates;
	}
	
	public void BreakBlock (Vector3 hitPoint, Vector3 hitNormal, Vector3 hitDirection) {
		Vector3 blockPosition = (hitPoint + hitNormal * -0.05f);
		blockPosition = new Vector3(Mathf.Round(blockPosition.x), Mathf.Round(blockPosition.y), Mathf.Round(blockPosition.z));
		Vector3 chunkPosition = new Vector3(coordinates.x, coordinates.y, coordinates.z) * 16;

		Coordinates blockCoords = new Coordinates(blockPosition.x - chunkPosition.x, blockPosition.y - chunkPosition.y, blockPosition.z - chunkPosition.z);
		BreakBlock(blockCoords, hitDirection, true);
	}

	public void BreakBlock(Coordinates blockCoords, Vector3 hitDirection, bool spawnDebris) {

		Vector3 chunkPosition = new Vector3(coordinates.x, coordinates.y, coordinates.z) * 16;
		Vector3 blockPositionWorld = new Vector3(blockCoords.x, blockCoords.y, blockCoords.z) + chunkPosition;

		int blockType = blocks[blockCoords.x, blockCoords.y, blockCoords.z];
		blocks[blockCoords.x, blockCoords.y, blockCoords.z] = 0;

		meshUpdatePending = true;

		HashSet<Coordinates> blocksUpdating = new HashSet<Coordinates>();

		if (blockCoords.x == 0 && chunkLeft) {
			chunkLeft.meshUpdatePending = true;
			blocksUpdating.Add(new Coordinates(blockCoords.x + 1, blockCoords.y, blockCoords.z));
		} else if (blockCoords.x == 15 && chunkRight) {
			chunkRight.meshUpdatePending = true;
			blocksUpdating.Add(new Coordinates(blockCoords.x - 1, blockCoords.y, blockCoords.z));
		} else {
			blocksUpdating.Add(new Coordinates(blockCoords.x + 1, blockCoords.y, blockCoords.z));
			blocksUpdating.Add(new Coordinates(blockCoords.x - 1, blockCoords.y, blockCoords.z));
		}

		if (blockCoords.y == 0 && chunkBottom) {
			chunkBottom.meshUpdatePending = true;
			blocksUpdating.Add(new Coordinates(blockCoords.x, blockCoords.y + 1, blockCoords.z));
		} else if (blockCoords.y == 15 && chunkTop) {
			chunkTop.meshUpdatePending = true;
			blocksUpdating.Add(new Coordinates(blockCoords.x, blockCoords.y - 1, blockCoords.z));
		} else {
			blocksUpdating.Add(new Coordinates(blockCoords.x, blockCoords.y + 1, blockCoords.z));
			blocksUpdating.Add(new Coordinates(blockCoords.x, blockCoords.y - 1, blockCoords.z));
		}

		if (blockCoords.z == 0 && chunkBack) {
			chunkBack.meshUpdatePending = true;
			blocksUpdating.Add(new Coordinates(blockCoords.x, blockCoords.y, blockCoords.z + 1));
		} else if (blockCoords.z == 15 && chunkFront) {
			chunkFront.meshUpdatePending = true;
			blocksUpdating.Add(new Coordinates(blockCoords.x, blockCoords.y, blockCoords.z - 1));
		} else {
			blocksUpdating.Add(new Coordinates(blockCoords.x, blockCoords.y, blockCoords.z + 1));
			blocksUpdating.Add(new Coordinates(blockCoords.x, blockCoords.y, blockCoords.z - 1));
		}

		UpdateBlocks(blocksUpdating);

		if (blockCoords.x == 0) {
			chunkLeft.UpdateBlock(15, blockCoords.y, blockCoords.z);
		} else if (blockCoords.x == 15) {
			chunkRight.UpdateBlock(0, blockCoords.y, blockCoords.z);
		}

		if (blockCoords.y == 0) {
			chunkBottom.UpdateBlock(blockCoords.x, 15, blockCoords.z);
		} else if (blockCoords.y == 15) {
			chunkTop.UpdateBlock(blockCoords.x, 0, blockCoords.z);
		}

		if (blockCoords.z == 0) {
			chunkBack.UpdateBlock(blockCoords.x, blockCoords.y, 15);
		} else if (blockCoords.z == 15) {
			chunkFront.UpdateBlock(blockCoords.x, blockCoords.y, 0);
		}

		// Spawn Debris
		if (spawnDebris == true) {
			worldGenerator.particleManager.SpawnDebris(blockPositionWorld, hitDirection, worldGenerator.blockInfos[blockType].uvPosSide, blockType);
		}
	}

	public void AttemptPlaceBlock (Vector3 hitPoint, Vector3 hitNormal) {
		Vector3 chunkPosition = new Vector3(coordinates.x, coordinates.y, coordinates.z) * 16;
		Vector3 blockPosition = (hitPoint + hitNormal * -0.05f);

		blockPosition = new Vector3(Mathf.Round(blockPosition.x), Mathf.Round(blockPosition.y), Mathf.Round(blockPosition.z));
		blockPosition += hitNormal;
		blockPosition -= chunkPosition;

		Chunk chunkParent = this;			// Chunk this new block will be a parent of (Default this chunk)

		if (blockPosition.x < 0 && chunkLeft) {
			chunkParent = chunkLeft;
			blockPosition.x += 16;
		} else if (blockPosition.x > 15 && chunkRight) {
			chunkParent = chunkRight;
			blockPosition.x -= 16;
		} else if (blockPosition.y < 0 && chunkBottom) {
			chunkParent = chunkBottom;
			blockPosition.y += 16;
		} else if (blockPosition.y > 15 && chunkTop) {
			chunkParent = chunkTop;
			blockPosition.y -= 16;
		} else if (blockPosition.z < 0 && chunkBack) {
			chunkParent = chunkBack;
			blockPosition.z += 16;
		} else if (blockPosition.z > 15 && chunkFront) {
			chunkParent = chunkFront;
			blockPosition.z -= 16;
		}

		UnityEngine.Debug.Log(blockPosition);
		chunkParent.PlaceBlock(blockPosition);
	}

	public void PlaceBlock (Vector3 blockPos) {

		blocks[(int)blockPos.x, (int)blockPos.y, (int)blockPos.z] = 1;

		meshUpdatePending = true;

		if (blockPos.x == 0 && chunkLeft) {
			chunkLeft.meshUpdatePending = true;
		}

		if (blockPos.x == 15 && chunkRight) {
			chunkRight.meshUpdatePending = true;
		}

		if (blockPos.y == 0 && chunkBottom) {
			chunkBottom.meshUpdatePending = true;
		}

		if (blockPos.y == 15 && chunkTop) {
			chunkTop.meshUpdatePending = true;
		}

		if (blockPos.z == 0 && chunkBack) {
			chunkBack.meshUpdatePending = true;
		}

		if (blockPos.z == 15 && chunkFront) {
			chunkFront.meshUpdatePending = true;
		}
	}

	public void UpdateChunk () {

	}

	public void UpdateBlocks (HashSet<Coordinates> blocksUpdating) {
		foreach (Coordinates blockUpdating in blocksUpdating) {
			UpdateBlock(blockUpdating.x, blockUpdating.y, blockUpdating.z);
		}
	}

	public void UpdateBlock(int x, int y, int z) {
		if (blocks[x, y, z] != 0) {
			switch (blocks[x, y, z]) {
				case (1):   // Stone
					break;
				case (4):   // Gravel
					DropBlock(x, y, z);
					break;
			}
		}
	}

	public void DropBlock (int x, int y, int z) {
		if (y > 0) {
			if (blocks[x, y - 1, z] == 0) {
				BreakBlock(new Coordinates(x, y, z), Vector3.zero, false);
			}
		} else if (chunkBottom != null && chunkBottom.blocks[x, 15, z] == 0) {
			BreakBlock(new Coordinates(x, y, z), Vector3.zero, false);
		}
	}

}
