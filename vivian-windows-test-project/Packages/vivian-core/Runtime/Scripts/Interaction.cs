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
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace de.ugoe.cs.vivian.core
{
    abstract class Interaction
    {
        public VirtualPrototypeElement vpElement { get; private set; }

        internal Interaction(VirtualPrototypeElement vpElement)
        {
            this.vpElement = vpElement;
        }

        internal abstract void Start(Pose pose, int poseId);

        internal abstract void Continue(Pose pose, int poseId);

        internal abstract void Stop(Pose pose, int poseId);
    }

    class MultiPoseInteraction : Interaction
    {
        private Dictionary<int, Pose> currentPoses = new Dictionary<int, Pose>();

        public MultiPoseInteraction(VirtualPrototypeElement vpElement) : base(vpElement)
        {

        }

        internal override void Start(Pose pose, int poseId)
        {
            if (currentPoses.Count > 0)
            {
                StopAverageInteraction();
            }

            this.currentPoses.Add(poseId, pose);

            StartAverageInteraction();
        }

        internal override void Continue(Pose pose, int poseId)
        {
            this.currentPoses[poseId] = pose;
            ContinueAverageInteraction();
        }

        internal override void Stop(Pose pose, int poseId)
        {
            StopAverageInteraction();
            this.currentPoses.Remove(poseId);

            if (this.currentPoses.Count > 0)
            {
                StartAverageInteraction();
            }
        }

        private void StartAverageInteraction()
        {
            Pose averagePose = getAveragePose();
            Debug.DrawRay(averagePose.position, averagePose.forward, Color.red);
            Debug.DrawRay(averagePose.position, averagePose.up, Color.yellow);
            base.vpElement.TriggerInteractionStarts(averagePose);
        }

        private void ContinueAverageInteraction()
        {
            Pose averagePose = getAveragePose();
            Debug.DrawRay(averagePose.position, averagePose.forward, Color.red);
            Debug.DrawRay(averagePose.position, averagePose.up, Color.yellow);
            base.vpElement.TriggerInteractionContinues(averagePose);
        }

        private void StopAverageInteraction()
        {
            Pose averagePose = getAveragePose();
            Debug.DrawRay(averagePose.position, averagePose.forward, Color.red);
            Debug.DrawRay(averagePose.position, averagePose.up, Color.yellow);
            base.vpElement.TriggerInteractionEnds(averagePose);
        }

        private Pose getAveragePose()
        {
            if (this.currentPoses.Count == 1)
            {
                return this.currentPoses.First().Value;
            }
            else if (this.currentPoses.Count == 2)
            {
                return getPoseFromTwoPoses();
            }
            else
            {
                return new Pose
                {
                    position = getAveragePositionForMultiplePoses(),
                    rotation = getAverageRotationForMultiplePoses()
                };
            }
        }

        private Pose getPoseFromTwoPoses()
        {
            Pose pose1 = this.currentPoses.First().Value;
            Pose pose2 = this.currentPoses.Last().Value;

            Vector3 position = pose1.position;
            Vector3 direction = (pose2.position - position).normalized;

            if (direction == Vector3.zero)
            {
                direction = Vector3.right;
            }

            Vector3 up = Vector3.Cross(pose1.forward, direction).normalized;

            if (up == Vector3.zero)
            {
                up = Vector3.up;
            }

            Quaternion rotation = Quaternion.LookRotation(direction, up);

            return new Pose(position, rotation);
        }

        Vector3 getAveragePositionForMultiplePoses()
        {
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var p in this.currentPoses.Values)
            {
                sum += p.position;
                count++;
            }
            return sum / count;
        }

        private Quaternion getAverageRotationForMultiplePoses()
        {
            var list = this.currentPoses.Values.ToList();
            Quaternion avg = list[0].rotation;
            for (int i = 1; i < list.Count; i++)
            {
                // each time blend in the next by 1/(i+1)
                avg = Quaternion.Slerp(avg, list[i].rotation, 1f / (i + 1));
            }
            return avg;
        }
    }
}
