/**************************************									
	Copyright 2015 Unluck Software	
 	www.chemicalbliss.com								
***************************************/

using UnityEngine;
using System.Collections;
using DG.Tweening;


public class SchoolChild:MonoBehaviour{
	[HideInInspector]
	public SchoolController _spawner;
	[HideInInspector]
	public WaterfallClimb _climb;
	Vector3 _wayPoint;
	[HideInInspector]
	public float _speed= 10.0f;				//Fish Speed
	float _stuckCounter;			//prevents looping around a waypoint
	float _damping;					//Turn speed
	Transform _model;				//Model with animations
	float _targetSpeed;				//Fish target speed
	float tParam = 0.0f;				//
	float _rotateCounterR;			//Used to increase avoidance speed over time
	float _rotateCounterL;			
	public Transform _scanner;				//Scanner object used for push, this rotates to check for collisions
	bool _scan = true;			
	bool _instantiated;			//Has this been instantiated
	static int _updateNextSeed = 0;	//When using frameskip seed will prevent calculations for all fish to be on the same frame
	int _updateSeed = -1;
	[HideInInspector]
	public Transform _cacheTransform;
	
	#if UNITY_EDITOR
	public static bool _sWarning;
#endif

	public Transform _front;
	public GameObject _effect;
	public Collider _collider;
	public Renderer _render;
	public Material[] _materials;

	private float _orignal_sca;

	//private bool _active_flag = false;
	//private float _active_val = 1f;

	private bool _is_avoidance;
	private float _speed_target, _speed_val;
	private float _weight_target, _weight_val, _defalt_weight;

	public void Start(){

		//Check if there is a controller attached
		if (_cacheTransform == null) _cacheTransform = transform;
		if(_spawner != null){	
			SetRandomScale();			
			LocateRequiredChildren();
		    RandomizeStartAnimationFrame();
		    SkewModelForLessUniformedMovement();
			_speed = Random.Range(_spawner._minSpeed, _spawner._maxSpeed);
			Wander(0.0f);
			SetRandomWaypoint();
			CheckForBubblesThenInvoke();	
			_instantiated = true;
			GetStartPos();
			FrameSkipSeedInit();
			_spawner._activeChildren++;
			return;
		}
		
		this.enabled = false;

		Debug.Log(gameObject + " found no school to swim in: " + this + " disabled... Standalone fish not supported, please use the SchoolController"); 
	}

	public void SetMat(int index) {
		Material[] mat = _render.materials;
		mat[0] = _materials[index];
		_render.materials = mat;
	}

	public void Update() {
		
		if (Mode == ActionMode.Climb) {
			SetAnimationSpeed();
			SetMotionWeight();
		}
		else if (_spawner._updateDivisor <=1 || _spawner._updateCounter == _updateSeed){
			CheckForDistanceToWaypoint();
		   	RotationBasedOnWaypointOrAvoidance();
		    ForwardMovement();
			RayCastToPushAwayFromObstacles();
			SetAnimationSpeed();
			SetMotionWeight();
		}

		// -180～180の範囲に変換
		float rotateZ = (transform.eulerAngles.z > 180) ? transform.eulerAngles.z - 360 : transform.eulerAngles.z;

		// 0.2は遊び値
		if(rotateZ <= -10f) {
			transform.eulerAngles += new Vector3(0.0f, 0.0f, 10.0f) * Time.deltaTime;
		}
		else if(rotateZ >= 10f) {
			transform.eulerAngles -= new Vector3(0.0f, 0.0f, 10.0f) * Time.deltaTime;
		}

	}
	
	public void FrameSkipSeedInit(){
		if(_spawner._updateDivisor > 1){
			int _updateSeedCap = _spawner._updateDivisor -1;
			_updateNextSeed++;
		    this._updateSeed = _updateNextSeed;
		    _updateNextSeed = _updateNextSeed % _updateSeedCap;
		}
	}
	
	public void CheckForBubblesThenInvoke() {
		if(_spawner._bubbles != null)
			InvokeRepeating("EmitBubbles", (_spawner._bubbles._emitEverySecond*Random.value)+1 , _spawner._bubbles._emitEverySecond);	
	}
	
	public void EmitBubbles(){
		_spawner._bubbles.EmitBubbles(_cacheTransform.position, _speed);
	}
	
	public void OnDisable() {
		CancelInvoke();
		_spawner._activeChildren--;
	}
	
