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
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace de.ugoe.cs.autoquest.genericeventmonitor
{
    /**
     * This class can be used to monitor the usage of an AR/VR. For this, it needs to be set as a component of
     * a game object in a scene. There is a prefab coming with the script, that contains only the script itself.
     * The script automatically searches the scene for objects of type Selectable, ScrollRect, and EventTrigger
     * and registers with them for any event that they handle. So for example, for any button in the scene, the
     * script registers for PointerClick events. If any of the registered events occurs, the method LogEvent()
     * is called with respective parameters being the event type and the target. The event type is a string
     * denoting which event took place. The event target is the game object on which the event was observed.
     * The events are collected until a number of 10 events were observed or the scene is ending. Then the
     * collected events are sent in JSON format to a monitoring server, which stores them. The server URL can
     * be configured using the public class variable serverURL.
     * The events are always connected to an appId and a clientId. This is required by the server to identify
     * which events belong to which running instance of an AR/VR. The app id is basically the name of the app.
     * It can be changes per instance to allow event separation for different instances of this script. The
     * client id is a pseudo id calculated based on environment variables of the system on which the app is
     * running. It cannot be used to trace back the individual user.
     * The logging can also be triggered in a static fashion from any other script upon request. For this, the
     * first instance of this class in the scene graph can be retrieved using the getInstance() method. Then
     * on this instance, LogEvent() can be called with the above mentioned parameters. A call of this method
     * is handled in the same manner as the other calls caused by the self registration of this class for any
     * scene internal events.
     * The searching for all objects in a scene for which the usage must be monitored is done continuously in
     * form of a coroutine. The reason for this is, that the objects in a scene may change over time and the
     * script cannot be notified about new objects of interest. Hence, the coroutine performs a kind of polling
     * mechanism. This mechanism is implemented in a fashion so that not too much action happens in a frame. In
     * addition, the coroutine prevents to continuously check the full scene graph at once for objects of
     * interest but only sub graphs starting at the different root nodes in a scene. This may cause, that some
     * events on objects of interest may be lost. But this will not be the default case as the whole scene will
     * be reconsidered in only a few frames and afterwards, the coroutine will wait only a further second
     * before it starts rescanning the scene.
     * In two further coroutines, the class continously traces the movement and rotation of the main camera
     * which is considered to be the head of the AR/VR user. It must be done in coroutines as there is no
     * notification possibility on location changes for the camera. And in addition, the check can be spread
     * over time so that it takes place only once per 100 ms. If the coroutines determine a change in location
     * or rotation, they produce corresponding events. Location and rotation changes usually take some time.
     * Hence, they are logged, when they are finished. If in the meantime, other events happen, they are
     * logged before that. But this is no drawback, as the location and rotation changes are logged together
     * with time stamps indicating when the movement or rotation started and ended.
     */
    public class AutoQUESTGenericMonitorUnity : MonoBehaviour
    {
        /** The instance of this script which is considered the one to be returned when calling getInstance() */
        private static AutoQUESTGenericMonitorUnity instance;

        /** The id of the client, i.e., user, for which the actions are recorded (static, because it is the 
         *  same for all instances of this script) */
        private static string clientId;

        /** the registered event targets (static, because this map can become large and should, hence, be shared
         *  between different instances of this script) */
        private static Dictionary<GameObject, string> eventTargetIds = new Dictionary<GameObject, string>();

        /** a lock for multiple reads but only single writes on the collected event target ids */
        private static ReaderWriterLockSlim readWriteLockEventTargetIds = new ReaderWriterLockSlim();

        /** the current list of events per server collected so far (static, because multiple instances of this
         *  script may want to log to the same server and the events should always be sorted in the order in 
         *  which they occurred) */
        private static Dictionary<string, List<EventData>> collectedEvents = new Dictionary<string, List<EventData>>();

        /** a lock for multiple reads but only single writes on the collected event target ids */
        private static ReaderWriterLockSlim readWriteLockCollectedEvents = new ReaderWriterLockSlim();

        /** The AutoQUEST generic monitor server to send the logged data to (non static to allow for multiple
         * script instances logging different things to different servers and to be configurable via unity) */
        public string serverURL = "https://swe-tooling.informatik.uni-goettingen.de/autoquest-genericmonitor/";

        /** The id of the application that the client uses (non static to allow for multiple script instances
         *  logging different things for different virtual apps and to be configurable via unity) */
        public string appId;

        /** defines if for each session, a new client id shall be generated */
        public bool oneClientIdPerSession = false;

        /** allows to enable automatic event handling registration */
        public bool eventHandlingRegistration = true;

        /** allows to enable automatic movement tracking */
        public bool movementTracking = true;

        /** allows to enable automatic gaze tracking */
        public bool gazeTracking = true;

        /** allows to enable event reduction by not sending multiple, identical, and subsequent events only once.
         * IMPORTANT: Use this with caution as this may prevent sending events that should be sent twice. */
        public bool eventDuplicateFilter = false;

        /** Used to enable or disable logging */
        public bool verbose = false;

        /**
         * stores for any relevant game object the list of event listeners registered on it. This is required to
         * remove them afterwards for clean up as removing can only be done for known listener objects
         */
        private Dictionary<UnityEngine.Object, Listeners> registeredUnityActions = new Dictionary<UnityEngine.Object, Listeners>();

        /** determine the client id */
        static AutoQUESTGenericMonitorUnity()
        {
            // determine a client id
            StringBuilder id = new StringBuilder();
            foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
            {
                id.Append(de.Key);
                id.Append(de.Value);
            }

            clientId = toMD5Hash(id.ToString());
        }

        /**
         * similar to a singleton, returns the single instance of this script in the scene. If there are multiple instances of this
         * script, the first one found is taken. If there is not instance of this script attached to any active object in the scene
         * an exception is thrown.
         */
        public static AutoQUESTGenericMonitorUnity getInstance()
        {
            readWriteLockCollectedEvents.EnterUpgradeableReadLock();

            if (instance == null)
            {
                readWriteLockCollectedEvents.EnterWriteLock();
                // search for the first instances of this script in active objects and take it as the single instance
                instance = FindObjectOfType<AutoQUESTGenericMonitorUnity>();

                if (instance == null)
                {
                    // there is no instance of this script belonging to an active object in this scene. Therefore, we throw an
                    // exception, as this is a prerequisite
                    throw new Exception("getInstance() must not be called without an instance of this script attached to an " +
                                        "active object in the scene. This is a programming mistake and must be corrected. You can, e.g., " +
                                        "add the AutoQUEST Monitor Prefab to the scene to solve this problem.");
                }

                readWriteLockCollectedEvents.ExitWriteLock();
            }

            readWriteLockCollectedEvents.ExitUpgradeableReadLock();
            return instance;
        }

        /**
         * generates an app id if non is already set and starts all coroutines
         */
        void Start()
        {
            readWriteLockCollectedEvents.EnterWriteLock();

            if (oneClientIdPerSession)
            {
                clientId += "_" + DateTime.UtcNow.Ticks;
            }

            // get an id for the application
            if ((appId == null) || ("".Equals(appId)))
            {
                appId = Application.productName;

                if ((appId == null) || ("".Equals(appId)))
                {
                    appId = Application.identifier;
                }
            }

            if (eventHandlingRegistration)
            {
                StartCoroutine("EventHandlingRegistrationCoroutine");
            }

            if (movementTracking)
            {
                StartCoroutine("MovementTrackingCoroutine");
            }

            if (gazeTracking)
            {
                StartCoroutine("GazeTrackingCoroutine");
            }

            readWriteLockCollectedEvents.ExitWriteLock();
        }

        /**
         * Sends all remaining collected events to the server
         */
        void OnApplicationQuit()
        {
            StopAllCoroutines();
            SendEventsToServer(true);
        }

        /**
         * collects an event for a given game object and triggers sending events to the server, if sufficient events have been collected
         */
        public void LogEvent(string eventType, GameObject eventTarget, params KeyValuePair<string, string>[] parameters)
        {
            // determine the target
            RegisterEventTarget(eventTarget);

            if (verbose) Debug.Log("logEvent called: " + eventType + " " + eventTarget.name + " (" + GetEventTargetId(eventTarget) + ")");

            EventData eventData = new EventData
            {
                type = eventType,
                target = eventTarget,
                timestamp = DateTime.UtcNow.Ticks,
                targetPosition = eventTarget.transform.position,
                parameters = parameters
            };

            readWriteLockCollectedEvents.EnterUpgradeableReadLock();
            List<EventData> eventList;

            if (!collectedEvents.ContainsKey(serverURL))
            {
                eventList = new List<EventData>();
                readWriteLockCollectedEvents.EnterWriteLock();
                collectedEvents.Add(serverURL, eventList);
                readWriteLockCollectedEvents.ExitWriteLock();
            }
            else
            {
                eventList = collectedEvents[serverURL];
            }

            if (!eventDuplicateFilter || (eventList.Count == 0) || (!eventList[eventList.Count - 1].IsSame(eventData)))
            {
                eventList.Add(eventData);

                if (eventList.Count >= 10)
                {
                    SendEventsToServer(false);
                }
            }

            readWriteLockCollectedEvents.ExitUpgradeableReadLock();
        }

        /**
         * collects an event and triggers sending events to the server, if sufficient events have been collected.
         * The event target considered for the given event is the game object of which the instance of this class
         * is a component. Hence, this method must be called with care to ensure, that the correct correct
         * event target is considered.
         */
        public void LogEvent(string eventType, params KeyValuePair<string, string>[] parameters)
        {
            LogEvent(eventType, this.gameObject, parameters);
        }

        /**
         * used for storing event targets and their hierarchies and for calculating their unique ids
         */
        private void RegisterEventTarget(GameObject eventTarget)
        {
            // check, if the target is already registered. If so, do nothing
            readWriteLockEventTargetIds.EnterReadLock();

            if (eventTargetIds.ContainsKey(eventTarget))
            {
                readWriteLockEventTargetIds.ExitReadLock();
                return;
            }

            readWriteLockEventTargetIds.ExitReadLock();

            // before adding the target itself, check for the parents
            GameObject parent = eventTarget;
            string id = "";

            while (parent != null)
            {
                id += parent.name;
                if (parent.transform.parent != null)
                {
                    parent = parent.transform.parent.gameObject;
                    RegisterEventTarget(parent);
                }
                else
                {
                    parent = null;
                }
            }

            // now, register the target. Ensure, that no other thread added it in the meantime
            readWriteLockEventTargetIds.EnterUpgradeableReadLock();

            if (eventTargetIds.ContainsKey(eventTarget))
            {
                readWriteLockEventTargetIds.ExitUpgradeableReadLock();
                return;
            }

            id += eventTarget.scene.name;

            id = toMD5Hash(id);

            readWriteLockEventTargetIds.EnterWriteLock();
            eventTargetIds.Add(eventTarget, id);
            readWriteLockEventTargetIds.ExitWriteLock();

            if (verbose) Debug.Log("registerEventTarget called: " + eventTarget.name + " (" + id + ")");

            readWriteLockEventTargetIds.ExitUpgradeableReadLock();
        }

        /**
         * returns the id calculated for an event target for its identification
         */
        private string GetEventTargetId(GameObject eventTarget)
        {
            readWriteLockEventTargetIds.EnterReadLock();
            string result = eventTargetIds[eventTarget];
            readWriteLockEventTargetIds.ExitReadLock();

            return result;
        }

        /**
         * sends all events to the server
         */
        private void SendEventsToServer(bool wait)
        {
            EventData[] eventsToSend = null;

            // get a copy and directly release the lock so that further events can be collected in parallel.
            readWriteLockCollectedEvents.EnterReadLock();

            if (collectedEvents.ContainsKey(serverURL))
            {
                eventsToSend = collectedEvents[serverURL].ToArray();
                collectedEvents[serverURL].Clear();
            }

            readWriteLockCollectedEvents.ExitReadLock();

            // send events only, if there are some
            if ((eventsToSend == null) || (eventsToSend.Length <= 0))
            {
                return;
            }

            if (verbose) Debug.Log("sending " + eventsToSend.Length + " events to the server");

            string message = CreateMessage(eventsToSend);

            UnityWebRequest www = new UnityWebRequest(serverURL, UnityWebRequest.kHttpVerbPOST);
            UploadHandlerRaw uH = new UploadHandlerRaw(Encoding.UTF8.GetBytes(message));
            www.uploadHandler = uH;
            www.SetRequestHeader("Content-Type", "application/json");

            UnityWebRequestAsyncOperation result = www.SendWebRequest();

            if (wait)
            {
                while (!result.isDone)
                {
                    new WaitForSeconds(0.1f);
                }
            }
        }

        /**
         * creates a message containing the events and the corresponding targets
         */
        private string CreateMessage(EventData[] eventsToSend)
        {
            // create the message
            StringBuilder message = new StringBuilder();
            message.AppendLine("{");
            message.AppendLine("  \"message\": {");
            message.AppendLine("    \"clientInfos\": {");
            message.AppendLine("      \"appId\": \"" + appId + "\"");
            message.AppendLine("      \"clientId\": \"" + clientId + "\"");
            message.AppendLine("    },");

            // determine the targets to be send to the server
            Dictionary<GameObject, List<GameObject>> targetsToSend = new Dictionary<GameObject, List<GameObject>>();
            List<GameObject> rootTargets = new List<GameObject>();

            for (int i = 0; i < eventsToSend.Length; i++)
            {
                GameObject targetToSend = eventsToSend[i].target;
                GameObject lastChild = null;

                while (targetToSend != null)
                {
                    List<GameObject> children = null;

                    if (!targetsToSend.ContainsKey(targetToSend))
                    {
                        children = new List<GameObject>();
                        if (lastChild != null)
                        {
                            children.Add(lastChild);
                        }

                        targetsToSend.Add(targetToSend, children);

                        if (targetToSend.transform.parent != null)
                        {
                            lastChild = targetToSend;
                            targetToSend = targetToSend.transform.parent.gameObject;
                        }
                        else
                        {
                            rootTargets.Add(targetToSend);
                            targetToSend = null;
                        }
                    }
                    else
                    {
                        if (lastChild != null)
                        {
                            children = targetsToSend[targetToSend];
                            children.Add(lastChild);
                        }
                        targetToSend = null;
                    }
                }
            }

            // add the targets to be send to the server
            message.AppendLine("    \"targetStructure\": [");
            message.AppendLine("      {");
            message.AppendLine("        \"targetId\": \"" + toMD5Hash(SceneManager.GetActiveScene().name) + "\",");
            message.AppendLine("        \"name\": \"" + SceneManager.GetActiveScene().name + "\",");
            message.AppendLine("        \"children\": [");

            foreach (GameObject root in rootTargets)
            {
                DumpTarget(message, root, targetsToSend, "        ");
                message.AppendLine("        ,");
            }

            message.AppendLine("        ]");
            message.AppendLine("      }");
            message.AppendLine("    ],");

            // now add the events
            message.AppendLine("    \"events\": [");

            for (int i = 0; i < eventsToSend.Length; i++)
            {
                message.AppendLine("      {");
                message.AppendLine("        \"time\": \"" + eventsToSend[i].timestamp + "\",");
                message.AppendLine("        \"type\": \"" + eventsToSend[i].type + "\",");
                message.AppendLine("        \"targetId\": \"" + GetEventTargetId(eventsToSend[i].target) + "\",");
                message.AppendLine("        \"targetPosition\": \"" + eventsToSend[i].targetPosition + "\",");
                if ((eventsToSend[i].parameters != null) && (eventsToSend[i].parameters.Length > 0))
                {
                    foreach (KeyValuePair<string, string> parameter in eventsToSend[i].parameters)
                    {
                        message.AppendLine("        \"" + parameter.Key + "\": \"" + parameter.Value + "\",");
                    }
                }

                message.AppendLine("      },");
            }

            message.AppendLine("    ]");

            message.AppendLine("  }");
            message.AppendLine("}");

            string result = message.ToString();
            if (verbose) Debug.Log(result);

            return result;
        }

        /**
         * dumps a target to the given string builder by considering the indentation. Calls itself recursively to dump the target hierarchy.
         */
        private void DumpTarget(StringBuilder message, GameObject eventTarget, Dictionary<GameObject, List<GameObject>> targetsToSend, string indent)
        {
            message.Append(indent);
            message.AppendLine("{");
            message.Append(indent);
            message.AppendLine("  \"targetId\": \"" + GetEventTargetId(eventTarget) + "\",");
            message.Append(indent);
            message.AppendLine("  \"name\": \"" + eventTarget.name + "\",");

            List<GameObject> children = targetsToSend[eventTarget];

            if (children.Count > 0)
            {
                message.Append(indent);
                message.AppendLine("  \"children\": [");

                foreach (GameObject child in children)
                {
                    DumpTarget(message, child, targetsToSend, indent + "    ");
                    message.Append(indent);
                    message.AppendLine("    ,");
                }

                message.Append(indent);
                message.AppendLine("  ]");
            }

            message.Append(indent);
            message.AppendLine("}");
        }

        /**
         * convenience method to calculate MD5 hashes for a given string
         */
        private static string toMD5Hash(string str)
        {
            MD5 md5Algorithm = new MD5CryptoServiceProvider();
            byte[] strAsBytes = Encoding.UTF8.GetBytes(str);
            strAsBytes = md5Algorithm.ComputeHash(strAsBytes);

            return BitConverter.ToString(strAsBytes).Replace("-", "");
        }


        /**
         * In this coroutine, we continuously check all game objects in the scene for changes so that we can register
         * event listeners for objects on which they should exist. To not cause too much effort for that, the coroutine yields from
         * time to time. The event listeners are created only once and then stored in a dictionary. This is required as event listeners
         * can only be removed using their reference. But we need to be able to remove them to ensure that they are registered only
         * once per object.
         * After some time, the objects on which the event listeners are registers may be destroyed. Hence, the dictionary may
         * contain event listeners for objects that do not exist anymore. To handle this, the coroutine also stores the last time
         * since when the event listeners have been considered last during a registration cycle of this coroutine. If this point
         * in time is more than 5 seconds ago, the coroutine considers the object to be destroyed and, therefore, also destroys
         * the event listeners.
         */
        private IEnumerator EventHandlingRegistrationCoroutine()
        {
            List<GameObject> currentRootGameObjects = new List<GameObject>(100);
            int rootGameObjectsPointer = 0;
            LinkedList<UnityEngine.Object> currentRelevantGameObjects = new LinkedList<UnityEngine.Object>();
            int gameObjectsHandledPerFrame = 10;

            while (true)
            {
                if (rootGameObjectsPointer > (currentRootGameObjects.Count - 1))
                {
                    // we need to reread the root objects of all loaded scenes
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        Scene scene = SceneManager.GetSceneAt(i);
                        if (scene.isLoaded)
                        {
                            currentRootGameObjects.AddRange(scene.GetRootGameObjects());
                        }
                    }

                    rootGameObjectsPointer = 0;

                    // yield and continue in next frame
                    yield return null;
                    //print("considering " + currentRootGameObjects.Count + " root objects");
                }

                // check, if there are enough game objects to be processed in the current frame
                while ((currentRelevantGameObjects.Count < gameObjectsHandledPerFrame) &&
                       (rootGameObjectsPointer < currentRootGameObjects.Count))
                {
                    // we need to read some further game objects into the list of those to be handled next
                    addRelevantGameObjects(currentRootGameObjects[rootGameObjectsPointer++], currentRelevantGameObjects);
                    yield return null;
                }

                //print("considering next " + currentRelevantGameObjects.Count + " game objects for which actions must be logged");

                // now handle the relevant objects, but at most gameObjectsHandledPerFrame at once
                readWriteLockCollectedEvents.EnterWriteLock();
                int currentlyHandled = 0;
                while (currentRelevantGameObjects.Count > 0)
                {
                    UnityEngine.Object relevantObject = currentRelevantGameObjects.First.Value;

                    currentRelevantGameObjects.RemoveFirst();

                    if (relevantObject != null)
                    {
                        //print("ensuring event handling for " + relevantObject);
                        ensureEventHandling(relevantObject);
                    }

                    currentlyHandled++;

                    if (currentlyHandled % gameObjectsHandledPerFrame == 0)
                    {
                        // we handled enough relevant game objects in this frame. Release the lock, yield, and continue in next frame
                        // by obtaining the lock again
                        readWriteLockCollectedEvents.ExitWriteLock();
                        yield return null;
                        readWriteLockCollectedEvents.EnterWriteLock();
                    }
                }
                readWriteLockCollectedEvents.ExitWriteLock();

                yield return null;

                readWriteLockCollectedEvents.EnterWriteLock();
                // now check for any listener in the dictionary, if it was touched in the last cycles, and if not, destroy them
                long thresholdTime = DateTime.UtcNow.Ticks - 50000000;
                List<UnityEngine.Object> obsoleteEntries = new List<UnityEngine.Object>();
                foreach (KeyValuePair<UnityEngine.Object, Listeners> entry in registeredUnityActions)
                {
                    if (entry.Value.lastTouched < thresholdTime)
                    {
                        obsoleteEntries.Add(entry.Key);
                    }
                }

                foreach (UnityEngine.Object obsoleteEntry in obsoleteEntries)
                {
                    registeredUnityActions.Remove(obsoleteEntry);
                }
                readWriteLockCollectedEvents.ExitWriteLock();

                if (rootGameObjectsPointer > (currentRootGameObjects.Count - 1))
                {
                    // we now checked all relevant objects. Let's wait a second before starting the next registration cycle
                    yield return new WaitForSeconds(1f);
                    //yield return null;
                }
                else
                {
                    yield return null;
                }
            }
        }

        /**
         * Traverses a hierarchy of game objects and adds game objects relevant for event logging to the given list
         */
        virtual protected void addRelevantGameObjects(GameObject gameObject, LinkedList<UnityEngine.Object> currentRelevantGameObjects)
        {
            if (gameObject)
            {
                Component[] selectables = gameObject.GetComponentsInChildren(typeof(Selectable));
                Component[] scrollRects = gameObject.GetComponentsInChildren(typeof(ScrollRect));
                Component[] eventTriggers = gameObject.GetComponentsInChildren(typeof(EventTrigger));

                foreach (Component relevantObject in selectables)
                {
                    currentRelevantGameObjects.AddLast(relevantObject);
                }

                foreach (Component relevantObject in scrollRects)
                {
                    currentRelevantGameObjects.AddLast(relevantObject);
                }

                foreach (Component relevantObject in eventTriggers)
                {
                    currentRelevantGameObjects.AddLast(relevantObject);
                }
            }
        }

        /**
         * registers event handling for the given relevant object
         */
        virtual protected void ensureEventHandling(UnityEngine.Object relevantObject)
        {
            if (relevantObject is Button)
            {
                ensureEventHandling(((Button)relevantObject).onClick, "PointerClick", ((Button)relevantObject).gameObject);
            }
            else if (relevantObject is Toggle)
            {
                ensureEventHandling(((Toggle)relevantObject).onValueChanged, "ValueChanged", ((Toggle)relevantObject).gameObject);
            }
            else if (relevantObject is Slider)
            {
                ensureEventHandling(((Slider)relevantObject).onValueChanged, "ValueChanged", ((Slider)relevantObject).gameObject);
            }
            else if (relevantObject is Scrollbar)
            {
                ensureEventHandling(((Scrollbar)relevantObject).onValueChanged, "ValueChanged", ((Scrollbar)relevantObject).gameObject);
            }
            else if (relevantObject is Dropdown)
            {
                ensureEventHandling(((Dropdown)relevantObject).onValueChanged, "ValueChanged", ((Dropdown)relevantObject).gameObject);
            }
            else if (relevantObject is InputField)
            {
                ensureEventHandling(((InputField)relevantObject).onValueChanged, "ValueChanged", ((InputField)relevantObject).gameObject);
                ensureEventHandling(((InputField)relevantObject).onEndEdit, "EditingEnded", ((InputField)relevantObject).gameObject);
            }
            else if (relevantObject is ScrollRect)
            {
                ensureEventHandling(((ScrollRect)relevantObject).onValueChanged, "ValueChanged", ((ScrollRect)relevantObject).gameObject);
            }
            else if (relevantObject is EventTrigger)
            {
                foreach (EventTrigger.Entry entry in ((EventTrigger)relevantObject).triggers)
                {
                    ensureEventHandling(entry.callback, entry.eventID.ToString(), ((EventTrigger)relevantObject).gameObject);
                }
            }
            else
            {
                if (verbose) Debug.Log("unknown type of relevant object: " + relevantObject.GetType());
            }
        }

        /**
         * ensures that there is a listener for the given event on the given game object
         */
        private void ensureEventHandling(UnityEvent unityEvent, string eventType, GameObject eventTarget)
        {
            UnityAction listener = getListeners(eventTarget).getCreateListener(eventType, eventTarget);

            // remove the listener first to ensure, it is not registered twice
            unityEvent.RemoveListener(listener);
            unityEvent.AddListener(listener);
        }

        /**
         * ensures that there is a listener for the given event on the given game object
         */
        private void ensureEventHandling<T>(UnityEvent<T> unityEvent, string eventType, GameObject eventTarget)
        {
            UnityAction<T> listener = getListeners(eventTarget).getCreateListener<T>(eventType, eventTarget);

            // remove the listener first to ensure, it is not registered twice
            unityEvent.RemoveListener(listener);
            unityEvent.AddListener(listener);
        }

        /**
         * Determines the event listeners for a given eventTarget
         */
        protected Listeners getListeners(GameObject eventTarget)
        {
            Listeners listeners = null;

            registeredUnityActions.TryGetValue(eventTarget, out listeners);

            if (listeners == null)
            {
                listeners = createListeners();
                registeredUnityActions.Add(eventTarget, listeners);
            }

            return listeners;
        }

        /**
         * used to allow subclasses to create other, more concrete types of listeners
         */
        virtual protected Listeners createListeners()
        {
            return new Listeners(LogEvent);
        }

        /**
         * In this coroutine, we continuously check the position of the main camera to do movement tracking.
         * For this the coroutine runs every 100 ms and compares the preceding location with the current one.
         * If their is a change, then a motion is considered. If there is no change, then no motion is
         * considered. If before there was a motion, then an event is logged. If the motion continues but
         * the direction vectors of the previous and the current motion are more different than 5°, also
         * an event is logged for the first motion.
         */
        private IEnumerator MovementTrackingCoroutine()
        {
            Vector3 previousPosition = Camera.main.transform.position;
            Vector3 currentTranslationVector = new Vector3(0, 0, 0);
            bool headIsMoving = false;
            long timestamp = 0;

            while (true)
            {
                Vector3 currentPosition = Camera.main.transform.position;
                Vector3 translationVector = currentPosition - previousPosition;

                if (translationVector.magnitude > 0.05)
                {
                    // movement detected (position change larger than 5cm). Check if we are already in a movement
                    if (headIsMoving)
                    {
                        // the movement is continuing. Check, if it is almost the same direction
                        if (Vector3.Angle(currentTranslationVector, translationVector) < 5)
                        {
                            // we are still almost in the same direction. Extend the current translation vector
                            // and continue
                            currentTranslationVector += translationVector;
                        }
                        else
                        {
                            // the movement changed the direction. Therefore, log an event for the previous movement
                            // and set everything as if the new movement just started
                            Vector3 source = previousPosition - currentTranslationVector;
                            roundVector(ref source);
                            Vector3 dest = previousPosition;
                            roundVector(ref dest);

                            LogEvent("headMoved", Camera.main.gameObject,
                                     new KeyValuePair<string, string>("movementStart", timestamp.ToString()),
                                     new KeyValuePair<string, string>("movementStop", DateTime.UtcNow.Ticks.ToString()),
                                     new KeyValuePair<string, string>("from", "(" + source.x + ", " + source.y +
                                                                      ", " + source.z + ")"),
                                     new KeyValuePair<string, string>("to", "(" + dest.x + ", " + dest.y +
                                                                      ", " + dest.z + ")"));

                            currentTranslationVector = translationVector;
                            timestamp = DateTime.UtcNow.Ticks;
                        }
                    }
                    else
                    {
                        // head is started moving--> store the translation vector, set the flag, and continue
                        currentTranslationVector = translationVector;
                        headIsMoving = true;
                        timestamp = DateTime.UtcNow.Ticks;
                    }
                }
                else
                {
                    if (headIsMoving)
                    {
                        // the movement of the head stopped --> log event and store that movement stopped
                        Vector3 source = previousPosition - currentTranslationVector;
                        roundVector(ref source);
                        Vector3 dest = previousPosition;
                        roundVector(ref dest);

                        LogEvent("headMoved", Camera.main.gameObject,
                                 new KeyValuePair<string, string>("movementStart", timestamp.ToString()),
                                 new KeyValuePair<string, string>("movementStop", DateTime.UtcNow.Ticks.ToString()),
                                 new KeyValuePair<string, string>("from", "(" + source.x + ", " + source.y +
                                                                  ", " + source.z + ")"),
                                 new KeyValuePair<string, string>("to", "(" + dest.x + ", " + dest.y +
                                                                  ", " + dest.z + ")"));

                        currentTranslationVector = new Vector3(0, 0, 0);
                        headIsMoving = false;
                        timestamp = 0;
                    }
                    // else no movement. Do nothing.
                }

                previousPosition = currentPosition;

                // wait for the next execution
                yield return new WaitForSeconds(0.1f);
            }
        }

        /**
         * In this coroutine, we continuously check the rotation of the main camera to do rotation tracking.
         * For this the coroutine runs every 100 ms and compares the preceding rotation with the current one.
         * If their is a change, then a rotation is considered. If there is no change, then no rotation is
         * considered. If before there was a rotation, then an event is logged.
         */
        private IEnumerator GazeTrackingCoroutine()
        {
            Vector3 initialOrientation = new Vector3(0, 0, 0);
            Vector3 previousOrientation = Camera.main.transform.forward;
            float currentRotationAngle = 0f;
            bool headIsRotating = false;
            long timestamp = 0;

            while (true)
            {
                Vector3 currentOrientation = Camera.main.transform.forward;
                float rotationAngle = Vector3.Angle(previousOrientation, currentOrientation);

                if (rotationAngle > 2)
                {
                    // rotation detected (gaze direction change larger than 5°). Check if we are already in a rotation
                    if (headIsRotating)
                    {
                        // the rotation is continuing. Extend the current rotation angle and continue
                        currentRotationAngle += rotationAngle;
                    }
                    else
                    {
                        // head has started rotating--> store the roation angle, set the flag, and continue
                        initialOrientation = previousOrientation;
                        currentRotationAngle = rotationAngle;
                        headIsRotating = true;
                        timestamp = DateTime.UtcNow.Ticks;
                    }
                }
                else
                {
                    if (headIsRotating)
                    {
                        // the rotation of the head stopped --> log event and store that rotation stopped
                        Vector3 source = initialOrientation;
                        roundVector(ref source);
                        Vector3 dest = currentOrientation;
                        roundVector(ref dest);

                        LogEvent("headRotated", Camera.main.gameObject,
                                 new KeyValuePair<string, string>("rotationStart", timestamp.ToString()),
                                 new KeyValuePair<string, string>("rotationStop", DateTime.UtcNow.Ticks.ToString()),
                                 new KeyValuePair<string, string>("from", "(" + source.x + ", " + source.y +
                                                                  ", " + source.z + ")"),
                                 new KeyValuePair<string, string>("to", "(" + dest.x + ", " + dest.y +
                                                                  ", " + dest.z + ")"));

                        initialOrientation = new Vector3(0, 0, 0);
                        currentRotationAngle = 0;
                        headIsRotating = false;
                        timestamp = 0;
                    }
                    // else no movement. Do nothing.
                }

                previousOrientation = currentOrientation;

                // wait for the next execution
                yield return new WaitForSeconds(0.1f);
            }
        }

        /**
         * convenience method to round the values of a vector to two decimal positions
         */
        private void roundVector(ref Vector3 vector)
        {
            vector.x = (float)Math.Round(vector.x, 2);
            vector.y = (float)Math.Round(vector.y, 2);
            vector.z = (float)Math.Round(vector.z, 2);
        }

        /**
         * inner structure to use for storing event data
         */
        private struct EventData
        {
            public string type;
            public GameObject target;
            public long timestamp;
            public Vector3 targetPosition;
            public KeyValuePair<string, string>[] parameters;

            public bool IsSame(EventData other)
            {
                if ((this.type != other.type) || (this.target != other.target) || (this.targetPosition != other.targetPosition))
                {
                    return false;
                }

                if ((this.parameters == null) || (this.parameters.Length <= 0))
                {
                    if ((other.parameters == null) || (other.parameters.Length == 0))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                if ((other.parameters == null) || (other.parameters.Length <= 0))
                {
                    if ((this.parameters == null) || (this.parameters.Length == 0))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (this.parameters.Length != other.parameters.Length)
                {
                    return true;
                }

                bool allFound = true;

                foreach (KeyValuePair<string, string> parameter in this.parameters)
                {
                    bool found = false;
                    foreach (KeyValuePair<string, string> candidate in other.parameters)
                    {
                        if ((parameter.Key == candidate.Key) && (parameter.Value == candidate.Value))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        allFound = false;
                        break;
                    }
                }

                return allFound;
            }
        }

        /**
         * inner structure to use for storing listeners for scene objects
         */
        protected class Listeners
        {
            /** the delegate for callback methods to call when events occur */
            public delegate void EventHandlingCallback(string eventType, GameObject eventTarget, params KeyValuePair<string, string>[] parameters);

            /** stores when this information has been touched the last time */
            internal long lastTouched = DateTime.UtcNow.Ticks;

            /** stores the list of listener managed in this structure */
            protected List<KeyValuePair<string, object>> listeners = new List<KeyValuePair<string, object>>();

            /** the callback method to call if an event occurs */
            protected EventHandlingCallback callback;

            /**
             * used to pass the event handling callback method
             */
            public Listeners(EventHandlingCallback callback)
            {
                this.callback = callback;
            }

            /**
             * returns a listener for the given event type and updates the internal timestamp. If there is no listener
             * one will be created and registered.
             */
            internal UnityAction getCreateListener(string eventType, GameObject eventTarget)
            {
                UnityAction listener = null;

                foreach (KeyValuePair<string, object> candidate in listeners)
                {
                    if (candidate.Key == eventType)
                    {
                        listener = (UnityAction)candidate.Value;
                        break;
                    }
                }

                if (listener == null)
                {
                    listener = delegate { callback(eventType, eventTarget); };
                    addListener(eventType, listener);
                    //Debug.Log("ensuring handling of event " + eventType + " for  " + eventTarget);
                }

                lastTouched = DateTime.UtcNow.Ticks;
                return listener;
            }

            /**
             * returns a listener for the given event type and updates the internal timestamp. If there is no listener
             * one will be created and registered.
             */
            internal UnityAction<T> getCreateListener<T>(string eventType, GameObject eventTarget)
            {
                UnityAction<T> listener = null;

                foreach (KeyValuePair<string, object> candidate in listeners)
                {
                    if (candidate.Key == eventType)
                    {
                        listener = (UnityAction<T>)candidate.Value;
                        break;
                    }
                }

                if (listener == null)
                {
                    listener = delegate (T arg)
                    {
                        if (arg != null)
                        {
                            if (arg.Equals(eventTarget))
                            {
                                callback(eventType, eventTarget);
                            }
                            else if (arg is PointerEventData)
                            {
                                PointerEventData eventData = arg as PointerEventData;
                                callback(eventType, eventTarget,
                                         new KeyValuePair<string, string>("distance", eventData.pointerCurrentRaycast.distance.ToString()));
                            }
                            else
                            {
                                callback(eventType, eventTarget, new KeyValuePair<string, string>("value", arg.ToString()));
                            }
                        }
                        else
                        {
                            callback(eventType, eventTarget);
                        }
                    };

                    addListener(eventType, listener);
                    //Debug.Log("ensuring handling of event " + eventType + " for  " + eventTarget);
                }

                lastTouched = DateTime.UtcNow.Ticks;
                return listener;
            }

            /**
             * Adds a listener to the internal listener list while also updating the timestamp
             */
            internal void addListener(string eventType, object listener)
            {
                listeners.Add(new KeyValuePair<string, object>(eventType, listener));
                lastTouched = DateTime.UtcNow.Ticks;
            }
        }

    }
}
 