using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class SEManager : MonoBehaviour 
{
	#region Singleton

	private static SEManager instance;
	public static SEManager Instance 
	{
		get 
		{
			if (instance == null) 
			{
				instance = (SEManager)FindObjectOfType (typeof(SEManager));

				if (instance == null) {
						Debug.LogError (typeof(SEManager) + "is nothing");
				}
			}

			return instance;
		}
	}

	#endregion Singleton

	[SerializeField] string _audiofile_path;

	/// <summary>
	/// オーディオ
	/// </summary>
	private AudioSource _audio = null;

	/// <summary>
	/// オーディオリスト
	/// </summary>
	public Dictionary<string,AudioClip> _audio_list = null;

	public void Awake()
	{
		//シングルトンのためのコード
		if( this != Instance ) 
		{
			Destroy (this.gameObject);
			return;
		}
		DontDestroyOnLoad( this.gameObject );

		// [Resources/Audio/SE]フォルダからSEを探す
		this._audio_list = new Dictionary<string, AudioClip> ();
		foreach( AudioClip bgm in Resources.LoadAll<AudioClip>( "Audio/SE" ) ) 
		{
			this._audio_list.Add (bgm.name, bgm);
		}

		// 外部のオーディオファイルを読み込みます
		StartCoroutine(StreamPlayAudioFile(_audiofile_path));

		// オーディオの作成
		_audio = this.gameObject.AddComponent<AudioSource>();
	}

	/// <summary>
	/// SEの再生を行ないます
	/// </summary>
	/// <param name="seName"></param>
	public void Play( string seName, float volume )
	{
		if( !this._audio_list.ContainsKey( seName ) ) return;
		_audio.PlayOneShot( this._audio_list[seName], volume );
	}

	/// <summary>
	/// SEの再生を行ないます
	/// </summary>
	/// <param name="seName"></param>
	public void Play( string seName )
	{ Play( seName, 1f ); }

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	IEnumerator StreamPlayAudioFile(string fileName)
	{
		if(System.IO.File.Exists(fileName))
		{
			//ソース指定し音楽流す
			//音楽ファイルロード
			using (WWW www = new WWW("file:///" + fileName))
			{
				//読み込み完了まで待機
				yield return www;
				this._audio_list.Add("telop", www.GetAudioClip(true, false));
			}
		}
	}
}
