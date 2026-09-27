using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

public class BG_Control : SingletonMonoBehaviour<BG_Control>
{
	[System.Serializable]
	public class BG_Scene{
		[SerializeField]
		public GameObject _bg_obj;

		[SerializeField]
		public string _logo_path;
		[SerializeField]
		public Sprite _logo_img;

		[SerializeField]
		public string _telop_path;
		[SerializeField]
		public Sprite _telop_img;

		[SerializeField]
		public string _side_L_path;
		[SerializeField]
		public Sprite _side_L_img;

		[SerializeField]
		public string _side_R_path;
		[SerializeField]
		public Sprite _side_R_img;

		[SerializeField]
		public Color _hamon_effect_color;

		[SerializeField]
		public ParticleSystem _action_effect;
	}

	[SerializeField]
	TelopControl _telop_pond;

	[SerializeField]
	TelopControl _telop_fall;

	[SerializeField]
	TelopControlLR _LR_telop;

	[SerializeField]
	SchoolController _school;

	[SerializeField]
	WaterfallClimb _waterfallClimb;

	[SerializeField]
 	public int _scene_minute = 10;

	[SerializeField]
	string _bg_data_path;

	[SerializeField]
	BG_Scene[] _scenes;

	public BG_Scene _now_scn;
	int _scn_seek_index;
	DateTime _change_time;

	[HideInInspector]
	public bool _is_changeScene;

	// Start is called before the first frame update
	void Start()
    {
		_is_changeScene = false;
		_scn_seek_index = 0;
		_now_scn = _scenes[0];
		_change_time = DateTime.Now;

		// 画像データ読み込み
		foreach(BG_Scene scn in _scenes) {
			if(File.Exists(_bg_data_path + scn._logo_path)) {
				scn._logo_img = ReadImage.ReadSprite(_bg_data_path + scn._logo_path);
			}

			if(File.Exists(_bg_data_path + scn._telop_path)) {
				scn._telop_img = ReadImage.ReadSprite(_bg_data_path + scn._telop_path);
			}

			if(File.Exists(_bg_data_path + scn._side_L_path)) {
				scn._side_L_img = ReadImage.ReadSprite(_bg_data_path + scn._side_L_path);
			}

			if(File.Exists(_bg_data_path + scn._side_R_path)) {
				scn._side_R_img = ReadImage.ReadSprite(_bg_data_path + scn._side_R_path);
			}
		}

		_telop_fall.SetImage(_now_scn._logo_img);
		_telop_pond.SetImage(_now_scn._telop_img);
		_LR_telop.SetImage(_now_scn._side_L_img, _now_scn._side_R_img);
	}

    // Update is called once per frame
    void Update()
    {
		if (Input.GetKeyDown(KeyCode.B)) {
			StartCoroutine(ChangeScene());
		}

		if((DateTime.Now - _change_time).TotalMinutes > _scene_minute) {
			StartCoroutine(ChangeScene());
			_change_time = DateTime.Now;
		}
	}

	IEnumerator ChangeScene() {
		_is_changeScene = true;

		// 黒フェード
		_telop_pond.FadeBlackPanel(1f, 2f);
		_telop_fall.FadeBlackPanel(1f, 2f);
		_LR_telop.FadeTelop(0f, 2f);

		yield return new WaitForSeconds(2f);

		// 魚入れ替え
		_school.ResetFish();

		// BG入れ替え
		_now_scn._bg_obj.SetActive(false);
		_scn_seek_index++;
		_scn_seek_index = _scn_seek_index % _scenes.Length;
		_now_scn = _scenes[_scn_seek_index];
		_now_scn._bg_obj.SetActive(true);
		_waterfallClimb.ClimbReset();

		// 画像入れ替え
		_telop_fall.SetImage(_now_scn._logo_img);
		_telop_pond.SetImage(_now_scn._telop_img);
		_LR_telop.SetImage(_now_scn._side_L_img, _now_scn._side_R_img);

		yield return new WaitForSeconds(1f);

		// ロゴ表示
		_telop_fall.FadeTextPanel(1f, 1f);

		yield return new WaitForSeconds(1f);

		// テロップ表示
		_telop_pond.FadeTextPanel(1f, 1f);

		yield return new WaitForSeconds(3f);

		// 黒フェード
		_telop_pond.FadeBlackPanel(0f, 4f);
		_telop_fall.FadeBlackPanel(0f, 4f);

		yield return new WaitForSeconds(4f);

		// テロップ削除
		_telop_fall.FadeTextPanel(0f, 1f);
		_telop_pond.FadeTextPanel(0f, 1f);

		yield return new WaitForSeconds(2f);
		_LR_telop.FadeTelop(1f, 1f);

		_is_changeScene = false;
	}
}
