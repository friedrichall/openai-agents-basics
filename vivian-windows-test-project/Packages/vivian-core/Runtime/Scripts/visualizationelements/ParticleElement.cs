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
     * This class represents a particle element to be switched on or off
     */
    public class ParticleElement : VisualizationElement<ParticleSpec, bool>
    {
        // the particle system to be activated
        private ParticleSystem RepresentedParticleSystem;

        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal new void Initialize(ParticleSpec spec, GameObject representedObject)
        {
            base.Initialize(spec, representedObject);

            this.RepresentedParticleSystem = representedObject.GetComponent<ParticleSystem>();

            if (this.RepresentedParticleSystem == null)
            {
                throw new ArgumentException("represented object with name " + representedObject.name +
                                            " does not have a particle system");
            }
        }

        /**
         * Called to visualize a bool value
         */
        public override void Visualize(bool value)
        {
            if (value)
            {
                this.RepresentedParticleSystem.Play();
            }
            else
            {
                this.RepresentedParticleSystem.Stop();
            }

            this.Value = value;
        }

        /**
         * Called to visualize a float value
         */
        public override void Visualize(float value)
        {
            if (value > 0.0)
            {
                this.RepresentedParticleSystem.Play();
                this.Value = true;
            }
            else
            {
                this.RepresentedParticleSystem.Stop();
                this.Value = false;
            }
        }

        /**
         * This is called when the visualization element is destroyed and required to clean up any changes
         * on the original element. In this case, it needs to stop the particles.
         */
        public override void OnDestroy()
        {
            this.RepresentedParticleSystem.Stop();
            base.OnDestroy();
        }
    }
}
