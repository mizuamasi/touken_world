using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartcleScaContorl : MonoBehaviour
{
    // Start is called before the first frame update
    void Start() {
		ParticleSystem par = GetComponent<ParticleSystem>();
		var main = par.main;
		main.startSize = main.startSize.constant * DebugControl.Instance._param_setting._hotal_size;
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}
