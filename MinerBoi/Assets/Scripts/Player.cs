using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

	[Header("LayerMasks")][Space(10)]
	public LayerMask blockMask;

	Vector2 rotation;
	Vector3 velocity;

	float attackRange = 5f;

	private void Update () {
		UpdateMovement();
		UpdateBuilding();
	}

	private void UpdateMovement () {
		// Rotation
		rotation.y += Input.GetAxis("Mouse X");
		rotation.x = Mathf.Clamp(rotation.x + -Input.GetAxis("Mouse Y"), -90, 90);

		transform.rotation = Quaternion.Euler(rotation);

		// Movement
		Vector3 movementInput = new Vector3(
			(Input.GetKey(KeyCode.D) == Input.GetKey(KeyCode.A) ? 0 : (Input.GetKey(KeyCode.D) ? 1 : -1)),
			(Input.GetKey(KeyCode.Space) == Input.GetKey(KeyCode.LeftControl) ? 0 : (Input.GetKey(KeyCode.Space) ? 1 : -1)),
			(Input.GetKey(KeyCode.W) == Input.GetKey(KeyCode.S) ? 0 : (Input.GetKey(KeyCode.W) ? 1 : -1)));

		Vector3 velocityDesired = (Quaternion.Euler(0, transform.localEulerAngles.y, 0) * new Vector3(movementInput.x, 0, movementInput.z)) + new Vector3(0, movementInput.y, 0);
		velocityDesired = Vector3.ClampMagnitude(velocityDesired * 15, 15);
		
		velocity = Vector3.Lerp(velocity, velocityDesired, 5 * Time.deltaTime);
		transform.position += velocity * Time.deltaTime;
	}

	private void UpdateBuilding () {
		if (Input.GetMouseButtonDown(0)) {
			RaycastHit hit;
			if (Physics.Raycast(transform.position, transform.forward, out hit, attackRange, blockMask)) {
				hit.transform.gameObject.GetComponent<Chunk>().BreakBlock(hit.point, hit.normal);
			}
		}

		if (Input.GetMouseButtonDown(1)) {
			RaycastHit hit;
			if (Physics.Raycast(transform.position, transform.forward, out hit, attackRange, blockMask)) {
				hit.transform.gameObject.GetComponent<Chunk>().PlaceBlock(hit.point, hit.normal);
			}
		}
	}

}
