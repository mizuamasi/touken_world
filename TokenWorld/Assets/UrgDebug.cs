using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrgDebug : MonoBehaviour
{
	[SerializeField] UrgControl _control;
	[SerializeField] UrgSensing _sensing;

	public Vector3 Postion {
		get {
			return transform.position;
		}
		set {
			transform.position = value;
		}
	}

	public float Rotation {
		get {
			return _control.angleOffset;
		}
		set {
			_control.angleOffset = value;
		}
	}

	public float Scale {
		get {
			return _control.sca;
		}
		set {
			_control.sca = value;
		}
	}

	public float Threshold { 
		get {
			return _sensing.objThreshold;
		}
		set {
			_sensing.objThreshold = value;
		}
	}

	public float MinWidth {
		get {
			return _sensing.minWidth;
		}
		set {
			_sensing.minWidth = value;
		}
	}

	public Vector3 AreaCenter
	{
		get {
			return _sensing.sensingArea.center;
		}
		set {
			_sensing.sensingArea.center = value;
		}
	}

	public Vector3 AreaExtents{
		get {
			return _sensing.sensingArea.extents;
		}
		set {
			_sensing.sensingArea.extents = value;
		}
	}

	public string IP {
		get {
			return _control.ip;
		}
		set {
			_control.ip = value;
		}
	}

	public int Port
	{
		get {
			return _control.port;
		}
		set {
			_control.port = value;
		}
	}
	/*
	[ContextMenu("設定の読み込みと適用")]
	private void Method() {
		string setting_file = Application.streamingAssetsPath + @"/setting.xml";
		// 読み込み
		DebugControl.Setting data = XmlFunctions.XmlDeserialize<DebugControl.Setting>(setting_file);

		UrgControl control = GetComponent<UrgControl>();
		UrgSensing sensing = GetComponent<UrgSensing>();

		// パラメータ反映
		Postion = data._postion;
		Rotation = data._rotation;
		Scale = data._scale;
		Threshold = data._threshold;
		MinWidth = data._min;
		AreaCenter = data._area_center;
		AreaExtents = data._area_extents;
		IP = data._ip;
		Port = data._port;

		Debug.Log("設定の読み込みと適用 成功");
	}
	*/
	// Start is called before the first frame update
	void Start()
	{ }

    // Update is called once per frame
    void Update()
    {
		// デバッグ状態を取得する
		if(DebugControl.Instance._is_debug) {
			// センサー座標
			{
				Vector3 now_pos = Postion;
				if(InputKey(KeyCode.P, KeyCode.UpArrow)) now_pos.z += 0.01f;
				if(InputKey(KeyCode.P, KeyCode.DownArrow)) now_pos.z += -0.01f;
				if(InputKey(KeyCode.P, KeyCode.LeftArrow)) now_pos.x += -0.01f;
				if(InputKey(KeyCode.P, KeyCode.RightArrow)) now_pos.x += 0.01f;
				Postion = now_pos;
			}

			// センサー角度
			{
				float now_rot = Rotation;
				if(InputKey(KeyCode.R, KeyCode.UpArrow)) now_rot += 0.1f;
				if(InputKey(KeyCode.R, KeyCode.DownArrow)) now_rot += -0.1f;
				Rotation = now_rot;
			}

			// センサースケール
			{
				float now_sca = Scale;
				if(InputKey(KeyCode.S, KeyCode.UpArrow)) now_sca += 0.1f;
				if(InputKey(KeyCode.S, KeyCode.DownArrow)) now_sca += -0.1f;
				Scale = now_sca;
			}

			// センサーしきい値
			{
				float now_threshold = Threshold;
				if(InputKey(KeyCode.T, KeyCode.UpArrow)) Threshold += 0.01f;
				if(InputKey(KeyCode.T, KeyCode.DownArrow)) Threshold += -0.01f;
				Threshold = now_threshold;
			}

			// センサー最低認識値
			{
				float now_minWidth = MinWidth;
				if(InputKey(KeyCode.M, KeyCode.UpArrow)) now_minWidth += 0.001f;
				if(InputKey(KeyCode.M, KeyCode.DownArrow)) now_minWidth += -0.001f;
				MinWidth = now_minWidth;
			}

			// センサーエリア
			{
				Vector3 now_center = _sensing.sensingArea.center;
				if(InputKey(KeyCode.E, KeyCode.UpArrow)) now_center.z += 0.01f;
				if(InputKey(KeyCode.E, KeyCode.DownArrow)) now_center.z += -0.01f;
				if(InputKey(KeyCode.E, KeyCode.LeftArrow)) now_center.x += -0.01f;
				if(InputKey(KeyCode.E, KeyCode.RightArrow)) now_center.x += 0.01f;
				_sensing.sensingArea.center = now_center;

				Vector3 now_extents = _sensing.sensingArea.extents;
				if(InputKey(KeyCode.A, KeyCode.UpArrow)) now_extents.z += 0.01f;
				if(InputKey(KeyCode.A, KeyCode.DownArrow)) now_extents.z += -0.01f;
				if(InputKey(KeyCode.A, KeyCode.LeftArrow)) now_extents.x += -0.01f;
				if(InputKey(KeyCode.A, KeyCode.RightArrow)) now_extents.x += 0.01f;
				_sensing.sensingArea.extents = now_extents;
			}
		}
    }

	bool InputKey(KeyCode a, KeyCode b) {
		return (Input.GetKey(a) && Input.GetKey(b));
	}
}