	public void OnEnable() {
		if(_instantiated){
			CheckForBubblesThenInvoke();
			_spawner._activeChildren++;
		}
	}
	
	public void LocateRequiredChildren(){
		if(_model == null) _model = _cacheTransform.Find("Model");
		if(_scanner == null){
			_scanner = new GameObject().transform;
			_scanner.parent = this.transform;
			_scanner.localRotation = Quaternion.identity;
			_scanner.localPosition = Vector3.zero;
			#if UNITY_EDITOR
			if(!_sWarning){
				Debug.Log("No scanner assigned: creating... (Increase instantiate performance by manually creating a scanner object)");
				_sWarning = true;
			}
			#endif
		}
	}
	
	public void SkewModelForLessUniformedMovement() {
		// Adds a slight rotation to the model so that the fish get a little less uniformed movement	
		Quaternion rx = Quaternion.identity;
		rx.eulerAngles = new Vector3(0.0f, 0.0f , (float)Random.Range(-25, 25));
		_model.	rotation =	rx;
	}
	
	public void SetRandomScale(){
		float sc = Random.Range(_spawner._minScale, _spawner._maxScale);
		_orignal_sca = sc;
		_cacheTransform.localScale=Vector3.one*sc;
	}

	public void RandomizeStartAnimationFrame(){
		SimpleAnimation sa = _model.GetComponent<SimpleAnimation>();
		_defalt_weight = Random.Range(0.1f, 0.4f);
		sa.Blend("swim", _defalt_weight, 0f);

		SimpleAnimation.State st = sa.GetState("swim");
		st.time = Random.value * st.length;

		/*
		foreach(AnimationState state in _model.GetComponent<Animation>()) {
		 	state.time = Random.value * state.length;
		}
		*/
	}

	float _weight_velocity = 0f;
	public void SetMotionWeight() {
		float weight = _defalt_weight + _weight_val;
		weight = Mathf.Clamp(weight, 0.1f, 0.99f);

		SimpleAnimation sa = _model.GetComponent<SimpleAnimation>();
		sa.GetState("Default").weight = 1.0f - weight;
		sa.GetState("swim").weight = weight;

		_weight_val = Mathf.SmoothDamp(_weight_val, _weight_target, ref _weight_velocity, _is_avoidance? 0.2f: 1.5f);
	}

	public void GetStartPos(){
		//-Vector is to avoid zero rotation warning
		_cacheTransform.position = _wayPoint - new Vector3(.1f,.1f,.1f);
	}
	
	public Vector3 findWaypoint(){
		Vector3 t = Vector3.zero;
		t.x = Random.Range(-_spawner._spawnSphere, _spawner._spawnSphere) + _spawner._posBuffer.x;
		t.z = Random.Range(-_spawner._spawnSphereDepth, _spawner._spawnSphereDepth) + _spawner._posBuffer.z;
		t.y = Random.Range(-_spawner._spawnSphereHeight, _spawner._spawnSphereHeight) + _spawner._posBuffer.y;
		return t;
	}
	
	//Uses scanner to push away from obstacles
	public void RayCastToPushAwayFromObstacles() {
		if(_spawner._push){
			RotateScanner();
			RayCastToPushAwayFromObstaclesCheckForCollision();
		}
	}
	
	public void RayCastToPushAwayFromObstaclesCheckForCollision() {
		RaycastHit hit = new RaycastHit();
		float d = 0.0f;
		Vector3 cacheForward = _scanner.forward;
		if (Physics.Raycast(_cacheTransform.position, cacheForward, out hit, _spawner._pushDistance, _spawner._avoidanceMask)){		
			SchoolChild s = null;
			s = hit.transform.GetComponent<SchoolChild>();	
			d = (_spawner._pushDistance - hit.distance)/_spawner._pushDistance;	// Equals zero to one. One is close, zero is far	
			if(s != null){
				_cacheTransform.position -= cacheForward*_spawner._newDelta*d*_spawner._pushForce;	
			}
			else{
				_speed -= .01f*_spawner._newDelta;
				if(_speed < .1f)
				_speed = .1f;
				_cacheTransform.position -= cacheForward*_spawner._newDelta*d*_spawner._pushForce*2;
				//Tell scanner to rotate slowly
				_scan = false;
			}					
		}else{
			//Tell scanner to rotate randomly
			_scan = true;
		}
	}
	
