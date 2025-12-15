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
     * This class represents a light for visualizing values
     */
    public class LightElement : VisualizationElement<LightSpec, float>
    {
        /** the mesh renderer component used for visualization */
        private MeshRenderer MeshRenderer;

        /** the light component used for visualization */
        private Light Light;

        // stores the initial state of the represented ligt, if any
        private bool InitialLightState;

        /**
         * Called to initialize the visualization element with the specification and the represented game object
         */
        internal new void Initialize(LightSpec spec, GameObject representedObject)
        {
            base.Initialize(spec, representedObject);

            this.Light = representedObject.GetComponent<Light>();
            this.InitialLightState = this.Light != null ? this.Light.enabled : false;

            if (this.Light == null)
            {
                // create temporary duplicate to read the mesh filter from
                GameObject duplicate = Instantiate(base.RepresentedObject);
                duplicate.transform.localPosition = Vector3.zero;
                duplicate.transform.localEulerAngles = Vector3.zero;
                duplicate.transform.localScale = Vector3.one;
                
                // combine the meshes of the child meshfilters into a single new mesh
                MeshFilter[] originalMeshFilters = duplicate.GetComponentsInChildren<MeshFilter>();
                CombineInstance[] combine = new CombineInstance[originalMeshFilters.Length * 2];

                if ((originalMeshFilters == null) || (originalMeshFilters.Length <= 0))
                {
                    throw new ArgumentException("The light " + spec.Name + " does not have a mesh. " +
                                                "It requires a mesh, because otherwise there is nothing to glow.");
                }

                // copy the mesh once a bit larger and once a bit smaller, so that the glow is on both sides, inside and outside
                for (int i = 0; i < originalMeshFilters.Length; i++)
                {
                    combine[i].mesh = originalMeshFilters[i].sharedMesh;
                    combine[i].transform = originalMeshFilters[i].transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(1.005f, 1.005f, 1.005f));
                }

                for (int i = 0; i < originalMeshFilters.Length; i++)
                {
                    combine[i + originalMeshFilters.Length].mesh = originalMeshFilters[i].sharedMesh;
                    combine[i + originalMeshFilters.Length].transform = originalMeshFilters[i].transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(0.995f, 0.995f, 0.995f));
                }

                /*GameObject dummy = new GameObject(this.gameObject.name + "-testdummy");
                dummy.transform.SetParent(base.RepresentedObject.transform.parent, false);
                MeshFilter dummyMeshFilter = dummy.AddComponent<MeshFilter>();
                dummyMeshFilter.mesh = new Mesh();
                dummyMeshFilter.mesh.CombineMeshes(combine, true, true);
                MeshRenderer dummyMeshRenderer = dummy.AddComponent<MeshRenderer>();
                dummyMeshRenderer.material = new Material(Shader.Find("UI/Default"));
                dummyMeshRenderer.material.color = Color.gray;
                //dummy.transform.SetParent(base.RepresentedObject.transform, true);*/

                MeshFilter newMeshFilter = this.gameObject.AddComponent<MeshFilter>();
                newMeshFilter.mesh = new Mesh();
                newMeshFilter.mesh.CombineMeshes(combine, true, true);

                // create a new mesh renderer
                this.MeshRenderer = this.gameObject.AddComponent<MeshRenderer>();

                this.MeshRenderer.material = new Material(Shader.Find("UI/Default"));
                //this.Material.SetColor("_Color", Color.clear);
                this.MeshRenderer.material.color = new Color(this.Spec.EmissionColor.r, this.Spec.EmissionColor.g, this.Spec.EmissionColor.b, 0);

                // immediately destroy temporary duplicate (late destroy may be too late, as the VP continues initialization within the same frame)
                DestroyImmediate(duplicate);
            }
        }

        /**
         * Called to visualize a bool value
         */
        public override void Visualize(bool value)
        {
            if (value)
            {
                if (this.MeshRenderer != null)
                {
                    this.MeshRenderer.material.color = new Color(this.Spec.EmissionColor.r, this.Spec.EmissionColor.g, this.Spec.EmissionColor.b, 1);
                }

                if (this.Light != null)
                {
                    this.Light.enabled = true;
                }

                this.Value = 1.0f;
            }
            else
            {
                if (this.MeshRenderer != null)
                {
                    this.MeshRenderer.material.color = new Color(this.Spec.EmissionColor.r, this.Spec.EmissionColor.g, this.Spec.EmissionColor.b, 0);
                }

                if (this.Light != null)
                {
                    this.Light.enabled = false;
                }

                this.Value = 1.0f;
            }
        }

        /**
         * Called to visualize a float value
         */
        public override void Visualize(float value)
        {
            if (value > 0.0)
            {
                if (this.MeshRenderer != null)
                {
                    this.MeshRenderer.material.color = new Color(this.Spec.EmissionColor.r, this.Spec.EmissionColor.g, this.Spec.EmissionColor.b, value);
                }

                if (this.Light != null)
                {
                    this.Light.enabled = true;
                }
            }
            else
            {
                if (this.MeshRenderer != null)
                {
                    this.MeshRenderer.material.color = new Color(this.Spec.EmissionColor.r, this.Spec.EmissionColor.g, this.Spec.EmissionColor.b, 0);
                }

                if (this.Light != null)
                {
                    this.Light.enabled = false;
                }
            }

            this.Value = value;
        }

        /**
         * This is called when the visualization element is destroyed and required to clean up any changes
         * on the original element. In this case, it needs to remove the create mesh and its render or
         * set the represented light to its initial state.
         */
        public override void OnDestroy()
        {
            if (this.MeshRenderer != null)
            {
                Destroy(this.MeshRenderer);
                Destroy(this.GetComponent<MeshFilter>());
            }

            if (this.Light != null)
            {
                this.Light.enabled = this.InitialLightState;
            }

            base.OnDestroy();
        }
    }
}
