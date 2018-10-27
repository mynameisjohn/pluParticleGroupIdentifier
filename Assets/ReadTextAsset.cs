using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadTextAsset : MonoBehaviour {
    public string TextFile;
    public float Radius = 1;

    Dictionary<int, Color> colorMap = new Dictionary<int, Color>();
    class TimeSlice
    {
        public int _idx;
        public List<Vector3> _positions = new List<Vector3>();
        public TimeSlice(int idx)
        {
            _idx = idx;
        }
    }


    // Use this for initialization
    void Start()
    {
        StartCoroutine(printStrings(Resources.Load(TextFile) as TextAsset));
    }

    IEnumerator printStrings(TextAsset textFile)
    {
        if (textFile == null)
            yield break;

        Dictionary<int, GameObject> timeSliceDict = new Dictionary<int, GameObject>();

        foreach (string s in textFile.text.Split('\n'))
        {
            if (!string.IsNullOrEmpty(s))
            {
                string[] stringValues = s.Replace('\t', ' ').Split(' ');
                if (stringValues.Length >= 4)
                {
                    int timeSlice = int.Parse(stringValues[0]);

                    float posX = float.Parse(stringValues[1]);
                    float posY = float.Parse(stringValues[2]);
                    float posZ = float.Parse(stringValues[3]);

                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.AddComponent<ParticleCollider>().Init(this, timeSlice, Radius, new Vector3(posX, posY, posZ));
                }
            }
        }


        foreach (ParticleCollider pc in GetComponentsInChildren<ParticleCollider>())
        {
            if (!timeSliceDict.ContainsKey(pc._timeslice))
            {
                timeSliceDict[pc._timeslice] = new GameObject("Slice_" + pc._timeslice);
                timeSliceDict[pc._timeslice].transform.parent = transform;

                Dictionary<int, List<ParticleCollider>> d = new Dictionary<int, List<ParticleCollider>>();

                foreach (ParticleCollider pc in GetComponentsInChildren<ParticleCollider>())
                {
                    int group = pc.FindGroups();
                    pc.name += "_" + group;
                    if (!colorMap.ContainsKey(group))
                        colorMap[group] = Random.ColorHSV();
                    if (!d.ContainsKey(group))
                        d[group] = new List<ParticleCollider>();

                    pc.GetComponent<MeshRenderer>().material.color = colorMap[group];

                    d[group].Add(pc);
                }

                foreach (KeyValuePair<int, List<ParticleCollider>> shit in d)
                {
                    GameObject goGroup = new GameObject("Group_" + shit.Key);
                    foreach (ParticleCollider pc in shit.Value)
                        pc.transform.parent = goGroup.transform;
                    goGroup.transform.parent = timeSliceDict[timeSlice].transform;

                    if (timeSlice > 1)
                        goGroup.SetActive(false);
                }
            }
        }
        yield break;
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
