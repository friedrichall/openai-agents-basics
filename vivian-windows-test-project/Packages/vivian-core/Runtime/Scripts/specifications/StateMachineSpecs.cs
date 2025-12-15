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

    //#################################################################################################################
    // all specifications for states
    //#################################################################################################################

    /**
     * 
     */
    public class StateSpec : IEquatable<StateSpec>
    {
        public string Name { get; }

        public IConditionSpec[] Conditions { get; }

        /**
         * 
         */
        public StateSpec(string name, params IConditionSpec[] conditions)
        {
            this.Name = name;
            this.Conditions = conditions;
        }

        /**
         * 
         */
        public static bool operator ==(StateSpec obj1, StateSpec obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }

            if (ReferenceEquals(obj1, null))
            {
                return false;
            }
            if (ReferenceEquals(obj2, null))
            {
                return false;
            }

            return obj1.Equals(obj2);
        }

        /**
         * 
         */
        public static bool operator !=(StateSpec obj1, StateSpec obj2)
        {
            return !(obj1 == obj2);
        }

        /**
         * 
         */
        public bool Equals(StateSpec other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Name.Equals(other.Name);
        }

        /**
         * 
         */
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((StateSpec)obj);
        }

        /**
         * 
         */
        public override int GetHashCode()
        {
            unchecked
            {
                return Name.GetHashCode();
            }
        }
    }

    /**
     * 
     */
    public interface IConditionSpec
    {
        // nothing to do
    }

    /**
     * 
     */
    public interface IVisualizationSpec
    {
        VisualizationSpec VisualizationSpec { get; }
    }

    /**
     * 
     */
    public class FloatValueSpec : IConditionSpec
    {
        public string Name { get; }

        public float Value { get; }

        internal FloatValueSpec(string name, float value)
        {
            this.Name = name;
            this.Value = value;
        }
    }

    /**
     * 
     */
    public class FloatValueVisualizationSpec : FloatValueSpec, IVisualizationSpec
    {
        public VisualizationSpec VisualizationSpec { get; }

        internal FloatValueVisualizationSpec(VisualizationSpec visualizationSpec, float value)
            : base(visualizationSpec.Name, value)
        {
            this.VisualizationSpec = visualizationSpec;
        }
    }

    /**
     * 
     */
    public class ValueOfInteractionElementVisualizationSpec : IConditionSpec, IVisualizationSpec
    {
        public VisualizationSpec VisualizationSpec { get; }

        public InteractionElementSpec InteractionElementSpec { get; }

        internal ValueOfInteractionElementVisualizationSpec(VisualizationSpec visualizationSpec,
                                                            InteractionElementSpec interactionElementSpec)
        {
            this.VisualizationSpec = visualizationSpec;
            this.InteractionElementSpec = interactionElementSpec;
        }
    }

    /**
     * 
     */
    public class TextSpec
    {
        public string Text { get; }

        public int Size { get; }

        public Vector2 Position { get; }

        public string Color { get; }

        internal TextSpec(string text, int size, Vector2 position, string color)
        {
            this.Text = text;
            this.Size = size;
            this.Position = position;
            this.Color = color;
        }
    }

    /**
     * 
     */
    public class FileVisualizationSpec : IConditionSpec, IVisualizationSpec
    {
        public VisualizationSpec VisualizationSpec { get; }

        public string FileName { get; }

        internal FileVisualizationSpec(VisualizationSpec visualizationSpec, string fileName)
        {
            this.VisualizationSpec = visualizationSpec;
            this.FileName = fileName;
        }
    }

    /**
     * 
     */
    public class ScreenContentVisualizationSpec : FileVisualizationSpec
    {
        public ScreenSpec ScreenSpec { get; }

        public TextSpec[] Texts { get; }

        internal ScreenContentVisualizationSpec(ScreenSpec screenSpec, string fileName, TextSpec[] texts)
            : base(screenSpec, fileName)
        {
            this.ScreenSpec = screenSpec;
            this.Texts = texts;
        }
    }

    /**
     * 
     */
    public class InteractionElementConditionSpec : IConditionSpec
    {
        public InteractionElementSpec InteractionElementSpec { get; }

        public InteractionElementSpec.Attribute Attribute { get; }

        public object Value { get; }

        internal InteractionElementConditionSpec(InteractionElementSpec interactionElementSpec, InteractionElementSpec.Attribute attribute, object value)
        {
            this.InteractionElementSpec = interactionElementSpec;
            this.Attribute = attribute;
            this.Value = value;
        }
    }

    //#################################################################################################################
    // all specifications for transitions
    //#################################################################################################################

    /**
     * 
     */
    public class TransitionSpec
    {
        public StateSpec SourceState { get; }

        public InteractionElementSpec InteractionElement { get; }

        public EventSpec Event { get; }

        public TimeoutSpec Timeout { get; }

        public StateSpec DestinationState { get; }

        public GuardSpec[] Guards { get; }

        internal TransitionSpec(StateSpec sourceState,
                                InteractionElementSpec interactionElement,
                                EventSpec eventSpec,
                                StateSpec destinationState,
                                params GuardSpec[] guards)
        {
            this.SourceState = sourceState;
            this.InteractionElement = interactionElement;
            this.Event = eventSpec;
            this.DestinationState = destinationState;
            this.Guards = guards;
        }

        internal TransitionSpec(StateSpec sourceState,
                                TimeoutSpec timeout,
                                StateSpec destinationState,
                                params GuardSpec[] guards)
        {
            this.SourceState = sourceState;
            this.Timeout = timeout;
            this.DestinationState = destinationState;
            this.Guards = guards;
        }

    }

    /**
     * 
     */
    public enum EventSpec
    {
        BUTTON_PRESS = 0,
        BUTTON_RELEASE = 1,

        SLIDER_DRAG_START = 10,
        SLIDER_DRAG = 11,
        SLIDER_DRAG_END = 12,

        ROTATABLE_DRAG_START = 20,
        ROTATABLE_DRAG = 21,
        ROTATABLE_DRAG_END = 22,

        TOUCH_START = 30,
        TOUCH_SLIDE = 31,
        TOUCH_END = 32,

        OBJECT_MOVE_START = 40,
        OBJECT_MOVE = 41,
        OBJECT_MOVE_END = 42,
        
        SNAPPOSES_CHECK = 50
    }

    /**
     * 
     */
    public enum EventParameterSpec
    {
        SELECTED_VALUE = 0,
        TOUCH_X_COORDINATE = 10,
        TOUCH_Y_COORDINATE = 11
    }

    /**
     * 
     */
    public enum OperatorSpec
    {
        LARGER = -2,
        LARGER_EQUALS = -1,
        EQUALS = 0,
        SMALLER_EQUALS = 1,
        SMALLER = 2,
        NOT_EQUALS = 3
    }

    /**
     * 
     */
    public class TimeoutSpec
    {
        public long Timeout { get; }

        internal TimeoutSpec(long timeout)
        {
            this.Timeout = timeout;
        }
    }

    /**
     * 
     */
    public abstract class GuardSpec
    {
        public OperatorSpec Operator { get; }

        public object CompareValue { get; }

        internal GuardSpec(OperatorSpec operatorSpec, object compareValue)
        {
            this.Operator = operatorSpec;
            this.CompareValue = compareValue;
            
        }

        internal bool Matches(object value)
        {
            if (value.GetType() != this.CompareValue.GetType())
            {
                throw new ArgumentException("the value of type " + value.GetType() + " shall be compared with the guard value " +
                                            this.CompareValue + " of type " + this.CompareValue.GetType() + ". This is not supported");
            }
            else if (value is bool)
            {
                switch (Operator)
                {
                    case OperatorSpec.EQUALS: return ((bool)value) == ((bool)this.CompareValue);
                }
            }
            else if (value is float)
            {
                switch (Operator)
                {
                    case OperatorSpec.LARGER: return ((float)value) > ((float)this.CompareValue);
                    case OperatorSpec.LARGER_EQUALS: return ((float)value) >= ((float)this.CompareValue);
                    case OperatorSpec.EQUALS: return ((float)value) == ((float)this.CompareValue);
                    case OperatorSpec.NOT_EQUALS: return ((float)value) != ((float)this.CompareValue);
                    case OperatorSpec.SMALLER_EQUALS: return ((float)value) <= ((float)this.CompareValue);
                    case OperatorSpec.SMALLER: return ((float)value) < ((float)this.CompareValue);
                }
            }
            else if (value is string)
            {
                switch (Operator)
                {
                    case OperatorSpec.EQUALS: return ((string)value) == ((string)this.CompareValue);
                }
            }
            else if (value is Vector3)
            {
                Vector3 vector = (Vector3)value;
                Vector3 compareVector = (Vector3)this.CompareValue;

                switch (Operator)
                {
                    case OperatorSpec.LARGER:
                        return (vector.x > compareVector.x) && (vector.y > compareVector.y) && (vector.z > compareVector.z);
                    case OperatorSpec.LARGER_EQUALS:
                        return (vector.x >= compareVector.x) && (vector.y >= compareVector.y) && (vector.z >= compareVector.z);
                    case OperatorSpec.EQUALS:
                        return vector == compareVector;
                    case OperatorSpec.NOT_EQUALS:
                        return vector != compareVector;
                    case OperatorSpec.SMALLER_EQUALS:
                        return (vector.x <= compareVector.x) && (vector.y <= compareVector.y) && (vector.z <= compareVector.z);
                    case OperatorSpec.SMALLER:
                        return (vector.x < compareVector.x) && (vector.y < compareVector.y) && (vector.z < compareVector.z);
                }
            }
            else
            {
                throw new ArgumentException("a compare value of type " + CompareValue.GetType() + " for guards is not supported");
            }


            throw new ArgumentException("the comparison operator " + Operator +
                                        " is not supported for a guard compare value of type " +
                                        CompareValue.GetType());
        }
    }

    /**
     * 
     */
    public class EventGuardSpec : GuardSpec
    {
        public EventParameterSpec EventParameter { get; }

        internal EventGuardSpec(EventParameterSpec eventParameter, OperatorSpec operatorSpec, object compareValue)
            : base(operatorSpec, compareValue)
        {
            this.EventParameter = eventParameter;
        }
    }

    /**
     * 
     */
    public class InteractionElementGuardSpec : GuardSpec
    {
        public InteractionElementSpec InteractionElement { get; }

        public InteractionElementSpec.Attribute Attribute { get; }

        internal InteractionElementGuardSpec(InteractionElementSpec interactionElement,
                                             InteractionElementSpec.Attribute attribute,
                                             OperatorSpec operatorSpec,
                                             object compareValue)
            : base(operatorSpec, compareValue)
        {
            this.InteractionElement = interactionElement;
            this.Attribute = attribute;
        }
    }

    //#################################################################################################################
    // all specifications for serializing states and transitions
    //#################################################################################################################

    /**
     * 
     */
    [System.Serializable]
    public class StateSpecJSONWrapper
    {
        public string Name;

        public ConditionSpecJSONWrapper[] Conditions;

        public StateSpecJSONWrapper(StateSpec stateSpec)
        {
            this.Name = stateSpec.Name;
            this.Conditions = new ConditionSpecJSONWrapper[stateSpec.Conditions.Length];

            for (int i = 0; i < stateSpec.Conditions.Length; i++)
            {
                this.Conditions[i] = new ConditionSpecJSONWrapper(stateSpec.Conditions[i]);
            }
        }

        public StateSpec GetSpec(InteractionElementSpec[] interactionElements, VisualizationSpec[] visualizations)
        {
            IConditionSpec[] conditions = null;

            if (this.Conditions != null)
            {
                conditions = new IConditionSpec[this.Conditions.Length];

                for (int i = 0; i < this.Conditions.Length; i++)
                {
                    conditions[i] = this.Conditions[i].GetSpec(interactionElements, visualizations);
                }
            }

            return new StateSpec(this.Name, conditions);
        }
    }


    /**
     * 
     */
    [System.Serializable]
    public class ConditionSpecJSONWrapper
    {
        public string Type;

        public string Name;

        public string Value;

        public string VisualizationElement;

        public string InteractionElement;

        public string FileName;

        public TextSpecJSONWrapper[] Texts;

        public string Attribute;

        public ConditionSpecJSONWrapper(IConditionSpec conditionSpec)
        {
            if (conditionSpec is FloatValueVisualizationSpec)
            {
                this.Type = "FloatValueVisualization";
                this.Value = ((FloatValueVisualizationSpec)conditionSpec).Value.ToString();
                this.VisualizationElement = ((FloatValueVisualizationSpec)conditionSpec).VisualizationSpec.Name;
            }
            else if (conditionSpec is FloatValueSpec)
            {
                this.Type = "FloatValue";
                this.Name = ((FloatValueSpec)conditionSpec).Name;
                this.Value = ((FloatValueSpec)conditionSpec).Value.ToString();
            }
            else if (conditionSpec is ValueOfInteractionElementVisualizationSpec)
            {
                this.Type = "ValueOfInteractionElementVisualization";
                this.InteractionElement = ((ValueOfInteractionElementVisualizationSpec)conditionSpec).InteractionElementSpec.Name;
                this.VisualizationElement = ((ValueOfInteractionElementVisualizationSpec)conditionSpec).VisualizationSpec.Name;
            }
            else if (conditionSpec is FileVisualizationSpec)
            {
                this.Type = "FileVisualization";
                this.FileName = ((FileVisualizationSpec)conditionSpec).FileName;
                this.VisualizationElement = ((FileVisualizationSpec)conditionSpec).VisualizationSpec.Name;
            }
            else if (conditionSpec is ScreenContentVisualizationSpec)
            {
                this.Type = "ScreenContentVisualization";
                this.FileName = ((ScreenContentVisualizationSpec)conditionSpec).FileName;
                this.VisualizationElement = ((ScreenContentVisualizationSpec)conditionSpec).VisualizationSpec.Name;

                if (((ScreenContentVisualizationSpec)conditionSpec).Texts != null) {
                    this.Texts = new TextSpecJSONWrapper[((ScreenContentVisualizationSpec)conditionSpec).Texts.Length];

                    for (int i = 0; i < this.Texts.Length; i++)
                    {
                       this.Texts[i] = new TextSpecJSONWrapper(((ScreenContentVisualizationSpec)conditionSpec).Texts[i]);
                    }
                }
            }
            else if (conditionSpec is InteractionElementConditionSpec)
            {
                this.Type = "InteractionElementCondition";
                this.InteractionElement = ((InteractionElementConditionSpec)conditionSpec).InteractionElementSpec.Name;
                this.Attribute = ((InteractionElementConditionSpec)conditionSpec).Attribute.ToString();
                this.Value = ((InteractionElementConditionSpec)conditionSpec).Value.ToString();
            }
        }

        public IConditionSpec GetSpec(InteractionElementSpec[] interactionElements, VisualizationSpec[] visualizations)
        {
            object value = Utils.ParseValue(this.Value);

            if (this.Type == "FloatValue")
            {
                if (!(value is float))
                {
                    throw new ArgumentException
                        ("The value specified for a float value state condition must be a float, but is " + this.Value);
                }

                return new FloatValueSpec(this.Name, (float)value);
            }
            else if (this.Type == "FloatValueVisualization")
            {
                if (!(value is float))
                {
                    throw new ArgumentException
                        ("The value specified for a float value visualization as state condition must be a float, but is " + this.Value);
                }

                foreach (VisualizationSpec visualization in visualizations)
                {
                    if (visualization.Name == this.VisualizationElement)
                    {
                        return new FloatValueVisualizationSpec(visualization, (float)value);
                    }
                }

                throw new System.ArgumentException("unknown visualization " + this.VisualizationElement);
            }
            else if (this.Type == "ValueOfInteractionElementVisualization")
            {
                foreach (VisualizationSpec visualization in visualizations)
                {
                    if (visualization.Name == this.VisualizationElement)
                    {
                        foreach (InteractionElementSpec interactionElement in interactionElements)
                        {
                            if (interactionElement.Name == this.InteractionElement)
                            {
                                return new ValueOfInteractionElementVisualizationSpec(visualization, interactionElement);
                            }
                        }

                        throw new System.ArgumentException("unknown interaction element " + this.InteractionElement);
                    }
                }

                throw new System.ArgumentException("unknown visualization " + this.VisualizationElement);
            }
            else if (this.Type == "FileVisualization")
            {
                foreach (VisualizationSpec visualization in visualizations)
                {
                    if (visualization.Name == this.VisualizationElement)
                    {
                        return new FileVisualizationSpec(visualization, this.FileName);
                    }
                }

                throw new System.ArgumentException("unknown visualization element " + this.VisualizationElement);
            }
            else if (this.Type == "ScreenContentVisualization")
            {
                foreach (VisualizationSpec visualization in visualizations)
                {
                    if ((visualization.Name == this.VisualizationElement) && (visualization is ScreenSpec))
                    {
                        TextSpec[] texts = null;

                        if (this.Texts != null)
                        {
                            texts = new TextSpec[this.Texts.Length];

                            for (int i = 0; i < this.Texts.Length; i++)
                            {
                                texts[i] = this.Texts[i].GetSpec();
                            }
                        }

                        return new ScreenContentVisualizationSpec((ScreenSpec)visualization, this.FileName, texts);
                    }
                }

                throw new System.ArgumentException("unknown visualization element " + this.VisualizationElement);
            }
            else if (this.Type == "InteractionElementCondition")
            {
                foreach (InteractionElementSpec interactionElement in interactionElements)
                {
                    if (interactionElement.Name == this.InteractionElement)
                    {
                        return new InteractionElementConditionSpec
                            (interactionElement,
                             (InteractionElementSpec.Attribute)Enum.Parse(typeof(InteractionElementSpec.Attribute), this.Attribute),
                             value);
                    }
                }

                throw new System.ArgumentException("unknown positionable element " + this.InteractionElement);
            }
            else
            {
                throw new System.ArgumentException("unknown state condition type " + this.Type);
            }
        }
    }

    /**
     * 
     */
    [System.Serializable]
    public class TextSpecJSONWrapper
    {
        public string Text;

        public int Size;

        public string Position;

        public string Color;

        public TextSpecJSONWrapper(TextSpec textSpec)
        {
            this.Text = textSpec.Text;
            this.Size = textSpec.Size;
            this.Position = textSpec.Position.ToString();
            this.Color = textSpec.Color;
        }

        public TextSpec GetSpec()
        {
            return new TextSpec(Text, Size, (Vector2)Utils.ParseValue(this.Position), this.Color);
        }
    }

    /**
     * 
     */
    [System.Serializable]
    public class StateSpecArrayJSONWrapper
    {
        public StateSpecJSONWrapper[] States;

        public StateSpecArrayJSONWrapper(StateSpec[] states)
        {
            this.States = new StateSpecJSONWrapper[states.Length];

            for (int i = 0; i < states.Length; i++)
            {
                this.States[i] = new StateSpecJSONWrapper(states[i]);
            }
        }

        public StateSpec[] GetSpecsArray(InteractionElementSpec[] interactionElements, VisualizationSpec[] visualizations)
        {
            StateSpec[] result = null;

            if (this.States != null)
            {
                result = new StateSpec[this.States.Length];

                for (int i = 0; i < this.States.Length; i++)
                {
                    result[i] = this.States[i].GetSpec(interactionElements, visualizations);
                }
            }

            return result;
        }
    }

    /**
     * 
     */
    [System.Serializable]
    public class GuardSpecJSONWrapper
    {
        public string EventParameter;

        public string InteractionElement;

        public string Attribute;

        public string Operator;

        public string CompareValue;

        public GuardSpecJSONWrapper(GuardSpec guardSpec)
        {
            if (guardSpec is EventGuardSpec eventGuard)
            {
                this.EventParameter = eventGuard.EventParameter.ToString();
            }
            else if (guardSpec is InteractionElementGuardSpec interactionElementGuard)
            {
                this.InteractionElement = interactionElementGuard.InteractionElement.ToString();
                this.Attribute = interactionElementGuard.Attribute.ToString();
            }
            else
            {
                throw new ArgumentException("unknown type of guard spec " + guardSpec);
            }

            this.Operator = guardSpec.Operator.ToString();
            this.CompareValue = guardSpec.CompareValue.ToString();
        }

        public GuardSpec GetSpec(InteractionElementSpec[] interactionElements)
        {
            object effectiveCompareValue = Utils.ParseValue(this.CompareValue);

            if (this.EventParameter != null)
            {
                return new EventGuardSpec((EventParameterSpec)Enum.Parse(typeof(EventParameterSpec), this.EventParameter),
                                          (OperatorSpec)Enum.Parse(typeof(OperatorSpec), this.Operator),
                                          effectiveCompareValue);
            }
            else if (this.InteractionElement != null)
            {
                InteractionElementSpec interactionElement = null;
                foreach (InteractionElementSpec candidate in interactionElements)
                {
                    if (candidate.Name == this.InteractionElement)
                    {
                        interactionElement = candidate;
                        break;
                    }
                }

                if (interactionElement != null)
                {
                    return new InteractionElementGuardSpec(interactionElement,
                                                           (InteractionElementSpec.Attribute)Enum.Parse(typeof(InteractionElementSpec.Attribute), this.Attribute),
                                                           (OperatorSpec)Enum.Parse(typeof(OperatorSpec), this.Operator),
                                                           effectiveCompareValue);
                }

                throw new ArgumentException("the interaction element " + this.InteractionElement +
                                            " specified in an interaction element guard does not exist");
            }

            throw new ArgumentException("unknown type of guard found. Please provide either an interaction element or a parameter");
        }
    }

    /**
     * 
     */
    [System.Serializable]
    public class TransitionSpecJSONWrapper
    {
        public string SourceState;

        public string InteractionElement;

        public string Event;

        public long Timeout = long.MinValue;

        public string DestinationState;

        public GuardSpecJSONWrapper[] Guards;

        public TransitionSpecJSONWrapper(TransitionSpec transitionSpec)
        {
            this.SourceState = transitionSpec.SourceState.Name;
            this.Event = transitionSpec.Event.ToString();

            if (transitionSpec.InteractionElement != null)
            {
                this.InteractionElement = transitionSpec.InteractionElement.Name;
            }

            if (transitionSpec.Timeout != null)
            {
                this.Timeout = transitionSpec.Timeout.Timeout;
            }

            this.DestinationState = transitionSpec.DestinationState.Name;

            if (transitionSpec.Guards != null)
            {
                this.Guards = new GuardSpecJSONWrapper[transitionSpec.Guards.Length];

                for (int i = 0; i < transitionSpec.Guards.Length; i++)
                {
                    this.Guards[i] = new GuardSpecJSONWrapper(transitionSpec.Guards[i]);
                }
            }
        }

        public TransitionSpec GetSpec(StateSpec[] states, InteractionElementSpec[] interactionElements)
        {
            StateSpec source = null;
            StateSpec dest = null;

            foreach (StateSpec state in states)
            {
                if (state.Name == this.SourceState)
                {
                    source = state;
                }

                if (state.Name == this.DestinationState)
                {
                    dest = state;
                }
            }

            if (source == null)
            {
                throw new System.ArgumentException("unknown state " + this.SourceState);
            }

            if (dest == null)
            {
                throw new System.ArgumentException("unknown state " + this.DestinationState);
            }

            InteractionElementSpec interactionElement = null;
            foreach (InteractionElementSpec candidate in interactionElements)
            {
                if (candidate.Name == this.InteractionElement)
                {
                    interactionElement = candidate;
                    break;
                }
            }

            GuardSpec[] guards = null;

            if (this.Guards != null)
            {
                guards = new GuardSpec[this.Guards.Length];

                for (int i = 0; i < this.Guards.Length; i++)
                {
                    guards[i] = this.Guards[i].GetSpec(interactionElements);
                }
            }

            if (interactionElement != null)
            {
                return new TransitionSpec(source, interactionElement, (EventSpec)Enum.Parse(typeof(EventSpec), this.Event), dest, guards);
            }
            else if (this.Timeout != long.MinValue)
            {
                return new TransitionSpec(source, new TimeoutSpec(this.Timeout), dest, guards);
            }

            throw new System.ArgumentException("neither timeout nor valid interaction element given for transition between " +
                                               this.SourceState + " and " + this.DestinationState);
        }
    }

    /**
     * 
     */
    [System.Serializable]
    public class TransitionSpecArrayJSONWrapper
    {
        public TransitionSpecJSONWrapper[] Transitions;

        public TransitionSpecArrayJSONWrapper(TransitionSpec[] transitions)
        {
            this.Transitions = new TransitionSpecJSONWrapper[transitions.Length];

            for (int i = 0; i < transitions.Length; i++)
            {
                this.Transitions[i] = new TransitionSpecJSONWrapper(transitions[i]);
            }
        }

        public TransitionSpec[] GetSpecsArray(StateSpec[] states, InteractionElementSpec[] interactionElements)
        {
            TransitionSpec[] result = null;

            if (this.Transitions != null)
            {
                result = new TransitionSpec[this.Transitions.Length];

                for (int i = 0; i < this.Transitions.Length; i++)
                {
                    result[i] = this.Transitions[i].GetSpec(states, interactionElements);
                }
            }

            return result;
        }
    }
}