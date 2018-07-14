using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShliceMaster : MonoBehaviour
{
    #region Variables

    [SerializeField]
    private float sliceExpandSpeed, expandSuccessThreshold;
    [SerializeField]
    private GameObject ballObj;
    [SerializeField]
    private int numLives;

    private float width = 5, height = 2.5f, depth = 2.5f,
                  totalScore = 0;

    private Mesh mesh, expandingMesh;
    private MeshFilter meshFitler = new MeshFilter();
    private GameObject meshObj, sliceObj, meshParent, colliderParent, side1, side2, expandingShliceObj, standbySlice, standBySlice2, standBySlice3;
    private List<Mesh> meshes = new List<Mesh>();
    private List<Triangle> triangles_left = new List<Triangle>(),
                          triangles_right = new List<Triangle>(),
                          oldTriangles = new List<Triangle>(),
                          newTriangles = new List<Triangle>(),
                          shliceTris = new List<Triangle>(),
                          allTris = new List<Triangle>();
    private List<Vert> newVerts = new List<Vert>(),
                       allIntersections = new List<Vert>(),
                       shliceVerts = new List<Vert>();

    private Vector3 centerOfAllIntersections = new Vector3(), centerOfBox = new Vector3();
    private List<GameObject> colliders = new List<GameObject>();
    private List<Level> levels = new List<Level>();
    private Level currentLevel = new Level(0, 0, 0, 0);
    public State currentState = State.Menu;
    private float volumeAtStart = 0f;

    public float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float v321 = p3.x * p2.y * p1.z;
        float v231 = p2.x * p3.y * p1.z;
        float v312 = p3.x * p1.y * p2.z;
        float v132 = p1.x * p3.y * p2.z;
        float v213 = p2.x * p1.y * p3.z;
        float v123 = p1.x * p2.y * p3.z;
        return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
    }

    public float VolumeOfMesh(Mesh _mesh)
    {
        float volume = 0;
        Vector3[] vertices = _mesh.vertices;
        int[] triangles = _mesh.triangles;

        for (int i = 0; i < _mesh.triangles.Length; i += 3)
        {
            Vector3 p1 = vertices[triangles[i + 0]];
            Vector3 p2 = vertices[triangles[i + 1]];
            Vector3 p3 = vertices[triangles[i + 2]];
            volume += SignedVolumeOfTriangle(p1, p2, p3);
        }
        return Mathf.Abs(volume);
    }

    #endregion

    void Start()
    {
        expandingShliceObj = GameObject.FindGameObjectWithTag("SliceExpand");
        meshParent = GameObject.Find("Meshes");
        meshObj = GameObject.Find("Mesh");
        sliceObj = GameObject.Find("Slicer");
        side1 = GameObject.Find("Side1");
        side2 = GameObject.Find("Side2");
        colliderParent = GameObject.Find("Colliders");
        CreateBaseBox();

        volumeAtStart = VolumeOfMesh(mesh);
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Mesh"))
            meshes.Add(go.GetComponent<MeshFilter>().mesh);

        for (int i = 0; i < 20; i++)
        {
            GameObject _parent = new GameObject();
            _parent.name = "Level " + (i + 1).ToString();

            levels.Add(new Level(_percentClearToAdvance: Mathf.Lerp(50f, 90f, (float)i / 20f),
                                 _ballSpeed: Mathf.Lerp(2f, 8f, (float)i / 20f),
                                 _numBalls: (int)Mathf.Ceil((float)(i + 1) / 5f),
                                 _levelNumber: i));

            for (int j = 0; j < levels[i].numBalls; j++)
            {
                GameObject go = GameObject.Instantiate(ballObj);
                go.name = "Ball (Level " + (i + 1).ToString() + ")";
                go.transform.position = meshObj.transform.position;
                BallLogic newBallLogic = go.GetComponent<BallLogic>();
                newBallLogic.speed = levels[i].ballSpeed;
                while (Mathf.Abs(newBallLogic.direction.x) < .5f && Mathf.Abs(newBallLogic.direction.y) < .5f && Mathf.Abs(newBallLogic.direction.z) < .5f)
                    newBallLogic.direction = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
                go.transform.parent = _parent.transform;
                go.SetActive(false);
                levels[i].balls.Add(go);
                levels[i].parent = _parent;
            }
        }

        currentLevel = levels[0];
    }

    void CreateBaseBox()
    {

        expandingMesh = new Mesh();
        mesh = new Mesh();
        meshFitler = meshObj.GetComponent<MeshFilter>();
        meshFitler.mesh = mesh;
        mesh.Clear();

        List<Vector3> baseVertices = new List<Vector3>();
        List<Vert> _baseVertices = new List<Vert>();

        baseVertices.Add(new Vector3(0, 0, 0));
        baseVertices.Add(new Vector3(width, 0, 0));
        baseVertices.Add(new Vector3(0, 0, depth));
        baseVertices.Add(new Vector3(width, 0, depth));
        baseVertices.Add(new Vector3(0, height, 0));
        baseVertices.Add(new Vector3(width, height, 0));
        baseVertices.Add(new Vector3(0, height, depth));
        baseVertices.Add(new Vector3(width, height, depth));

        for (int i = 0; i < baseVertices.Count; i++)
            _baseVertices.Add(new Vert() { index = i, pos = baseVertices[0] });

        mesh.vertices = baseVertices.ToArray();

        List<int> baseTriangles = new List<int>();
        List<Triangle> _baseTriangles = new List<Triangle>();

        //bottom
        baseTriangles.Add(1);
        baseTriangles.Add(3);
        baseTriangles.Add(0);
        baseTriangles.Add(3);
        baseTriangles.Add(2);
        baseTriangles.Add(0);
        //top
        baseTriangles.Add(4);
        baseTriangles.Add(6);
        baseTriangles.Add(5);
        baseTriangles.Add(6);
        baseTriangles.Add(7);
        baseTriangles.Add(5);
        //side (closest)
        baseTriangles.Add(0);
        baseTriangles.Add(4);
        baseTriangles.Add(1);
        baseTriangles.Add(4);
        baseTriangles.Add(5);
        baseTriangles.Add(1);
        //side (left)
        baseTriangles.Add(0);
        baseTriangles.Add(2);
        baseTriangles.Add(4);
        baseTriangles.Add(2);
        baseTriangles.Add(6);
        baseTriangles.Add(4);
        //side (right)
        baseTriangles.Add(1);
        baseTriangles.Add(5);
        baseTriangles.Add(3);
        baseTriangles.Add(5);
        baseTriangles.Add(7);
        baseTriangles.Add(3);
        //side (back)
        baseTriangles.Add(2);
        baseTriangles.Add(3);
        baseTriangles.Add(6);
        baseTriangles.Add(3);
        baseTriangles.Add(7);
        baseTriangles.Add(6);
        ////bottom
        //baseTriangles.Add(1);
        //baseTriangles.Add(0);
        //baseTriangles.Add(3);
        //baseTriangles.Add(3);
        //baseTriangles.Add(0);
        //baseTriangles.Add(2);
        ////top
        //baseTriangles.Add(4);
        //baseTriangles.Add(5);
        //baseTriangles.Add(6);
        //baseTriangles.Add(6);
        //baseTriangles.Add(5);
        //baseTriangles.Add(7);
        ////side (closest)
        //baseTriangles.Add(0);
        //baseTriangles.Add(1);
        //baseTriangles.Add(4);
        //baseTriangles.Add(4);
        //baseTriangles.Add(1);
        //baseTriangles.Add(5);
        ////side (left)
        //baseTriangles.Add(0);
        //baseTriangles.Add(4);
        //baseTriangles.Add(2);
        //baseTriangles.Add(2);
        //baseTriangles.Add(4);
        //baseTriangles.Add(6);
        ////side (right)
        //baseTriangles.Add(1);
        //baseTriangles.Add(3);
        //baseTriangles.Add(5);
        //baseTriangles.Add(5);
        //baseTriangles.Add(3);
        //baseTriangles.Add(7);
        ////side(back)
        //baseTriangles.Add(2);
        //baseTriangles.Add(6);
        //baseTriangles.Add(3);
        //baseTriangles.Add(3);
        //baseTriangles.Add(6);
        //baseTriangles.Add(7);

        for (int i = 0; i < baseTriangles.Count; i += 3)
        {
            _baseTriangles.Add(new Triangle()
            {
                verts = new List<Vert>()
                    {
                            new Vert() { index = baseTriangles[i], pos = baseVertices[baseTriangles[i]] },
                            new Vert() { index = baseTriangles[i+1], pos = baseVertices[baseTriangles[i+1]] },
                            new Vert() { index = baseTriangles[i+2], pos = baseVertices[baseTriangles[i+2]] }
                    }

            });
            centerOfBox += _baseTriangles[_baseTriangles.Count - 1].GetCenter();
        }

        centerOfBox /= _baseTriangles.Count;

        mesh.triangles = baseTriangles.ToArray();

        MeshUtility.Optimize(mesh);
        mesh.RecalculateNormals();

        foreach (GameObject go in colliders)
            DestroyImmediate(go);

        colliders.Clear();
        for (int i = 0; i < _baseTriangles.Count; i++)
        {
            GameObject newcollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newcollider.transform.position = _baseTriangles[i].GetCenter();
            newcollider.transform.rotation = Quaternion.LookRotation(_baseTriangles[i].GetNormal());
            newcollider.transform.localScale = new Vector3(5f, 5f, .1f);
            newcollider.transform.parent = colliderParent.transform;
            newcollider.name = "Collider " + i;
            newcollider.tag = "Collider";
            newcollider.GetComponent<MeshRenderer>().enabled = false;
            newcollider.GetComponent<BoxCollider>().isTrigger = true;
            newcollider.AddComponent<Rigidbody>();
            newcollider.GetComponent<Rigidbody>().isKinematic = true;
            newcollider.GetComponent<Rigidbody>().useGravity = false;
            colliders.Add(newcollider);
        }
    }

    void Update()
    {
        #region DebugLines
        //// DRAW DEBUG LINES FROM MESH 
        //foreach (Mesh mesh in meshes)
        //{
        //    for (int i = 0; i < mesh.triangles.Count(); i += 3)
        //    {
        //        Vector3 center = Vector3.zero;
        //        foreach (Vert intersection in allIntersections)
        //            center += intersection.pos;
        //        center /= (float)allIntersections.Count;

        //        Debug.DrawLine(transform.TransformPoint(mesh.vertices[mesh.triangles[i]]), transform.TransformPoint(mesh.vertices[mesh.triangles[i + 1]]), Color.white);
        //        Debug.DrawLine(transform.TransformPoint(mesh.vertices[mesh.triangles[i + 1]]), transform.TransformPoint(mesh.vertices[mesh.triangles[i + 2]]), Color.white);
        //        Debug.DrawLine(transform.TransformPoint(mesh.vertices[mesh.triangles[i + 2]]), transform.TransformPoint(mesh.vertices[mesh.triangles[i]]), Color.white);

        //    }
        //}

        ////// DRAW DEBUG LINES FROM MESH 

        //for (int i = 0; i < expandingMesh.triangles.Count(); i += 3)
        //{
        //    Vector3 center = Vector3.zero;
        //    foreach (Vert intersection in allIntersections)
        //        center += intersection.pos;
        //    center /= (float)allIntersections.Count;

        //    Debug.DrawLine(transform.TransformPoint(expandingMesh.vertices[expandingMesh.triangles[i]]), transform.TransformPoint(expandingMesh.vertices[expandingMesh.triangles[i + 1]]), Color.white);
        //    Debug.DrawLine(transform.TransformPoint(expandingMesh.vertices[expandingMesh.triangles[i + 1]]), transform.TransformPoint(expandingMesh.vertices[expandingMesh.triangles[i + 2]]), Color.white);
        //    Debug.DrawLine(transform.TransformPoint(expandingMesh.vertices[expandingMesh.triangles[i + 2]]), transform.TransformPoint(expandingMesh.vertices[expandingMesh.triangles[i]]), Color.white);

        //}



        ////// DRAW DEBUG LINES FROM NEWTRIANGLES 
        //foreach (Triangle triangle in newTriangles)
        //{
        //    float r = UnityEngine.Random.Range(0f, 1f);
        //    float g = UnityEngine.Random.Range(0f, 1f);
        //    float b = UnityEngine.Random.Range(0f, 1f);

        //    Color newcolor = new Color(r, g, b, 1f);
        //    Debug.DrawLine(newVerts[triangle.index1].pos, newVerts[triangle.index2].pos, Color.red);
        //    Debug.DrawLine(newVerts[triangle.index2].pos, newVerts[triangle.index3].pos, Color.red);
        //    Debug.DrawLine(newVerts[triangle.index3].pos, newVerts[triangle.index1].pos, Color.red);
        //}
        //// DRAW DEBUG LINES FROM NEWTRIANGLES 
        for (int i = 0; i < mesh.triangles.Count(); i += 3)
        {

            Color newcolor = new Color(1f, 1f, 1f, .1f);
            Debug.DrawLine(mesh.vertices[mesh.triangles[i]], mesh.vertices[mesh.triangles[i + 1]], newcolor);
            Debug.DrawLine(mesh.vertices[mesh.triangles[i + 1]], mesh.vertices[mesh.triangles[i + 2]], newcolor);
            Debug.DrawLine(mesh.vertices[mesh.triangles[i + 2]], mesh.vertices[mesh.triangles[i]], newcolor);
        }
        ////// DRAW DEBUG LINES FROM NEWTRIANGLES 
        //foreach (Triangle triangle in triangles_left)
        //{

        //    Color newcolor = Color.red;
        //    Debug.DrawLine(newVerts[triangle.index1].pos, newVerts[triangle.index2].pos, Color.blue);
        //    Debug.DrawLine(newVerts[triangle.index2].pos, newVerts[triangle.index3].pos, Color.blue);
        //    Debug.DrawLine(newVerts[triangle.index3].pos, newVerts[triangle.index1].pos, Color.blue);
        //}
        //foreach (Triangle triangle in triangles_right)
        //{

        //    Color newcolor = Color.red;
        //    Debug.DrawLine(newVerts[triangle.index1].pos, newVerts[triangle.index2].pos, Color.red);
        //    Debug.DrawLine(newVerts[triangle.index2].pos, newVerts[triangle.index3].pos, Color.red);
        //    Debug.DrawLine(newVerts[triangle.index3].pos, newVerts[triangle.index1].pos, Color.red);
        //}

        //foreach (Vector3 vert in mesh.vertices)
        //{
        //    float distbox = .03f;

        //    Debug.DrawLine(vert + new Vector3(-distbox, -distbox, 0f), vert + new Vector3(-distbox, distbox, 0f), Color.red);
        //    Debug.DrawLine(vert + new Vector3(-distbox, distbox, 0f), vert + new Vector3(distbox, distbox, 0f), Color.red);
        //    Debug.DrawLine(vert + new Vector3(distbox, distbox, 0f), vert + new Vector3(distbox, -distbox, 0f), Color.red);
        //    Debug.DrawLine(vert + new Vector3(distbox, -distbox, 0f), vert + new Vector3(-distbox, -distbox, 0f), Color.red);
        //}
        //foreach (Triangle triangle in newTriangles)
        //{
        //    float distbox = .2f;
        //    Debug.DrawLine(triangle.GetCenter() + new Vector3(-distbox, -distbox, 0f), triangle.GetCenter() + new Vector3(-distbox, distbox, 0f), Color.red);
        //    Debug.DrawLine(triangle.GetCenter() + new Vector3(-distbox, distbox, 0f), triangle.GetCenter() + new Vector3(distbox, distbox, 0f), Color.red);
        //    Debug.DrawLine(triangle.GetCenter() + new Vector3(distbox, distbox, 0f), triangle.GetCenter() + new Vector3(distbox, -distbox, 0f), Color.red);
        //    Debug.DrawLine(triangle.GetCenter() + new Vector3(distbox, -distbox, 0f), triangle.GetCenter() + new Vector3(-distbox, -distbox, 0f), Color.red);
        //}
        ////// DRAW INTERSECTIONS 
        //foreach (Vert vert in allIntersections)
        //{
        //    float distbox = .2f;

        //    Debug.DrawLine(vert.pos + new Vector3(-distbox, -distbox, 0f), vert.pos + new Vector3(-distbox, distbox, 0f), Color.red);
        //    Debug.DrawLine(vert.pos + new Vector3(-distbox, distbox, 0f), vert.pos + new Vector3(distbox, distbox, 0f), Color.red);
        //    Debug.DrawLine(vert.pos + new Vector3(distbox, distbox, 0f), vert.pos + new Vector3(distbox, -distbox, 0f), Color.red);
        //    Debug.DrawLine(vert.pos + new Vector3(distbox, -distbox, 0f), vert.pos + new Vector3(-distbox, -distbox, 0f), Color.red);
        //}
        #endregion

        if (currentState == State.Menu)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                InitializeGame();
        }
        else if (currentState == State.Results)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                SceneManager.LoadScene(0);
        }
        else if (currentState == State.Playing)
        {
            foreach (GameObject go in currentLevel.balls)
            {
                if (Vector3.Distance(go.transform.position, centerOfBox) > 7f)
                    go.transform.position = centerOfBox;
            }

            if (GameObject.Find("SlicerCube").GetComponent<MeshRenderer>().enabled)
            {
                if (Input.GetKey(KeyCode.A))
                    sliceObj.transform.Translate(-.1f * Time.deltaTime * 25f, 0f, 0f, Space.World);
                if (Input.GetKey(KeyCode.D))
                    sliceObj.transform.Translate(.1f * Time.deltaTime * 25f, 0f, 0f, Space.World);
                if (Input.GetKey(KeyCode.W))
                    sliceObj.transform.Translate(0f, 0f, .1f * Time.deltaTime * 25f, Space.World);
                if (Input.GetKey(KeyCode.S))
                    sliceObj.transform.Translate(0f, 0f, -.1f * Time.deltaTime * 25f, Space.World);
                if (Input.GetKey(KeyCode.E))
                    sliceObj.transform.Translate(0f, .1f * Time.deltaTime * 25f, 0f, Space.World);
                if (Input.GetKey(KeyCode.Q))
                    sliceObj.transform.Translate(0f, -.1f * Time.deltaTime * 25f, 0f, Space.World);

                if (Input.GetKey(KeyCode.UpArrow))
                    sliceObj.transform.Rotate(-1f * Time.deltaTime * 200f, 0f, 0f, Space.World);
                if (Input.GetKey(KeyCode.DownArrow))
                    sliceObj.transform.Rotate(1f * Time.deltaTime * 200f, 0f, 0f, Space.World);
                if (Input.GetKey(KeyCode.LeftArrow))
                    sliceObj.transform.Rotate(0f, 1f * Time.deltaTime * 200f, 0f, Space.World);
                if (Input.GetKey(KeyCode.RightArrow))
                    sliceObj.transform.Rotate(0f, -1f * Time.deltaTime * 200f, 0f, Space.World);

                if (Input.GetKeyDown(KeyCode.Space))
                    Shlice();
                if (Input.GetKeyDown(KeyCode.Escape))
                    SceneManager.LoadScene(0);
            }
        }
    }

    public void InitializeGame()
    {
        totalScore = 0f;
        currentLevel = levels[0];
        foreach (Level level in levels)
            foreach (GameObject go in level.balls)
            {
                go.SetActive(level == currentLevel);
                go.transform.position = centerOfBox;
            }

        currentState = State.Playing;

    }

    private void Shlice()
    {
        oldTriangles.Clear();
        newTriangles.Clear();
        newVerts.Clear();
        triangles_left.Clear();
        triangles_right.Clear();
        allIntersections.Clear();

        //old verts will always be new verts
        for (int i = 0; i < mesh.vertices.Count(); i++)
        {
            Vert oldVert = new Vert() { index = i, pos = mesh.vertices[i] };
            if (!newVerts.Contains(oldVert))
                newVerts.Add(oldVert);
        }

        //grab existing tris directly from the mesh
        for (int i = 0; i < mesh.triangles.Count(); i += 3)
        {
            Triangle _oldTri = new Triangle()
            {
                verts = new List<Vert>() {new Vert() { index = mesh.triangles[i], pos = mesh.vertices[mesh.triangles[i]] },
                                          new Vert() { index = mesh.triangles[i+1], pos = mesh.vertices[mesh.triangles[i+1]] },
                                          new Vert() { index = mesh.triangles[i+2], pos = mesh.vertices[mesh.triangles[i+2]] }}
            };
            oldTriangles.Add(_oldTri);
        }

        //iterate through ever current triangle
        foreach (Triangle oldTri in oldTriangles)
        {
            // RaycastHit _hit;
            RaycastHit[] _hits;

            List<Vert> myIntersections = new List<Vert>();
            Vector3 dir = oldTri.pos2 - oldTri.pos1;
            Vert lastIntersection = new Vert(), firstIntersection = new Vert();
            bool hitSide1 = false, hitSide2 = false, hitSide3 = false;

            //check for intersections from point1 -> point2
            _hits = Physics.RaycastAll(new Ray(oldTri.pos1, dir), Vector3.Distance(oldTri.pos1, oldTri.pos2));
            foreach (RaycastHit _hit in _hits)
            {
                if (_hit.transform.tag == "Slicer")
                {
                    hitSide1 = true;

                    Vector3 intersection = _hit.point;
                    if (!newVerts.Exists(v => Vector3.Distance(v.pos, intersection) < .001f))
                        newVerts.Add(new Vert() { index = newVerts.Count, pos = intersection });

                    myIntersections.Add(newVerts.Find(v => Vector3.Distance(v.pos, intersection) < .001f));
                    break;
                }

            }
            dir = oldTri.pos3 - oldTri.pos2;

            _hits = Physics.RaycastAll(new Ray(oldTri.pos2, dir), Vector3.Distance(oldTri.pos3, oldTri.pos2));
            foreach (RaycastHit _hit in _hits)
            {

                if (_hit.transform.tag == "Slicer")
                {
                    hitSide2 = true;

                    Vector3 intersection = _hit.point;
                    if (!newVerts.Exists(v => Vector3.Distance(v.pos, intersection) < .001f))
                        newVerts.Add(new Vert() { index = newVerts.Count, pos = intersection });

                    myIntersections.Add(newVerts.Find(v => Vector3.Distance(v.pos, intersection) < .001f));
                    break;
                }
            }

            dir = oldTri.pos1 - oldTri.pos3;

            //check for intersections from point3 -> point1
            _hits = Physics.RaycastAll(new Ray(oldTri.pos3, dir), Vector3.Distance(oldTri.pos3, oldTri.pos1));
            foreach (RaycastHit _hit in _hits)
            {
                if (_hit.transform.tag == "Slicer")
                {
                    hitSide3 = true;

                    Vector3 intersection = _hit.point;
                    if (!newVerts.Exists(v => Vector3.Distance(v.pos, intersection) < .001f))
                        newVerts.Add(new Vert() { index = newVerts.Count, pos = intersection });

                    myIntersections.Add(newVerts.Find(v => Vector3.Distance(v.pos, intersection) < .001f));
                    break;
                }
            }

            //found some intersections in this triangle
            if (myIntersections.Count > 0)
            {
                allIntersections.AddRange(myIntersections);
                Vert lonelyVert = new Vert(), secondVert = new Vert(), thirdVert = new Vert();

                //when we slice a triangle, one half will be a new triangle, and the other half will have 4 sides (to be subdivided further)
                //these values below help us easily determine where those new verts/tris are. 
                //(sortaaaa hacky bs)
                if (hitSide1 && hitSide2) { lonelyVert = oldTri.verts[1]; secondVert = oldTri.verts[2]; thirdVert = oldTri.verts[0]; firstIntersection = myIntersections[0]; lastIntersection = myIntersections[1]; }
                if (hitSide2 && hitSide3) { lonelyVert = oldTri.verts[2]; secondVert = oldTri.verts[0]; thirdVert = oldTri.verts[1]; firstIntersection = myIntersections[0]; lastIntersection = myIntersections[1]; }
                if (hitSide3 && hitSide1) { lonelyVert = oldTri.verts[0]; secondVert = oldTri.verts[1]; thirdVert = oldTri.verts[2]; firstIntersection = myIntersections[1]; lastIntersection = myIntersections[0]; }

                Triangle _dividedTriangle0 = new Triangle()  //this is the half that already has 3 sides
                {
                    verts = new List<Vert>() { new Vert() { index = firstIntersection.index, pos = firstIntersection.pos },
                                               new Vert() { index = lastIntersection.index, pos = lastIntersection.pos },
                                               new Vert() { index = lonelyVert.index, pos = lonelyVert.pos }}
                };
                Triangle _dividedTriangle1 = new Triangle() //subdividing the 4-sided shape into 2 separate triangles
                {
                    verts = new List<Vert>(){ new Vert() { index = firstIntersection.index, pos = firstIntersection.pos },
                                              new Vert() { index = lastIntersection.index, pos = lastIntersection.pos },
                                              new Vert() { index = secondVert.index, pos = secondVert.pos }}
                };
                Triangle _dividedTriangle2 = new Triangle() //subdividing the 4-sided shape into 2 separate triangles
                {
                    verts = new List<Vert>() {new Vert() { index = firstIntersection.index, pos = firstIntersection.pos },
                                              new Vert() { index = secondVert.index, pos = secondVert.pos },
                                              new Vert() { index = thirdVert.index, pos = thirdVert.pos }}
                };

                _dividedTriangle0.MatchDirection(oldTri.GetNormal());
                _dividedTriangle1.MatchDirection(oldTri.GetNormal());
                _dividedTriangle2.MatchDirection(oldTri.GetNormal());

                newTriangles.Add(_dividedTriangle0);
                newTriangles.Add(_dividedTriangle1);
                newTriangles.Add(_dividedTriangle2);
            }

            else  //no intersections. bring these triangles over unaltered
            {
                newTriangles.Add(oldTri);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////
        //// we need to add some new geometry for the part of the cube that was sliced. 
        //// we do this in a "spoke wheel" fashion based on the center of all intersections
        //// this code currently breaks when attempting multiple slices in one play session, so it is commented out for now
        ///////////////////////////////////////////////////////////////////////////////
        if (allIntersections.Count > 0)
        {
            Vector3 center = Vector3.zero;
            foreach (Vert intersection in allIntersections)
                center += intersection.pos;
            center /= (float)allIntersections.Count;
            centerOfAllIntersections = center;
            newVerts.Add(new Vert() { index = newVerts.Count, pos = center });

            for (int i = 0; i < allIntersections.Count; i += 2)
            {

                Triangle tri = new Triangle()
                {
                    verts = new List<Vert>() { allIntersections[i],
                                          new Vert() {index = newVerts.Count-1, pos = center },
                                          allIntersections[i+1] },
                    isNewSliceGeometry = true
                };
                tri.MatchDirection(-1f * (side1.transform.position - sliceObj.transform.position).normalized);
                triangles_left.Add(tri);

                tri = new Triangle()
                {
                    verts = new List<Vert>() { allIntersections[i],
                                          new Vert() {index = newVerts.Count-1, pos = center },
                                          allIntersections[i+1] },
                    isNewSliceGeometry = true
                };
                tri.MatchDirection((side1.transform.position - sliceObj.transform.position).normalized);
                triangles_right.Add(tri);
            }

            //now to deal with the actual gameobjects/meshes
            Material mat = meshObj.GetComponent<MeshRenderer>().material;
            List<int> indices = new List<int>();
            List<Vector3> newVertPositions = new List<Vector3>();
            Mesh meshL = new Mesh(),
                 meshR = new Mesh(),
                 meshFull = new Mesh();

            for (int i = 0; i < newVerts.Count; i++)
                newVertPositions.Add(newVerts[i].pos);

            foreach (Triangle triangle in newTriangles)
            {
                if (getSide(triangle.GetCenter()) == 0)
                    triangles_left.Add(triangle);
                else
                    triangles_right.Add(triangle);
            }

            //LEFT SIDE
            foreach (Triangle triangle in triangles_left)
                for (int i = 0; i < triangle.verts.Count; i++)
                    indices.Add(triangle.verts[i].index);
            meshL.vertices = newVertPositions.ToArray();
            meshL.triangles = indices.ToArray();
            MeshUtility.Optimize(meshL);
            meshL.RecalculateNormals();
            meshL.RecalculateBounds();
            GameObject go1 = new GameObject();
            go1.name = "Slice 1";
            go1.AddComponent<Rigidbody>();
            go1.GetComponent<Rigidbody>().isKinematic = true;
            go1.GetComponent<Rigidbody>().useGravity = false;
            go1.transform.parent = meshParent.transform;
            MeshFilter mf1 = go1.AddComponent<MeshFilter>();
            mf1.mesh = meshL;
            MeshRenderer mr1 = go1.AddComponent<MeshRenderer>();
            mr1.material = mat;
            go1.SetActive(false);
            standbySlice = go1;

            indices.Clear();

            //RIGHT SIDE
            foreach (Triangle triangle in triangles_right)
                for (int i = 0; i < triangle.verts.Count; i++)
                    indices.Add(triangle.verts[i].index);
            meshR.vertices = newVertPositions.ToArray();
            meshR.triangles = indices.ToArray();
            MeshUtility.Optimize(meshR);
            meshR.RecalculateNormals();
            meshR.RecalculateBounds();
            GameObject go2 = new GameObject();
            go2.name = "Slice 2";
            go2.tag = "Mesh";
            go2.AddComponent<Rigidbody>();
            go2.GetComponent<Rigidbody>().isKinematic = true;
            go2.GetComponent<Rigidbody>().useGravity = false;
            go2.transform.parent = meshParent.transform;
            MeshFilter mf2 = go2.AddComponent<MeshFilter>();
            mf2.mesh = meshR;
            MeshRenderer mr2 = go2.AddComponent<MeshRenderer>();
            mr2.material = mat;
            go2.SetActive(false);
            standBySlice2 = go2;


            indices.Clear();

            //RIGHT SIDE
            allTris = triangles_left;
            allTris.AddRange(triangles_right);

            foreach (Triangle triangle in allTris)
                for (int i = 0; i < triangle.verts.Count; i++)
                    indices.Add(triangle.verts[i].index);
            meshFull.vertices = newVertPositions.ToArray();
            meshFull.triangles = indices.ToArray();
            MeshUtility.Optimize(meshFull);
            meshFull.RecalculateNormals();
            meshFull.RecalculateBounds();
            GameObject go3 = new GameObject();
            go3.name = "Slice 3";
            go3.tag = "Mesh";
            go3.AddComponent<Rigidbody>();
            go3.GetComponent<Rigidbody>().isKinematic = true;
            go3.GetComponent<Rigidbody>().useGravity = false;
            go3.transform.parent = meshParent.transform;
            MeshFilter mf3 = go3.AddComponent<MeshFilter>();
            mf3.mesh = meshFull;
            MeshRenderer mr3 = go3.AddComponent<MeshRenderer>();
            mr3.material = mat;
            go3.SetActive(false);
            standBySlice3 = go3;


            StopCoroutine("ShliceExpand");
            StartCoroutine("ShliceExpand");
            //SuccessfulSlice();

        }
    }

    private bool isExpanding = false;

    private IEnumerator ShliceExpand()
    {
        isExpanding = true;
        Vector3 sliceStartingPoint = new Vector3(sliceObj.transform.position.x, sliceObj.transform.position.y, sliceObj.transform.position.z);
        shliceVerts.Clear();
        shliceTris.Clear();

        slicerCubeShow(false);
        for (int i = 0; i < allIntersections.Count; i++)
            shliceVerts.Add(new Vert() { index = i, pos = sliceStartingPoint });
        shliceVerts.Add(new Vert() { index = shliceVerts.Count, pos = sliceStartingPoint });

        for (int j = 0; j < shliceVerts.Count - 1; j++)
            shliceVerts[j].pos = Vector3.Lerp(shliceVerts[j].pos, allIntersections[j].pos, sliceExpandSpeed * (Time.deltaTime * 50f));

        expandingShliceObj.SetActive(true);

        expandingMesh = expandingShliceObj.GetComponent<MeshFilter>().mesh;
        expandingShliceObj.GetComponent<MeshCollider>().sharedMesh = expandingShliceObj.GetComponent<MeshFilter>().mesh;

        List<Vector3> _shliceVerts = new List<Vector3>();
        List<int> _shliceTris = new List<int>();

        for (int i = 0; i < shliceVerts.Count - 1; i += 2)
        {

            Triangle tri = new Triangle()
            {
                verts = new List<Vert>() { shliceVerts[i],
                                          new Vert() {index = shliceVerts.Count-1, pos = sliceStartingPoint },
                                          shliceVerts[i+1] },
                isNewSliceGeometry = true
            };
            tri.MatchDirection(-1f * tri.GetNormal());
            shliceTris.Add(tri);

            tri = new Triangle()
            {
                verts = new List<Vert>() { shliceVerts[i],
                                          new Vert() {index = shliceVerts.Count-1, pos = sliceStartingPoint },
                                          shliceVerts[i+1] },
                isNewSliceGeometry = true
            };
            tri.MatchDirection(tri.GetNormal());
            shliceTris.Add(tri);
        }



        _shliceVerts.Clear();
        _shliceTris.Clear();
        for (int i = 0; i < shliceVerts.Count; i++)
            _shliceVerts.Add(shliceVerts[i].pos);

        foreach (Triangle tri in shliceTris)
        {
            _shliceTris.Add(tri.index1);
            _shliceTris.Add(tri.index2);
            _shliceTris.Add(tri.index3);
        }
        expandingMesh.Clear();
        expandingMesh.vertices = _shliceVerts.ToArray();
        expandingMesh.triangles = _shliceTris.ToArray();

        List<int> indexesCompletes = new List<int>();

        while (indexesCompletes.Count < shliceVerts.Count - 1)
        {
            for (int j = 0; j < shliceVerts.Count - 1; j++)
            {

                shliceVerts[j].pos = Vector3.Lerp(shliceVerts[j].pos, allIntersections[j].pos, sliceExpandSpeed * (Time.deltaTime * 50f));
                if (Vector3.Distance(shliceVerts[j].pos, allIntersections[j].pos) < expandSuccessThreshold)
                {
                    if (!indexesCompletes.Contains(j))
                        indexesCompletes.Add(j);

                    shliceVerts[j].pos = allIntersections[j].pos;
                }

            }

            _shliceVerts.Clear();
            _shliceTris.Clear();
            for (int k = 0; k < shliceVerts.Count; k++)
                _shliceVerts.Add(shliceVerts[k].pos);

            foreach (Triangle tri in shliceTris)
            {
                _shliceTris.Add(tri.index1);
                _shliceTris.Add(tri.index2);
                _shliceTris.Add(tri.index3);
            }
            expandingMesh.Clear();
            expandingMesh.vertices = _shliceVerts.ToArray();
            expandingMesh.triangles = _shliceTris.ToArray();

            expandingShliceObj.GetComponent<MeshCollider>().skinWidth = UnityEngine.Random.Range(.01f, .011f);

            yield return new WaitForEndOfFrame();
        }

        if (isExpanding)
            SuccessfulSlice();


        isExpanding = false;
    }


    public void CancelExpand()
    {
        if (isExpanding)
        {
            numLives--;
            isExpanding = false;
            StopCoroutine("ShliceExpand");

            if (numLives <= 0)
            {
                currentState = State.Results;
                foreach (Level level in levels)
                    foreach (GameObject go in level.balls)
                        go.GetComponent<BallLogic>().enabled = false;

            }
            else
            {
                slicerCubeShow(true);
                expandingShliceObj.SetActive(false);
            }
        }
    }

    private void slicerCubeShow(bool _enabled)
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("SlicerCube"))
            go.GetComponent<MeshRenderer>().enabled = _enabled;
    }

    private void SuccessfulSlice()
    {

        expandingMesh.Clear();
        expandingShliceObj.SetActive(false);
        slicerCubeShow(true);
        List<Triangle> trianglesToUse = new List<Triangle>();
        bool allBallsOnSameSide = true;

        int currentSide = getSide(currentLevel.balls[0].transform.position);

        for (int i = 1; i < currentLevel.balls.Count; i++)
        {
            if (getSide(currentLevel.balls[i].transform.position) != currentSide)
                allBallsOnSameSide = false;
        }
        DestroyImmediate(meshObj);


        if (allBallsOnSameSide)
        {


            if (currentSide == 0)
            {
                standbySlice.gameObject.tag = "Mesh";
                standbySlice.SetActive(true);
                GameObject.Destroy(standBySlice2);
                GameObject.Destroy(standBySlice3);

                trianglesToUse = triangles_left;
            }
            else
            {
                standBySlice2.gameObject.tag = "Mesh";
                standBySlice2.SetActive(true);
                GameObject.Destroy(standbySlice);
                GameObject.Destroy(standBySlice3);

                trianglesToUse = triangles_right;
            }
        }
        else
        {
            standBySlice3.gameObject.tag = "Mesh";
            standBySlice3.SetActive(true);
            GameObject.Destroy(standbySlice);
            GameObject.Destroy(standBySlice2);
            trianglesToUse = allTris;
        }

        meshObj = GameObject.FindGameObjectWithTag("Mesh");
        meshes.Clear();
        meshes.Add(meshObj.GetComponent<MeshFilter>().mesh);
        mesh = meshObj.GetComponent<MeshFilter>().mesh;


        foreach (GameObject go in colliders)
            DestroyImmediate(go);
        colliders.Clear();

        centerOfBox = new Vector3(0f, 0f, 0f);

        for (int i = 0; i < trianglesToUse.Count(); i++)
        {
            centerOfBox += trianglesToUse[i].GetCenter();
            GameObject newcollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newcollider.transform.position = trianglesToUse[i].GetCenter();
            newcollider.transform.rotation = Quaternion.LookRotation(trianglesToUse[i].GetNormal());
            newcollider.transform.localScale = new Vector3(5f, 5f, .1f);
            newcollider.transform.parent = colliderParent.transform;
            newcollider.name = "Collider " + i;
            newcollider.tag = "Collider";
            newcollider.GetComponent<MeshRenderer>().enabled = false;
            newcollider.GetComponent<BoxCollider>().isTrigger = true;
            newcollider.AddComponent<Rigidbody>();
            newcollider.GetComponent<Rigidbody>().isKinematic = true;
            newcollider.GetComponent<Rigidbody>().useGravity = false;
            colliders.Add(newcollider);
        }
        centerOfBox /= trianglesToUse.Count();

        currentLevel.areaLeft = (VolumeOfMesh(mesh) / volumeAtStart) * 100f;
        currentLevel.areaLeft = (float)Math.Round(currentLevel.areaLeft, 2);

        if (currentLevel.areaLeft < 100f - currentLevel.percentClearToAdvance)
        {
            AdvanceToNextLevel();
        }
    }

    public void AdvanceToNextLevel()
    {
        currentLevel = levels[currentLevel.levelNumber + 1];

        foreach (Level level in levels)
            foreach (GameObject go in level.balls)
            {
                go.SetActive(level == currentLevel);
                go.transform.position = centerOfBox;
            }

        CreateBaseBox();

    }

    private void OnGUI()
    {
        if (currentState == State.Playing || currentState == State.Results)
        {
            Rect center = new Rect(Screen.width / 2f, Screen.height / 2f, 200f, 50f);
            Rect bottom = new Rect(Screen.width / 2f, Screen.height, 200f, 50f);
            Rect topbottom = new Rect(Screen.width / 2f, 0f, 200f, 50f);
            Rect left = new Rect(0f, Screen.height / 2f, 200f, 50f);
            Rect right = new Rect(Screen.width, Screen.height / 2f, 200f, 50f);
            GUIStyle style = new GUIStyle();
            style.fontSize = 30;
            style.normal.textColor = Color.black;

            Vector2 _level = left.position + new Vector2(10f, -50f);
            Vector2 _area = left.position + new Vector2(10f, -100f);
            Vector2 _area2 = left.position + new Vector2(10f, -150f);
            Vector2 _lives = left.position + new Vector2(10f, -200f);

            GUI.Label(new Rect(_level, center.size), "Level " + (currentLevel.levelNumber + 1), style);
            GUI.Label(new Rect(_area, center.size), "Volume Left: " + currentLevel.areaLeft + "%", style);
            GUI.Label(new Rect(_area2, center.size), "Next Level at: " + (100f - currentLevel.percentClearToAdvance) + "%", style);
            GUI.Label(new Rect(_lives, center.size), "Lives: " + numLives, style);

            _level += new Vector2(-2f, -2f);
            _area += new Vector2(-2f, -2f);
            _area2 += new Vector2(-2f, -2f);
            _lives += new Vector2(-2f, -2f);

            style.normal.textColor = Color.white;
            GUI.Label(new Rect(_level, center.size), "Level " + (currentLevel.levelNumber + 1), style);
            GUI.Label(new Rect(_area, center.size), "Volume Left: " + currentLevel.areaLeft + "%", style);
            GUI.Label(new Rect(_area2, center.size), "Next Level at: " + (100f - currentLevel.percentClearToAdvance) + "%", style);
            GUI.Label(new Rect(_lives, center.size), "Lives: " + numLives, style);
        }
    }

    #region Helpers



    private int getSide(Vector3 point) //returns which side of the slicer this point is on
    {
        float side1dist = Vector3.Distance(point, GameObject.Find("Side1").transform.position);
        float side2dist = Vector3.Distance(point, GameObject.Find("Side2").transform.position);

        if (side1dist > side2dist)
            return 1;
        else
            return 0;

        RaycastHit[] hits;
        hits = Physics.BoxCastAll(point, new Vector3(.1f, .1f, .1f), transform.forward);
        int ret = 0;

        foreach (RaycastHit hit in hits)
        {

            if (hit.transform.tag == "Side")
            {
                return Convert.ToInt16(hit.transform.name.Replace("Side", "")) - 1;
            }
        }

        return ret;
    }

    #endregion

}

