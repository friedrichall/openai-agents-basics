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

namespace de.ugoe.cs.vivian.core
{

    /**
     * This class represents a button. It only reacts on button press and release events
     */
    public class ButtonElement : InteractionElement<ButtonSpec>
    {
        private KeyValuePair<EventParameterSpec, float> ButtonPressed = new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.SELECTED_VALUE, 1);
        private KeyValuePair<EventParameterSpec, float> ButtonReleased = new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.SELECTED_VALUE, 0);

        /**
         * Called to initialize the element with the specification and the represented game object
         */
        internal override void Initialize(ButtonSpec spec, GameObject representedObject)
        {
            base.Value = false;
            base.Initialize(spec, representedObject);
        }

        /**
         * This is called when the button is pressed
         */
        public override void TriggerInteractionStarts(Pose pose)
        {
            Debug.Log(base.Spec.Name + ": BUTTON_PRESS");
            base.Value = true;
            base.RaiseInteractionElementEvent(EventSpec.BUTTON_PRESS, ButtonPressed);
        }

        /**
         * This is called between the button press and release and it is ignored
         */
        public override void TriggerInteractionContinues(Pose pose)
        {
            // do nothing
        }

        /**
         * This is called when the button is released
         */
        public override void TriggerInteractionEnds(Pose pose)
        {
            Debug.Log(base.Spec.Name + ": BUTTON_RELEASE");
            base.Value = false;
            base.RaiseInteractionElementEvent(EventSpec.BUTTON_RELEASE, ButtonReleased);
        }
    }

    /**
     * This class represents a toogle button. This means its value is toggled only with presses,
     * but not with releases
     */
    public class ToggleButtonElement : InteractionElement<ToggleButtonSpec>
    {
        private KeyValuePair<EventParameterSpec, float> Enabled = new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.SELECTED_VALUE, 1);
        private KeyValuePair<EventParameterSpec, float> Disabled = new KeyValuePair<EventParameterSpec, float>(EventParameterSpec.SELECTED_VALUE, 0);

        /**
         * Called to initialize the element with the specification and the represented game object
         */
        internal override void Initialize(ToggleButtonSpec spec, GameObject representedObject)
        {
            base.Value = false;
            base.Initialize(spec, representedObject);
        }

        /**
         * This is called when the button is pressed
         */
        public override void TriggerInteractionStarts(Pose pose)
        {
            Debug.Log(base.Spec.Name + ": BUTTON_PRESS");
            base.Value = !(bool)base.Value;
            base.RaiseInteractionElementEvent(EventSpec.BUTTON_PRESS, (bool)base.Value ? Enabled : Disabled);
        }

        /**
         * This is called between the button press and release and it is ignored
         */
        public override void TriggerInteractionContinues(Pose pose)
        {
            // do nothing
        }

        /**
         * This is called when the button is released
         */
        public override void TriggerInteractionEnds(Pose pose)
        {
            Debug.Log(base.Spec.Name + ": BUTTON_RELEASE");
            base.RaiseInteractionElementEvent(EventSpec.BUTTON_RELEASE, (bool)base.Value ? Enabled : Disabled);
        }
    }
}
