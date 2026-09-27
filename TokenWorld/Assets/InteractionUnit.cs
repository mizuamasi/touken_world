using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionUnit : MonoBehaviour
{
	[SerializeField] ParticleSystem _particle;
	[SerializeField] SpriteRenderer _sp;

	bool _active = false;
	Vector3 _sp_sca;
	Color _sp_color;
	float _sp_color_a, _sp_color_tag, _sp_color_vec;

	float _noise_pos_sca, _noise_pos_color;

	// Start is called before the first frame update
	void Start()
    {
		_sp_sca = _sp.transform.localScale;
		_sp_color = _sp.color;
		_sp_color_a = 0;
		_sp_color_tag = 0;
		_noise_pos_sca = Random.Range(-100f, 100);
		_noise_pos_color = Random.Range(-100f, 100);
	}

    // Update is called once per frame
    void Update()
    {
		_noise_pos_sca += 0.025f;
		_sp.transform.localScale = _sp_sca * (1.0f+(Mathf.PerlinNoise(_noise_pos_sca, 0f)*1f));

		_noise_pos_color += 0.1f;
		_sp.color = new Color(_sp_color.r, _sp_color.g, _sp_color.b,
			(_sp_color.a + Mathf.PerlinNoise(_noise_pos_color, 0f) * 0.2f)) * _sp_color_a;

		_sp_color_a = Mathf.SmoothDamp(_sp_color_a, _sp_color_tag, ref _sp_color_vec, 0.25f);
	}

	public void SetAvtive(bool flag) {
		if(!_active && flag) {
			_sp_color_tag = 1f;
			_particle.Play();
		}

		if(_active && !flag) {
			_sp_color_tag = 0f;
			_particle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
		}

		_active = flag;
	}

	public void SetWorldPostion(Vector3 pos) {
		Vector3 screen_pos = Camera.main.WorldToScreenPoint(pos);
		screen_pos.z = 10f;
		transform.position = Camera.main.ScreenToWorldPoint(screen_pos);
	}

	public void DestroyProc() {
		StartCoroutine(DestroyCoroutine());
	}

	IEnumerator DestroyCoroutine() {
		yield return new WaitForSeconds(3f);
		Destroy(gameObject);
	}
}
