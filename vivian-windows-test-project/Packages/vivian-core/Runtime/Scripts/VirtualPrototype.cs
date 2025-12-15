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
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace de.ugoe.cs.vivian.core
{
    public class VirtualPrototype : MonoBehaviour
    {
        // Reference to the bundle url that contains the prototype.
        public string BundleURL;

        // name of the prototype prefab to load.
        public string PrototypePrefabName;

        // Flag indicating if the ObjectPositioner script triggers the instantiation of the VP 
        [NonSerialized] public bool InstantiatedByObjectPositioner = false;

        // flag indicating if the prototype was loaded completely
        public bool Loaded { get; private set; } = false;

        // the state machine of the prototype
        public StateMachine StateMachine { get; private set; }

        // Reference to the actual virtual prototype
        private GameObject prototypeInstance;

        // information about current interactions
        private Dictionary<int, Interaction> currentInteractions = new Dictionary<int, Interaction>();
        
        // The Virtual Prototype's prefab
        private GameObject virtualPrototypePrefab;
        
        // Flag indicating if loading of the resources is done
        private bool isLoadingReady;

        // Arrays loaded with the info parsed from the JSON files
        private InteractionElementSpec[] interactionElementSpecs;

        private VisualizationElementSpec[] visualizationElementSpecs;

        private VisualizationArraySpec[] visualizationArraySpecs;

        // all instantiated prototype elements
        private List<VirtualPrototypeElement> instantiatedPrototypeElements = new List<VirtualPrototypeElement>();

        // all instantiated visualization array (cause they are not prototype elements)
        private VisualizationArray[] instantiatedVisualizationArrays;

        private StateSpec[] states;

        private TransitionSpec[] transitionSpecs;
        
        private IResourceLoader resourceLoader;


        // This script will simply instantiate the Prefab when the game starts.
        void Start()
        {
            StartCoroutine(StartPrototypeCoroutine());
        }

        /**
         * coroutine to load the virtual prototype and also start it, if it is not started externally
         */
        private IEnumerator StartPrototypeCoroutine()
        {
            yield return LoadPrototypeResources();

            // give Unity some time for setting up everything
            yield return null;

            // Once the resources are loaded, instantiate the prototype 
            // Only instantiate if not triggered by the ObjectPositioner 
            if (!this.InstantiatedByObjectPositioner)
            {
                InstantiatePrototype();
            }
        }

        /**
         * This is called when the user starts interacting with this element
         */
        public virtual void TriggerInteractionStarts(Pose pose, int poseId)
        {
            EnsurePoseIdDoesNotInteractYet(poseId);

            VirtualPrototypeElement vpElement = this.GetSelectedObject(pose);

            if (vpElement != null)
            {
                StartInteraction(vpElement, pose, poseId);
            }
            else
            {
                Debug.Log("the pose intended to start an interaction does not select an element of prototype " + this.prototypeInstance);
            }
        }

        private void EnsurePoseIdDoesNotInteractYet(int poseId)
        {
            if (this.currentInteractions.ContainsKey(poseId))
            {
                throw new ArgumentException("the given pose id " + poseId + " is already in interaction with " +
                                            this.currentInteractions[poseId]);
            }
        }

        private void StartInteraction(VirtualPrototypeElement vpElement, Pose pose, int poseId)
        {
            Interaction interaction = getExistingInteractionWithVPElement(vpElement);
            if (interaction == null)
            {
                interaction = CreateInteractionForPoseId(vpElement, poseId);
            }
            else
            {
                StoreInteractionForNewPose(interaction, poseId);
            }

            interaction.Start(pose, poseId);
        }

        private Interaction getExistingInteractionWithVPElement(VirtualPrototypeElement vpElement)
        {
            foreach (Interaction interaction in this.currentInteractions.Values)
            {
                if (interaction.vpElement == vpElement)
                {
                    return interaction;
                }
            }

            return null;
        }

        private Interaction CreateInteractionForPoseId(VirtualPrototypeElement vpElement, int poseId)
        {
            Interaction interaction = new MultiPoseInteraction(vpElement);
            this.currentInteractions.Add(poseId, interaction);
            return interaction;
        }

        private void StoreInteractionForNewPose(Interaction interaction, int poseId)
        {
            this.currentInteractions.Add(poseId, interaction);
        }

        /**
         * This is called when the user continues interacting with this element
         */
        public virtual void TriggerInteractionContinues(Pose pose, int poseId)
        {
            if (this.currentInteractions.TryGetValue(poseId, out Interaction interaction))
            {
                interaction.Continue(pose, poseId);
            }
            else
            {
                Debug.Log("the pose id is currently not in interaction");
            }
        }

        /**
         * This is called when the user ends interacting with this element
         */
        public virtual void TriggerInteractionEnds(Pose pose, int poseId)
        {
            if (this.currentInteractions.TryGetValue(poseId, out Interaction interaction))
            {
                interaction.Stop(pose, poseId);
                this.currentInteractions.Remove(poseId);
            }
            else
            {
                Debug.Log("the pose id is currently not in interaction");
            }
        }

        /**
         * 
         */
        private VirtualPrototypeElement GetSelectedObject(Pose pose)
        {
            VirtualPrototypeElement closestVPElement = null;

            RaycastHit[] hits = Physics.RaycastAll(pose.position, pose.forward);

            if (hits != null)
            {
                //Debug.Log("hits " + hits.Length);
                InteractionElement closestInteractionElement = null;
                VisualizationElement closestVisualizationElement = null;

                float closestInteractionElementDistance = float.MaxValue;
                float closestVisualizationElementDistance = float.MaxValue;
                float closestVPElementDistance = float.MaxValue;

                foreach (RaycastHit hit in hits)
                {
                    //Debug.Log("checking " + hit.collider);
                    VirtualPrototypeElement vpElement = hit.collider.GetComponent<VirtualPrototypeElement>();

                    if ((vpElement != null) && this.instantiatedPrototypeElements.Contains(vpElement))
                    {
                        if ((vpElement is InteractionElement) && (hit.distance < closestInteractionElementDistance))
                        {
                            //Debug.Log("variant 1");
                            closestInteractionElement = (InteractionElement)vpElement;
                            closestInteractionElementDistance = hit.distance;
                        }
                        else if ((vpElement is VisualizationElement) && (hit.distance < closestVisualizationElementDistance))
                        {
                            //Debug.Log("variant 2");
                            closestVisualizationElement = (VisualizationElement)vpElement;
                            closestVisualizationElementDistance = hit.distance;
                        }
                        else if (hit.distance < closestVPElementDistance)
                        {
                            //Debug.Log("variant 3");
                            closestVPElement = vpElement;
                            closestVPElementDistance = hit.distance;
                        }
                    }
                }

                //Debug.Log(closestInteractionElement + "  " + closestInteractionElementDistance + "  " + closestVisualizationElement + "  " + closestVisualizationElementDistance);

                if (closestVisualizationElementDistance < float.MaxValue)
                {
                    // this penalty is used to ensure that interaction elements being on the same distance than visualization
                    // elements are preferred.
                    closestVisualizationElementDistance += 0.001f;
                }

                if ((closestInteractionElement != null) &&
                    (closestInteractionElementDistance <= closestVisualizationElementDistance))
                {
                    closestVPElement = closestInteractionElement;
                }

                if ((closestVisualizationElement != null) &&
                    (closestVisualizationElementDistance <= closestInteractionElementDistance))
                {
                    closestVPElement = closestVisualizationElement;
                }
            }

            return closestVPElement;
        }

        /**
         * Loads the resources of the VP (Prefab and JSON files)
         * If InstantiatedByObjectPositioner is false, this method also triggers the instantiation of the VP 
         */
        private IEnumerator LoadPrototypeResources()
        {
            resourceLoader = null;

            if (this.BundleURL.StartsWith("zipContentBase64://"))
            {
                resourceLoader = new Base64ZipContentResourceLoader(this.BundleURL.Substring("zipContentBase64://".Length));
            }
            else if (this.BundleURL.Contains("://") ||
                     this.BundleURL.StartsWith("AssetBundles/") ||
                     this.BundleURL.StartsWith("StreamingAssets/") ||
                     Regex.IsMatch(this.BundleURL, @"^\d+"))
            {
                resourceLoader = new AssetBundleResourceLoader(this.BundleURL);
            }
            else
            {
                resourceLoader = new PackedResourceLoader(this.BundleURL);
            }

            yield return resourceLoader.Init();

            try
            {
                this.virtualPrototypePrefab = resourceLoader.LoadAsset<GameObject>(this.PrototypePrefabName);
            }
            catch (ArgumentException)
            {
                // ignore, but ensure that the virtual prototype prefab is null. It is allowed to be null in case it is already
                // instatiated as a child
                this.virtualPrototypePrefab = null;
            }
            
            
            Debug.Log("loaded resources");

            // load all elements of the prototype
            this.interactionElementSpecs =
                this.GetFromJSON<InteractionElementSpecArrayJSONWrapper>(resourceLoader, "InteractionElements.json").GetSpecsArray();

            this.visualizationElementSpecs =
                this.GetFromJSON<VisualizationElementSpecArrayJSONWrapper>(resourceLoader, "VisualizationElements.json").GetSpecsArray();

            this.visualizationArraySpecs =
                this.GetFromJSON<VisualizationArraySpecArrayJSONWrapper>(resourceLoader, "VisualizationArrays.json").GetSpecsArray(visualizationElementSpecs);

            VisualizationSpec[] allVisualizationElements =
                new VisualizationSpec[visualizationElementSpecs.Length + visualizationArraySpecs.Length];

            visualizationElementSpecs.CopyTo(allVisualizationElements, 0);
            visualizationArraySpecs.CopyTo(allVisualizationElements, visualizationElementSpecs.Length);

            Debug.Log("loaded prototype configuration");
            yield return null;

            // load the state machine
            this.states =
                this.GetFromJSON<StateSpecArrayJSONWrapper>(resourceLoader, "States.json").GetSpecsArray(interactionElementSpecs, allVisualizationElements);

            this.transitionSpecs =
                this.GetFromJSON<TransitionSpecArrayJSONWrapper>(resourceLoader, "Transitions.json").GetSpecsArray(states, interactionElementSpecs);

            Debug.Log("loaded prototype statemachine");
            yield return null;

            this.isLoadingReady = true;
        }

        /*
         * Instantiates the Virtual Prototype
         */
        public void InstantiatePrototype()
        {
            // if there is already an instance of the prefab as the child of this, reuse it, else instantiate it
            bool foundPrefabInstance = false;

            string nameToSearchFor = this.virtualPrototypePrefab == null ? this.PrototypePrefabName : this.virtualPrototypePrefab.name;

            for (int i = 0; i < this.transform.childCount; i++) {
                GameObject child = this.transform.GetChild(i).gameObject;
                if (child.name == nameToSearchFor)
                {
                    this.prototypeInstance = child;
                    foundPrefabInstance = true;
                    Debug.Log("There is already an object with the same name of the prefab to be instantiated (" +
                              nameToSearchFor + "). Reusing the object as prototype instance and " +
                              "crossing fingers that this object is the right one.");
                    break;
                }
            }

            if (!foundPrefabInstance)
            {
                if (this.virtualPrototypePrefab == null)
                {
                    throw new ArgumentException("The prefab to use as virtual prototype is supposed to be named " +
                                                nameToSearchFor + " but an object with this name was neither found in the bundle " +
                                                "at URL " + this.BundleURL + " nor as child of the virtual prototype script. Please " +
                                                "add a prefab with this name to the bundle or add a child object with that name to " +
                                                "the scene");
                }

                // Instantiate at position (0, 0, 0) and zero rotation.
                this.prototypeInstance = Instantiate(this.virtualPrototypePrefab, new Vector3(0, 0, 0), Quaternion.identity);
                //this.PrototypeInstance = Instantiate(virtualPrototypePrefab, new Vector3(0, 0, 0), Quaternion.Euler(new Vector3(-5, -45, 10)));
                this.prototypeInstance.name = this.virtualPrototypePrefab.name;
                this.prototypeInstance.transform.SetParent(this.transform, false);
            }

            // finally, create the state machine, register all prototype elements and start the state machine
            this.StateMachine = new StateMachine(transitionSpecs, this);

            // determine all children of the prototype model to extend them depending on the configuration
            Dictionary<string, List<GameObject>> allPrototypeElements = new Dictionary<string, List<GameObject>>();
            this.GetAllPrototypeElements(allPrototypeElements, this.prototypeInstance, new List<string> { this.prototypeInstance.name });

            // actually create and store the virtual prototype elements
            VisualizationElement[] visualizationElements = CreateVisualizationElements(visualizationElementSpecs, allPrototypeElements, resourceLoader);
            instantiatedVisualizationArrays = CreateVisualizationArrays(visualizationArraySpecs);
            InteractionElement[] interactionElements = CreateInteractionElements(interactionElementSpecs, allPrototypeElements);
            instantiatedPrototypeElements.AddRange(visualizationElements);
            instantiatedPrototypeElements.AddRange(interactionElements);
            CreateOtherVirtualPrototypeElements(this.prototypeInstance.transform, instantiatedPrototypeElements);

            // TODO lightArrays must also be destroyed
            IVisualization[] visualizations = new IVisualization[visualizationElements.Length + instantiatedVisualizationArrays.Length];
            visualizationElements.CopyTo(visualizations, 0);
            instantiatedVisualizationArrays.CopyTo(visualizations, visualizationElements.Length);

            Debug.Log("created elements");
            if (states.Length > 0)
            {
                this.StateMachine.Start(states[0], interactionElements, visualizations);
                Debug.Log("started state machine");
            }
            else
            {
                Debug.Log("state machine not started as no states are configured");
            }

            this.Loaded = true;
        } 

        /*
         * 
         */
        private T GetFromJSON<T>(IResourceLoader resourceLoader, string fileName)
        {
            TextAsset textAsset = resourceLoader.LoadAsset<TextAsset>(fileName);
            if (textAsset == null)
            {
                throw new ArgumentException("no " + fileName + " found in prototype bundle");
            }

            return JsonUtility.FromJson<T>(textAsset.text);
        }

        /*
         * stores for each possible path of an element an entry in the provided "allElements" parameter. For example,
         * if a VP Element is located at "CoffeeMachine/ButtonGroup/Button1", then there will be at least three entrie
         * in the dictionary, one for "Button1", one for "ButtonGroup/Button1" and one for "CoffeeMachine/ButtonGroup/Button1".
         * In addition, a path may not be unique. For example, there may be multiple objects named "Button1". Hence,
         * there is a list of elements for each path.
         */
        private void GetAllPrototypeElements(Dictionary<string, List<GameObject>> allElements, GameObject parent, List<string> parentPath)
        {
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                GameObject child = parent.transform.GetChild(i).gameObject;

                // for the name, create an entry in the elements
                if (!allElements.TryGetValue(child.name, out List<GameObject> existing))
                {
                    existing = new List<GameObject>();
                    allElements.Add(child.name, existing);
                }

                existing.Add(child);

                // for every possible and allowed path, create an entry in the elements
                for (int j = parentPath.Count - 1; j >= 0; j--)
                {
                    string pathVariant = child.name;

                    for (int k = parentPath.Count - 1; k >= j; k--)
                    {
                        pathVariant = parentPath[k] + "/" + pathVariant;
                    }

                    if (!allElements.TryGetValue(pathVariant, out existing))
                    {
                        existing = new List<GameObject>();
                        allElements.Add(pathVariant, existing);
                    }
                    
                    existing.Add(child);
                }

                parentPath.Add(child.name);
                this.GetAllPrototypeElements(allElements, child, parentPath);
                parentPath.RemoveAt(parentPath.Count - 1);
            }
        }

        /*
         * 
         */
        private InteractionElement[] CreateInteractionElements(InteractionElementSpec[] elementSpecs, Dictionary<string, List<GameObject>> allPrototypeElements)
        {
            // we first need to order the interaction elements so that we create them starting with the deepest nodes
            // and finishing with the highest parent nodes
            List<KeyValuePair<string, object>> paths = new List<KeyValuePair<string, object>>();

            foreach (InteractionElementSpec elementSpec in elementSpecs)
            {
                paths.Add(new KeyValuePair<string, object>(elementSpec.Name, elementSpec));
            }

            List<KeyValuePair<KeyValuePair<string, object>, GameObject>> sortedElements =
                this.getElementsSortedByDepth(paths.ToArray(), allPrototypeElements);

            List<InteractionElement> instantiatedElements = new List<InteractionElement>();

            // after sorting we can now create the interaction elements in the required order
            foreach (KeyValuePair<KeyValuePair<string, object>, GameObject> element in sortedElements)
            {
                InteractionElement interactionElement = CreateInteractionElement((InteractionElementSpec) element.Key.Value, element.Value);

                // register the state machine to handle events
                interactionElement.VPElementEvent += this.StateMachine.HandleInteractionEvent;

                instantiatedElements.Add(interactionElement);
            }

            return instantiatedElements.ToArray();
        }

        /*
         * 
         */
        public static InteractionElement CreateInteractionElement(InteractionElementSpec elementSpec, GameObject effectiveElement)
        {
            GameObject interactionElementGo = new GameObject("ColliderObject" + elementSpec.Name);

            interactionElementGo.transform.SetParent(effectiveElement.transform, false);

            InteractionElement interactionElement;

            if (elementSpec is ToggleButtonSpec)
            {
                interactionElement = interactionElementGo.AddComponent<ToggleButtonElement>();
                ((ToggleButtonElement)interactionElement).Initialize((ToggleButtonSpec)elementSpec, effectiveElement);
            }
            else if (elementSpec is ButtonSpec)
            {
                interactionElement = interactionElementGo.AddComponent<ButtonElement>();
                ((ButtonElement)interactionElement).Initialize((ButtonSpec)elementSpec, effectiveElement);
            }
            else if (elementSpec is SliderSpec)
            {
                interactionElement = interactionElementGo.AddComponent<SliderElement>();
                ((SliderElement)interactionElement).Initialize((SliderSpec)elementSpec, effectiveElement);
            }
            else if (elementSpec is RotatableSpec)
            {
                interactionElement = interactionElementGo.AddComponent<RotatableElement>();
                ((RotatableElement)interactionElement).Initialize((RotatableSpec)elementSpec, effectiveElement);
            }
            else if (elementSpec is TouchAreaSpec)
            {
                interactionElement = interactionElementGo.AddComponent<TouchElement>();
                ((TouchElement)interactionElement).Initialize((TouchAreaSpec)elementSpec, effectiveElement);
            }
            else if (elementSpec is MovableSpec)
            {
                interactionElement = interactionElementGo.AddComponent<MovableElement>();
                ((MovableElement)interactionElement).Initialize((MovableSpec)elementSpec, effectiveElement);
            }
            else
            {
                throw new NotSupportedException("interaction element spec of type " + elementSpec.GetType() + " not supported");
            }

            return interactionElement;
        }

        /*
         * 
         */
        private VisualizationElement[] CreateVisualizationElements(VisualizationElementSpec[] elementSpecs,
                                                                   Dictionary<string, List<GameObject>> allPrototypeElements,
                                                                   IResourceLoader resourceLoader)
        {
            // we first need to order the visualization elements so that we create them starting with the deepest nodes
            // and finishing with the highest parent nodes
            List<KeyValuePair<string, object>> paths = new List<KeyValuePair<string, object>>();

            foreach (VisualizationElementSpec elementSpec in elementSpecs)
            {
                paths.Add(new KeyValuePair<string, object>(elementSpec.Name, elementSpec));
            }

            List<KeyValuePair<KeyValuePair<string, object>, GameObject>> sortedElements =
                this.getElementsSortedByDepth(paths.ToArray(), allPrototypeElements);

            List<VisualizationElement> instantiatedElements = new List<VisualizationElement>();

            // after sorting we can now create the visualization elements in the required order
            foreach (KeyValuePair<KeyValuePair<string, object>, GameObject> element in sortedElements)
            {
                instantiatedElements.Add(CreateVisualizationElement((VisualizationElementSpec) element.Key.Value, element.Value, resourceLoader));
            }

            return instantiatedElements.ToArray();
        }

        /*
         * 
         */
        public static VisualizationElement CreateVisualizationElement(VisualizationElementSpec elementSpec, GameObject effectiveElement, IResourceLoader resourceLoader)
        {
            GameObject visualizationElementGo = new GameObject("VisualizationObject" + elementSpec.Name);

            visualizationElementGo.transform.SetParent(effectiveElement.transform, false);

            VisualizationElement visualizationElement;

            if (elementSpec is LightSpec)
            {
                visualizationElement = visualizationElementGo.AddComponent<LightElement>();
                ((LightElement) visualizationElement).Initialize((LightSpec)elementSpec, effectiveElement);
            }
            else if (elementSpec is ScreenSpec)
            {
                visualizationElement = visualizationElementGo.AddComponent<ScreenElement>();
                ((ScreenElement) visualizationElement).Initialize((ScreenSpec)elementSpec, effectiveElement, resourceLoader);
            }
            else if (elementSpec is ParticleSpec)
            {
                visualizationElement = visualizationElementGo.AddComponent<ParticleElement>();
                ((ParticleElement) visualizationElement).Initialize((ParticleSpec)elementSpec, effectiveElement);
            }
            else if (elementSpec is AnimationSpec)
            {
                visualizationElement = visualizationElementGo.AddComponent<AnimationElement>();
                ((AnimationElement) visualizationElement).Initialize((AnimationSpec)elementSpec, effectiveElement);
            }
            else if (elementSpec is AppearingObjectSpec)
            {
                visualizationElement = visualizationElementGo.AddComponent<AppearingElement>();
                ((AppearingElement) visualizationElement).Initialize((AppearingObjectSpec)elementSpec, effectiveElement);
            }
            else if (elementSpec is SoundSourceSpec)
            {
                visualizationElement = visualizationElementGo.AddComponent<SoundSourceElement>();
                ((SoundSourceElement) visualizationElement).Initialize((SoundSourceSpec)elementSpec, effectiveElement, resourceLoader);
            }
            else
            {
                throw new NotSupportedException("visualization spec of type " + elementSpec.GetType() + " not supported");
            }

            return visualizationElement;
        }

        /*
         * 
         */
        private VisualizationArray[] CreateVisualizationArrays(VisualizationArraySpec[] arraySpecs)
        {
            List<VisualizationArray> visualizationArrays = new List<VisualizationArray>();

            foreach (VisualizationArraySpec arraySpec in arraySpecs)
            {
                GameObject visualizationArray = new GameObject("VisualizationArray" + arraySpec.Name);

                if (arraySpec is LightArraySpec)
                {
                    LightArrayElement lightArray = visualizationArray.AddComponent<LightArrayElement>();
                    lightArray.Initialize((LightArraySpec)arraySpec, this.prototypeInstance);

                    visualizationArrays.Add(lightArray);
                }
                else
                {
                    throw new NotSupportedException("cannot handle visualization arrays of type " + arraySpec.GetType());
                }

                visualizationArray.transform.SetParent(this.transform, false);
            }

            return visualizationArrays.ToArray();
        }

        /*
         * 
         */
        private void CreateOtherVirtualPrototypeElements(Transform transform, List<VirtualPrototypeElement> results)
        {
            bool isInteractionElement = false;

            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).GetComponent<VirtualPrototypeElement>() != null)
                {
                    isInteractionElement = true;
                    break;
                }
            }

            if (!isInteractionElement)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    CreateOtherVirtualPrototypeElements(transform.GetChild(i), results);
                }

                GameObject vpElementObject = new GameObject("VPElementObject" + transform.name);

                vpElementObject.transform.SetParent(transform, false);
                VirtualPrototypeElement vpElement = vpElementObject.AddComponent<VirtualPrototypeElement>();
                vpElement.Initialize(transform.gameObject);

                results.Add(vpElement);
            }
        }


        /*
         * determines a sorted list of elements ordered by their depth in the tree
         */
        private List<KeyValuePair<KeyValuePair<string, object>, GameObject>> getElementsSortedByDepth(KeyValuePair<string, object>[] elementPathsAndPayloads,
                                                                                                      Dictionary<string, List<GameObject>> allPrototypeElements)
        {
            List<KeyValuePair<int, KeyValuePair<KeyValuePair<string, object>, GameObject>>> sortedElements =
                new List<KeyValuePair<int, KeyValuePair<KeyValuePair<string, object>, GameObject>>>();

            foreach (KeyValuePair<string, object> elementPathAndPayload in elementPathsAndPayloads)
            {
                allPrototypeElements.TryGetValue(elementPathAndPayload.Key, out List<GameObject> candidates);

                if ((candidates == null) || (candidates.Count < 1))
                {
                    throw new ArgumentException("could not find object with name or path " + elementPathAndPayload.Key);
                }

                if (candidates.Count > 1)
                {
                    Debug.LogWarning("path " + elementPathAndPayload.Key + " is ambigious. Applying the same configuration for all "
                                     + candidates.Count + " occurrences of that name or path.");
                }

                foreach (GameObject candidate in candidates)
                {
                    int depth = 1;
                    Transform transform = candidate.transform;

                    while ((transform.parent != null) && (transform != this.prototypeInstance.transform))
                    {
                        depth++;
                        transform = transform.parent;
                    }

                    bool added = false;

                    KeyValuePair<KeyValuePair<string, object>, GameObject> element = new KeyValuePair<KeyValuePair<string, object>, GameObject>(elementPathAndPayload, candidate);
                    KeyValuePair<int, KeyValuePair<KeyValuePair<string, object>, GameObject>> entry = new KeyValuePair<int, KeyValuePair<KeyValuePair<string, object>, GameObject>>(depth, element);

                    for (int i = 0; i < sortedElements.Count; i++)
                    {
                        if (depth > sortedElements[i].Key)
                        {
                            sortedElements.Insert(i, entry);
                            added = true;
                            break;
                        }
                    }

                    if (!added)
                    {
                        sortedElements.Add(entry);
                    }
                }
            }

            List<KeyValuePair<KeyValuePair<string, object>, GameObject>> effectiveElements =
                new List<KeyValuePair<KeyValuePair<string, object>, GameObject>>();

            foreach (KeyValuePair<int, KeyValuePair<KeyValuePair<string, object>, GameObject>> element in sortedElements)
            {
                effectiveElements.Add(element.Value);
            }

            return effectiveElements;
        }

        /*
         * 
         */
        public void SetPrototypeActive(bool active)
        {
            prototypeInstance.SetActive(active);
        }

        /*
         * 
         */
        public void SetPrototypePosition(Vector3 position)
        {
            prototypeInstance.transform.position = position;
        }

        /*
         * 
         */
        public void SetPrototypeRotation(Quaternion rotation)
        {
            prototypeInstance.transform.rotation = rotation;
        }

        /*
         * Getter for the VP's prefab
         */
        public GameObject GetVirtualPrototypePrefab()
        {
            return this.virtualPrototypePrefab;
        }

        /*
         * Returns true if the LoadPrototypeResources() function has finished executing
         */
        public bool IsResourceLoadingReady()
        {
            return this.isLoadingReady;
        }

        /*
         * called when this component is destroyed. Removes all virtual prototype elements from the
         * game object representing the prototype
         */
        public void Reset()
        {
            if (this.Loaded)
            {
                Debug.Log("Resetting prototype functionality to its initial state");
                this.Loaded = false;
                this.StateMachine.Stop();
                this.StateMachine = null;
                StartCoroutine(ResetPrototypeCoroutine());
            }
        }

        /*
         * called when this component is destroyed. Removes all virtual prototype elements from the
         * game object representing the prototype
         */
        public void OnDestroy()
        {
            Debug.Log("Destroying prototype functionality including all additional objects");
            this.ClearInstantiatedPrototypeElements();
        }

        /**
         * Coroutine to reset the virtual prototype
         */
        IEnumerator ResetPrototypeCoroutine()
        {
            this.ClearInstantiatedPrototypeElements();

            // give unity some time to clean up
            yield return new WaitForEndOfFrame();

            InstantiatePrototype();
        }

        /*
         * clears all instantiated elements (used as utility for destroying the virtual
         * prototype as well as resetting it)
         */
        private void ClearInstantiatedPrototypeElements()
        {
            Debug.Log("Destroying prototype functionality including all additional objects");

            foreach (VirtualPrototypeElement element in this.instantiatedPrototypeElements)
            {
                Destroy(element.gameObject);
            }

            foreach (VisualizationArray array in instantiatedVisualizationArrays)
            {
                Destroy(array.gameObject);
            }

            this.instantiatedPrototypeElements.Clear();
        }
    }
}
