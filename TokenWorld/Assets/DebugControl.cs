using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugControl : SingletonMonoBehaviour<DebugControl>
{
	public class URGSetting {
		public Vector3 _postion;
		public float _rotation;
		public float _scale;
		public float _threshold;
		public float _min;
		public Vector3 _area_center;
		public Vector3 _area_extents;
		public string _ip;
		public int _port;
	}

	public class ParamSetting {
		public int _reaction_frequency = 2;
		public float _reaction_interval = 3f;
		public float _telop_area_size = 1f;
		public float _hotal_size = 1f;
		public int _scn_minute = 3;
	}

	public bool _is_debug = false;
	private string _urg_setting_file, _param_setting_file;

	[HideInInspector] public ParamSetting _param_setting;

	[SerializeField] GameObject _debug_ui;
	[SerializeField] UrgDebug _urgDebug;
	[SerializeField] string _setting_file_path;

	[SerializeField] Text _ui_pos, _ui_rot, _ui_sca, _ui_th, _ui_min, _ui_area_p, _ui_area_s, _ui_ip, _ui_port;

	private void Awake() {

		// パラメータのパスを作成
		_param_setting_file = _setting_file_path + @"_setting.xml";
		ParamSetting param = new ParamSetting();
		try
		{
			// ファイルが存在しない場合は作成
			if (!File.Exists(_param_setting_file)){
				XmlFunctions.XmlSerialize<ParamSetting>(_param_setting_file, param);
			}
			// 読み込み
			param = XmlFunctions.XmlDeserialize<ParamSetting>(_param_setting_file);
		}
		catch
		{
			param = new ParamSetting();
		}

		_param_setting = param;

		_urg_setting_file = _setting_file_path + @"_urg.xml";
		URGSetting data = new URGSetting();
		try {
			// ファイルが存在しない場合は作成
			if(!File.Exists(_urg_setting_file)) {
				SaveURGSetting();
			}
			// 読み込み
			data = XmlFunctions.XmlDeserialize<URGSetting>(_urg_setting_file);
		}
		catch {
			data = new URGSetting();
		}

		try{
			// パラメータ反映
			_urgDebug.Postion = data._postion;
			_urgDebug.Rotation = data._rotation;
			_urgDebug.Scale = data._scale;
			_urgDebug.Threshold = data._threshold;
			_urgDebug.MinWidth = data._min;
			_urgDebug.AreaCenter = data._area_center;
			_urgDebug.AreaExtents = data._area_extents;
			_urgDebug.IP = data._ip;
			_urgDebug.Port = data._port;

			BG_Control.Instance._scene_minute = _param_setting._scn_minute;
		}
		catch{

		}
	}

		// Start is called before the first frame update
	void Start()
    {
		_debug_ui.SetActive(_is_debug);
	}

    // Update is called once per frame
    void Update()
    {
		// デバッグの切り替え
		if(Input.GetKeyDown(KeyCode.F1)) {
			_is_debug = !_is_debug;
			// UIの有効化
			_debug_ui.SetActive(_is_debug);
			// カーソルの有効化
			Cursor.visible = _is_debug;
		}

		// 設定の保存
		if(_is_debug) {
			// 保存キーの取得
			if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S)) {
				SaveURGSetting();
			}
		}

		// UIの更新
		_ui_pos.text = _urgDebug.Postion.ToString();
		_ui_rot.text = _urgDebug.Rotation.ToString();
		_ui_sca.text = _urgDebug.Scale.ToString();
		_ui_th.text = _urgDebug.Threshold.ToString();
		_ui_min.text = _urgDebug.MinWidth.ToString();
		_ui_area_p.text = _urgDebug.AreaCenter.ToString();
		_ui_area_s.text = _urgDebug.AreaExtents.ToString();
		_ui_ip.text = _urgDebug.IP.ToString();
		_ui_port.text = _urgDebug.Port.ToString();
	}

	public void SaveURGSetting() {
		URGSetting setting = new URGSetting() {
			_postion = _urgDebug.Postion,
			_rotation = _urgDebug.Rotation,
			_scale = _urgDebug.Scale,
			_threshold = _urgDebug.Threshold,
			_min = _urgDebug.MinWidth,
			_area_center = _urgDebug.AreaCenter,
			_area_extents = _urgDebug.AreaExtents,
			_ip = _urgDebug.IP,
			_port = _urgDebug.Port,
		};

		// 設定ファイルの保存
		try {
			XmlFunctions.XmlSerialize<URGSetting>(_urg_setting_file, setting);
		}
		catch { }
	}
}
