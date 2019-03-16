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

	public int[,,] blocksExtended;
	public int[,,] blocks;

	public Vector3 coordinates;

	const float blockAtlasSize = 4;
	float blockAtlasIncrement = 1f / blockAtlasSize;

	public void LoadChunk (int chunkSize, float scale, int seed, int octaves, float persistance, float lacunarity, Vector3 offset) {

		transform.position = coordinates * chunkSize;
		offset += coordinates * chunkSize;

		blocksExtended = new int[chunkSize + 2, chunkSize + 2, chunkSize + 2];

		float[,,] noiseMap3D = Noise.GenerateNoiseMap3D(chunkSize + 2, scale, seed, octaves, persistance, lacunarity, offset + new Vector3(-1, -1, -1));

		// Generate Blocks
		for (int x = 0; x < chunkSize + 2; x++) {
			for (int y = 0; y < chunkSize + 2; y++) {
				for (int z = 0; z < chunkSize + 2; z++) {
					if (noiseMap3D[x, y, z] >= 0f) {
						blocksExtended[x, y, z] = 1;
					} else {
						blocksExtended[x, y, z] = 0;
					}
				}
			}
		}

		// Generate Grass / Dirt
		for (int x = 0; x < chunkSize + 2; x++) {
			for (int z = 0; z < chunkSize + 2; z++) {
				for (int y = 0; y < chunkSize + 1; y++) {
					if (blocksExtended[x, y, z] == 1 && blocksExtended[x, y + 1, z] == 0) {
						blocksExtended[x, y, z] = 3;        // Set Grass
						if (y > 0 && blocksExtended[x, y - 1, z] == 1) {
							blocksExtended[x, y - 1, z] = 2;    // Set Dirt
							if (y > 2 && blocksExtended[x, y - 2, z] == 1) {
								blocksExtended[x, y - 2, z] = 2;    // Set Dirt
							}
						}

					}
				}
			}
		}

		GenerateMesh();

		//UnityEngine.Debug.Log("LoadChunk took: " + swTotal.ElapsedMilliseconds + " {Generate Mesh: " + swGM.ElapsedMilliseconds + "}");
	}

	public void UnloadChunk () {
		coordinates = new Vector3(0, 0, 0);
		blocksExtended = new int[0, 0, 0];
		blocks = new int[0, 0, 0];
		transform.position = Vector3.zero;

		ClearMesh();
	}

	public void GenerateMesh () {		
		int width = blocksExtended.GetLength(0);
		int height = blocksExtended.GetLength(1);
		int depth = blocksExtended.GetLength(2);

		// Create Mesh Pieces
		Mesh newMesh = new Mesh();
		List<int> triangles = new List<int>();
		List<Vector3> vertices = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		int vertCount = 0;

		Vector3 blockOffset = new Vector3(-1, -1, -1);

		for (int x = 1; x < width - 1; x++) {
			for (int y = 1; y < height - 1; y++) {
				for (int z = 1; z < depth - 1; z++) {
					if (blocksExtended[x, y, z] != 0) {		// Check if this block is NOT air
						// Top Face
						if (y < height - 1 && blocksExtended[x, y + 1, z] == 0) {
							vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f) + blockOffset);
							vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) + blockOffset);
							vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f) + blockOffset);
							vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f) + blockOffset);

							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 1);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 3);

							Vector2 faceUVPos = worldGenerator.blockInfos[blocksExtended[x, y, z]].uvPosTop * blockAtlasIncrement;
							uvs.Add(new Vector2(faceUVPos.x, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - (faceUVPos.y + blockAtlasIncrement)));
							uvs.Add(new Vector2(faceUVPos.x, 1 - (faceUVPos.y + blockAtlasIncrement)));

							vertCount += 4;
						}

						// Bottom Face
						if (y > 0 && blocksExtended[x, y - 1, z] == 0) {
							vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f) + blockOffset);
							vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f) + blockOffset);
							vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f) + blockOffset);
							vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f) + blockOffset);

							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 1);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 3);

							Vector2 faceUVPos = worldGenerator.blockInfos[blocksExtended[x, y, z]].uvPosBottom * blockAtlasIncrement;
							uvs.Add(new Vector2(faceUVPos.x, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - (faceUVPos.y + blockAtlasIncrement)));
							uvs.Add(new Vector2(faceUVPos.x, 1 - (faceUVPos.y + blockAtlasIncrement)));

							vertCount += 4;
						}

						// Left Face
						if (x > 0 && blocksExtended[x - 1, y, z] == 0) {
							vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f) + blockOffset);
							vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f) + blockOffset);
							vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f) + blockOffset);
							vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f) + blockOffset);

							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 1);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 3);

							Vector2 faceUVPos = worldGenerator.blockInfos[blocksExtended[x, y, z]].uvPosSide * blockAtlasIncrement;
							uvs.Add(new Vector2(faceUVPos.x, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - (faceUVPos.y + blockAtlasIncrement)));
							uvs.Add(new Vector2(faceUVPos.x, 1 - (faceUVPos.y + blockAtlasIncrement)));

							vertCount += 4;
						}

						// Right Face
						if (x < width - 1 && blocksExtended[x + 1, y, z] == 0) {
							vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f) + blockOffset);
							vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) + blockOffset);
							vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f) + blockOffset);
							vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f) + blockOffset);

							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 1);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 3);

							Vector2 faceUVPos = worldGenerator.blockInfos[blocksExtended[x, y, z]].uvPosSide * blockAtlasIncrement;
							uvs.Add(new Vector2(faceUVPos.x, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - (faceUVPos.y + blockAtlasIncrement)));
							uvs.Add(new Vector2(faceUVPos.x, 1 - (faceUVPos.y + blockAtlasIncrement)));

							vertCount += 4;
						}

						// Front Face
						if (z > 0 && blocksExtended[x, y, z - 1] == 0) {
							vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z - 0.5f) + blockOffset);
							vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z - 0.5f) + blockOffset);
							vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z - 0.5f) + blockOffset);
							vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z - 0.5f) + blockOffset);

							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 1);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 3);

							Vector2 faceUVPos = worldGenerator.blockInfos[blocksExtended[x, y, z]].uvPosSide * blockAtlasIncrement;
							uvs.Add(new Vector2(faceUVPos.x, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - faceUVPos.y));
							uvs.Add(new Vector2(faceUVPos.x + blockAtlasIncrement, 1 - (faceUVPos.y + blockAtlasIncrement)));
							uvs.Add(new Vector2(faceUVPos.x, 1 - (faceUVPos.y + blockAtlasIncrement)));

							vertCount += 4;
						}

						// Back Face
						if (z < depth - 1 && blocksExtended[x, y, z + 1] == 0) {
							vertices.Add(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) + blockOffset);
							vertices.Add(new Vector3(x - 0.5f, y + 0.5f, z + 0.5f) + blockOffset);
							vertices.Add(new Vector3(x - 0.5f, y - 0.5f, z + 0.5f) + blockOffset);
							vertices.Add(new Vector3(x + 0.5f, y - 0.5f, z + 0.5f) + blockOffset);

							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 1);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 0);
							triangles.Add(vertCount + 2);
							triangles.Add(vertCount + 3);

							Vector2 faceUVPos = worldGenerator.blockInfos[blocksExtended[x, y, z]].uvPosSide * blockAtlasIncrement;
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

		// Set Blocks
		blocks = new int[blocksExtended.GetLength(0) - 2, blocksExtended.GetLength(0) - 2, blocksExtended.GetLength(0) - 2];
		for (int i = 0; i < blocksExtended.GetLength(0) - 2; i++) {
			for (int j = 0; j < blocksExtended.GetLength(1) - 2; j++) {
				for (int k = 0; k < blocksExtended.GetLength(2) - 2; k++) {
					blocks[i, j, k] = blocksExtended[i + 1, j + 1, k + 1];
				}
			}
		}
	}	

	public void ClearMesh () {
		meshFilter.sharedMesh = null;
		meshCollider.sharedMesh = null;
	}

	public void SetChunkCoordinates (Vector3 newCoordinates) {
		coordinates = newCoordinates;
	}

	public void BreakBlock (Vector3 hitPoint, Vector3 hitNormal) {

		Vector3 chunkPosition = coordinates * 16;
		Vector3 blockPosition = (hitPoint + hitNormal * -0.05f);
		blockPosition = new Vector3(Mathf.Round(blockPosition.x), Mathf.Round(blockPosition.y), Mathf.Round(blockPosition.z));
		blockPosition -= chunkPosition;

		blocks[(int)blockPosition.x, (int)blockPosition.y, (int)blockPosition.z] = 0;
		blocksExtended[(int)blockPosition.x + 1, (int)blockPosition.y + 1, (int)blockPosition.z + 1] = blocks[(int)blockPosition.x, (int)blockPosition.y, (int)blockPosition.z];

		GenerateMesh();

		if (blockPosition.x == 0) {
			//WorldGenerator.UpdateChunk
		}
	}
	
	public void PlaceBlock (Vector3 hitPoint, Vector3 hitNormal) {
		Vector3 chunkPosition = coordinates * 16;
		Vector3 blockPosition = (hitPoint + hitNormal * -0.05f);
		blockPosition = new Vector3(Mathf.Round(blockPosition.x), Mathf.Round(blockPosition.y), Mathf.Round(blockPosition.z));
		blockPosition += hitNormal;
		blockPosition -= chunkPosition;

		blocks[(int)blockPosition.x, (int)blockPosition.y, (int)blockPosition.z] = 1;
		blocksExtended[(int)blockPosition.x + 1, (int)blockPosition.y + 1, (int)blockPosition.z + 1] = blocks[(int)blockPosition.x, (int)blockPosition.y, (int)blockPosition.z];

		GenerateMesh();
	}

	public void UpdateChunk () {

	}

}
