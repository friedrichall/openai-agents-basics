using de.ugoe.cs.vivian.core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public GameObject representedObject;
    // Start is called before the first frame update
    void Start()
    {
        /*TKTapRecognizer recognizer = new TKTapRecognizer();

        recognizer.gestureRecognizedEvent += (r) =>
        {
            Debug.Log("hello");
        };

        TouchKit.addGestureRecognizer(recognizer);*/
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Jippieh " + collision.collider.name);
    }

    void OnTriggerEnter(Collider collider)
    {
        Debug.Log("Jippieh " + collider.name);
    }

    // Update is called once per frame
    void Update()
    {
        // determine the object local coordinate system defined by the plane normal
        /*Vector3 meshLocalPlaneNormal = new Vector3(0, 0, 1);

        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            meshLocalPlaneNormal = -ray.direction;
            Debug.DrawLine(ray.origin, ray.direction);
        }

        getMeshLocalCoordinateSystem(representedObject, meshLocalPlaneNormal,
                                     out Vector3 meshLocalXAxisDirection,
                                     out Vector3 meshLocalYAxisDirection);

        // get the bounds of the object considered we were looking at its touchable plane
        Vector3[] points = Utils.GetLocalPointsRepresentingMesh(representedObject.GetComponent<MeshFilter>());
        DrawMesh(points, new Color(0, 1, 1, 0.5f));

        Quaternion rotation = Quaternion.LookRotation(meshLocalPlaneNormal, -meshLocalYAxisDirection);
        Matrix4x4 matrix = Matrix4x4.Rotate(Quaternion.Inverse(rotation));

        Bounds boundsRelativeToLocalCoordinateSystem = GeometryUtility.CalculateBounds(points, matrix);

        DrawBounds(boundsRelativeToLocalCoordinateSystem, new Color(0, 1, 1, 1f));

        // now use these bounds to determine the effective length of the x and y axis as well as the
        // plane offset (x and y axis must be inverted to point right and downward);
        Vector3 boundsLocalOrigin = boundsRelativeToLocalCoordinateSystem.center + boundsRelativeToLocalCoordinateSystem.extents;
        Vector3 boundsLocalXAxis = -Vector3.Project(boundsRelativeToLocalCoordinateSystem.size, Vector3.right);
        Vector3 boundsLocalYAxis = -Vector3.Project(boundsRelativeToLocalCoordinateSystem.size, Vector3.up);

        /*Debug.DrawLine(boundsLocalOrigin, boundsLocalOrigin + Vector3.forward, Color.red);
        Debug.DrawLine(boundsLocalOrigin, boundsLocalOrigin + boundsLocalXAxis, Color.green);
        Debug.DrawLine(boundsLocalOrigin, boundsLocalOrigin + boundsLocalYAxis, Color.blue);*/

        // now we need to rotate everything back to the local coordinate system of the object
        /*Vector3 meshLocalOrigin = rotation * boundsLocalOrigin;
        Vector3 meshLocalXAxis = rotation * boundsLocalXAxis;
        Vector3 meshLocalYAxis = rotation * boundsLocalYAxis;

        Debug.DrawLine(meshLocalOrigin, meshLocalOrigin + meshLocalPlaneNormal, Color.red);
        Debug.DrawLine(meshLocalOrigin, meshLocalOrigin + meshLocalXAxis, Color.green);
        Debug.DrawLine(meshLocalOrigin, meshLocalOrigin + meshLocalYAxis, Color.blue);

        // and finally we need to transform this to world space
        Vector3 worldOrigin = representedObject.transform.TransformVector(meshLocalOrigin);
        Vector3 worldXAxis = representedObject.transform.TransformVector(meshLocalXAxis);
        Vector3 worldYAxis = representedObject.transform.TransformVector(meshLocalYAxis);

        Debug.DrawLine(representedObject.transform.position, representedObject.transform.position + worldOrigin, Color.red);
        Debug.DrawLine(representedObject.transform.position + worldOrigin, representedObject.transform.position + worldOrigin + worldXAxis, Color.green);
        Debug.DrawLine(representedObject.transform.position + worldOrigin, representedObject.transform.position + worldOrigin + worldYAxis, Color.blue);


        /*MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.mesh != null)
            {
                Vector3[] boundPoints = {
                    meshFilter.mesh.bounds.min,
                    meshFilter.mesh.bounds.max,
                    new Vector3(meshFilter.mesh.bounds.min.x, meshFilter.mesh.bounds.min.y, meshFilter.mesh.bounds.max.z),
                    new Vector3(meshFilter.mesh.bounds.min.x, meshFilter.mesh.bounds.max.y, meshFilter.mesh.bounds.min.z),
                    new Vector3(meshFilter.mesh.bounds.max.x, meshFilter.mesh.bounds.min.y, meshFilter.mesh.bounds.min.z),
                    new Vector3(meshFilter.mesh.bounds.min.x, meshFilter.mesh.bounds.max.y, meshFilter.mesh.bounds.max.z),
                    new Vector3(meshFilter.mesh.bounds.max.x, meshFilter.mesh.bounds.min.y, meshFilter.mesh.bounds.max.z),
                    new Vector3(meshFilter.mesh.bounds.max.x, meshFilter.mesh.bounds.max.y, meshFilter.mesh.bounds.min.z) };

                for (int i = 0; i < boundPoints.Length; i++)
                {
                    Debug.DrawLine(Vector3.zero, boundPoints[i], Color.green);
                    boundPoints[i] = meshFilter.transform.TransformPoint(boundPoints[i]);
                    Debug.DrawLine(Vector3.zero, boundPoints[i], Color.red);
                }

                for (int i = 0; i < boundPoints.Length; i++)
                {
                    boundPoints[i] = transform.InverseTransformPoint(boundPoints[i]);
                    Debug.DrawLine(transform.position, transform.position + transform.rotation * boundPoints[i], Color.blue);
                }

                for (int i = 1; i < boundPoints.Length; i++)
                {
                    for (int j = 0; j < boundPoints.Length; j++)
                    {
                        Debug.DrawLine(boundPoints[j], boundPoints[i], Color.cyan);
                    }
                }

                Bounds bounds = GeometryUtility.CalculateBounds(boundPoints, Matrix4x4.identity);

                Debug.DrawLine(Vector3.zero, bounds.center, Color.yellow);

                BoxCollider collider = this.gameObject.AddComponent<BoxCollider>();
                collider.size = bounds.size;
                collider.center = bounds.center;


                //Debug.DrawLine(Vector3.zero, bounds.size);
                /*Gizmos.color = new Color(1, 1, 1, 0.25f);
                Gizmos.DrawCube(transform.position, bounds.size);
                Gizmos.DrawWireCube(transform.position, bounds.size);*/
        /* }

         break;
     }*/
    }

    /**
     * 
     */
    private void getMeshLocalCoordinateSystem(GameObject representedObject,
                                              Vector3 meshLocalPlaneNormal,
                                              out Vector3 meshLocalXAxisDirection,
                                              out Vector3 meshLocalYAxisDirection)
    {
        float angleToUpward = Vector3.Angle(meshLocalPlaneNormal, Vector3.up);

        if (angleToUpward == 0)
        {
            // the plane normal points upward, i.e. 
            meshLocalXAxisDirection = new Vector3(1, 0, 0);
            meshLocalYAxisDirection = new Vector3(0, 0, -1);
        }
        else if (angleToUpward == 180)
        {
            // the plane normal points downward, i.e. 
            meshLocalXAxisDirection = new Vector3(1, 0, 0);
            meshLocalYAxisDirection = new Vector3(0, 0, 1);
        }
        else
        {
            meshLocalXAxisDirection = Vector3.Cross(meshLocalPlaneNormal, Vector3.up);
            meshLocalYAxisDirection = Vector3.Cross(meshLocalPlaneNormal, meshLocalXAxisDirection);
        }

        Debug.DrawRay(Vector3.zero, meshLocalPlaneNormal, new Color(1, 0, 0, 0.5f));
        Debug.DrawRay(Vector3.zero, meshLocalXAxisDirection, new Color(0, 1, 0, 0.5f));
        Debug.DrawRay(Vector3.zero, meshLocalYAxisDirection, new Color(0, 0, 1, 0.5f));
    }

    /**
     * 
     */
    private void DrawBounds(Bounds bounds, Color color)
    {
        Vector3[] points = new Vector3[]
        {
            bounds.center + bounds.extents,
            bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z),
            bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z),
            bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z),
            bounds.center - bounds.extents,
            bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z),
            bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z),
            bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z)
        };

        DrawMesh(points, color);
    }

    /**
     * 
     */
    private void DrawMesh(Vector3[] points, Color color)
    {
        foreach (Vector3 point1 in points)
        {
            foreach (Vector3 point2 in points)
            {
                if ((point1.x == point2.x) || (point1.y == point2.y) || (point1.z == point2.z))
                {
                    Debug.DrawLine(point1, point2, color);
                }
            }
        }
    }
}
