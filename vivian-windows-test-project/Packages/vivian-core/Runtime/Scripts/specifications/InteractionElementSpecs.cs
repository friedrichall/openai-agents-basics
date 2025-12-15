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
using System.Globalization;
using UnityEngine;

namespace de.ugoe.cs.vivian.core
{
    /**
     * 
     */
    public abstract class InteractionElementSpec
    {
        public enum Attribute
        {
            VALUE = 0,
            POSITION = 1,
            ROTATION = 2,
            ENABLED = 3,
            FIXED = 4
        }

        public class AttributeValue
        {
            public Attribute Attribute { get; }

            public object Value { get; }

            public AttributeValue(Attribute attribute, object value)
            {
                this.Attribute = attribute;
                this.Value = value;
            }
        }

        public string Name { get; }

        public AttributeValue[] InitialAttributeValues { get; internal set; }

        internal InteractionElementSpec(string name, params AttributeValue[] initialAttributeValues)
        {
            this.Name = name;
            this.InitialAttributeValues = initialAttributeValues;
        }
    }

    /**
     * 
     */
    public class ButtonSpec : InteractionElementSpec
    {
        public ButtonSpec(string name) : base(name, new AttributeValue(Attribute.VALUE, false))
        {
            // do nothing
        }
    }

    /**
     * 
     */
    public class ToggleButtonSpec : InteractionElementSpec
    {
        public ToggleButtonSpec(string name, params AttributeValue[] initialAttributeValues)
            : base(name, initialAttributeValues)
        {
            // do nothing
        }

    }

    /**
     * 
     */
    public class TouchAreaSpec : InteractionElementSpec
    {
        public Vector3 Plane { get; }

        public Vector2 Resolution { get; }

        public TouchAreaSpec(string name, Vector3 plane, Vector2 resolution) : base(name)
        {
            this.Plane = plane;
            this.Resolution = resolution;
        }

    }

    /**
     * 
     */
    public abstract class PositionableElementSpec : InteractionElementSpec
    {
        public int PositionResolution { get; } = int.MaxValue;

        public int TransitionTimeInMs { get; } = 0;

        internal PositionableElementSpec(string name,
                                         int positionResolution,
                                         int transitionTimeInMs,
                                         params AttributeValue[] initialAttributeValues)
            : base(name, initialAttributeValues)
        {
            this.PositionResolution = positionResolution;

            if (this.PositionResolution < 2)
            {
                throw new ArgumentException("the position resolution of the interaction element " + name +
                                            " is " + this.PositionResolution +
                                            " but only values between 2 and max int inclusive are allowed.");
            }

            if (transitionTimeInMs >= 0) {
                this.TransitionTimeInMs = transitionTimeInMs;
            }
        }
    }

    /**
     * 
     */
    public class RotatableSpec : PositionableElementSpec
    {
        public float MinRotation { get; }

        public float MaxRotation { get; }

        public AxisSpec RotationAxis { get; }

        public bool AllowsForInfiniteRotation { get; }

        public RotatableSpec(string name,
                             float minRotation,
                             float maxRotation,
                             AxisSpec rotationAxis,
                             bool allowsForInfiniteRotation = false,
                             int rotationResolution = int.MaxValue,
                             int transitionTimeInMs = 0,
                             params AttributeValue[] initialAttributeValues)
            : base(name, rotationResolution, transitionTimeInMs, initialAttributeValues)
        {
            this.MinRotation = minRotation;
            this.MaxRotation = maxRotation;
            this.RotationAxis = rotationAxis;
            this.AllowsForInfiniteRotation = allowsForInfiniteRotation;

            if (this.MinRotation > this.MaxRotation)
            {
                throw new ArgumentException("for rotatables the minRotation must be smaller than the maxRotation. This is not given for " +
                                            name + " (minRotation = " + this.MinRotation + ", maxRotation = " + this.MaxRotation +
                                            ", minRotation !< maxRotation)");
            }

            if (this.AllowsForInfiniteRotation)
            {
                if (this.MaxRotation - this.MinRotation < 360)
                {
                    throw new ArgumentException("rotatables that allow for infinite rotation must have a rotation interval " +
                                                "of at least 360° between minimum and maximum. This is not given for " +
                                                name + " (minRotation = " + this.MinRotation + ", maxRotation = " + this.MaxRotation +
                                                ", maxRotation - minRotation  = " + (this.MaxRotation - this.MinRotation) + " < 360)");
                }
            }
        }

    }

    /**
     * 
     */
    public class SliderSpec : PositionableElementSpec
    {
        public Vector3 MinPosition { get; }

        public Vector3 MaxPosition { get; }

