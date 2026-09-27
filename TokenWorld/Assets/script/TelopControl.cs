using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TelopControl : MonoBehaviour
{
	[SerializeField]
	Image _black_panel;

	[SerializeField]
	Image _text_telop;

	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void SetImage(Sprite img) {
		_text_telop.sprite = img;
	}

	public void FadeBlackPanel(float target_alpha, float fade_time) {
		DOTween.ToAlpha(
			() => _black_panel.color,
			color => _black_panel.color = color,
			target_alpha,
			fade_time
		);
	}

	public void FadeTextPanel(float target_alpha, float fade_time) {
		DOTween.ToAlpha(
			() => _text_telop.color,
			color => _text_telop.color = color,
			target_alpha,
			fade_time
		);
	}
}
