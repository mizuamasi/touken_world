using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DG.Tweening;

public class Telop : SingletonMonoBehaviour<Telop>
{
	[SerializeField] GameObject _telop;
	[SerializeField] string _telop_path;

	List<Sprite> _sprite_list = new List<Sprite>();

	// Start is called before the first frame update
	void Start() {
		string[] files = Directory.GetFiles(_telop_path, "*.png");
		foreach(string path in files) {
			_sprite_list.Add(ReadImage.ReadSprite(path));
		}
    }

    // Update is called once per frame
    void Update() {
        
    }

	public void StartTelop(Vector3 pos) {
		SEManager.Instance.Play("telop");
		pos.y = transform.position.y;
		pos.z = transform.position.z;
		StartCoroutine(CoroutineProc(pos));
	}

	IEnumerator CoroutineProc(Vector3 pos) {

		// インスタンス作成
		GameObject new_obj = Instantiate(_telop, transform);
		new_obj.transform.position = pos;
		SpriteRenderer sr = new_obj.GetComponent<SpriteRenderer>();
		sr.sprite = _sprite_list[Random.Range(0, _sprite_list.Count)];

		DOTween.ToAlpha(
			() => sr.color,
			color => sr.color = color,
			1f,                                // 最終的なalpha値
			1.5f
		);

		yield return new WaitForSeconds(6f);

		DOTween.ToAlpha(
			() => sr.color,
			color => sr.color = color,
			0f,                                // 最終的なalpha値
			1f
		);

		yield return new WaitForSeconds(2f);

		// object破棄
		Destroy(new_obj);
	}

}
