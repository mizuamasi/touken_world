using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UrgControl : MonoBehaviour
{
    [Header("device settings")]
    public string ip = "192.168.0.10";
    public int port = 10940;
    public int startStep = 0;
    public int endStep = 1080;
    public float angleOffset = 135f;
    public float angleDelta = 135f * 2f / 1080f;
	public float sca = 10f;
	public float DistancesSca {
		get { return sca * 0.0001f; }
	}
	//default setting for UST-20LX

	UrgDeviceEthernet urg;
    List<long> distances = new List<long>();

    // Start is called before the first frame update
    void Start()
    {
        urg = GetComponent<UrgDeviceEthernet>();
        var sensing = GetComponent<UrgSensing>();
        if (urg == null || (sensing != null && sensing.SimulationEnabled))
            return;
        urg.StartTCP(ip, port);
        if (urg.IsConnected)
            urg.Write(SCIP_library.SCIP_Writer.MD(startStep, endStep));
    }

    private void OnDrawGizmosSelected()
    {
        if (urg != null && urg.distances != null && 0 < urg.distances.Count)
        {
            distances.Clear();
            distances.AddRange(urg.distances);
            var origin = transform.position;
            var right = transform.right;
            var forward = transform.forward;
            for (var i = 0; i < distances.Count; i++)
            {
                var angle = i * angleDelta - angleOffset + 90f;
                var x = Mathf.Cos(angle * Mathf.Deg2Rad);
                var y = Mathf.Sin(angle * Mathf.Deg2Rad);
                var d = distances[i] * DistancesSca;

                var pos = (x * right + y * forward) * d;
                Gizmos.DrawLine(origin, origin + pos);
            }
        }
    }
}
