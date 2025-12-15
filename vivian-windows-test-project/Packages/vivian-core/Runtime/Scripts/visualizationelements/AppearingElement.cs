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
using UnityEngine;

namespace de.ugoe.cs.vivian.core
{
    /**
     * This class represents an AppearingObject element to be appear or disappear.
     */
    public class AppearingElement : VisualizationElement<AppearingObjectSpec, bool>
    {
        // the represented game object
        private GameObject representedGameObject;

        // stores all mesh renderers that we may have to set active or not
        private MeshRenderer[] meshRenderers;

        // stores whether the mesh renderers were initially enabled or not
        private bool[] initiallyEnabled;

        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal new void Initialize(AppearingObjectSpec spec, GameObject representedObject)
        {
            meshRenderers = representedObject.GetComponentsInChildren<MeshRenderer>();
            initiallyEnabled = new bool[meshRenderers.Length];

            for (int i = 0; i < meshRenderers.Length; i++)
            {
                initiallyEnabled[i] = meshRenderers[i].enabled;
            }

            base.Initialize(spec, representedObject);
        }

        /**
         * Called to visualize a bool value
         */
        public override void Visualize(bool value)
        {
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                meshRenderers[i].enabled = value;
            }

            this.Value = value;
        }

        /**
         * Called to visualize a float value
         */
        public override void Visualize(float value)
        {
            this.Visualize(value > 0.0);
        }

        /**
         * This is called when the visualization element is destroyed and required to clean up any changes
         * on the original element. In this case, it needs to make the object visible or not depending on
         * the initial state.
         */
        public override void OnDestroy()
        {
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                meshRenderers[i].enabled = initiallyEnabled[i];
            }

            base.OnDestroy();
        }
    }
}