	public void RotateScanner() {
		//Scan random if not pushing
		if(_scan){
			_scanner.rotation = Random.rotation;
			return;
		}
		//Scan slow if pushing
		_scanner.Rotate(new Vector3(150*_spawner._newDelta,0.0f,0.0f));
	}
	
	public bool Avoidance() {
		//Avoidance () - Returns true if there is an obstacle in the way
		if(!_spawner._avoidance)
			return false;		
		RaycastHit hit = new RaycastHit();
		float d = 0.0f;
		Quaternion rx = _cacheTransform.rotation;
		Vector3 ex = _cacheTransform.rotation.eulerAngles;
		Vector3 cacheForward = _cacheTransform.forward;
		Vector3 cacheRight = _cacheTransform.right;
		//Up / Down avoidance
		if (Physics.Raycast(_cacheTransform.position, -Vector3.up+(cacheForward*.1f), out hit, _spawner._avoidDistance, _spawner._avoidanceMask)){			
			//Debug.DrawLine(_cacheTransform.position,hit.point);
			d = (_spawner._avoidDistance - hit.distance)/_spawner._avoidDistance;
			ex.x -= _spawner._avoidSpeed*d*_spawner._newDelta*(_speed +1);
			rx.eulerAngles = ex;
			_cacheTransform.rotation = rx;
		}
		if (Physics.Raycast(_cacheTransform.position, Vector3.up+(cacheForward*.1f), out hit, _spawner._avoidDistance, _spawner._avoidanceMask)){
			//Debug.DrawLine(_cacheTransform.position,hit.point);
			d = (_spawner._avoidDistance - hit.distance)/_spawner._avoidDistance;			
			ex.x += _spawner._avoidSpeed*d*_spawner._newDelta*(_speed +1);	
			rx.eulerAngles = ex;
			_cacheTransform.rotation = rx;	
		}
		
		//Crash avoidance //Checks for obstacles forward
		if (Physics.Raycast(_cacheTransform.position, cacheForward+(cacheRight*Random.Range(-.1f, .1f)), out hit, _spawner._stopDistance, _spawner._avoidanceMask)){		
	//					Debug.DrawLine(_cacheTransform.position,hit.point);
					d = (_spawner._stopDistance - hit.distance)/_spawner._stopDistance;				
					ex.y -= _spawner._avoidSpeed*d*_spawner._newDelta*(_targetSpeed +3);
					rx.eulerAngles = ex;
					_cacheTransform.rotation = rx;
					_speed -= d*_spawner._newDelta*_spawner._stopSpeedMultiplier*_speed;				
					if(_speed < 0.01f){
						_speed = 0.01f;	
					}
					return true;
		}else if (Physics.Raycast(_cacheTransform.position, cacheForward+(cacheRight*(_spawner._avoidAngle+_rotateCounterL)), out hit, _spawner._avoidDistance, _spawner._avoidanceMask)){
	//				Debug.DrawLine(_cacheTransform.position,hit.point);
					d = (_spawner._avoidDistance - hit.distance)/_spawner._avoidDistance;				
					_rotateCounterL+=.1f;
					ex.y -= _spawner._avoidSpeed*d*_spawner._newDelta*_rotateCounterL*(_speed +1);
					rx.eulerAngles = ex;
					_cacheTransform.rotation = rx;				
					if(_rotateCounterL > 1.5f)
						_rotateCounterL = 1.5f;				
					_rotateCounterR = 0.0f;
					return true;		
		}else if (Physics.Raycast(_cacheTransform.position, cacheForward+(cacheRight*-(_spawner._avoidAngle+_rotateCounterR)), out hit, _spawner._avoidDistance, _spawner._avoidanceMask)){
	//			Debug.DrawLine(_cacheTransform.position,hit.point);
					d = (_spawner._avoidDistance - hit.distance)/_spawner._avoidDistance;
					if(hit.point.y < _cacheTransform.position.y){
						ex.y -= _spawner._avoidSpeed*d*_spawner._newDelta*(_speed +1);
					}
					else{
						ex.x += _spawner._avoidSpeed*d*_spawner._newDelta*(_speed +1);
					}
					_rotateCounterR +=.1f;
					ex.y += _spawner._avoidSpeed*d*_spawner._newDelta*_rotateCounterR*(_speed +1);
					rx.eulerAngles = ex;
					_cacheTransform.rotation = rx;	
					if(_rotateCounterR > 1.5f)
						_rotateCounterR = 1.5f;	
					_rotateCounterL = 0.0f;
					return true;
		}else{
			_rotateCounterL = 0.0f;
			_rotateCounterR = 0.0f;
		}
		return false;																	    																																				    																				
	}
	
