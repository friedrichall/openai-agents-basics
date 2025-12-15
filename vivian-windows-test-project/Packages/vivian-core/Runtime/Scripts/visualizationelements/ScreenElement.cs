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
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace de.ugoe.cs.vivian.core
{
    /**
     * This class represents a screen for visualizing content
     */
    public class ScreenElement : VisualizationElement<ScreenSpec, string>
    {
        /** the image component used for visualization */
        private RawImage Image;

        /** the content of the currently shown file */
        private Texture2D ImageContent = null;

        /** the video player component used for visualization */
        private VideoPlayer VideoPlayer;

        /** the content of the currently shown file */
        private VideoClip VideoContent;

        /** the URL of the currently shown video file */
        private String VideoURL;

        /** the list of visualized texts */
        private readonly List<GameObject> Texts = new List<GameObject>();

        /** the asset bundle to use for loading screen content */
        private IResourceLoader ResourceLoader;

        /** the currently selected screen content */
        private string ScreenContent = null;

        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal new void Initialize(ScreenSpec spec, GameObject representedObject)
        {
            throw new System.NotSupportedException("you need to call the other initialize method for this component");
        }

        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal void Initialize(ScreenSpec spec, GameObject representedObject, IResourceLoader resourceLoader)
        {
            // First initialize the canvas and its transform, afterwards initialize the base object.
            // only through this, the case class will calcuate a correct collider
            this.InitializeCanvas(spec, representedObject);
            base.Initialize(spec, representedObject);

            this.ResourceLoader = resourceLoader;

            this.HideContent();
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
            else if (value is Texture2D)
            {
                this.Visualize((Texture2D)value);
            }
            else if (value is ScreenContentVisualizationSpec)
            {
                this.Visualize((ScreenContentVisualizationSpec)value);
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
            if (value)
            {
                ShowContent();
            }
            else
            {
                HideContent();
            }
        }

        /**
         * Called to visualize a float value
         */
        public override void Visualize(float value)
        {
            if (value > 0.0)
            {
                ShowContent();
            }
            else
            {
                HideContent();
            }
        }

        /**
         * Called to visualize a string value, which is supposed to be screen content
         */
        internal void Visualize(string value)
        {
            try
            {
                this.ImageContent = this.ResourceLoader.LoadAsset<Texture2D>(value);
                this.VideoContent = null;
                this.VideoURL = null;
            }
            catch (Exception)
            {
                if (!value.Contains("://"))
                {
                    this.VideoContent = this.ResourceLoader.LoadAsset<VideoClip>(value);
                    this.ImageContent = null;
                    this.VideoURL = null;
                }
                else
                {
                    this.VideoURL = value;
                    this.ImageContent = null;
                    this.VideoContent = null;
                }
            }

            this.ScreenContent = value;
            ShowContent();
        }

        /**
         * Called to visualize a Texture2D
         **/
        public void Visualize(Texture2D value)
        {
            this.ImageContent = value;
            this.VideoContent = null;
            this.VideoURL = null;
            
            ShowContent();
        }

        /**
         * Called to visualize a complete ScreenContentVisualizationSpec including a file and texts
         */
        internal void Visualize(ScreenContentVisualizationSpec value)
        {
            // set the main view
            if (value.FileName != null)
            {
                this.Visualize((string)value.FileName);
            }

            // destroy existing texts
            for (int i = 0; i < this.Texts.Count; i++)
            {
                Destroy(this.Texts[i]);
            }

            // create new ones if required
            if (value.Texts != null)
            {
                for (int i = 0; i < value.Texts.Length; i++)
                {
                    this.Texts.Add(CreateText(value.Texts[i], i));
                }
            }

            ShowContent();
        }

        /**
         * convenience method to actually show the image content
         */
        private void ShowContent()
        {
            if (this.ImageContent != null)
            {
                this.VideoPlayer.enabled = false;
                this.Image.texture = this.ImageContent;
            }
            else if (this.VideoContent != null)
            {
                this.VideoPlayer.enabled = true;
                this.VideoPlayer.url = null;
                this.Image.texture = this.VideoPlayer.targetTexture;
                this.VideoPlayer.clip = this.VideoContent;
                this.VideoPlayer.source = VideoSource.VideoClip;
                this.VideoPlayer.Play();
            }
            else if (this.VideoURL != null)
            {
                this.VideoPlayer.enabled = true;
                this.VideoPlayer.clip = null;
                this.Image.texture = this.VideoPlayer.targetTexture;
                this.VideoPlayer.url = this.VideoURL;
                this.VideoPlayer.source = VideoSource.Url;
                this.VideoPlayer.Play();
            }

            this.Image.color = Color.white;
            this.Value = this.ScreenContent;
        }

        /**
         * convenience method to actually hide the image content
         */
        private void HideContent()
        {
            if (this.ImageContent != null)
            {
                this.Image.texture = null;
            }
            else
            {
                this.VideoPlayer.Stop();
                this.VideoPlayer.enabled = false;
            }

            this.Image.color = Color.clear;
            this.Value = null;
        }

        /**
         * Convenience method to initialize the canvas and the rect transform
         */
        private void InitializeCanvas(ScreenSpec spec, GameObject representedObject)
        {
            // add a canvas to draw the screen content onto.
            Canvas canvas = this.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // scale the canvas to that it fits the object size
            MeshFilter meshFilter = representedObject.GetComponent<MeshFilter>();

            if ((meshFilter == null) || (meshFilter.mesh == null))
            {
                throw new ArgumentException("The screen element " + base.Spec.Name + " does not have a mesh. " +
                                            "It requires a mesh, because otherwise there is nothing to project content on.");
            }

            RectTransform transform = this.gameObject.GetComponent<RectTransform>();

            // get the surface
            Surface surface = Utils.GetSurfaceFromMesh(meshFilter, spec.Plane, representedObject.transform.InverseTransformDirection(Vector3.up));

            // adjust the scale of the transform to match the determined surface divided by the resolution
            transform.localScale = new Vector3(surface.xAxis.magnitude / spec.Resolution.x,
                                               surface.yAxis.magnitude / spec.Resolution.y,
                                               1);

            // set the resolution of the canvas to the provided one
            transform.sizeDelta = spec.Resolution;

            // the canvas will not have the right position and rotation yet. So move it accordingly and use the
            // information of the surface for this. Thereby compensate for the usually wrong orientation of the
            // canvas (away from camera)
            transform.localPosition = surface.center;
            transform.localRotation = Quaternion.LookRotation(-spec.Plane, representedObject.transform.InverseTransformDirection(Vector3.up));

            // move the canvas so that it is minimally above the screen so that it is definitely visible
            transform.Translate(representedObject.transform.TransformDirection(spec.Plane).normalized * 0.0005f, Space.World);

            // add an image object to draw image contents
            GameObject imageObject = new GameObject("ScreenContent" + spec.Name);
            imageObject.transform.parent = this.gameObject.transform;

            this.Image = imageObject.AddComponent<RawImage>();
            transform = imageObject.GetComponent<RectTransform>();
            transform.sizeDelta = spec.Resolution;
            transform.localPosition = Vector3.zero;
            transform.localScale = new Vector3(1, 1, 1);
            transform.rotation = new Quaternion();

            // add a video player to potentially also play videos on the image
            RenderTexture renderTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            renderTexture.Create();

            this.VideoPlayer = this.Image.gameObject.AddComponent<VideoPlayer>();

            // render the video to the render texture
            this.VideoPlayer.targetTexture = renderTexture;

            // Match the ratio to the screen's
            this.VideoPlayer.aspectRatio = VideoAspectRatio.Stretch;
            this.VideoPlayer.enabled = false;
        }

        /**
         * Convenience method to create some text element
         */
        private GameObject CreateText(TextSpec spec, int index)
        {
            GameObject textObject = new GameObject("ScreenContentText" + index);
            textObject.transform.parent = this.gameObject.transform;

            Text text = textObject.AddComponent<Text>();
            text.font = Font.CreateDynamicFontFromOSFont("OpenSans", spec.Size);
            text.text = spec.Text;
            text.fontSize = spec.Size;

            Color newColor = new Color();
            if (ColorUtility.TryParseHtmlString(spec.Color, out newColor))
            {
                text.color = newColor;
            }
            else
            {
                Debug.LogWarning("color code \"" + spec.Color + "\" of text \"" + spec.Text +
                                 "\" not recognized. Using default black instead.");
                text.color = Color.black;
            }

            text.alignment = TextAnchor.MiddleCenter;

            RectTransform transform = textObject.GetComponent<RectTransform>();
            transform.sizeDelta = base.Spec.Resolution;
            transform.localPosition = new Vector3(spec.Position.x - (base.Spec.Resolution.x / 2),
                                                  -spec.Position.y + (base.Spec.Resolution.y / 2), 0);
            transform.localScale = new Vector3(1, 1, 1);
            transform.rotation = new Quaternion();

            return textObject;
        }

        /**
         * This is called when the visualization element is destroyed and required to clean up any changes
         * on the original element. In this case, it needs to clean the created screen objects.
         */
        public override void OnDestroy()
        {
            Destroy(this.Image.gameObject);
            Destroy(this.GetComponent<Canvas>());

            base.OnDestroy();
        }
    }
}
