using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ShliceMaster : MonoBehaviour
{
    #region Variables

    private float width = 5, height = 5, depth = 5;
    private Mesh mesh;
    private MeshFilter meshFitler = new MeshFilter();
    private GameObject meshObj, sliceObj, meshParent, side1, side2;
    private List<Mesh> meshes = new List<Mesh>();
    private List<Triangle> triangles_left = new List<Triangle>(),
                          triangles_right = new List<Triangle>(),
                          oldTriangles = new List<Triangle>(),
                          newTriangles = new List<Triangle>();
    private List<Vert> newVerts = new List<Vert>(),
                       allIntersections = new List<Vert>();
    #endregion

    void Start()
    {
        meshParent = GameObject.Find("Meshes");
        meshObj = GameObject.Find("Mesh");
        sliceObj = GameObject.Find("Slicer");
        side1 = GameObject.Find("Side1");
        side2 = GameObject.Find("Side2");

        CreateBaseBox();

        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Mesh"))
            meshes.Add(go.GetComponent<MeshFilter>().mesh);
    }

    void CreateBaseBox()
    {
        mesh = new Mesh();
        meshFitler = meshObj.GetComponent<MeshFilter>();
        meshFitler.mesh = mesh;
        mesh.Clear();

        List<Vector3> baseVertices = new List<Vector3>();
        baseVertices.Add(new Vector3(0, 0, 0));
        baseVertices.Add(new Vector3(width, 0, 0));
        baseVertices.Add(new Vector3(0, 0, depth));
        baseVertices.Add(new Vector3(width, 0, depth));
        baseVertices.Add(new Vector3(0, height, 0));
        baseVertices.Add(new Vector3(width, height, 0));
        baseVertices.Add(new Vector3(0, height, depth));
        baseVertices.Add(new Vector3(width, height, depth));
        mesh.vertices = baseVertices.ToArray();

        List<int> baseTriangles = new List<int>();
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
        mesh.triangles = baseTriangles.ToArray();

        MeshUtility.Optimize(mesh);
        mesh.RecalculateNormals();
        meshObj.GetComponent<MeshCollider>().sharedMesh = mesh;
        meshObj.GetComponent<MeshCollider>().convex = true;
    }

    void Update()
    {
        #region DebugLines
        ////// DRAW DEBUG LINES FROM MESH 
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

        if (Input.GetKey(KeyCode.A))
            sliceObj.transform.Translate(-.1f, 0f, 0f, Space.World);
        if (Input.GetKey(KeyCode.D))
            sliceObj.transform.Translate(.1f, 0f, 0f, Space.World);
        if (Input.GetKey(KeyCode.W))
            sliceObj.transform.Translate(0f, 0f, .1f, Space.World);
        if (Input.GetKey(KeyCode.S))
            sliceObj.transform.Translate(0f, 0f, -.1f, Space.World);
        if (Input.GetKey(KeyCode.E))
            sliceObj.transform.Translate(0f, .1f, 0f, Space.World);
        if (Input.GetKey(KeyCode.Q))
            sliceObj.transform.Translate(0f, -.1f, 0f, Space.World);

        if (Input.GetKey(KeyCode.UpArrow))
            sliceObj.transform.Rotate(-1f, 0f, 0f, Space.World);
        if (Input.GetKey(KeyCode.DownArrow))
            sliceObj.transform.Rotate(1f, 0f, 0f, Space.World);
        if (Input.GetKey(KeyCode.LeftArrow))
            sliceObj.transform.Rotate(0f, 1f, 0f, Space.World);
        if (Input.GetKey(KeyCode.RightArrow))
            sliceObj.transform.Rotate(0f, -1f, 0f, Space.World);

        if (Input.GetKeyDown(KeyCode.Space))
            Shlice();
    }

    void Shlice()
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
            RaycastHit _hit;
            List<Vert> myIntersections = new List<Vert>();
            Vector3 dir = oldTri.pos2 - oldTri.pos1;
            Vert lastIntersection = new Vert(), firstIntersection = new Vert();
            bool hitSide1 = false, hitSide2 = false, hitSide3 = false;

            //check for intersections from point1 -> point2
            if (Physics.Raycast(new Ray(oldTri.pos1, dir), out _hit, Vector3.Distance(oldTri.pos1, oldTri.pos2)))
            {
                if (_hit.transform.tag == "Slicer")
                {
                    hitSide1 = true;

                    Vector3 intersection = oldTri.pos1 + _hit.distance * dir.normalized;
                    if (!newVerts.Exists(v => v.pos == intersection))
                        newVerts.Add(new Vert() { index = newVerts.Count, pos = intersection });

                    myIntersections.Add(newVerts.Find(v => v.pos == intersection));
                }
            }

            //check for intersections from point2 -> point3
            dir = oldTri.pos3 - oldTri.pos2;
            if (Physics.Raycast(new Ray(oldTri.pos2, dir), out _hit, Vector3.Distance(oldTri.pos3, oldTri.pos2))) // check for intersections in the first edge
            {
                if (_hit.transform.tag == "Slicer")
                {
                    hitSide2 = true;

                    Vector3 intersection = oldTri.pos2 + _hit.distance * dir.normalized;
                    if (!newVerts.Exists(v => v.pos == intersection))
                        newVerts.Add(new Vert() { index = newVerts.Count, pos = intersection });

                    myIntersections.Add(newVerts.Find(v => v.pos == intersection));
                }
            }

            //check for intersections from point3 -> point1
            dir = oldTri.pos1 - oldTri.pos3;
            if (Physics.Raycast(new Ray(oldTri.pos3, dir), out _hit, Vector3.Distance(oldTri.pos3, oldTri.pos1))) // check for intersections in the first edge
            {
                if (_hit.transform.tag == "Slicer")
                {
                    hitSide3 = true;

                    Vector3 intersection = oldTri.pos3 + _hit.distance * dir.normalized;
                    if (!newVerts.Exists(v => v.pos == intersection))
                        newVerts.Add(new Vert() { index = newVerts.Count, pos = intersection });

                    myIntersections.Add(newVerts.Find(v => v.pos == intersection));
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
                 meshR = new Mesh();

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
            go1.tag = "Mesh";
            go1.AddComponent<Rigidbody>();
            go1.GetComponent<Rigidbody>().isKinematic = true;
            go1.GetComponent<Rigidbody>().useGravity = false;
            go1.transform.parent = meshParent.transform;
            MeshFilter mf1 = go1.AddComponent<MeshFilter>();
            mf1.mesh = meshL;
            MeshRenderer mr1 = go1.AddComponent<MeshRenderer>();
            mr1.material = mat;

            indices.Clear();

            ////RIGHT SIDE
            //foreach (Triangle triangle in triangles_right)
            //    for (int i = 0; i < triangle.verts.Count; i++)
            //        indices.Add(triangle.verts[i].index);
            //meshR.vertices = newVertPositions.ToArray();
            //meshR.triangles = indices.ToArray();
            //MeshUtility.Optimize(meshR);
            //meshR.RecalculateNormals();
            //meshR.RecalculateBounds();
            //GameObject go2 = new GameObject();
            //go2.name = "Slice 2";
            //go2.tag = "Mesh";
            //go2.AddComponent<Rigidbody>();
            //go2.GetComponent<Rigidbody>().isKinematic = true;
            //go2.GetComponent<Rigidbody>().useGravity = false;
            //go2.transform.parent = meshParent.transform;
            //MeshFilter mf2 = go2.AddComponent<MeshFilter>();
            //mf2.mesh = meshR;
            //MeshRenderer mr2 = go2.AddComponent<MeshRenderer>();
            //mr2.material = mat;


            meshes.Clear();
            meshes.Add(meshL);
            mesh = meshL;

            DestroyImmediate(meshObj);
            meshObj = GameObject.FindGameObjectWithTag("Mesh");
        }
    }

    #region Helpers

    private int getSide(Vector3 point) //returns which side of the slicer this point is on
    {
        RaycastHit[] hits;
        hits = Physics.BoxCastAll(point, new Vector3(.1f, .1f, .1f), transform.forward);

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.tag == "Side")
            {
                return Convert.ToInt16(hit.transform.name.Replace("Side", "")) - 1;
            }
        }

        return 0;
    }

    #endregion

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