public enum State
{
    Menu = 1,
    Playing = 2,
    Results = 3
}

public class Triangle
{
    public List<Vert> verts = new List<Vert>();
    public Color color = new Color();
    public bool isNewSliceGeometry = false;

    public int index1 { get { return verts[0].index; } }
    public int index2 { get { return verts[1].index; } }
    public int index3 { get { return verts[2].index; } }
    public Vector3 pos1 { get { return verts[0].pos; } }
    public Vector3 pos2 { get { return verts[1].pos; } }
    public Vector3 pos3 { get { return verts[2].pos; } }

    public Vector3 GetNormal()
    {
        return Vector3.Cross(pos1 - pos2, pos1 - pos3).normalized;
    }
    public void MatchDirection(Vector3 dir)
    {
        if (Vector3.Dot(GetNormal(), dir) > 0)
        {
            return;
        }
        else
        {
            Vert v1 = verts[0];
            verts[0] = verts[2];
            verts[2] = v1;
        }
    }
    public void SetRdandomColor()
    {
        float r = UnityEngine.Random.Range(0f, 1f),
              g = UnityEngine.Random.Range(0f, 1f),
              b = UnityEngine.Random.Range(0f, 1f);

        color = new Color(r, g, b, 1f);
    }
    public Vector3 GetCenter()
    {
        return (pos1 + pos2 + pos3) / 3f;
    }
}

public class Vert
{
    public Vector3 pos { get; set; }
    public int index { get; set; }
}

public class Level
{
    public float percentClearToAdvance { get; set; }
    public float ballSpeed { get; set; }
    public int numBalls { get; set; }
    public int levelNumber { get; set; }
    public List<GameObject> balls { get; set; }
    public GameObject parent { get; set; }
    public float areaLeft { get; set; }
    public Level(float _percentClearToAdvance, float _ballSpeed, int _numBalls, int _levelNumber)
    {
        percentClearToAdvance = _percentClearToAdvance;
        ballSpeed = _ballSpeed;
        numBalls = _numBalls;
        levelNumber = _levelNumber;
        balls = new List<GameObject>();
        areaLeft = 100f;
    }
}