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

using UnityEngine;

namespace de.ugoe.cs.vivian.core
{
    /**
     * This class represents a light array.
     */
    public class LightArrayElement : VisualizationArray<LightArraySpec, float>
    {
        /** the effective lights instantiated for the given light specs */
        private LightElement[] LightElements;

        /**
         * Called to initialize the visualization element with the specification
         */
        internal new void Initialize(LightArraySpec spec)
        {
            throw new System.NotSupportedException("you need to call the other initialize method for this component");
        }

        /**
         * Called to initialize the visualization array with the specification and the complete virtual prototype
         */
        internal void Initialize(LightArraySpec spec, GameObject virtualPrototype)
        {
            base.Initialize(spec);

            LightElements = new LightElement[spec.Lights.Length];
            LightElement[] allLightElements = virtualPrototype.GetComponentsInChildren<LightElement>();

            for (int i = 0; i < spec.Lights.Length; i++)
            {
                foreach (LightElement candidate in allLightElements)
                {
                    if (candidate.Spec.Name == spec.Lights[i].Name)
                    {
                        LightElements[i] = candidate;
                    }
                }
            }

        }

        /**
         * Called to visualize a bool value
         */
        public override void Visualize(bool value)
        {
            foreach (LightElement light in this.LightElements)
            {
                light.Visualize(value);
            }

            this.Value = 1.0f;
        }

        /**
         * Called to visualize a float value
         */
        public override void Visualize(float value)
        {
            for (int i = 0; i < this.LightElements.Length; i++)
            {
                float floatI = i;
                float lightMinValue = floatI / this.LightElements.Length;
                float lightMaxValue = (floatI + 1) / this.LightElements.Length;

                if (value < lightMinValue)
                {
                    this.LightElements[i].Visualize(0);
                }
                else if (value < lightMaxValue)
                {
                    float effectiveValue = (value - lightMinValue) / (lightMaxValue - lightMinValue);
                    this.LightElements[i].Visualize(effectiveValue);
                }
                else
                {
                    this.LightElements[i].Visualize(1);
                }
            }

            this.Value = value;
        }
    }
}
