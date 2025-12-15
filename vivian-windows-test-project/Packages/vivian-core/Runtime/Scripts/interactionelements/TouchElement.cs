// Copyright 2019 Patrick Harms
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace de.ugoe.cs.vivian.core
{
    /**
     * This class represents a touch area.
     */
    public class TouchElement : InteractionElement<TouchAreaSpec>
    {
        /** the plane of the touch area */
        private Plane Plane;

        /** the top left point of the touch area */
        private Vector3 TopLeftPoint;

        /** the x-Axis of the touch area */
        private Vector3 XAxis;

        /** the y-Axis of the touch area */
        private Vector3 YAxis;

        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal override void Initialize(TouchAreaSpec spec, GameObject representedObject)
        {
            base.Initialize(spec, representedObject);

            this.InitializePlane();
        }

        /**
         * This is called when the touch area is touched
         */
        public override void TriggerInteractionStarts(Pose pose)
        {
            Vector2 coordinates = GetTouchCoordinates(pose);

            Debug.Log(base.Spec.Name + ": TOUCH_START " + coordinates);

            if (coordinates.x == Mathf.NegativeInfinity)
            {
                // this happens in case the object got moved. We reinitialize the interaction plane and retry.
                this.InitializePlane();
                coordinates = GetTouchCoordinates(pose);
            }

            base.RaiseInteractionElementEvent(EventSpec.TOUCH_START,
                                              new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.TOUCH_X_COORDINATE, coordinates.x),
                                              new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.TOUCH_Y_COORDINATE, coordinates.y));
        }

        /**
         * handles the slide on the touch area. The ray defined by the pose is considered to show the user intended position of the slide.
         */
        public override void TriggerInteractionContinues(Pose pose)
        {
            //Debug.Log("trigger touch slide");

            this.GetEffectiveTouchPlane(out Vector3 topLeftPoint, out Vector3 xAxis, out Vector3 yAxis);

            Vector2 coordinates = GetTouchCoordinates(pose);

            if (coordinates.x == Mathf.NegativeInfinity)
            {
                // this my happen in case the object got moved. We reinitialize the interaction plane and retry.
                this.InitializePlane();
                coordinates = GetTouchCoordinates(pose);
            }

            base.RaiseInteractionElementEvent(EventSpec.TOUCH_SLIDE,
                                              new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.TOUCH_X_COORDINATE, coordinates.x),
                                              new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.TOUCH_Y_COORDINATE, coordinates.y));

            //Debug.Log(base.Spec.Name + ": TOUCH_SLIDE " + coordinates);
        }

        /**
         * This is called when the touch area is not touched anymore
         */
        public override void TriggerInteractionEnds(Pose pose)
        {
            Vector2 coordinates = GetTouchCoordinates(pose);

            base.RaiseInteractionElementEvent(EventSpec.TOUCH_END,
                                              new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.TOUCH_X_COORDINATE, coordinates.x),
                                              new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.TOUCH_Y_COORDINATE, coordinates.y));

            Debug.Log(base.Spec.Name + ": TOUCH_END " + coordinates);
        }

        public Vector3 GetPlaneSurfaceInWorldSpace()
        {
            return base.RepresentedObject.transform.TransformDirection(base.Spec.Plane);
        }

        /**
         * convenience method to get the location of the hit of the ray defined by the pose
         */
        private Vector2 GetTouchCoordinates(Pose pose)
        {
            Vector3 planeHit = GetPlaneHit(pose);

            if (planeHit.x == Mathf.NegativeInfinity)
            {
                Debug.Log("not hitting object");
                return Vector2.negativeInfinity;
            }

            Vector3 touchAreaHit = planeHit - this.TopLeftPoint;

            float xAxisRatio = Vector3.Project(touchAreaHit, this.XAxis).magnitude / this.XAxis.magnitude;
            float yAxisRatio = Vector3.Project(touchAreaHit, this.YAxis).magnitude / this.YAxis.magnitude;

            if (Vector3.Dot(base.RepresentedObject.transform.TransformVector(base.Spec.Plane), pose.forward) > 0)
            {
                // we are pointing at the plane from behind --> inverse the axis ratios
                xAxisRatio = 1.0f - xAxisRatio;
                yAxisRatio = 1.0f - yAxisRatio;
            }

            return new Vector2(xAxisRatio * this.Spec.Resolution.x, yAxisRatio * this.Spec.Resolution.y);
        }

        /**
         * convenience method to get the location of the hit of the ray defined by the pose
         */
        private Vector3 GetPlaneHit(Pose pose)
        {
            Collider collider = this.GetComponent<Collider>();
            Ray destination = new Ray(pose.position, pose.forward);

            if (collider.Raycast(destination, out RaycastHit hitInfo, 100))
            {
                // the collider is hit by the ray, let us project this point onto the plane
                Vector3 planeHit = this.Plane.ClosestPointOnPlane(hitInfo.point);

                // if this plane hit is not too far from the collider hit (not more than 1mm in world space),
                // we consider the hit on the right side of the collider, i.e. in the plane
                if ((planeHit - hitInfo.point).magnitude < 0.01)
                {
                    //Debug.DrawLine(Vector3.zero, planeHit, Color.green);
                    //Debug.DrawLine(this.RepresentedObject.transform.position, hitInfo.point, Color.blue);

                    return planeHit;
                }
            }

            return Vector3.negativeInfinity;
        }

        /**
         * 
         */
        private void InitializePlane()
        {
            this.GetEffectiveTouchPlane(out Vector3 topLeftPoint, out Vector3 xAxis, out Vector3 yAxis);

            this.TopLeftPoint = this.RepresentedObject.transform.position + topLeftPoint;
            this.XAxis = xAxis;
            this.YAxis = yAxis;

            this.Plane = new Plane(Vector3.Cross(xAxis, yAxis),
                                   this.TopLeftPoint + this.XAxis / 2 + this.YAxis / 2);
        }

        /**
         * 
         */
        private void GetEffectiveTouchPlane(out Vector3 topLeftPoint, out Vector3 xAxis, out Vector3 yAxis)
        {
            // determine the object local coordinate system defined by the plane normal
            Vector3 meshLocalPlaneNormal = base.Spec.Plane;

            MeshFilter meshFilter = base.RepresentedObject.GetComponent<MeshFilter>();

            if (meshFilter == null)
            {
                throw new ArgumentException("The touch element " + base.Spec.Name + " does not have a mesh. " +
                                            "It requires a mesh, because otherwise there is nothing to touch on.");
            }

            Surface surface = Utils.GetSurfaceFromMesh(meshFilter, base.Spec.Plane, base.RepresentedObject.transform.InverseTransformVector(Vector3.up));

            // and finally we need to transform this to world space
            topLeftPoint = base.RepresentedObject.transform.TransformVector(surface.topLeft);
            xAxis = base.RepresentedObject.transform.TransformVector(surface.xAxis);
            yAxis = base.RepresentedObject.transform.TransformVector(surface.yAxis);

            //Debug.DrawLine(base.RepresentedObject.transform.position,
            //               base.RepresentedObject.transform.position + topLeftPoint, Color.red);
            //Debug.DrawLine(base.RepresentedObject.transform.position + topLeftPoint,
            //               base.RepresentedObject.transform.position + topLeftPoint + xAxis, Color.green);
            //Debug.DrawLine(base.RepresentedObject.transform.position + topLeftPoint,
            //               base.RepresentedObject.transform.position + topLeftPoint + yAxis, Color.blue);
        }
    }
}
