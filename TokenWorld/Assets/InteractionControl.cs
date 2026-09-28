using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionControl : MonoBehaviour
{
	public class Unit
	{
		public Vector3 _target_pos, _pos;
		public Vector3 velocity = Vector3.zero;
		public List<Vector3> _cache_pos;
		public int _active_count, _del_count;
		public System.DateTime _hamon_prev;
	}

	[SerializeField] SchoolController _school;
	[SerializeField] float _radius;
	[SerializeField] int _active_frmae;
	[SerializeField] int _delete_frmae;
	[SerializeField] ParticleSystem _hamon_effect;
	private List<Unit> _units = new List<Unit>();
	public int ActiveUnitCount { get { return _units.Count; } }
	public void ClearInteractions() { _units.Clear(); }

	// Start is called before the first frame update
	void Start() {

	}

	private void OnDrawGizmos() {
		
		foreach(Unit unit in _units) {
			Gizmos.color = Color.green;
			if(unit._active_count > _active_frmae) {
				Gizmos.color = Color.red;
			}
			Gizmos.DrawWireSphere(unit._pos, _radius);
		}
	}

	private void GetInsideUnits(Vector3 pos) {
		float lengs = float.MaxValue;
		Unit temp = null;
		foreach(Unit unit in _units) {
			// 距離を求める
			float magnitude = (unit._target_pos - pos).magnitude;
			// ユニットの範囲内に座標が存在するか調べる
			if(magnitude < _radius) {
				// 最も近いユニットを探す
				if(magnitude < lengs) {
					lengs = magnitude;
					temp = unit;
				}
			}
		}

		// ユニットが存在しない場合新規で作成
		if(temp == null) {
			// 範囲内に既存のオブジェクトが存在しない場合追加
			bool add_flag = true;
			foreach(Unit unit in _units) {
				float len = (pos - unit._target_pos).magnitude;
				if(len < (_radius * 2)) {
					add_flag = false;
				}
			}
			if(add_flag) {
				temp = new Unit() {
					_target_pos = pos,
					_pos = pos,
					_cache_pos = new List<Vector3>(),
					_hamon_prev = System.DateTime.Now,
				};
				_units.Add(temp);
			}
		}
		else {
			// ユニットのキャッシュ座標に登録
			temp._cache_pos.Add(pos);
		}
	}

	// Update is called once per frame
	void Update() {
		// センサー
		UrgSensing senser = UrgSensing.Instance;
		foreach(UrgSensing.SensedObject obj in senser.sensedObjs) {
			Vector3 world_pos = senser.GetWorldPostion(obj);
			// ユニットを検索
			GetInsideUnits(world_pos);
		}

		// ユニットの更新
		List<Unit> remove_list = new List<Unit>();
		foreach(Unit unit in _units) {
			// オブジェクトのスムージング
			unit._pos = Vector3.SmoothDamp(unit._pos, unit._target_pos, ref unit.velocity, 0.6f);

			if(unit._cache_pos.Count != 0) {
				unit._active_count++;
				unit._del_count = 0;
				// 座標の更新
				Vector3 av = Vector3.zero;
				foreach(Vector3 pos in unit._cache_pos) {
					av += pos;
				}
				unit._target_pos = av / unit._cache_pos.Count;
			}
			else {
				unit._active_count = 0;
				unit._del_count++;
				if(unit._del_count > _delete_frmae) {
					remove_list.Add(unit);
				}
			}
			unit._cache_pos.Clear();

			// 鯉へのインタラクション
			if(unit._active_count > _active_frmae &&
				!BG_Control.Instance._is_changeScene) {
				Vector3 screen_pos = Camera.main.WorldToScreenPoint(unit._pos);
				screen_pos.z = 0f;
				_school.SetAction(screen_pos);

				// エフェクトの発生
				if((System.DateTime.Now - unit._hamon_prev).TotalSeconds > 0.4) {
					EffectEmit(screen_pos);
					unit._hamon_prev = System.DateTime.Now;
				}
			}
		}
		// ユニットの削除
		foreach(Unit unit in remove_list) {
			_units.Remove(unit);
		}
	}

	void EffectEmit(Vector2 screen_pos) {
		// スクリーン座標をワールド座標に変換
		Vector3 pos = Camera.main.ScreenToWorldPoint(new Vector3(screen_pos.x, screen_pos.y, 12f));
		
		ParticleSystem.MainModule par = _hamon_effect.main;
		par.startColor = BG_Control.Instance._now_scn._hamon_effect_color;
		_hamon_effect.transform.position = pos;
		_hamon_effect.Emit(1);

		if(Random.Range(0, 2) == 0) {
			switch(Random.Range(0, 2)) {
				case 0: SEManager.Instance.Play("teardrop1", 0.4f); break;
				case 1: SEManager.Instance.Play("teardrop2", 0.4f); break;
			}
		}
	}
}
