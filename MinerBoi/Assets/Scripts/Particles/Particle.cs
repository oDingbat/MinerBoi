using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Particle : MonoBehaviour {

	public ParticleManager particleManager;
	public float spawnTime = 0;

	public virtual void Initialize (ParticleManager _particleManager) {
		particleManager = _particleManager;
	}

	public virtual void Spawn (Vector3 position, Vector3 velocity) {
		transform.position = position;
		spawnTime = Time.time;
	}

	public virtual void Despawn () {
		gameObject.SetActive(false);
	}

}