	public void ForwardMovement(){
		_cacheTransform.position += _cacheTransform.TransformDirection(Vector3.forward)*_speed*_spawner._newDelta;
		if (tParam < 1) {
			if(_speed > _targetSpeed){
				tParam += _spawner._newDelta * _spawner._acceleration;
			}else{
				tParam += _spawner._newDelta * _spawner._brake;		
			}
			_speed = Mathf.Lerp(_speed, _targetSpeed,tParam);	
		}
	}
	
	public void RotationBasedOnWaypointOrAvoidance(){
		Quaternion rotation = Quaternion.identity;
	    rotation = Quaternion.LookRotation(_wayPoint - _cacheTransform.position);
	    if(!Avoidance()){
			_cacheTransform.rotation = Quaternion.Slerp(_cacheTransform.rotation, rotation, _spawner._newDelta * _damping);
			_speed_target = 0f;
			_weight_target = 0f;
			_is_avoidance = false;
		}
		else {
			_speed_target = 5f;
			_weight_target = 1f;
			_is_avoidance = true;
		}
		//Limit rotation up and down to avoid freaky behavior
		float angle = _cacheTransform.localEulerAngles.x;
	    angle = (angle > 180) ? angle - 360 : angle;
		Quaternion rx = _cacheTransform.rotation;
	    Vector3 rxea = rx.eulerAngles;
	    rxea.x = ClampAngle(angle, -50.0f , 50.0f);
	    rx.eulerAngles = rxea;
		_cacheTransform.rotation = rx;
	}
	
	public void CheckForDistanceToWaypoint(){
		if((_cacheTransform.position - _wayPoint).magnitude < _spawner._waypointDistance+_stuckCounter){
	      	Wander(0.0f);	//create a new waypoint
	        _stuckCounter=0.0f;
	        CheckIfThisShouldTriggerNewFlockWaypoint();
	        return;
	    }
	    _stuckCounter+=_spawner._newDelta*(_spawner._waypointDistance*.25f);
	}
	
	public void CheckIfThisShouldTriggerNewFlockWaypoint(){
		if(_spawner._childTriggerPos){
			_spawner.SetRandomWaypointPosition();
		}
	}
	
	public static float ClampAngle(float angle,float min,float max) {
		if (angle < -360)angle += 360.0f;
		if (angle > 360)angle -= 360.0f;
		return Mathf.Clamp (angle, min, max);
	}

	float _speed_velocity = 0f;
	public void SetAnimationSpeed(){
		SimpleAnimation sa = _model.GetComponent<SimpleAnimation>();
		SimpleAnimation.State state = sa.GetState("swim");
		float speed = (Random.Range(_spawner._minAnimationSpeed, _spawner._maxAnimationSpeed) * _spawner._schoolSpeed * this._speed) + .1f;
		state.speed = speed * (1f + _speed_val);

		_speed_val = Mathf.SmoothDamp(_speed_val, _speed_target, ref _speed_velocity, _is_avoidance ? 0.2f : 1f);
		//_active_rot_val = _active_rot_val + ((_active_rot_flag?1f:0f) - _active_rot_val) * 0.1f;

		/*
		foreach(AnimationState state in _model.GetComponent<Animation>()) {
	    	state.speed = (Random.Range(_spawner._minAnimationSpeed, _spawner._maxAnimationSpeed)*_spawner._schoolSpeed*this._speed)+.1f;
		}
		*/
	}
	
	public void Wander(float delay){
		_damping = Random.Range(_spawner._minDamping, _spawner._maxDamping);
	    _targetSpeed = Random.Range(_spawner._minSpeed, _spawner._maxSpeed)*_spawner._speedCurveMultiplier.Evaluate(Random.value)*_spawner._schoolSpeed;
		Invoke("SetRandomWaypoint", delay);
	}
	
	public void SetRandomWaypoint(){
		tParam = 0.0f;
		_wayPoint = findWaypoint();
	}

	enum ActionMode {
		Auto,
		Climb,
		Escape,
	}

	private ActionMode Mode { get; set; }

	public void ReStart() {
		
		
	}

