using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (MeshFilter))]
public class BlockDebris : PhysicsParticle {

	MeshFilter meshFilter;

	int blockType = 0;

	const float size = 0.25f;
	const float sizeH = 0.125f;

	public override void Initialize(ParticleManager _particleManager) {
		particleManager = _particleManager;

		// Fetch References
		rigidbody = GetComponent<Rigidbody>();
		boxCollider = GetComponent<BoxCollider>();
		meshFilter = GetComponent<MeshFilter>();

		Mesh newMesh = new Mesh();

		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();

		// Left Face
		vertices.Add(new Vector3(-sizeH, -sizeH, -sizeH));
		vertices.Add(new Vector3(-sizeH, sizeH, -sizeH));
		vertices.Add(new Vector3(-sizeH, sizeH, sizeH));
		vertices.Add(new Vector3(-sizeH, -sizeH, sizeH));

		triangles.Add(1);
		triangles.Add(3);
		triangles.Add(2);
		triangles.Add(1);
		triangles.Add(0);
		triangles.Add(3);

		// Right Face
		vertices.Add(new Vector3(sizeH, -sizeH, sizeH));
		vertices.Add(new Vector3(sizeH, sizeH, sizeH));
		vertices.Add(new Vector3(sizeH, sizeH, -sizeH));
		vertices.Add(new Vector3(sizeH, -sizeH, -sizeH));

		triangles.Add(5);
		triangles.Add(7);
		triangles.Add(6);
		triangles.Add(5);
		triangles.Add(4);
		triangles.Add(7);

		// Top Face
		vertices.Add(new Vector3(-sizeH, sizeH, sizeH));
		vertices.Add(new Vector3(-sizeH, sizeH, -sizeH));
		vertices.Add(new Vector3(sizeH, sizeH, -sizeH));
		vertices.Add(new Vector3(sizeH, sizeH, sizeH));

		triangles.Add(9);
		triangles.Add(11);
		triangles.Add(10);
		triangles.Add(9);
		triangles.Add(8);
		triangles.Add(11);

		// Bottom Face
		vertices.Add(new Vector3(-sizeH, -sizeH, -sizeH));
		vertices.Add(new Vector3(-sizeH, -sizeH, sizeH));
		vertices.Add(new Vector3(sizeH, -sizeH, sizeH));
		vertices.Add(new Vector3(sizeH, -sizeH, -sizeH));

		triangles.Add(13);
		triangles.Add(15);
		triangles.Add(14);
		triangles.Add(13);
		triangles.Add(12);
		triangles.Add(15);

		// Back Face
		vertices.Add(new Vector3(sizeH, -sizeH, -sizeH));
		vertices.Add(new Vector3(sizeH, sizeH, -sizeH));
		vertices.Add(new Vector3(-sizeH, sizeH, -sizeH));
		vertices.Add(new Vector3(-sizeH, -sizeH, -sizeH));

		triangles.Add(17);
		triangles.Add(19);
		triangles.Add(18);
		triangles.Add(17);
		triangles.Add(16);
		triangles.Add(19);

		// Front Face
		vertices.Add(new Vector3(-sizeH, -sizeH, sizeH));
		vertices.Add(new Vector3(-sizeH, sizeH, sizeH));
		vertices.Add(new Vector3(sizeH, sizeH, sizeH));
		vertices.Add(new Vector3(sizeH, -sizeH, sizeH));

		triangles.Add(21);
		triangles.Add(23);
		triangles.Add(22);
		triangles.Add(21);
		triangles.Add(20);
		triangles.Add(23);

		newMesh.vertices = vertices.ToArray();
		newMesh.triangles = triangles.ToArray();
		newMesh.RecalculateNormals();

		boxCollider.size = new Vector3(size, size, size);

		meshFilter.sharedMesh = newMesh;
	}
	
	public override void Spawn (Vector3 position, Vector3 velocity) {
		transform.position = position;
		rigidbody.velocity = velocity;
		spawnTime = Time.time;
	}

	public override void Despawn() {
		gameObject.SetActive(false);
	}

	public void SetupUVs (Vector2 uvPos, int newBlockType) {
		if (blockType != newBlockType) {
			blockType = newBlockType;

			// Generate New UVs
			Vector2 blockUVPos = new Vector2(uvPos.x * 0.25f, 0.75f - (uvPos.y * 0.25f));
			Vector2 midpoint = new Vector2(0.125f, 0.125f);
			float uvDebrisSize = sizeH / 4;
			Vector2[] uvs = new Vector2[24];
			for (int i = 0; i < 6; i++) {
				uvs[0 + (i * 4)] = new Vector2(uvDebrisSize, -uvDebrisSize) + midpoint + blockUVPos;
				uvs[1 + (i * 4)] = new Vector2(uvDebrisSize, uvDebrisSize) + midpoint + blockUVPos;
				uvs[2 + (i * 4)] = new Vector2(-uvDebrisSize, uvDebrisSize) + midpoint + blockUVPos;
				uvs[3 + (i * 4)] = new Vector2(-uvDebrisSize, -uvDebrisSize) + midpoint + blockUVPos;
			}

			meshFilter.sharedMesh.uv = uvs;
		}
	}

}
