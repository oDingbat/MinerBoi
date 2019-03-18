using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Coordinates {

	public int x;
	public int y;
	public int z;

	public Coordinates (int _x, int _y, int _z) {
		x = _x;
		y = _y;
		z = _z;
	}

	public Coordinates (Vector3 vector) {
		x = (int)vector.x;
		y = (int)vector.y;
		z = (int)vector.z;
	}

	public Coordinates (float _x, float _y, float _z) {
		x = (int)_x;
		y = (int)_y;
		z = (int)_z;
	}

	public static bool operator == (Coordinates c1, Coordinates c2) {
		return (c1.x == c2.x && c1.y == c2.y && c1.z == c2.z);
	}

	public static bool operator != (Coordinates c1, Coordinates c2) {
		return (c1.x != c2.x || c1.y != c2.y || c1.z != c2.z);
	}

	public static Coordinates operator + (Coordinates c1, Coordinates c2) {
		return new Coordinates(c1.x + c2.x, c1.y + c2.y, c1.z + c2.z);
	}

	public static Coordinates operator - (Coordinates c1, Coordinates c2) {
		return new Coordinates(c1.x - c2.x, c1.y - c2.y, c1.z - c2.z);
	}

}
