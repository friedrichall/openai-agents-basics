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
using System.Collections;
using UnityEngine;

namespace de.ugoe.cs.vivian.core
{

    /**
     * This class represents a movable object. In can be freely moved in space but has an initial and a final position
     */
    public class MovableElement : PositionableElement<MovableSpec>
    {
        /** the interaction triangle representing the interaction start */
        private InteractionTriangle interactionStartTriangle;

        /** the default distance from snap poses where snapping starts (equals size of objects) */
        private float defaultSnapPoseDistance = 0;

        /**
         * Called to initialize the movable element with the specification and the represented game object
         */
        internal override void Initialize(MovableSpec spec, GameObject representedObject)
        {
            base.Initialize(spec, representedObject);

            Bounds bounds = new Bounds();
            MeshFilter[] meshFilters = representedObject.GetComponentsInChildren<MeshFilter>();

            foreach (MeshFilter meshFilter in meshFilters)
            {

                Mesh mesh = meshFilter.sharedMesh;

                if (mesh == null)
                {
                    mesh = meshFilter.mesh;
                }

                if (mesh != null)
                {
                    Vector3 meshOffset = meshFilter.transform.position - base.RepresentedObject.transform.position;
                    Vector3 meshMin = meshFilter.transform.TransformVector(mesh.bounds.min);
                    Vector3 meshMax = meshFilter.transform.TransformVector(mesh.bounds.max);
                    bounds.Encapsulate(meshOffset + meshMin);
                    bounds.Encapsulate(meshOffset + meshMax);
                }
            }

            this.defaultSnapPoseDistance = bounds.extents.magnitude / 2;
        }

        /**
         * called to apply a certain attribute
         */
        internal override void SetAttribute(InteractionElementSpec.Attribute attribute, object value)
        {
            switch (attribute)
            {
                case InteractionElementSpec.Attribute.POSITION:
                    if (!(value is Vector3))
                    {
                        throw new ArgumentException("setting the attribute POSITION to a movable element requires " +
                                                    "a value of type Vector3 but was " + value + " when setting it for the movable element " +
                                                    base.Spec.Name);
                    }

                    base.RepresentedObject.transform.localPosition = (Vector3)value;
                    break;

                case InteractionElementSpec.Attribute.ROTATION:
                    if (!(value is Vector3))
                    {
                        throw new ArgumentException("setting the attribute ROTATION to a movable element requires " +
                                                    "a value of type Vector3 but was " + value + " when setting it for the movable element " +
                                                    base.Spec.Name);
                    }

                    base.RepresentedObject.transform.localRotation = Quaternion.Euler((Vector3)value);
                    break;

                case InteractionElementSpec.Attribute.FIXED:
                    base.SetAttribute(attribute, value); break;


                default:
                    throw new ArgumentException("the movable element " + base.Spec.Name + " cannot handle an attribute of type " +
                                                attribute + "with the value " + value + " of type " + value.GetType() + ". Allowed are: " +
                                                InteractionElementSpec.Attribute.FIXED + " of type bool, " +
                                                InteractionElementSpec.Attribute.POSITION + " of type vector3");
            }
        }

        /**
         * This is called when the object starts to move
         */
        public override void TriggerInteractionStarts(Pose pose)
        {
            Debug.Log(base.Spec.Name + ": OBJECT_MOVE_START " + base.RepresentedObject.transform.position);

            // ensure that current snappings become interrupted
            this.StopAllCoroutines();

            // store some information about when and how the interaction started
            this.interactionStartTriangle = new InteractionTriangle(pose, base.RepresentedObject.transform);

            base.RaiseInteractionElementEvent(EventSpec.OBJECT_MOVE_START);
        }

        /**
         * This is called between the beginning of the move and the end
         */
        public override void TriggerInteractionContinues(Pose pose)
        {
            if (this.UpdateObject(pose))
            {
                base.RaiseInteractionElementEvent(EventSpec.OBJECT_MOVE);
                //Debug.Log(base.Spec.Name + ": OBJECT_MOVE " + base.RepresentedObject.transform.position);
            }
        }

        /**
         * This is called when the object shall finish moving
         */
        public override void TriggerInteractionEnds(Pose pose)
        {
            this.UpdateObject(pose);

            base.RaiseInteractionElementEvent(EventSpec.OBJECT_MOVE_END);

            Debug.Log(base.Spec.Name + ": OBJECT_MOVE_END " + base.RepresentedObject.transform.position);
            this.StartCoroutine(SnapToPose(this.GetClosestSnapPose()));
        }

        /**
         * This is called during the interaction to update the objects position and rotation
         */
        private bool UpdateObject(Pose pose)
        {
            if (!this.IsFixed)
            {
                var result = this.interactionStartTriangle.AdaptObjectTransformToPose(pose, base.RepresentedObject);

                if (result)
                {
                    this.AdaptRotationToSnapPose();
                }

                return result;
            }

            return false;
        }

