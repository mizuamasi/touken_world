using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TelopControlLR : MonoBehaviour
{
	[SerializeField]
	Image _L_img;

	[SerializeField]
	Image _R_img;

	// Start is called before the first frame update
	void Start() {

	}

	// Update is called once per frame
	void Update() {

	}

	public void SetImage(Sprite l_img, Sprite r_img) {
		_L_img.sprite = l_img;
		_R_img.sprite = r_img;
	}

	public void FadeTelop(float target_alpha, float fade_time) {
		DOTween.ToAlpha(
			() => _L_img.color,
			color => _L_img.color = color,
			target_alpha,
			fade_time
		);

		DOTween.ToAlpha(
			() => _R_img.color,
			color => _R_img.color = color,
			target_alpha,
			fade_time
		);
	}
}