        public SliderSpec(string name,
                          Vector3 minPosition,
                          Vector3 maxPosition,
                          int positionResolution = int.MaxValue,
                          int transitionTimeInMs = 0,
                          params AttributeValue[] initialAttributeValues)
            : base(name, positionResolution, transitionTimeInMs,initialAttributeValues)
        {
            this.MinPosition = minPosition;
            this.MaxPosition = maxPosition;
        }

    }

    /**
     * 
     */
    public class MovableSpec : PositionableElementSpec
    {
        public SnapPoseSpec[] SnapPoses { get; }

        public MovableSpec(string name, SnapPoseSpec[] snapPoses,
                           int transitionTimeInMs = 0,
                           params AttributeValue[] initialAttributeValues)
            : base(name, int.MaxValue, transitionTimeInMs, initialAttributeValues)
        {
            this.SnapPoses = snapPoses;
        }
    }

    /**
     * 
     */
    [System.Serializable]
    public struct AxisSpec
    {
        public Vector3 Origin;

        public Vector3 Direction;

        public AxisSpec(Vector3 origin, Vector3 direction)
        {
            this.Origin = origin;
            this.Direction = direction;
        }
    }

    /**
     * 
     */
    [System.Serializable]
    public struct SnapPoseSpec
    {
        public Vector3 Position;

        public Vector3 Rotation;

        public SnapPoseSpec(Vector3 position) : this(position, Vector3.negativeInfinity) { }

        public SnapPoseSpec(Vector3 position, Vector3 rotation)
        {
            this.Position = position;
            this.Rotation = rotation;
        }
    }

    /**
     * 
     */
    [System.Serializable]
    public class InteractionElementSpecJSONWrapper
    {
        public string Type;

        public string Name;

        public AttributeValueJSONWrapper[] InitialAttributeValues;

        public float MinRotation;

        public float MaxRotation;

        public AxisSpec RotationAxis;

        public Vector3 MinPosition;

        public Vector3 MaxPosition;

        public bool AllowsForInfiniteRotation;

        public int PositionResolution = int.MaxValue;

        public int TransitionTimeInMs = 0;

        public Vector3 Plane;

        public Vector2 Resolution;

        public SnapPoseJSONWrapper[] SnapPoses;


        public InteractionElementSpecJSONWrapper(InteractionElementSpec interactionElementSpec)
        {
            this.Name = interactionElementSpec.Name;
            this.InitialAttributeValues = new AttributeValueJSONWrapper[interactionElementSpec.InitialAttributeValues.Length];

            for (int i = 0; i < interactionElementSpec.InitialAttributeValues.Length; i++)
            {
                this.InitialAttributeValues[i] = new AttributeValueJSONWrapper(interactionElementSpec.InitialAttributeValues[i]);
            }

            if (interactionElementSpec is ButtonSpec)
            {
                this.Type = "Button";
            }
            else if (interactionElementSpec is ToggleButtonSpec)
            {
                this.Type = "ToggleButton";
            }
            else if (interactionElementSpec is TouchAreaSpec)
            {
                this.Type = "TouchArea";
                this.Plane = ((TouchAreaSpec)interactionElementSpec).Plane;
                this.Resolution = ((TouchAreaSpec)interactionElementSpec).Resolution;
            }
            else if (interactionElementSpec is RotatableSpec)
            {
                this.Type = "Rotatable";
                this.MinRotation = ((RotatableSpec)interactionElementSpec).MinRotation;
                this.MaxRotation = ((RotatableSpec)interactionElementSpec).MaxRotation;
                this.RotationAxis = ((RotatableSpec)interactionElementSpec).RotationAxis;
                this.PositionResolution = ((RotatableSpec)interactionElementSpec).PositionResolution;
                this.TransitionTimeInMs = ((RotatableSpec)interactionElementSpec).TransitionTimeInMs;
            }
            else if (interactionElementSpec is SliderSpec)
            {
                this.Type = "Slider";
                this.MinPosition = ((SliderSpec)interactionElementSpec).MinPosition;
                this.MaxPosition = ((SliderSpec)interactionElementSpec).MaxPosition;
                this.PositionResolution = ((SliderSpec)interactionElementSpec).PositionResolution;
                this.TransitionTimeInMs = ((SliderSpec)interactionElementSpec).TransitionTimeInMs;
            }
            else if (interactionElementSpec is MovableSpec)
            {
                this.Type = "Movable";
                this.SnapPoses = new SnapPoseJSONWrapper[((MovableSpec)interactionElementSpec).SnapPoses.Length];

                for (int i = 0; i < ((MovableSpec)interactionElementSpec).SnapPoses.Length; i++)
                {
                    this.SnapPoses[i] = new SnapPoseJSONWrapper(((MovableSpec)interactionElementSpec).SnapPoses[i]);
                }

                this.TransitionTimeInMs = ((MovableSpec)interactionElementSpec).TransitionTimeInMs;
            }
        }

