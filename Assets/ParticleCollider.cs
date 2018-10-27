using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleCollider : MonoBehaviour {
    public int _timeslice;
    public int _groupID;
    ReadTextAsset _manager;
    
    // Use this for initialization
    void Start () {
		
	}

    public void Init(ReadTextAsset manager, int timeslice, float Radius, Vector3 position)
    {
        SphereCollider sc = gameObject.GetComponent<SphereCollider>();
        if (sc == null)
            sc = gameObject.AddComponent<SphereCollider>();
        _timeslice = timeslice;
        transform.position = position;
        transform.parent = manager.transform;
    }

    public int FindGroups()
    {
        SphereCollider sc = gameObject.GetComponent<SphereCollider>();
        if (sc != null)
            _groupID = Physics.OverlapSphere(transform.position, 2.1f * sc.radius).Length - 1;
        
        return _groupID;
    }

    // Update is called once per frame
    void Update () { 
	}
}
