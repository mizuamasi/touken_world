using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeRenderQueue : MonoBehaviour
{
	[SerializeField] Material _mat;
	[SerializeField] int _renderQueue;

	private void Awake() {
		_mat.renderQueue = _renderQueue;
	}

	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		_mat.renderQueue = _renderQueue;
	}
}
