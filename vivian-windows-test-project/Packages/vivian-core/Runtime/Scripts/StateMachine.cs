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
using System.Collections.Generic;
using System;
using System.Collections;

namespace de.ugoe.cs.vivian.core
{
    /**
     * 
     */
    public class StateMachine
    {
        /** the events that can be fired by interaction elements */
        public event EventHandler<StateChangeEvent> StateChangeEvent;

        private readonly TransitionSpec[] Transitions;

        private StateSpec CurrentState = null;

        private MonoBehaviour TimeoutHandler;

        private int NextTimeoutId = 0;

        private List<int> TimeoutIds = new List<int>();

        private Dictionary<string, InteractionElement> InteractionElements = new Dictionary<string, InteractionElement>();

        private Dictionary<string, IVisualization> Visualizations = new Dictionary<string, IVisualization>();

        /**
         * 
         */
        internal StateMachine(TransitionSpec[] transitions, MonoBehaviour timeoutHandler)
        {
            if (transitions == null)
            {
                throw new ArgumentException("the transitions parameter must not be null");

            }

            if (timeoutHandler == null)
            {
                throw new ArgumentException("the timeoutHandler parameter must not be null");

            }

            this.Transitions = transitions;
            this.TimeoutHandler = timeoutHandler;
        }

        /**
         * 
         */
        internal void Start(StateSpec initialState, InteractionElement[] interactionElements, IVisualization[] visualizationElements)
        {
            this.CurrentState = initialState;
            RaiseStateChangeEvent(initialState.Name);
            GetInteractionElements(interactionElements);
            GetVisualizationElements(visualizationElements);

            ApplyVisualizations(true);
            ApplyInteractionElementConditions(true);
            SetupTimeouts();
        }

        /**
         * 
         */
        internal void Stop()
        {
            // stop as many coroutines as their where started for timeouts
            foreach(int timoutId in this.TimeoutIds)
            {
                this.TimeoutHandler.StopCoroutine("HandleTimeout");
            }

            this.TimeoutIds.Clear();
        }

        /**
         * 
         */
        private void GetInteractionElements(InteractionElement[] interactionElements)
        {
            foreach (InteractionElement interactionElement in interactionElements)
            {
                if (!this.InteractionElements.ContainsKey(interactionElement.Spec.Name))
                {
                    this.InteractionElements.Add(interactionElement.Spec.Name, interactionElement);
                }
                else
                {
                    Debug.LogWarning("there are multiple interaction elements with the name " + interactionElement.Spec.Name);
                }
            }
        }

        /**
         * 
         */
        private void GetVisualizationElements(IVisualization[] visualizationElements)
        {
            // there may not be any transitions, hence check at least the current state for visualizations
            GetVisualizationElementsFromState(this.CurrentState, visualizationElements);

            foreach (TransitionSpec transition in this.Transitions)
            {
                GetVisualizationElementsFromState(transition.SourceState, visualizationElements);
                GetVisualizationElementsFromState(transition.DestinationState, visualizationElements);
            }
        }

        /**
         * 
         */
        private void GetVisualizationElementsFromState(StateSpec state, IVisualization[] visualizations)
        {
            foreach (IConditionSpec conditionSpec in state.Conditions)
            {
                if ((conditionSpec is IVisualizationSpec) &&
                    (!this.Visualizations.ContainsKey(((IVisualizationSpec)conditionSpec).VisualizationSpec.Name)))
                {
                    string name = ((IVisualizationSpec)conditionSpec).VisualizationSpec.Name;

                    foreach (IVisualization candidate in visualizations)
                    {
                        if (((candidate is VisualizationElement) && (((VisualizationElement)candidate).Spec.Name == name)) ||
                            ((candidate is VisualizationArray) && (((VisualizationArray)candidate).Spec.Name == name)))
                        {
                            this.Visualizations.Add(name, candidate);
                            break;
                        }
                    }

                    if (!this.Visualizations.ContainsKey(name))
                    {
                        throw new ArgumentException("the defined visualization specification " + name +
                                                    " does not have its instantiated counterpart");
                    }
                }
            }
        }

