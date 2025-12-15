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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using de.ugoe.cs.vivian.core;

namespace de.ugoe.cs.vivian.mouseinteraction
{
    public class MouseInteractionHandler : MonoBehaviour
    {
        // the prototype in the scene while interacting
        private VirtualPrototype[] Prototypes = null;

        // the default pose id to use
        private const int POSE_ID = 0;

        // stores whether we are currently waiting for a mouse button up
        private bool InInteraction = false;

        // Update is called on every frame
        void Update()
        {
            if (this.InInteraction)
            {
                // we may get the up of the current interaction and a down for the next interaction in one frame
                // --> handle first the up and then the down
                this.HandleMouseButtonUp();
                this.HandleMouseButtonDown();
                this.HandleMouseButton();
            }
            else
            {
                // we may get the down and the up of the same interaction in the same frame
                // --> handle first the down and then the up
                this.HandleMouseButtonDown();
                this.HandleMouseButton();
                this.HandleMouseButtonUp();
            }

        }

        /**
         * convenience method to handle mouse button ups
         */
        private void HandleMouseButtonDown()
        {
            if (Input.GetMouseButtonDown(0))
            {
                this.Prototypes = FindObjectsOfType<VirtualPrototype>();

                if (this.Prototypes != null)
                {
                    Pose pose = this.GetPose();

                    foreach (VirtualPrototype prototype in this.Prototypes)
                    {
                        prototype.TriggerInteractionStarts(pose, POSE_ID);
                    }

                    this.InInteraction = true;
                }
            }
        }

        /**
         * convenience method to handle mouse button being down
         */
        private void HandleMouseButton()
        {
            if (Input.GetMouseButton(0))
            {
                Pose pose = this.GetPose();

                foreach (VirtualPrototype prototype in this.Prototypes)
                {
                    prototype.TriggerInteractionContinues(pose, POSE_ID);
                }
            }
        }

        /**
         * convenience method to handle mouse button ups
         */
        private void HandleMouseButtonUp()
        {
            if (Input.GetMouseButtonUp(0))
            {
                Pose pose = this.GetPose();

                foreach (VirtualPrototype prototype in this.Prototypes)
                {
                    prototype.TriggerInteractionEnds(pose, POSE_ID);
                }

                this.InInteraction = false;
            }
        }

        /**
         * convenience method to determine the pose of interaction
         */
        private Pose GetPose()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            return new Pose(ray.origin, Quaternion.LookRotation(ray.direction, Camera.main.transform.up));
        }
    }
}
