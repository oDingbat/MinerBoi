using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Rigidbody))][RequireComponent (typeof (BoxCollider))]
public abstract class PhysicsParticle : Particle {

	[Header("Components")][Space(10)]
	public Rigidbody rigidbody;
	public BoxCollider boxCollider;

	public override void Initialize (ParticleManager _particleManager) {
		particleManager = _particleManager;

		// Fetch References
		rigidbody = GetComponent<Rigidbody>();
		boxCollider = GetComponent<BoxCollider>();
	}

}
