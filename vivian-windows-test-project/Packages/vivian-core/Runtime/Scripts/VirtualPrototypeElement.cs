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
using System.Collections.Generic;
using UnityEngine;

namespace de.ugoe.cs.vivian.core
{

    /**
     * This class represents any element of a virtual prototype
     */
    public class VirtualPrototypeElement : MonoBehaviour
    {
        /** the represented game object */
        public GameObject RepresentedObject { get; private set; }

        /** the events that can be fired by elements without a function */
        public event EventHandler<VPElementEvent> VPElementEvent;

        /** the last pose of interaction */
        private Pose LastInteractionPose;

        /**
         * Called to initialize the element with the represented game object
         */
        internal void Initialize(GameObject representedObject)
        {
            this.RepresentedObject = representedObject;
            this.CreateColliders();
        }

        /**
         * creates the colliders for the represented object to be able to react on interactions
         */
        internal virtual void CreateColliders()
        {
            int addedColliders = 0;
            Collider[] originalColliders = this.RepresentedObject.GetComponents<Collider>();

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
                // determine a new collider matching the mesh size
                Utils.GetColliderFromMesh(this.gameObject, this.RepresentedObject.GetComponent<MeshFilter>());
            }
        }

        /**
         * This is called when the user starts interacting with this element
         */
        public virtual void TriggerInteractionStarts(Pose pose)
        {
            this.LastInteractionPose = pose;
            this.RaiseVPElementEvent(new VPElementEvent(this, "InteractionStarts"));
        }

        /**
         * This is called when the user continues interacting with this element
         */
        public virtual void TriggerInteractionContinues(Pose pose)
        {
            if (this.LastInteractionPose != pose)
            {
                this.LastInteractionPose = pose;
                this.RaiseVPElementEvent(new VPElementEvent(this, "InteractionContinues"));
            }
        }

        /**
         * This is called when the user ends interacting with this element
         */
        public virtual void TriggerInteractionEnds(Pose pose)
        {
            this.LastInteractionPose = pose;
            this.RaiseVPElementEvent(new VPElementEvent(this, "InteractionEnds"));
        }

        /**
         * This is called when the interaction element is destroyed and required to clean up any changes
         * on the original element. Must be implemented by any child class in case it changes something on
         * the virtual prototype.
         */
        public virtual void OnDestroy()
        {
            // nothing to do
        }

        /**
         * 
         */
        protected virtual void RaiseVPElementEvent(VPElementEvent theEvent)
        {
            VPElementEvent?.Invoke(this, theEvent);
        }
    }

    /**
     * 
     */
    public class VPElementEvent : EventArgs
    {
        public VirtualPrototypeElement Element;

        public string EventType;

        public KeyValuePair<EventParameterSpec, float>[] ParameterValues { get; }

        public VPElementEvent(VirtualPrototypeElement element,
                              string eventType,
                              params KeyValuePair<EventParameterSpec, float>[] parameterValues)
        {
            this.Element = element;
            this.EventType = eventType;
            this.ParameterValues = parameterValues;
        }
    }

}
