using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleDataVisualizer : MonoBehaviour
{
    // The text file asset we'll be reading
    public string DataFileName;
    public float Radius = 1;
    public int VisibleTimeSlice = 1;
    public Material _StandardMat;
    int _actualVisibleSlice = -1;
    int _maxTimeSlice = 0;

    public class Particle
    {
        public int groupIndex;
        public Vector3 position;
    }

    // One color per group
    Dictionary<int, Color> _colorMap = new Dictionary<int, Color>();

    // Each time slice is a time slice index and a list of particle positions for that time slice
    // (I wonder if this particle list should store group as well)
    class TimeSlice
    {
        public int index;
        public List<Particle> particles = new List<Particle>();
    }

    // Store a list of time slices
    List<TimeSlice> _timeSlices = new List<TimeSlice>();

    // For each time slice we'll be showing bunch of particle groups
    // Put each visible group in the hierarchy of one of these objects
    // (there should be one element of this array per group)
    // The last particle group here will be used as an object pool
    Dictionary<int, GameObject> _particleGroups = new Dictionary<int, GameObject>();
    int _visibleObjectLayer { get { return 0; } }
    void moveInToGroup(GameObject go, int groupIdx)
    {
        go.transform.parent = _particleGroups[groupIdx].transform;
        go.gameObject.layer = _visibleObjectLayer;
        go.GetComponent<Renderer>().enabled = true;
    }

    GameObject _objectPool;
    int _objectPoolLayer { get { return 2; } }
    void moveInToObjectPool(GameObject go)
    {
        go.transform.parent = _objectPool.transform;
        go.layer = _objectPoolLayer;
        go.GetComponent<Renderer>().enabled = false;
    }

    GameObject _particleNeighborObject;
    int _particleNeighborLayer { get { return 1; } }
    void moveObjectIntoNeighborFinder(GameObject go)
    {
        go.transform.parent = _particleNeighborObject.transform;
        go.layer = _particleNeighborLayer;
        go.GetComponent<Renderer>().enabled = false;
    }

    void Start()
    {
        _particleNeighborObject = new GameObject("Particle Neighbor Storage");
        _particleNeighborObject.transform.parent = transform;

        _objectPool = new GameObject("Object Pool");
        _objectPool.transform.parent = transform;

        _StandardMat = new Material(Shader.Find("Standard"));
        _StandardMat.enableInstancing = true;
        _StandardMat.name = "Particle Instance Material";

        var dataFile = Resources.Load(DataFileName);
        if (dataFile == null)
        {
            Camera.main.backgroundColor = Color.red;
            Application.Quit();
        }
        else
        {
            StartCoroutine(readTimeSlices(dataFile as TextAsset));
            StartCoroutine(findParticleGroups());
        }
    }

    // When this isn't empty the coroutine below picks it up and finds groups
    List<TimeSlice> _pendingGroupFind = new List<TimeSlice>();

    Vector3 _centroidSum;
    int _centroidSumCount = 1;
    public Vector3 centroid { get { return _centroidSum / _centroidSumCount; } }

    IEnumerator findParticleGroups()
    {
        // Keep this list in memory up here
        GameObject[] particleColliders = new GameObject[0];
        while (true)
        {
            yield return true;
            if (_pendingGroupFind.Count == 0)
            {
                continue;
            }

            // Pop off the first slice and find groups
            TimeSlice toFindGroupsIn = _pendingGroupFind[0];
            _pendingGroupFind.RemoveAt(0);

            // Construct a list of particle colliders for each particle in the slice
            List<Particle> particleList = toFindGroupsIn.particles;
            int numParticles = particleList.Count;

            // Make sure our array is big enough
            if (particleColliders.Length < numParticles)
                particleColliders = new GameObject[numParticles];

            for (int i = 0; i < toFindGroupsIn.particles.Count; i++)
            {
                // If we can, see if the object pool has things we can use
                if (i < _objectPool.transform.childCount)
                {
                    particleColliders[i] = _objectPool.transform.GetChild(i).gameObject;
                }
                // otherwise give it a new sphere
                else
                {
                    particleColliders[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    particleColliders[i].GetComponent<SphereCollider>().radius = Radius;
                }
            }

            // Make all particle colliders in that list children of the particleNeighbor object
            // Also disable their renderers and move to into layer 1
            // keep them in this state while we find the groups in this slice
            for (int i = 0; i < numParticles; i++)
            {
                particleColliders[i].transform.position = toFindGroupsIn.particles[i].position;
                particleColliders[i].transform.parent = _particleNeighborObject.transform;
                moveObjectIntoNeighborFinder(particleColliders[i].gameObject);
            }

            for (int i = 0; i < numParticles; i++)
            {
                SphereCollider sc = particleColliders[i].GetComponent<SphereCollider>();
                int layerMask = (1 << _particleNeighborLayer);
                toFindGroupsIn.particles[i].groupIndex = Physics.OverlapSphere(sc.transform.position, 2.1f * sc.radius, layerMask).Length - 1;
            }
            
            // Now for each particle, either
            for (int i = 0; i < numParticles; i++)
            {
                moveInToObjectPool(particleColliders[i].gameObject);
            }

            Debug.Log("Done finding groups for " + numParticles + " particles in slice " + toFindGroupsIn.index);
            _timeSlices.Add(toFindGroupsIn);
        }

        yield break;
    }

    IEnumerator readTimeSlices(TextAsset textFile)
    {
        if (textFile == null)
            yield break;

        TimeSlice sliceBeingRead = null;
        int lastSliceRead = -1;

        // Parse each time slice in the file
        foreach (string s in textFile.text.Split('\n'))
        {
            if (!string.IsNullOrEmpty(s))
            {
                string[] stringValues = s.Replace('\t', ' ').Split(' ');
                if (stringValues.Length >= 4)
                {
                    // Is this a new slice index?
                    int timeSliceIndex = int.Parse(stringValues[0]);
                    if (timeSliceIndex != lastSliceRead)
                    {
                        // If there is data for the previous slice, prepare to find groups
                        if(sliceBeingRead != null)
                        {
                            _pendingGroupFind.Add(sliceBeingRead);
                        }

                        // Add a new slice to the list and take a break
                        sliceBeingRead = new TimeSlice { index = timeSliceIndex };
                        yield return true;

                        lastSliceRead = timeSliceIndex;
                        Debug.Log("Reading time slice " + timeSliceIndex);
                    }

                    // Read slice data 
                    float posX = float.Parse(stringValues[1]);
                    float posY = float.Parse(stringValues[2]);
                    float posZ = float.Parse(stringValues[3]);
                    var pos = new Vector3(posX, posY, posZ);

                    _centroidSum += pos;
                    _centroidSumCount++;

                    // Create a particle object and store it in the time slice
                    Particle p = new Particle { position = pos };
                    sliceBeingRead.particles.Add(p);
                    _maxTimeSlice = System.Math.Max(_maxTimeSlice, timeSliceIndex);
                }
            }
        }

        // Find groups in the last slice
        if (sliceBeingRead != null)
        {
            _pendingGroupFind.Add(sliceBeingRead);
        }

        yield break;
    }

    IEnumerator changeVisibleSlice(int timeSliceIndex)
    {
        TimeSlice timeSlice = null;
        foreach (TimeSlice ts in _timeSlices)
        {
            if (ts.index == VisibleTimeSlice)
            {
                timeSlice = ts;
                break;
            }
        }

        if (timeSlice == null)
            yield break;

        _actualVisibleSlice = VisibleTimeSlice;
        Debug.Log("Displaying slice " + VisibleTimeSlice);
        yield return true;

        // Move everything we've got stored in groups into the object pool
        // and destroy the emptied particle groups
        foreach (KeyValuePair<int, GameObject> particleGroup in _particleGroups)
        {
            Transform particleGroupTransform = particleGroup.Value.transform;
            int particlesInGroup = particleGroupTransform.childCount;
            for (int i = 0; i < particlesInGroup; i++)
            {
                moveInToObjectPool(particleGroupTransform.GetChild(0).gameObject);
            }
            Destroy(particleGroupTransform.gameObject);
        }
        _particleGroups.Clear();

        // take particles for this time-slice from the pool and store them in a list
        int numParticles = timeSlice.particles.Count;
        List<GameObject> particleColliders = new List<GameObject>();
        for (int i = 0; i < numParticles; i++)
        {
            // If we can, see if the object pool has things we can use
            if (i < _objectPool.transform.childCount)
            {
                particleColliders.Add(_objectPool.transform.GetChild(i).gameObject);
            }
            // otherwise give it a new sphere
            else
            {
                Debug.LogError("Why weren't there enough objects in the pool?");
                particleColliders.Add(GameObject.CreatePrimitive(PrimitiveType.Sphere));
            }
        }
        yield return true;

        // Set the particle rendering and add particles to groups
        MaterialPropertyBlock props = new MaterialPropertyBlock();
        for (int i = 0; i < numParticles; i++)
        {
            // use the cached group index and make sure it has a color
            int groupIndex = timeSlice.particles[i].groupIndex;
            if (_colorMap.ContainsKey(groupIndex) == false)
            {
                _colorMap[groupIndex] = Random.ColorHSV();
            }

            // set up the particle renderer by updating the color block per group
            GameObject pc = particleColliders[i];
            Renderer particleRenderer = pc.GetComponent<Renderer>();
            particleRenderer.material = _StandardMat;
            props.SetColor("_Color", _colorMap[groupIndex]);
            particleRenderer.SetPropertyBlock(props);

            // create the particle group parent object
            if (_particleGroups.ContainsKey(groupIndex) == false)
            {
                GameObject groupObject = new GameObject("Group " + groupIndex);
                groupObject.transform.parent = transform;
                _particleGroups[groupIndex] = groupObject;
            }

            // store in group and make visible
            moveInToGroup(pc, groupIndex);
            pc.transform.position = timeSlice.particles[i].position;
        }

        // update visible time slice
        _changeVisibleSliceCoroutine = null;

        yield break;
    }

    Coroutine _changeVisibleSliceCoroutine;
    void Update() 
    {
        if (Input.GetKeyDown(KeyCode.RightArrow) && VisibleTimeSlice < _maxTimeSlice)
            VisibleTimeSlice++;

        else if (Input.GetKeyDown(KeyCode.LeftArrow) && VisibleTimeSlice > 1)
            VisibleTimeSlice--;

        // Don't do anything unless this changes and it's now a slice that we have
        if (VisibleTimeSlice == _actualVisibleSlice)
            return;

        if (_changeVisibleSliceCoroutine != null)
            StopCoroutine(_changeVisibleSliceCoroutine);

        _changeVisibleSliceCoroutine = StartCoroutine(changeVisibleSlice(VisibleTimeSlice));
    }
}
