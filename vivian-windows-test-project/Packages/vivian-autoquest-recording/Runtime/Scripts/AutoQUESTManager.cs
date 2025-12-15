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

using de.ugoe.cs.autoquest.genericeventmonitor;
using de.ugoe.cs.vivian.core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace de.ugoe.cs.vivian.autoquest
{
    public class AutoQUESTManager : MonoBehaviour
    {
        // the prefab of the AutoQUEST monitor to instantiate for logging
        public GameObject AutoQUESTMonitorPrefab;

        // the autoquest monitor instance
        private AutoQUESTGenericMonitorUnity AutoQUESTMonitor;

        // Start is called before the first frame update
        void Start()
        {
            GameObject autoQuestGO = Instantiate(AutoQUESTMonitorPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            autoQuestGO.transform.parent = this.transform;

            AutoQUESTMonitor = autoQuestGO.GetComponent<AutoQUESTGenericMonitorUnity>();
            AutoQUESTMonitor.oneClientIdPerSession = true;

            StartCoroutine(this.ExtendInteractionElements());
        }

        IEnumerator ExtendInteractionElements()
        {
            List<VirtualPrototype> registeredPrototypes = new List<VirtualPrototype>();
            VirtualPrototype[] prototypes = Resources.FindObjectsOfTypeAll<VirtualPrototype>();

            while ((registeredPrototypes.Count == 0) && (prototypes.Length > 0))
            {
                prototypes = Resources.FindObjectsOfTypeAll<VirtualPrototype>();

                foreach (VirtualPrototype prototype in prototypes)
                {
                    if (!registeredPrototypes.Contains(prototype) && prototype.Loaded)
                    {
                        VirtualPrototypeElement[] elements = Utils.GetVirtualPrototypeElements(prototype);

                        foreach (VirtualPrototypeElement element in elements)
                        {
                            element.VPElementEvent += HandleVPElementEvent;
                        }

                        prototype.StateMachine.StateChangeEvent += HandleStateChangeEvent;

                        registeredPrototypes.Add(prototype);
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        /**
         * 
         */
        internal void HandleVPElementEvent(object source, VPElementEvent elementEvent)
        {
            KeyValuePair<string, string>[] transformedParameters = new KeyValuePair<string, string>[elementEvent.ParameterValues.Length];

            for (int i = 0; i < elementEvent.ParameterValues.Length; i++)
            {
                KeyValuePair<EventParameterSpec, float> paramValue = elementEvent.ParameterValues[i];
                transformedParameters[i] = new KeyValuePair<string, string>(paramValue.Key.ToString(), paramValue.Value.ToString());
            }

            AutoQUESTMonitor.LogEvent(elementEvent.EventType, elementEvent.Element.RepresentedObject, transformedParameters);
        }

        /**
         * 
         */
        internal void HandleStateChangeEvent(object source, StateChangeEvent stateChangeEvent)
        {
            AutoQUESTMonitor.LogEvent("stateChange_" + stateChangeEvent.StateName,
                                      new KeyValuePair<string, string>("newState", stateChangeEvent.StateName));
        }
    }
}
