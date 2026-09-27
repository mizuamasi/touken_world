using UnityEngine;
using System.Collections;

public class MouseSynchronizeObjectScript : MonoBehaviour {
	// 位置座標
	private Vector3 position;
	// スクリーン座標をワールド座標に変換した位置座標
	private Vector3 screenToWorldPointPosition;

	private Collider _collider;

	public ParticleSystem _hamon_effect;
	private System.DateTime _hamon_prev;

	// Use this for initialization
	void Start () {
		_collider = GetComponent<Collider>();
		_hamon_prev = System.DateTime.Now;
	}

	// Update is called once per frame
	void Update() {
		// Vector3でマウス位置座標を取得する
		position = Input.mousePosition;
		// Z軸修正
		position.z = 12f;
		// マウス位置座標をスクリーン座標からワールド座標に変換する
		screenToWorldPointPosition = Camera.main.ScreenToWorldPoint(position);
		// ワールド座標に変換されたマウス座標を代入
		gameObject.transform.position = screenToWorldPointPosition;

		if(Input.GetKeyDown(KeyCode.Mouse0)) {
			_collider.enabled = true;
		}
		else {
			_collider.enabled = false;
		}

		// 波紋エフェクトの実行
		if(Input.GetKey(KeyCode.Mouse0) && (System.DateTime.Now - _hamon_prev).TotalSeconds > 0.4) {
			EffectEmit(screenToWorldPointPosition);
			if(Random.Range(0, 2) == 0) {
				switch(Random.Range(0, 2)) {
					case 0: SEManager.Instance.Play("teardrop1", 0.4f); break;
					case 1: SEManager.Instance.Play("teardrop2", 0.4f); break;
				}
			}
			_hamon_prev = System.DateTime.Now;
		}
	
	}

	void EffectEmit(Vector3 pos) {
		ParticleSystem.MainModule par = _hamon_effect.main;
		par.startColor = BG_Control.Instance._now_scn._hamon_effect_color;
		_hamon_effect.transform.position = pos;
		_hamon_effect.Emit(1);
	}
}