        /**
         * 
         */
        internal void HandleInteractionEvent(object source, VPElementEvent elementEvent)
        {
            if (!(elementEvent is InteractionElementEvent))
            {
                // ignore VP element events which are no interaction element events, as only those change the state
                return;
            }

            if (this.CurrentState == null)
            {
                // the state machine is not started. Ignore this.
                return;
            }

            InteractionElementEvent interactionEvent = (InteractionElementEvent)elementEvent;

            bool wasStateChange = false;

            foreach (TransitionSpec transition in Transitions)
            {
                if ((transition.SourceState == CurrentState) &&
                    (transition.InteractionElement == interactionEvent.InteractionElementSpec) &&
                    (transition.Event == interactionEvent.EventSpec))
                {
                    if (GuardsMatch(transition, interactionEvent))
                    {
                        Debug.Log("handled " + interactionEvent.EventSpec + " on " + interactionEvent.InteractionElementSpec.Name +
                                  " --> transitioning from " + this.CurrentState.Name + " to " + transition.DestinationState.Name);
                        this.CurrentState = transition.DestinationState;
                        SetupTimeouts();

                        RaiseStateChangeEvent(this.CurrentState.Name);
                        wasStateChange = true;

                        break;
                    }
                }
            }

            ApplyVisualizations(wasStateChange);
            ApplyInteractionElementConditions(wasStateChange);
        }

        /**
         * 
         */
        private bool GuardsMatch(TransitionSpec transition, InteractionElementEvent interactionEvent = null)
        {
            if (transition.Guards != null)
            {
                foreach (GuardSpec guard in transition.Guards)
                {
                    if (guard is EventGuardSpec eventGuard)
                    {
                        if (interactionEvent == null)
                        {
                            throw new ArgumentException("A guard for a transition from state " + transition.SourceState.Name +
                                                        " to state " + transition.DestinationState.Name +
                                                        " checks for the event parameter " + eventGuard.EventParameter +
                                                        " on an interaction event but was specified for a timeout event which" +
                                                        " does not have event parameters.");
                        }

                        bool parameterFound = false;

                        foreach (KeyValuePair<EventParameterSpec, float> value in interactionEvent.ParameterValues)
                        {
                            if (value.Key == eventGuard.EventParameter)
                            {
                                parameterFound = true;

                                if (!eventGuard.Matches(value.Value))
                                {
                                    return false;
                                }
                            }
                        }

                        if (!parameterFound)
                        {
                            throw new ArgumentException("A guard for a transition from state " + transition.SourceState.Name +
                                                        " to state " + transition.DestinationState.Name +
                                                        " checks for the event parameter " + eventGuard.EventParameter +
                                                        " on an interaction event of type " + transition.Event +
                                                        " that does not provide this parameter.");
                        }
                    }
                    else if (guard is InteractionElementGuardSpec interactionElementGuard)
                    {
                        if (!this.InteractionElements.TryGetValue(interactionElementGuard.InteractionElement.Name,
                                                                  out InteractionElement element))
                        {
                            throw new ArgumentException("the interaction element " + interactionElementGuard.InteractionElement.Name +
                                                        " specified in an interaction element guard does not exist.");
                        }

                        switch (interactionElementGuard.Attribute)
                        {
                            case InteractionElementSpec.Attribute.VALUE:
                                if (!interactionElementGuard.Matches(element.Value))
                                {
                                    return false;
                                }
                                break;
                            case InteractionElementSpec.Attribute.POSITION:
                                if (!interactionElementGuard.Matches(element.RepresentedObject.transform.localPosition))
                                {
                                    return false;
                                }
                                break;

                            default:
                                throw new ArgumentException("The interaction element attribute " + interactionElementGuard.Attribute +
                                                            " of a guard for a transition from state " + transition.SourceState.Name +
                                                            " to state " + transition.DestinationState.Name +
                                                            " is not supported.");
                        }
                    }
                    else
                    {
                        throw new ArgumentException("unknown type of guard spec " + guard);
                    }
                }
            }

            return true;
        }

