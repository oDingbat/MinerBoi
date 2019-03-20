using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public static class Noise {

	public static float[,] GenerateNoiseMap2D (int width, int height, int seed, NoiseSettings noiseSettings, Vector3 offset) {
		float[,] noiseMap2D = new float[width, height];

		System.Random prng = new System.Random(seed);
		Vector2[] octaveOffsets = new Vector2[noiseSettings.octaves];
		
		float maxNoiseHeight = 0;
		float[] frequencyScaleTable = new float[noiseSettings.octaves];

		for (int i = 0; i < noiseSettings.octaves; i++) {
			float offsetX = prng.Next(-100000, 100000) + offset.x;
			float offsetY = prng.Next(-100000, 100000) + offset.y;

			octaveOffsets[i] = new Vector2(offsetX, offsetY);
			frequencyScaleTable[i] = noiseSettings.frequencyTable[i] / noiseSettings.scale;

			maxNoiseHeight += noiseSettings.amplitudeTable[i];
		}

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				
				float noiseHeight = 0;

				for (int i = 0; i < noiseSettings.octaves; i++) {
					float sampleX = (x + octaveOffsets[i].x) * frequencyScaleTable[i];
					float sampleY = (y + octaveOffsets[i].y) * frequencyScaleTable[i];

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);

					noiseHeight += perlinValue * noiseSettings.amplitudeTable[i];
				}

				noiseMap2D[x, y] = (noiseHeight / maxNoiseHeight);
			}
		}

		return noiseMap2D;
	}

	public static float[,,] GenerateNoiseMap3D (int width, int height, int depth, int seed, NoiseSettings noiseSettings, Vector3 offset, bool debugTimer = false) {

		float[,,] noiseMap3D = new float[width, height, depth];

		System.Random prng = new System.Random(seed);
		Vector3[] octaveOffsets = new Vector3[noiseSettings.octaves];

		float maxNoiseHeight = 0;
		float[] frequencyScaleTable = new float[noiseSettings.octaves];

		for (int i = 0; i < noiseSettings.octaves; i++) {
			float offsetX = prng.Next(-100000, 100000) + offset.x;
			float offsetY = prng.Next(-100000, 100000) + offset.y;
			float offsetZ = prng.Next(-100000, 100000) + offset.z;

			octaveOffsets[i] = new Vector3(offsetX, offsetY, offsetZ);
			frequencyScaleTable[i] = noiseSettings.frequencyTable[i] / noiseSettings.scale;

			maxNoiseHeight += noiseSettings.amplitudeTable[i];
		}

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				for (int z = 0; z < depth; z++) {
					
					float noiseHeight = 0;

					for (int i = 0; i < noiseSettings.octaves; i++) {
						float sampleX = (x + octaveOffsets[i].x) * frequencyScaleTable[i];
						float sampleY = (y + octaveOffsets[i].y) * frequencyScaleTable[i];
						float sampleZ = (z + octaveOffsets[i].z) * frequencyScaleTable[i];

						float perlinValue = Perlin3D(sampleX, sampleY, sampleZ);

						noiseHeight += perlinValue * noiseSettings.amplitudeTable[i];
					}

					noiseMap3D[x, y, z] = ((noiseHeight / maxNoiseHeight) - 0.5f) * 12.5f; ;
				}
			}
		}
		
		return noiseMap3D;
	}

	public static float[,,] Convert2DTo3D (float[,] noiseMap2D, int height, AnimationCurve curve) {
		int width = noiseMap2D.GetLength(0);
		int depth = noiseMap2D.GetLength(1);
		
		float[,,] noiseMap3D = new float[width, height, depth];

		for (int x = 0; x < width; x++) {
			for (int z = 0; z < depth; z++) {
				for (int y = 0; y < height; y++) {
					if (y > ((float)height * curve.Evaluate(noiseMap2D[x, z]))) {
					//if (y > ((float)height * noiseMap2D[x, z])) {
						noiseMap3D[x, y, z] = -0.8f;
					} else {
						noiseMap3D[x, y, z] = 1.5f;
					}

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
					newNoiseMap[x, y, z] = (noiseMapA[x, y, z] + noiseMapB[x, y, z]) / 2f;
				}
			}
		}

		return newNoiseMap;
	}

	public static float Perlin3D (float x, float y, float z) {
		/*
		float ab = Mathf.PerlinNoise(x, y);
		float bc = Mathf.PerlinNoise(y, z);
		float ac = Mathf.PerlinNoise(x, z);

		float ba = Mathf.PerlinNoise(y, x);
		float cb = Mathf.PerlinNoise(z, y);
		float ca = Mathf.PerlinNoise(z, x);

		float abc = ab + bc + ac + ba + cb + ca;
		return abc / 6f;
		*/

		return (Mathf.PerlinNoise(x, y) + Mathf.PerlinNoise(y, z) + Mathf.PerlinNoise(x, z) + Mathf.PerlinNoise(y, x) + Mathf.PerlinNoise(z, y) + Mathf.PerlinNoise(z, x))  / 6f;
	}

}

[System.Serializable]
public class NoiseSettings {
	
	public float scale;
	public int octaves;
	public float[] amplitudeTable;
	public float[] frequencyTable;

	public NoiseSettings (float scale, int octaves, float persistance, float lacunarity) {

	}

}
