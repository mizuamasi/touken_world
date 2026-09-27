using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmissionControl : MonoBehaviour
{
	ParticleSystem _ps;
	public float _speed = 0.1f;
	public float  _to_val = 0f, _from_val = 100f;

	System.DateTime _prevTime;
	int _next_sec = 10;

	// Start is called before the first frame update
	void Start()
	{
		_ps = GetComponent<ParticleSystem>();
		_prevTime = System.DateTime.Now;
	}

	// Update is called once per frame
	void Update()
	{
		var emission = _ps.emission;

		if((System.DateTime.Now- _prevTime).TotalSeconds > _next_sec) {
			_prevTime = System.DateTime.Now;
			if(Mathf.PerlinNoise(Time.time * _speed, 0) > 0.5f) {
				emission.rateOverTime = _from_val;
				_next_sec = 20;
			}
			else {
				emission.rateOverTime = _to_val;
				_next_sec = 5;
			}
		}
	}

	float Map(float value, float fromTarget, float toTarget, float fromSource = 0f, float toSource = 1.0f) {
		return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
	}
}
