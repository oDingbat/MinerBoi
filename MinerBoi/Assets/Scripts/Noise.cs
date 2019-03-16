using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise {
 
	public static float[,] GenerateNoiseMap (int mapSize, float mapScale, int seed, int octaves, float persistance, float lacunarity) {
		float[,] noiseMap = new float[mapSize, mapSize];

		if (mapScale <= 0) {
			mapScale = 0.001f;
		}

		float maxNoiseHeight = float.MinValue;
		float minNoiseHeight = float.MaxValue;

		for (int x = 0; x < mapSize; x++) {
			for (int y = 0; y < mapSize; y++) {

				float amplitude = 1;
				float frequency = 1;
				float noiseHeight = 0;

				for (int i = 0; i < octaves; i++) {
					float sampleX = x / mapScale * frequency;
					float sampleY = y / mapScale * frequency;

					float perlinValue = (Mathf.PerlinNoise(sampleX, sampleY) * 2) - 1;

					noiseHeight += perlinValue * amplitude;
					
					// Increase amplitude and frequency
					amplitude *= persistance;
					frequency *= lacunarity;
				}

				if (noiseHeight > maxNoiseHeight) {
					maxNoiseHeight = noiseHeight;
				} else if (noiseHeight < minNoiseHeight) {
					minNoiseHeight = noiseHeight;
				}

				noiseMap[x, y] = noiseHeight;
			}
		}

		for (int x = 0; x < mapSize; x++) {
			for (int y = 0; y < mapSize; y++) {
				noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
			}
		}

		return noiseMap;
	}

	public static float[,,] GenerateNoiseMap3D (int chunkSize, float chunkScale, int seed, int octaves, float persistance, float lacunarity, Vector3 offset) {
		float[,,] noiseMap3D = new float[chunkSize, chunkSize, chunkSize];

		Vector3 midpointOffset = -new Vector3(chunkSize / 2, chunkSize / 2, chunkSize / 2);

		System.Random prng = new System.Random(seed);
		Vector3[] octaveOffsets = new Vector3[octaves];

		float amplitude = 1;
		float frequency = 1;
		float maxNoiseHeight = 0;

		for (int i = 0; i < octaves; i++) {
			float offsetX = prng.Next(-100000, 100000) + offset.x;
			float offsetY = prng.Next(-100000, 100000) + offset.y;
			float offsetZ = prng.Next(-100000, 100000) + offset.z;
			octaveOffsets[i] = new Vector3(offsetX, offsetY, offsetZ);

			maxNoiseHeight += amplitude;
			amplitude *= persistance;
		}

		if (chunkScale <= 0) {
			chunkScale = 0.001f;
		}

		for (int x = 0; x < chunkSize; x++) {
			for (int y = 0; y < chunkSize; y++) {
				for (int z = 0; z < chunkSize; z++) {

					amplitude = 1;
					frequency = 1;
					float noiseHeight = 0;

					for (int i = 0; i < octaves; i++) {
						float sampleX = ((x + octaveOffsets[i].x + midpointOffset.x) / chunkScale) * frequency;
						float sampleY = ((y + octaveOffsets[i].y + midpointOffset.y) / chunkScale) * frequency;
						float sampleZ = ((z + octaveOffsets[i].z + midpointOffset.z) / chunkScale) * frequency;

						float perlinValue = Perlin3D(sampleX, sampleY, sampleZ);

						noiseHeight += perlinValue * amplitude;

						// Increase amplitude and frequency
						amplitude *= persistance;
						frequency *= lacunarity;
					}

					noiseMap3D[x, y, z] = noiseHeight;
				}
			}
		}

		for (int x = 0; x < chunkSize; x++) {
			for (int y = 0; y < chunkSize; y++) {
				for (int z = 0; z < chunkSize; z++) {
					noiseMap3D[x, y, z] = (noiseMap3D[x, y, z] / maxNoiseHeight) - 0.5f;
				}
			}
		}

		return noiseMap3D;
	}

	public static float[,,] CombineNoiseMaps (float[,,] noiseMapA, float[,,] noiseMapB) {
		int width = noiseMapA.GetLength(0);
		int height = noiseMapA.GetLength(1);
		int depth = noiseMapA.GetLength(2);

		float[,,] newNoiseMap = new float[width, height, depth];

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				for (int z = 0; z < depth; z++) {
					newNoiseMap[x, y, z] = noiseMapA[x, y, z] + noiseMapB[x, y, z];
				}
			}
		}

		return newNoiseMap;
	}

	public static float Perlin3D (float x, float y, float z) {
		float ab = Mathf.PerlinNoise(x, y);
		float bc = Mathf.PerlinNoise(y, z);
		float ac = Mathf.PerlinNoise(x, z);

		float ba = Mathf.PerlinNoise(y, x);
		float cb = Mathf.PerlinNoise(z, y);
		float ca = Mathf.PerlinNoise(z, x);

		float abc = ab + bc + ac + ba + cb + ca;
		return abc / 6f;
	}

}
