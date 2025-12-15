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
     * This class represents an animation element to be played or stopped.
     */
    public class AnimationElement : VisualizationElement<AnimationSpec, bool>
    {
        // the animation to be played
        private Animation RepresentedAnimation;
        private Animator RepresentedAnimator;
        
        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal new void Initialize(AnimationSpec spec, GameObject representedObject)
        {
            base.Initialize(spec, representedObject);

            this.RepresentedAnimation = representedObject.GetComponent<Animation>();
            this.RepresentedAnimator = representedObject.GetComponent<Animator>();

            if ((this.RepresentedAnimation == null) && (this.RepresentedAnimator == null))
            {
                throw new ArgumentException("represented object with name " + representedObject.name +
                                            " does not have an animation");
            }
        }

        /**
         * Called to visualize a bool value
         */
        public override void Visualize(bool value)
        {
            if (value)
            {
                this.StartAnimation();
            }
            else
            {
                this.StopAnimation();
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
                this.StartAnimation();
                this.Value = true;
            }
            else
            {
                this.StopAnimation();
                this.Value = false;
            }
        }

        private void StartAnimation()
        {
            Debug.Log("starting animation");
            if (this.RepresentedAnimation != null)
            {
                this.RepresentedAnimation.Play();
            }
            if (this.RepresentedAnimator != null)
            {
                this.RepresentedAnimator.enabled = true;
            }
        }

        private void StopAnimation()
        {
            if (this.RepresentedAnimation != null)
            {
                this.RepresentedAnimation.Stop();
            }
            if (this.RepresentedAnimator != null)
            {
                this.RepresentedAnimator.enabled = false;
            }
        }

        /**
         * This is called when the visualization element is destroyed and required to clean up any changes
         * on the original element. In this case, it needs to stop the animation.
         */
        public override void OnDestroy()
        {
            this.StopAnimation();
            base.OnDestroy();
        }
    }
}
