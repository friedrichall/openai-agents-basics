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
     * This class represents a visualization element having a specification and a reference to the effective
     * game object it represents
     */
    public interface IVisualization
    {
        /**
         * Called to visualize any value
         */
        void Visualize(object value);

        /**
         * Called to visualize a bool value
         */
        void Visualize(bool value);

        /**
         * Called to visualize a float value
         */
        void Visualize(float value);
    }

    /**
     * This class represents a visualization element having a specification and a reference to the effective
     * game object it represents
     */
    public abstract class VisualizationElement : VirtualPrototypeElement, IVisualization
    {
        /** the specification of the visualization element */
        internal VisualizationElementSpec Spec { get; set; }

        /** the value of the visualization */
        public object Value { get; set; }

        /**
         * Called to visualize any value
         */
        public virtual void Visualize(object value)
        {
            if (value is bool)
            {
                this.Visualize((bool)value);
            }
            else if (value is float)
            {
                this.Visualize((float)value);
            }
            else
            {
                throw new System.NotSupportedException("cannot visualize values of type " + value.GetType());
            }
        }

        /**
         * Called to visualize a bool value
         */
        public abstract void Visualize(bool value);

        /**
         * Called to visualize a float value
         */
        public abstract void Visualize(float value);

        /**
         * Called to initialize the visualization element with the specification
         */
        internal void Initialize(VisualizationElementSpec spec, GameObject representedObject)
        {
            base.Initialize(representedObject);
            this.Spec = spec;
        }

        /**
         * This is called when the visualization element is destroyed and required to clean up any changes
         * on the original element. In this case, nothing needs to be done.
         */
        public override void OnDestroy()
        {
            // nothing to do
        }
    }

    public abstract class VisualizationElement<T, V> : VisualizationElement where T : VisualizationElementSpec
    {
        /** the specification of the visualization element */
        internal new T Spec
        {
            get { return (T)base.Spec; }
            set { base.Spec = value; }
        }

        /** the current value of this visualization element */
        internal new V Value
        {
            get { return (V)base.Value; }
            set { base.Value = value; }
        }

        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal void Initialize(T spec, GameObject representedObject)
        {
            base.Initialize(spec, representedObject);
        }
    }

    /**
     * This class represents a visualization array, i.e. a list of visualization elements
     */
    public abstract class VisualizationArray : MonoBehaviour, IVisualization
    {
        /** the specification of the visualization element */
        internal VisualizationArraySpec Spec { get; set; }

        /** the value of the visualization */
        public object Value { get; set; }

        /**
         * Called to visualize any value
         */
        public virtual void Visualize(object value)
        {
            if (value is bool)
            {
                this.Visualize((bool)value);
            }
            else if (value is float)
            {
                this.Visualize((float)value);
            }
            else
            {
                throw new System.NotSupportedException("cannot visualize values of type " + value.GetType());
            }
        }

        /**
         * Called to visualize a bool value
         */
        public abstract void Visualize(bool value);

        /**
         * Called to visualize a float value
         */
        public abstract void Visualize(float value);

        /**
         * Called to initialize the visualization array with the specification
         */
        internal void Initialize(VisualizationArraySpec spec)
        {
            this.Spec = spec;
        }
    }


    public abstract class VisualizationArray<T, V> : VisualizationArray where T : VisualizationArraySpec
    {
        /** the specification of the visualization element */
        internal new T Spec
        {
            get { return (T)base.Spec; }
            set { base.Spec = value; }
        }

        /** the current value of this visualization element */
        internal new V Value
        {
            get { return (V)base.Value; }
            set { base.Value = value; }
        }

        /**
         * Called to initialize the visualization array with the specification
         */
        internal void Initialize(T spec)
        {
            this.Spec = spec;
        }
    }
}
