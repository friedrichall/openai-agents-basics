// Copyright 2020 Patrick Harms
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
    public class SoundSourceElement : VisualizationElement<SoundSourceSpec, float>
    {
        /** the represented game object */
        private GameObject representedGameObject;

        /** the asset bundle to use for loading audio clips */
        private IResourceLoader ResourceLoader;

        /** sound file */
        private AudioSource audioSource;

        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal new void Initialize(SoundSourceSpec spec, GameObject representedObject)
        {
            throw new System.NotSupportedException("you need to call the other initialize method for this component");
        }

        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal void Initialize(SoundSourceSpec spec, GameObject representedObject, IResourceLoader resourceLoader)
        {
            base.Initialize(spec, representedObject);
            this.representedGameObject = representedObject;

            if (this.representedGameObject == null)
            {
                throw new ArgumentException("represented object with name " + spec.Name +
                                            " does not exist");
            }

            this.ResourceLoader = resourceLoader;

            this.audioSource = this.gameObject.AddComponent<AudioSource>();
            this.audioSource.playOnAwake = false;
        }

        /**
         * Called to visualize any value
         */
        public override void Visualize(object value)
        {
            if (value is bool)
            {
                this.Visualize((bool)value);
            }
            else if (value is float)
            {
                this.Visualize((float)value);
            }
            else if (value is string)
            {
                this.Visualize((string)value);
            }
            else if (value is FileVisualizationSpec)
            {
                this.Visualize((FileVisualizationSpec)value);
            }
            else
            {
                throw new System.NotSupportedException("cannot visualize values of type " + value.GetType());
            }
        }

        /**
         * Called to visualize a bool value
         */
        public override void Visualize(bool value)
        {
            this.Value = value ? 1.0f : 0.0f;
            PlaySound();
        }

        /**
         * Called to visualize a float value
         */
        public override void Visualize(float value)
        {
            this.Value = value;
            PlaySound();
        }

        /**
         * Called to visualize a FileVisualizationSpec
         */
        internal void Visualize(FileVisualizationSpec value)
        {
            this.audioSource.clip = this.ResourceLoader.LoadAsset<AudioClip>(value.FileName);
            this.Value = 1.0f;
            PlaySound();
        }

        /**
         * Convenience method to play sound or not depending on the internal value
         */
        private void PlaySound()
        {
            this.audioSource.volume = this.Value;
            if ((this.Value > 0) && !this.audioSource.isPlaying)
            {
                this.audioSource.Play();
            }
        }

        /**
         * This is called when the visualization element is destroyed and required to clean up any changes
         * on the original element. In this case, it needs to stop playing the sound.
         */
        public override void OnDestroy()
        {
            this.audioSource.Stop();
            base.OnDestroy();
        }
    }
}