	public void SetAction() {

		// 自動モードの時以外は無視する
		if(Mode != ActionMode.Auto) return;

		int index = _climb.IsClimb();
		if(index != -1) {
			// エリアを占有
			_climb.SetIndexFlag(index, true);
			StartCoroutine(WaterFallClimb(index));
			StartCoroutine(PlayEffect());
		}
		else {
			StartCoroutine(Escape());
		}
	}

	// 逃げる
	IEnumerator Escape() {
		Mode = ActionMode.Escape;

		DOVirtual.Float(_targetSpeed, Random.Range(2f, 3f), 0.5f, value => {
			_targetSpeed = value;
		});

		yield return new WaitForSeconds(Random.Range(2f, 3f));

		DOVirtual.Float(_targetSpeed, Random.Range(0.1f, 0.5f), 2f, value => {
			_targetSpeed = value;
		});

		yield return new WaitForSeconds(3f);

		// 自動モードに戻す
		Mode = ActionMode.Auto;
	}

	IEnumerator PlayEffect() {
		// 鯉の座標をスクリーン座標に置き換える
		Vector3 screen = Camera.main.WorldToScreenPoint(transform.position);
		// スクリーン座標にワールドに配置しなおす
		screen.z = 10f;
		Vector3 world = Camera.main.ScreenToWorldPoint(screen);

		ParticleSystem p = BG_Control.Instance._now_scn._action_effect;
		p.transform.position = world;
		//p.Emit(1);
		p.Play();

		SEManager.Instance.Play("bath-thapon1", 1f);

		// インスタンス作成
		//GameObject new_obj = Instantiate(_action_effect);
		//new_obj.transform.position = world;

		yield return new WaitForSeconds(1f);

		//Destroy(new_obj);
	}

	// 滝登り
	IEnumerator WaterFallClimb(int index) {
		Mode = ActionMode.Climb;
		Vector3 pos = transform.position;
		Vector3 target_pos = _climb.GetPostion(index);
		Vector3 sca = transform.localScale;
		Vector3[] path = {
			_front.transform.position,
			new Vector3(target_pos.x, target_pos.y, Random.Range(3f, 5f)),
			target_pos,
			new Vector3(target_pos.x, 4f, 8),
			new Vector3(target_pos.x, 10f, 8),
		};

		bool isComplete = false;
		float target_time_sca = 0f;
		Sequence seq = DOTween.Sequence();
		seq.Append(
			transform.DOPath(path, 6f, PathType.CatmullRom)
				.SetLookAt(0.05f, Vector3.forward)
				.SetEase(Ease.Linear)
				.SetOptions(false)
				.OnComplete(() => { isComplete = true; })
				.OnWaypointChange((int waypoint) => {
					switch (waypoint) {
						case 1: {
							target_time_sca = 0.75f;
						}
						break;
						case 2: {
							_speed_target = 9f;
							target_time_sca = 0.75f;
						}
						break;
						case 3: {
							_speed_target = 10f;
							target_time_sca = 0.6f;
							_effect.SetActive(true);
						} break;
					}
				})
		);

		// 値の初期化
		seq.timeScale = 0f;

		// ターゲット値の設定
		_speed_target = 5f;
		_weight_target = 0.5f;
		_is_avoidance = true;
		target_time_sca = 0.5f;

		// 終了するまで待機
		bool is_telop = false;
		while (!isComplete) {

			seq.timeScale += (target_time_sca - seq.timeScale) * 0.05f;

			// コライダーを入れたり切ったりする
			if(Random.Range(0, 60) == 0) _collider.enabled = !_collider.enabled;

			// テロップ表示
			if(seq.position > 4.5f && !is_telop) {

				Telop.Instance.StartTelop(transform.position);

				is_telop = true;
			}

			yield return null;
		}

		seq.Complete();

		// エフェクト系を切る
		_collider.enabled = false;
		_effect.SetActive(false);

		yield return new WaitForSeconds(5f);

		// エリアを解放
		_climb.SetIndexFlag(index, false);

		// オブジェクトを原点に戻す
		Mode = ActionMode.Auto;
		transform.position = new Vector3(0f, -3f, -11f);
		transform.rotation = Quaternion.Euler(0, 0, 0);
		transform.localScale = sca;

		_is_avoidance = false;
		_speed_target = 0f;
		_weight_target = 0f;
	}

	IEnumerator WaitTween(Tween tween) {
		while (!tween.IsComplete()) {
			yield return null;
		}
	}
}
