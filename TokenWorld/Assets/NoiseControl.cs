using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseControl : MonoBehaviour
{
	ParticleSystem _ps;
	public float _speed = 0.1f;
	public float _to_val = 0f, _from_val = 1f;

	// Start is called before the first frame update
	void Start()
	{
		_ps = GetComponent<ParticleSystem>();
	}

	// Update is called once per frame
	void Update() {
		var noise = _ps.noise;
		noise.strength = Map(Mathf.PerlinNoise(Time.time * _speed, 0), _to_val, _from_val);
	}

	float Map(float value, float fromTarget, float toTarget, float fromSource = 0f, float toSource = 1.0f) {
		return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
	}
}
