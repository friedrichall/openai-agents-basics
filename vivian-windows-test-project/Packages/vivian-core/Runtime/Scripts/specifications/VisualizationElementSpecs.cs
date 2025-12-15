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
      * 
      */
    public class VisualizationSpec
    {
        public string Name { get; }

        internal VisualizationSpec(string name)
        {
            this.Name = name;
        }

    }

    /**
     * 
     */
    public class VisualizationElementSpec : VisualizationSpec
    {
        internal VisualizationElementSpec(string name) : base(name)
        {
            // nothing to do
        }

    }

    /**
     * 
     */
    public class VisualizationArraySpec : VisualizationSpec
    {
        public VisualizationElementSpec[] CombinedElements;

        internal VisualizationArraySpec(string name, params VisualizationElementSpec[] combinedElements) : base(name)
        {
            this.CombinedElements = combinedElements;
        }

    }

    /**
     * 
     */
    public class ScreenSpec : VisualizationElementSpec
    {
        public Vector3 Plane { get; }

        public Vector2 Resolution { get; }

        public ScreenSpec(string name, Vector3 plane, Vector2 resolution) : base(name)
        {
            this.Plane = plane;
            this.Resolution = resolution;
        }

    }

    /**
     * 
     */
    public class LightSpec : VisualizationElementSpec
    {
        public Color EmissionColor { get; }

        public LightSpec(string name, Color emissionColor) : base(name)
        {
            this.EmissionColor = emissionColor;
        }

    }

    /**
     * 
     */
    public class LightArraySpec : VisualizationArraySpec
    {
        public LightSpec[] Lights
        {
            get { return (LightSpec[])base.CombinedElements; }
        }

        public LightArraySpec(string name, params LightSpec[] lights) : base(name, lights)
        {
            // nothing to do
        }

    }

    /**
     * 
     */
    public class ParticleSpec : VisualizationElementSpec
    {
        internal ParticleSpec(string name) : base(name)
        {
            // do nothing
        }

    }
    
    public class AnimationSpec : VisualizationElementSpec
    {
        internal AnimationSpec(string name) : base(name)
        {
            // do nothing
        }

    }

    public class AppearingObjectSpec : VisualizationElementSpec
    {
        internal AppearingObjectSpec(string name) : base(name)
        {
            // do nothing
        }
    }

    public class SoundSourceSpec : VisualizationElementSpec
    {
        internal SoundSourceSpec(string name) : base(name)
        {
            // do nothing
        }
    }

    /**
     * 
     */
    [System.Serializable]
    public class VisualizationElementSpecJSONWrapper
    {
        public string Type;

        public string Name;

        public Vector3 Plane;

        public Vector2 Resolution;

        public Color EmissionColor;

        public VisualizationElementSpecJSONWrapper(VisualizationElementSpec visualizationElementSpec)
        {
            this.Name = visualizationElementSpec.Name;

            if (visualizationElementSpec is ScreenSpec)
            {
                this.Type = "Screen";
                this.Plane = ((ScreenSpec)visualizationElementSpec).Plane;
                this.Resolution = ((ScreenSpec)visualizationElementSpec).Resolution;
            }
            else if (visualizationElementSpec is LightSpec)
            {
                this.Type = "Light";
                this.EmissionColor = ((LightSpec)visualizationElementSpec).EmissionColor;
            }
            else if (visualizationElementSpec is ParticleSpec)
            {
                this.Type = "Particles";
            }
            else if (visualizationElementSpec is AnimationSpec)
            {
                this.Type = "Animation";
            }
            else if (visualizationElementSpec is AppearingObjectSpec)
            {
                this.Type = "AppearingObject";
            }
            else if (visualizationElementSpec is SoundSourceSpec)
            {
                this.Type = "SoundSource";
            }
        }

        public VisualizationElementSpec GetSpec()
        {
            if (this.Type == "Screen")
            {
                return new ScreenSpec(this.Name, this.Plane, this.Resolution);
            }
            else if (this.Type == "Light")
            {
                return new LightSpec(this.Name, this.EmissionColor);
            }
            else if (this.Type == "Particles")
            {
                return new ParticleSpec(this.Name);
            }
            else if (this.Type == "Animation")
            {
                return new AnimationSpec(this.Name);
            }
            else if (this.Type == "AppearingObject")
            {
                return new AppearingObjectSpec(this.Name);
            }
            else if (this.Type == "SoundSource")
            {
                return new SoundSourceSpec(this.Name);
            }
            else
            {
                throw new System.ArgumentException("unknown visualization element type " + this.Type);
            }
        }
    }

    /**
     * 
     */
    [System.Serializable]
    public class VisualizationArraySpecJSONWrapper
    {
        public string Type;

        public string Name;

        public string[] CombinedElements;

        public VisualizationArraySpecJSONWrapper(VisualizationArraySpec visualizationArraySpec)
        {
            this.Name = visualizationArraySpec.Name;

            if (visualizationArraySpec is LightArraySpec)
            {
                this.Type = "LightArray";
                this.CombinedElements = new string[((LightArraySpec)visualizationArraySpec).Lights.Length];

                for (int i = 0; i < ((LightArraySpec)visualizationArraySpec).Lights.Length; i++)
                {
                    this.CombinedElements[i] = ((LightArraySpec)visualizationArraySpec).Lights[i].Name;
                }
            }
        }

        public VisualizationArraySpec GetSpec(VisualizationElementSpec[] visualizationElements)
        {
            if (this.Type == "LightArray")
            {
                LightSpec[] combinedElements = new LightSpec[this.CombinedElements.Length];

                for (int i = 0; i < this.CombinedElements.Length; i++)
                {
                    bool added = false;

                    foreach (VisualizationElementSpec visualizationElement in visualizationElements)
                    {
                        if ((this.CombinedElements[i] == visualizationElement.Name) &&
                            (visualizationElement is LightSpec))
                        {
                            combinedElements[i] = (LightSpec)visualizationElement;
                            added = true;
                            break;
                        }
                    }

                    if (!added)
                    {
                        throw new System.ArgumentException("did not find light spec with name " + this.CombinedElements[i] +
                                                           " in provided visualization elements");
                    }
                }

                return new LightArraySpec(this.Name, combinedElements);
            }
            else
            {
                throw new System.ArgumentException("unknown visualization array type " + this.Type);
            }
        }
    }

    /**
     * 
     */
    [System.Serializable]
    public class VisualizationElementSpecArrayJSONWrapper
    {
        public VisualizationElementSpecJSONWrapper[] Elements;

        public VisualizationElementSpecArrayJSONWrapper(VisualizationElementSpec[] elements)
        {
            this.Elements = new VisualizationElementSpecJSONWrapper[elements.Length];

            for (int i = 0; i < elements.Length; i++)
            {
                this.Elements[i] = new VisualizationElementSpecJSONWrapper(elements[i]);
            }
        }

        public VisualizationElementSpec[] GetSpecsArray()
        {
            VisualizationElementSpec[] result = new VisualizationElementSpec[this.Elements.Length];

            for (int i = 0; i < this.Elements.Length; i++)
            {
                result[i] = this.Elements[i].GetSpec();
            }


            return result;
        }
    }

    /**
     * 
     */
    [System.Serializable]
    public class VisualizationArraySpecArrayJSONWrapper
    {
        public VisualizationArraySpecJSONWrapper[] Elements;

        public VisualizationArraySpecArrayJSONWrapper(VisualizationArraySpec[] elements)
        {
            this.Elements = new VisualizationArraySpecJSONWrapper[elements.Length];

            for (int i = 0; i < elements.Length; i++)
            {
                this.Elements[i] = new VisualizationArraySpecJSONWrapper(elements[i]);
            }
        }

        public VisualizationArraySpec[] GetSpecsArray(VisualizationElementSpec[] visualizationElements)
        {
            VisualizationArraySpec[] result = new VisualizationArraySpec[this.Elements.Length];

            for (int i = 0; i < this.Elements.Length; i++)
            {
                result[i] = this.Elements[i].GetSpec(visualizationElements);
            }


            return result;
        }
    }
}
