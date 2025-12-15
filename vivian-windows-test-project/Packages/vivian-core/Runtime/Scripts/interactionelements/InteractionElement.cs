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
     * This class represents a generic interaction element having a specification and a reference to the effective
     * game object it represents
     */
    public abstract class InteractionElement : VirtualPrototypeElement
    {
        /** the specification of the interaction element */
        internal InteractionElementSpec Spec { get; set; }

        /** the current value of this interaction element */
        internal virtual object Value { get; set; }

        /**
         * Called to initialize the interaction element with the specification and the represented game object
         */
        internal void Initialize(InteractionElementSpec spec, GameObject representedObject)
        {
            base.Initialize(representedObject);

            this.Spec = spec;

            this.SetInitialAttributes();
        }


        /**
         * creates the colliders for the represented object to be able to react on interactions
         */
        internal override void CreateColliders()
        {
            int addedColliders = 0;
            Collider[] originalColliders = base.RepresentedObject.GetComponents<Collider>();

            if (originalColliders != null && originalColliders.Length > 0)
            {
                // take over the colliders of the represented object
                foreach (Collider originalCollider in originalColliders)
                {
                    // we need to skip mesh colliders. Rather often, they do not work as expected.
                    if (!(originalCollider is MeshCollider))
                    {
                        Collider collider = (Collider)this.gameObject.AddComponent(originalCollider.GetType());
                        Utils.CopyComponentValues(collider, originalCollider);
                        collider.isTrigger = true;
                        addedColliders++;
                    }
                }
            }
        
            if (addedColliders > 0)
            {
                //increase the original collider size minimally to be the first one triggered
                this.transform.localScale = 1.0005f * this.transform.localScale;
            }
            else
            {
                // create new colliders, one for each mesh filter belonging to this and child objects
                MeshFilter[] meshFilters = base.RepresentedObject.GetComponentsInChildren<MeshFilter>();

                foreach (MeshFilter meshFilter in meshFilters)
                {
                    // check whether the mesh is a child and if this child already is an InteractionElement
                    // if so, skip the creation of a corresponding collider
                    if (meshFilter.gameObject != base.RepresentedObject)
                    {
                        // if a child object is already an interaction element, ignore it and to not add colliders
                        InteractionElement[] interactionElements = meshFilter.GetComponentsInChildren<InteractionElement>();
                        if ((interactionElements != null) && (interactionElements.Length > 0))
                        {
                            continue;
                        }
                    }

                    Utils.GetColliderFromMesh(this.gameObject, meshFilter);
                }
            }
        }

        /**
         * This is called when the interaction with this element starts
         */
        public override abstract void TriggerInteractionStarts(Pose pose);

        /**
         * This is called when the interaction with this element continues
         */
        public override abstract void TriggerInteractionContinues(Pose pose);

        /**
         * This is called when the interaction with this element ends
         */
        public override abstract void TriggerInteractionEnds(Pose pose);

        /**
         * This is called when the interaction element is destroyed and required to clean up any changes
         * on the original element.
         */
        public override void OnDestroy()
        {
            Destroy(this.gameObject.GetComponent<Collider>());
        }

        /**
         * 
         */
        protected virtual void RaiseInteractionElementEvent(EventSpec eventSpec,
                                                            params KeyValuePair<EventParameterSpec, float>[] parameterValues)
        {
            base.RaiseVPElementEvent(new InteractionElementEvent(this, eventSpec, parameterValues));
        }

        /**
         * called to apply a certain attribute
         */
        internal virtual void SetAttribute(InteractionElementSpec.Attribute attribute, object value)
        {
            switch (attribute)
            {
                case InteractionElementSpec.Attribute.VALUE: this.Value = value; break;
            }
        }

        /**
         * called to set the default attribute values
         */
        internal void SetInitialAttributes()
        {
            if (this.Spec.InitialAttributeValues != null)
            {
                foreach (InteractionElementSpec.AttributeValue defaultValue in this.Spec.InitialAttributeValues)
                {
                    this.SetAttribute(defaultValue.Attribute, defaultValue.Value);
                }
            }
        }
    }

    /**
     * 
     */
    public abstract class InteractionElement<T> : InteractionElement where T : InteractionElementSpec
    {
        /** the specification of the interaction element */
        internal new T Spec
        {
            get { return (T)base.Spec; }
            set { base.Spec = value; }
        }

        /**
         * Called to initialize the interaction element with the specification and the represented game object
         */
        internal virtual void Initialize(T spec, GameObject representedObject)
        {
            base.Initialize(spec, representedObject);
        }

        /*void OnDrawGizmosSelected()
        {
            MeshFilter[] meshFilters = base.RepresentedObject.GetComponentsInChildren<MeshFilter>();

            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter.mesh != null)
                {
                    Debug.Log(meshFilter.name + "  " + meshFilter.mesh.bounds.min + "  " + meshFilter.mesh.bounds.max);
                    Debug.Log(meshFilter.name + "  " + meshFilter.transform.TransformPoint(meshFilter.mesh.bounds.min) + "  " +
                              meshFilter.transform.TransformPoint(meshFilter.mesh.bounds.max));
                    Debug.Log(meshFilter.name + "  " + transform.TransformPoint(meshFilter.mesh.bounds.min) + "  " +
                              transform.TransformPoint(meshFilter.mesh.bounds.max));
                    Bounds bounds =
                        GeometryUtility.CalculateBounds(new Vector3[] { meshFilter.transform.TransformPoint(meshFilter.mesh.bounds.min),
                                                                        meshFilter.transform.TransformPoint(meshFilter.mesh.bounds.max) },
                                                        transform.localToWorldMatrix);

                    Gizmos.color = new Color(1, 1, 1, 0.25f);
                    Gizmos.DrawCube(transform.position, bounds.size);
                    Gizmos.DrawWireCube(transform.position, bounds.size);
                }

                break;
            }

            Renderer[] renderers = base.RepresentedObject.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                if (renderer.bounds != null)
                {
                    Bounds bounds =
                        GeometryUtility.CalculateBounds(new Vector3[] { renderer.bounds.min, renderer.bounds.max },
                                                        transform.localToWorldMatrix);

                    Gizmos.color = new Color(20, 20, 20, 0.25f);
                    Gizmos.DrawCube(transform.position, bounds.size);
                    Gizmos.DrawWireCube(transform.position, bounds.size);
                }
            }
        }*/
    }

    /**
     * 
     */
    public abstract class PositionableElement : InteractionElement
    {
        /** the specification of the positionable element */
        internal new PositionableElementSpec Spec
        {
            get { return (PositionableElementSpec)base.Spec; }
            set { base.Spec = value; }
        }

        /** stores if the object is currently fixed or not */
        public bool IsFixed { get; internal set; } = false;

        /** stores the initial position of the represented object to be able to reset it on destroy */
        private Pose InitialPose;

        /**
         * Called to initialize the positionable element with the specification and the represented game object
         */
        internal void Initialize(PositionableElementSpec spec, GameObject representedObject)
        {
            this.InitialPose = new Pose(representedObject.transform.localPosition, representedObject.transform.localRotation);
            base.Initialize(spec, representedObject);
        }

        /**
         * called to apply a certain attribute
         */
        internal override void SetAttribute(InteractionElementSpec.Attribute attribute, object value)
        {
            switch (attribute)
            {
                case InteractionElementSpec.Attribute.FIXED:
                    if (!(value is bool))
                    {
                        throw new ArgumentException("setting the attribute FIXED to a positionable element requires " +
                                                    "a value of type bool but was " + value + " when setting it for the interaction element " +
                                                    base.Spec.Name);
                    }

                    this.IsFixed = (bool) value; break;

                default: base.SetAttribute(attribute, value); break;
            }
        }

        /**
         * This is called when the interaction element is destroyed and required to clean up any changes
         * on the original element.
         */
        public override void OnDestroy()
        {
            // reset the represented object to its initial pose
            base.RepresentedObject.transform.localPosition = this.InitialPose.position;
            base.RepresentedObject.transform.localRotation = this.InitialPose.rotation;
        }
    }

    /**
     * 
     */
    public abstract class PositionableElement<T> : PositionableElement where T : PositionableElementSpec
    {
        /** the specification of the positionable element */
        internal new T Spec
        {
            get { return (T)base.Spec; }
            set { base.Spec = value; }
        }

        /**
         * Called to initialize the positionable element with the specification and the represented game object
         */
        internal virtual void Initialize(T spec, GameObject representedObject)
        {
            base.Initialize(spec, representedObject);
        }
    }

    /**
     * 
     */
    public class InteractionElementEvent : VPElementEvent
    {
        public EventSpec EventSpec { get; }

        public InteractionElementSpec InteractionElementSpec { get; }

        public InteractionElementEvent(InteractionElement interactionElement,
                                       EventSpec eventSpec,
                                       params KeyValuePair<EventParameterSpec, float>[] parameterValues)
            : base(interactionElement, eventSpec.ToString(), parameterValues)
        {
            this.EventSpec = eventSpec;
            this.InteractionElementSpec = interactionElement.Spec;
        }
    }
}