        /**
         * This is called during the interaction to update the objects position and rotation
         */
        private void AdaptRotationToSnapPose()
        {
            SnapPoseSpec snapPose = this.GetClosestSnapPose();

            if (snapPose.Rotation.x != Vector3.negativeInfinity.x)
            {
                Vector3 distanceVector = base.RepresentedObject.transform.parent.TransformVector
                    (snapPose.Position - base.RepresentedObject.transform.localPosition);

                float distance = distanceVector.magnitude;
                float closeness = distance / defaultSnapPoseDistance;

                //Debug.Log(distance + "  " + defaultSnapPoseDistance + "  " + closeness);

                if (closeness < 1.0)
                {
                    base.RepresentedObject.transform.localRotation = Quaternion.Lerp
                        (base.RepresentedObject.transform.localRotation, Quaternion.Euler(snapPose.Rotation), 1 - closeness);
                }
            }
        }

        /**
         * This is called during the interaction to update the objects position and rotation
         */
        private SnapPoseSpec GetClosestSnapPose()
        {
            SnapPoseSpec result = new SnapPoseSpec(base.RepresentedObject.transform.localPosition,
                                                   base.RepresentedObject.transform.localRotation.eulerAngles);

            if (base.Spec.SnapPoses != null)
            {
                float minDist = float.MaxValue;

                foreach (SnapPoseSpec candidate in base.Spec.SnapPoses)
                {
                    float dist = (candidate.Position - base.RepresentedObject.transform.localPosition).magnitude;

                    if (minDist > dist)
                    {
                        result = candidate;
                        minDist = dist;
                    }
                }
            }

            return result;
        }

        /**
         * 
         */
        IEnumerator SnapToPose(SnapPoseSpec snapPose)
        {
            Vector3 startPosition = base.RepresentedObject.transform.localPosition;
            Vector3 endPosition = snapPose.Position;

            Quaternion startRotation = base.RepresentedObject.transform.localRotation;
            Quaternion endRotation = startRotation;

            if (snapPose.Rotation.x != Vector3.negativeInfinity.x)
            {
                endRotation = Quaternion.Euler(snapPose.Rotation);
            }

            float duration = ((float)base.Spec.TransitionTimeInMs) / 1000; // seconds

            float distance = base.RepresentedObject.transform.parent.TransformVector(endPosition - startPosition).magnitude;
            float closeness = distance / defaultSnapPoseDistance;

            if (closeness < 1.0)
            {
                while (closeness > 0)
                {
                    yield return null;

                    closeness = Math.Max(0, closeness - Time.deltaTime / duration);

                    base.RepresentedObject.transform.localPosition =
                        Vector3.Lerp(startPosition, endPosition, 1 - closeness);

                    base.RepresentedObject.transform.localRotation =
                        Quaternion.Lerp(startRotation, endRotation, 1 - closeness);
                    base.RaiseInteractionElementEvent(EventSpec.SNAPPOSES_CHECK);
                }

                Debug.Log(base.Spec.Name + ": Snapped to pose " + snapPose.Position);
            }
        }
            
        /**
         * convenience class to maintain the interaction triangle
         */
        private class InteractionTriangle
        {
            /** */
            private Quaternion initialInteractionRotation;

            /** */
            private Vector3 initialPoseRelativeObjectPosition;

            /** */
            private Quaternion initialObjectRotation;

            /**
             * 
             */
            internal InteractionTriangle(Pose interactionPose, Transform objectTransform)
            {
                this.initialInteractionRotation = Quaternion.LookRotation(interactionPose.forward, interactionPose.up);
                this.initialPoseRelativeObjectPosition = objectTransform.position - interactionPose.position;
                this.initialObjectRotation = objectTransform.rotation;
            }

            /**
             * 
             */
            internal bool AdaptObjectTransformToPose(Pose newInteractionPose, GameObject gameObject)
            {
                //Debug.DrawLine(newInteractionPose.position, newInteractionPose.position + newInteractionPose.forward, Color.cyan);
                //Debug.DrawLine(newInteractionPose.position, newInteractionPose.position + newInteractionPose.up, Color.blue);

                // get the rotation between the new pose and the initial pose
                Quaternion rotation = Quaternion.LookRotation(newInteractionPose.forward, newInteractionPose.up) *
                    Quaternion.Inverse(initialInteractionRotation);

                // determine, based on the rotation, where the object must now be located
                Vector3 newRelativeObjectPosition = rotation * initialPoseRelativeObjectPosition;

                //Debug.DrawLine(newInteractionPose.position, newInteractionPose.position + initialPoseRelativeObjectPosition, Color.magenta);
                //Debug.DrawLine(newInteractionPose.position, newInteractionPose.position + newRelativeObjectPosition, Color.red);
                Vector3 newPosition = newInteractionPose.position + newRelativeObjectPosition;
                // set the rotation of the object to its initial rotation plus the rotation change
                Quaternion newRotation = rotation * this.initialObjectRotation;

                if ((gameObject.transform.position != newPosition) || (gameObject.transform.rotation != newRotation))
                {
                    Rigidbody rb = gameObject.GetComponent<Rigidbody>();

                    if (rb == null)
                    {
                        // move and rotate object using hard coordinates
                        gameObject.transform.position = newPosition;
                        gameObject.transform.rotation = newRotation;
                    }
                    else
                    {
                        // move and rotate object using physics and the rigid body
                        rb.MovePosition(newPosition);
                        rb.MoveRotation(newRotation);
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
