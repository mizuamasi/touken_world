using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterfallClimb : MonoBehaviour
{
	[SerializeField] List<GameObject> _target_obj;
	List<bool> _act_list = new List<bool>();
	List<Vector3> _def_pos = new List<Vector3>();

	System.DateTime _prev_time;

	// Start is called before the first frame update
	void Start() {
		foreach (GameObject obj in _target_obj){
			_def_pos.Add(obj.transform.localPosition);
		}
		ClimbReset();
		_prev_time = System.DateTime.Now;
	}

	// Update is called once per frame
	void Update() {
		for(int i = 0; i < _target_obj.Count; i++) { 
			Vector3 pos = _target_obj[i].transform.localPosition;
			pos.x = _def_pos[i].x * DebugControl.Instance._param_setting._telop_area_size;
			_target_obj[i].transform.localPosition = pos;
		}
	}

	public void ClimbReset() {
		_act_list.Clear();
		foreach(GameObject obj in _target_obj) _act_list.Add(false);
	}

	public void SetIndexFlag(int index, bool flag) {
		_act_list[index] = flag;
	}

	/// <summary>
	/// 滝登りのターゲット座標が取得できるかチェックする
	/// </summary>
	/// <returns>取得できる場合：整数　取得できな場合：-1</returns>
	public int IsClimb() {
		if((System.DateTime.Now - _prev_time).TotalSeconds < DebugControl.Instance._param_setting._reaction_interval) {
			return -1;
		}

		if(Random.Range(1, DebugControl.Instance._param_setting._reaction_frequency) != 1) return -1;
		_prev_time = System.DateTime.Now;
		// リストアップする
		List<int> false_indexs = new List<int>();
		for(int i = 0; i < _act_list.Count; i++) {
			if(_act_list[i] == false) {
				false_indexs.Add(i);
			}
		}

		if(false_indexs.Count == 0) return -1;
		return false_indexs[Random.Range(0, false_indexs.Count)];
	}

	public Vector3 GetPostion(int index) {
		return _target_obj[index].transform.position;
	}
}