        /**
         * 
         */
        private void ApplyVisualizations(bool wasStateChange)
        {
            foreach (IConditionSpec conditionSpec in CurrentState.Conditions)
            {
                if (!(conditionSpec is IVisualizationSpec))
                {
                    continue;
                }

                IVisualizationSpec visualizationSpec = (IVisualizationSpec)conditionSpec;

                if (this.Visualizations.TryGetValue(visualizationSpec.VisualizationSpec.Name,
                                                    out IVisualization visualization))
                {
                    if (visualizationSpec is FloatValueVisualizationSpec)
                    {
                        // handle float value visualization, but only if the state changed. Calls to these
                        // methods without state change will not cause an update for the values.
                        if (wasStateChange)
                        {
                            visualization.Visualize(((FloatValueVisualizationSpec)visualizationSpec).Value);
                        }
                    }
                    else if (visualizationSpec is ValueOfInteractionElementVisualizationSpec)
                    {
                        // handle value of interaction element visualization. They need to be forwarded always,
                        // independent of state changes
                        string name = ((ValueOfInteractionElementVisualizationSpec)visualizationSpec).InteractionElementSpec.Name;

                        if (InteractionElements.TryGetValue(name, out InteractionElement interactionElement))
                        {
                            visualization.Visualize(interactionElement.Value);
                        }
                        else
                        {
                            throw new ArgumentException("the visualization spec references the interaction element " +
                                                        name + " for which its counterpart does not exist");
                        }
                    }
                    else if (visualizationSpec is ScreenContentVisualizationSpec)
                    {
                        // handle screen content visualization, but only if the state changed. Calls to these
                        // methods without state change will not cause an update for the values.
                        if (wasStateChange)
                        {
                            visualization.Visualize((ScreenContentVisualizationSpec)visualizationSpec);
                        }
                    }
                    else if (visualizationSpec is FileVisualizationSpec)
                    {
                        // handle file visualization, but only if the state changed. Calls to these
                        // methods without state change will not cause an update for the values.
                        if (wasStateChange)
                        {
                            visualization.Visualize((FileVisualizationSpec)visualizationSpec);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("cannot handle visualization specs of type " + visualizationSpec.GetType());
                    }
                }
                else
                {
                    throw new ArgumentException("the visualization spec references the visualization element " +
                                                visualizationSpec.VisualizationSpec.Name +
                                                " for which its counterpart does not exist");

                }
            }
        }

        /**
         * 
         */
        private void ApplyInteractionElementConditions(bool wasStateChange)
        {
            foreach (IConditionSpec conditionSpec in CurrentState.Conditions)
            {
                if (!(conditionSpec is InteractionElementConditionSpec interactionElementConditionSpec))
                {
                    continue;
                }

                if (this.InteractionElements.TryGetValue(interactionElementConditionSpec.InteractionElementSpec.Name,
                                                         out InteractionElement interactionElement))
                {
                    object value = interactionElementConditionSpec.Value;

                    if (isValueFromInteractionElement(value)) {
                        if (InteractionElements.TryGetValue((string)value, out InteractionElement interactionElementToGetValueFrom))
                        {
                            value = interactionElementToGetValueFrom.Value;
                            interactionElement.SetAttribute(interactionElementConditionSpec.Attribute, value);
                        }
                        else
                        {
                            throw new ArgumentException("the interaction element condition references the value of the interaction element " +
                                                        value + " which does not exist");
                        }
                    }
                    else if (wasStateChange)
                    {
                        // handle static value visualization for interaction elements only if the state changed
                        interactionElement.SetAttribute(interactionElementConditionSpec.Attribute, interactionElementConditionSpec.Value);
                    }
                }
                else
                {
                    throw new ArgumentException("the interaction element condition spec references the interaction element " +
                                                interactionElementConditionSpec.InteractionElementSpec.Name +
                                                " for which its counterpart does not exist");

                }
            }
        }

        /**
         * 
         */
        private bool isValueFromInteractionElement(object value)
        {
            if (value is string)
            {
                return this.InteractionElements.ContainsKey((string)value);
            }
            else
            {
                return false;
            }
        }

        /**
         * 
         */
        private void SetupTimeouts()
        {
            this.TimeoutIds.Clear();

            foreach (TransitionSpec transition in Transitions)
            {
                if ((transition.Timeout != null) && (transition.SourceState == CurrentState))
                {
                    this.TimeoutIds.Add(this.NextTimeoutId);
                    this.TimeoutHandler.StartCoroutine(HandleTimeout(this.NextTimeoutId++, transition));
                }
            }
        }

        /**
         * 
         */
        private IEnumerator HandleTimeout(int timeoutId, TransitionSpec transition)
        {
            yield return new WaitForSeconds(transition.Timeout.Timeout / 1000);

            if ((transition.SourceState == CurrentState) &&
                (this.TimeoutIds.Contains(timeoutId)) &&
                GuardsMatch(transition))
            { 
                Debug.Log("timeout occured --> transitioning from " + this.CurrentState.Name +
                          " to " + transition.DestinationState.Name);

                CurrentState = transition.DestinationState;
                SetupTimeouts();
                RaiseStateChangeEvent(this.CurrentState.Name);
                ApplyVisualizations(true);
                ApplyInteractionElementConditions(true);
            }
        }

        /**
         * 
         */
        protected virtual void RaiseStateChangeEvent(string stateName)
        {
            this.StateChangeEvent?.Invoke(this, new StateChangeEvent(stateName));
        }
    }

    public class StateChangeEvent : EventArgs
    {
        public string StateName { get; }

        public StateChangeEvent(string stateName)
        {
            this.StateName = stateName;
        }
    }
}