        public InteractionElementSpec GetSpec()
        {
            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.CurrencyDecimalSeparator = ".";

            InteractionElementSpec.AttributeValue[] defaultAttributeValues = null;

            if (this.InitialAttributeValues != null)
            {
                defaultAttributeValues = new InteractionElementSpec.AttributeValue[this.InitialAttributeValues.Length];

                for (int i = 0; i < this.InitialAttributeValues.Length; i++)
                {
                    defaultAttributeValues[i] = this.InitialAttributeValues[i].GetSpec();
                }
            }


            if (this.Type == "Button")
            {
                return new ButtonSpec(this.Name);
            }
            else if (this.Type == "ToggleButton")
            {
                return new ToggleButtonSpec(this.Name, defaultAttributeValues);
            }
            else if (this.Type == "TouchArea")
            {
                return new TouchAreaSpec(this.Name, this.Plane, this.Resolution);
            }
            else if (this.Type == "Rotatable")
            {
                if (this.PositionResolution == 0)
                {
                    this.PositionResolution = int.MaxValue;
                }

                return new RotatableSpec(this.Name, this.MinRotation, this.MaxRotation, this.RotationAxis,
                                         this.AllowsForInfiniteRotation, this.PositionResolution, this.TransitionTimeInMs,
                                         defaultAttributeValues);
            }
            else if (this.Type == "Slider")
            {
                if (this.PositionResolution == 0)
                {
                    this.PositionResolution = int.MaxValue;
                }

                return new SliderSpec(this.Name, this.MinPosition, this.MaxPosition,
                                      this.PositionResolution, this.TransitionTimeInMs, defaultAttributeValues);
            }
            else if (this.Type == "Movable")
            {
                SnapPoseSpec[] snapPoses = null;

                if (this.SnapPoses != null)
                {
                    snapPoses = new SnapPoseSpec[this.SnapPoses.Length];

                    for (int i = 0; i < this.SnapPoses.Length; i++)
                    {
                        snapPoses[i] = this.SnapPoses[i].GetSpec();
                    }
                }

                return new MovableSpec(this.Name, snapPoses, this.TransitionTimeInMs, defaultAttributeValues);
            }
            else
            {
                throw new System.ArgumentException("unknown interaction element type " + this.Type);
            }
        }
    }

    /**
     * 
     */
    [System.Serializable]
    public class AttributeValueJSONWrapper
    {
        public string Attribute;

        public string Value;

        public AttributeValueJSONWrapper(InteractionElementSpec.AttributeValue attributeValue)
        {
            this.Attribute = attributeValue.Attribute.ToString();
            this.Value = attributeValue.Value.ToString();
        }

        public InteractionElementSpec.AttributeValue GetSpec()
        {
            return new InteractionElementSpec.AttributeValue
                ((InteractionElementSpec.Attribute)Enum.Parse(typeof(InteractionElementSpec.Attribute), this.Attribute),
                 Utils.ParseValue(this.Value));

         }
    }

    /**
     * 
     */
    [System.Serializable]
    public class SnapPoseJSONWrapper
    {
        public string Position;

        public string Rotation;

        public SnapPoseJSONWrapper(SnapPoseSpec snapPose)
        {
            this.Position = snapPose.Position.ToString();
            this.Rotation = snapPose.Rotation.ToString();
        }

        public SnapPoseSpec GetSpec()
        {
            if (this.Position == null)
            {
                throw new ArgumentException("a snap pose requires at least a position vector");
            }

            if (this.Rotation == null)
            {
                return new SnapPoseSpec(Utils.ParseVector3(this.Position));
            }
            else
            {
                return new SnapPoseSpec
                    (Utils.ParseVector3(this.Position), Utils.ParseVector3(this.Rotation));
            }
        }
    }

    /**
     * 
     */
    [System.Serializable]
    public class InteractionElementSpecArrayJSONWrapper
    {
        public InteractionElementSpecJSONWrapper[] Elements;

        public InteractionElementSpecArrayJSONWrapper(InteractionElementSpec[] elements)
        {
            this.Elements = new InteractionElementSpecJSONWrapper[elements.Length];

            for (int i = 0; i < elements.Length; i++)
            {
                this.Elements[i] = new InteractionElementSpecJSONWrapper(elements[i]);
            }
        }

        public InteractionElementSpec[] GetSpecsArray()
        {
            InteractionElementSpec[] result = new InteractionElementSpec[this.Elements.Length];

            for (int i = 0; i < this.Elements.Length; i++)
            {
                result[i] = this.Elements[i].GetSpec();
            }

            return result;
        }
    }
}
