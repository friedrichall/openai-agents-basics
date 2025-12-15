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
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace de.ugoe.cs.vivian.core
{
    public class VivianFramework : MonoBehaviour
    {
    }

    public class Utils
    {
        /**
         * convenience to get all interaction elements instantiated by the framework
         */
        public static InteractionElement[] GetInteractionElements(VirtualPrototype Prototype)
        {
            return Prototype.GetComponentsInChildren<InteractionElement>();
        }
        
        /**
         * convenience to get all virtual prototype elements instantiated by the framework
         */
        public static VirtualPrototypeElement[] GetVirtualPrototypeElements(VirtualPrototype Prototype)
        {
            return Prototype.GetComponentsInChildren<VirtualPrototypeElement>();
        }

        /**
         * creates a box collider based on a given mesh
         */
        public static Collider GetColliderFromMesh(GameObject objectToAddColliderTo, MeshFilter meshFilter)
        {
            Vector3[] boundPoints = GetLocalPointsRepresentingMesh(meshFilter);

            if ((boundPoints == null) || (boundPoints.Length <= 0))
            {
                return null;
            }

            Collider collider = objectToAddColliderTo.AddComponent<BoxCollider>();

            for (int i = 0; i < boundPoints.Length; i++)
            {
                //Debug.DrawLine(Vector3.zero, boundPoints[i], Color.green);
                boundPoints[i] = meshFilter.transform.TransformPoint(boundPoints[i]);
                //Debug.DrawLine(Vector3.zero, boundPoints[i], Color.red);
            }

            Bounds bounds = GeometryUtility.CalculateBounds(boundPoints, objectToAddColliderTo.transform.worldToLocalMatrix);

            //Debug.DrawLine(Vector3.zero, bounds.center, Color.yellow);

            // increase the collider size minimally to compensate for rounding issues.
            ((BoxCollider)collider).size = 1.001f * bounds.size;
            ((BoxCollider)collider).center = bounds.center;

            collider.isTrigger = true;

            return collider;
        }

        /**
         * Returns a surface for a given mesh with respect to a given normal for the surface. The algorithm
         * uses a projection of the object to determine the position and size of the surface. Through this,
         * the surface will always be above the object and the object will exactly be hidden behind the surface
         * in case one is looking orthogonal onto the surface. This is required to determine the position of
         * screens or touch elements.
         */
        internal static Surface GetSurfaceFromMesh(MeshFilter meshFilter, Vector3 surfaceNormal, Vector3 expectedUpwardDirection)
        {
            // We need a surface behind which we can fully hide the object. For this, we first determine the
            // bounds of the object with respect to that surface. This is equal to determining the shadow the
            // object would cast, if the sun came from the opposite direction of the surface normal. To achieve
            // this, we first rotate the object inversely with respect to the surface normal. Then we calculate
            // its bounds.
            Vector3[] points = Utils.GetLocalPointsRepresentingMesh(meshFilter);

            Quaternion rotation = Quaternion.LookRotation(surfaceNormal, expectedUpwardDirection);
            Matrix4x4 matrix = Matrix4x4.Rotate(Quaternion.Inverse(rotation));

            Bounds projectedBounds = GeometryUtility.CalculateBounds(points, matrix);

            // now we have the bounds, that is the width and height of the object from the opposite direction
            // of the given plane normal, i.e. the size of its shadows. But we don't have them yet explicitely.
            // The bounds only provide a vector called size which is diagonal through the bounds. So we first
            // need the vectors representing only the extent in the x axis and the y axis.
            Vector3 projectedXAxisExtents = new Vector3(projectedBounds.size.x, 0, 0);
            Vector3 projectedYAxisExtents = new Vector3(0, projectedBounds.size.y, 0);

            // those extents are still rotated. So let us rotate them back
            Vector3 xAxis = rotation * projectedXAxisExtents;
            Vector3 yAxis = rotation * projectedYAxisExtents;

            // and in addition, the point in the usual way of Unity, that is x-axis to the left and y-axis upwards.
            // But we need them in the opposite direction, so let us turn them around
            xAxis = -xAxis;
            yAxis = -yAxis;

            // we finally need the surface's upper left corner. Using the bounds, we can determine the upper
            // left corner. For this, we use bounds.max, which actually points to that point, and rotate
            // it from the projection back to the coordinate system of the provided mesh.
            Vector3 upperLeft = rotation * projectedBounds.max;

            // now we can return the surface
            Surface result = new Surface(upperLeft, xAxis, yAxis);

            return result;
        }

        /**
         * convenience method to get the points representing the mesh best
         */
        public static Vector3[] GetLocalPointsRepresentingMesh(MeshFilter meshFilter)
        {
            if (meshFilter == null)
            {
                return null;
            }

            Mesh mesh = meshFilter.sharedMesh;

            if (mesh == null)
            {
                mesh = meshFilter.mesh;
            }

            if (mesh != null)
            {
                if (mesh.isReadable)
                {
                    return mesh.vertices;
                }
                else
                {
                    // return the bound points instead
                    return new Vector3[] {
                        mesh.bounds.min,
                        mesh.bounds.max,
                        new Vector3(mesh.bounds.min.x, mesh.bounds.min.y, mesh.bounds.max.z),
                        new Vector3(mesh.bounds.min.x, mesh.bounds.max.y, mesh.bounds.min.z),
                        new Vector3(mesh.bounds.max.x, mesh.bounds.min.y, mesh.bounds.min.z),
                        new Vector3(mesh.bounds.min.x, mesh.bounds.max.y, mesh.bounds.max.z),
                        new Vector3(mesh.bounds.max.x, mesh.bounds.min.y, mesh.bounds.max.z),
                        new Vector3(mesh.bounds.max.x, mesh.bounds.max.y, mesh.bounds.min.z)
                    };
                }
            }

            return null;
        }

        /**
         * convenience method to copy component values
         */
        internal static T CopyComponentValues<T>(T comp, T other) where T : Component
        {
            Type type = comp.GetType();

            if (type != other.GetType())
            {
                return null; // type mis-match
            }

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);

            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }

            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }

            return comp as T;
        }

        /**
         * 
         */
        public static Vector3 ParseVector3(string sVector)
        {
            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.CurrencyDecimalSeparator = ".";

            // Remove the parentheses
            if (sVector.StartsWith("(") && sVector.EndsWith(")"))
            {
                sVector = sVector.Substring(1, sVector.Length - 2);
            }

            // split the items
            string[] sArray = sVector.Split(',');

            // store as a Vector3
            Vector3 result = new Vector3(
                float.Parse(sArray[0], NumberStyles.Any, ci),
                float.Parse(sArray[1], NumberStyles.Any, ci),
                float.Parse(sArray[2], NumberStyles.Any, ci));

            return result;
        }


        /**
         * 
         */
        public static Vector2 ParseVector2(string sVector)
        {
            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.CurrencyDecimalSeparator = ".";

            // Remove the parentheses
            if (sVector.StartsWith("(") && sVector.EndsWith(")"))
            {
                sVector = sVector.Substring(1, sVector.Length - 2);
            }

            // split the items
            string[] sArray = sVector.Split(',');

            // store as a Vector2
            Vector2 result = new Vector2(
                float.Parse(sArray[0], NumberStyles.Any, ci),
                float.Parse(sArray[1], NumberStyles.Any, ci));

            return result;
        }

        /**
         * 
         */
        public static object ParseValue(string valueStr)
        {
            if (valueStr == null)
            {
                return null;
            }

            // try parsing a float
            try
            {
                CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
                ci.NumberFormat.CurrencyDecimalSeparator = ".";

                return float.Parse(valueStr, NumberStyles.Any, ci);
            }
            catch (Exception)
            {
                // it wasn't a float. Ignore this attempt and try something else
            }

            // try parsing a Vector3
            try
            {
                return ParseVector3(valueStr);
            }
            catch (Exception)
            {
                // it wasn't a vector 3. Ignore this attempt and try something else
            }

            // try parsing a Vector2
            try
            {
                return ParseVector2(valueStr);
            }
            catch (Exception)
            {
                // it wasn't a vector 2. Ignore this attempt and try something else
            }

            // try parsing a bool
            if ("true" == valueStr.ToLower())
            {
                return true;
            }
            else if ("false" == valueStr.ToLower())
            {
                return false;
            }

            // seems to be a normal string
            return valueStr;
        }

        /**
         * 
         */
        internal static void DrawBounds(Bounds bounds, Color color)
        {
            Vector3[] points = new Vector3[]
            {
                bounds.center + bounds.extents,
                bounds.center + new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z),
                bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z),
                bounds.center + new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z),
                bounds.center - bounds.extents,
                bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z),
                bounds.center + new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z),
                bounds.center + new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z)
            };

            DrawMesh(points, color);
        }

        /**
         * 
         */
        internal static void DrawSurface(Surface surface, Color color)
        {
            Vector3[] points = new Vector3[]
            {
                surface.topLeft,
                surface.topLeft + surface.xAxis,
                surface.topLeft + surface.xAxis + surface.yAxis,
                surface.topLeft + surface.yAxis
            };

            DrawMesh(points, color);
        }

        /**
         * 
         */
        internal static void DrawMesh(Vector3[] points, Color color)
        {
            foreach (Vector3 point1 in points)
            {
                foreach (Vector3 point2 in points)
                {
                    if ((point1.x == point2.x) || (point1.y == point2.y) || (point1.z == point2.z))
                    {
                        Debug.DrawLine(point1, point2, color);
                    }
                }
            }
        }
    }

    /**
     * represents a surface of an object in reference to the object's local coordinate system
     */
    internal class Surface
    {
        /** the vector pointing at the lower left corner of the surface */
        internal Vector3 topLeft { get; }

        /** the vector representing the x-axis starting at the lower left and pointing to the lower right */
        internal Vector3 xAxis { get; }

        /** the vector representing the y-axis starting at the lower left and pointing to the upper left */
        internal Vector3 yAxis { get; }

        /** the vector pointing to the center of the surface */
        internal Vector3 center { get; }

        /** constructor initializing the surface */
        internal Surface(Vector3 topLeft, Vector3 xAxis, Vector3 yAxis)
        {
            this.topLeft = topLeft;
            this.xAxis = xAxis;
            this.yAxis = yAxis;
            this.center = topLeft + xAxis / 2 + yAxis / 2;
        }
    }


    /**
     * Interface for loading ressources, either from asset bundles or from local resources
     */
    public interface IResourceLoader
    {
        /**
         * Must be called to initialize the loader, e.g. to prefetch data
         */
        IEnumerator Init();

        /**
         * Loads an individual asset using the loader
         */
        T LoadAsset<T>(string fileName) where T : UnityEngine.Object;
    }

    /**
     * Reader for resources from an asset bundle whose location is defined by a URL.
     * If the URL is relative, the loader considers the asset bundle to be in the
     * project directory.
     */
    class AssetBundleResourceLoader : IResourceLoader
    {
        /** */
        private string url;

        /** */
        private AssetBundle bundle;

        /**
         * 
         */
        public AssetBundleResourceLoader(string url)
        {
            //string url = "file:///" + Application.dataPath + "/AssetBundles/" + assetBundleName;
            this.url = url;

            if (!this.url.Contains("://") && !Regex.IsMatch(url, @"^\d+"))
            {
                // we denote a file on the disk. Check, whether it is an absolute or relative path
                if (this.url.StartsWith("/"))
                {
                    // its an absolute path. Add only file://
                    this.url = "file://" + this.url;
                }
                else if (this.url.StartsWith("StreamingAssets/"))
                {
                    // it's a relative path for Android. Include the jar prefix as well as the location of the project
                    this.url = Path.Combine("jar:file://" + Application.dataPath + "!/assets/", this.url.Substring(url.LastIndexOf('/') +1));
                }
                else
                {
                    // its a relative path. Add the protocol as well as location of the project
                    this.url = "file:///" + Application.dataPath + "/" + this.url;
                }
            }

            Debug.Log(this.url);
        }

        /**
         *
         */
        public IEnumerator Init()
        {
            UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(this.url, 0);
            yield return request.SendWebRequest();
            this.bundle = DownloadHandlerAssetBundle.GetContent(request);

            if (this.bundle == null)
            {
                throw new ArgumentException("cannot retrieve an asset bundle from the provided URL: " + this.url);
            }
            else
            {
                Debug.Log("bundle initialized");
            }
        }

        /**
         * tries to load an asset using the given name. If this fails and the name
         * denotes a path, the path is cut off and the effective name is tried.
         */
        public T LoadAsset<T>(string fileName) where T : UnityEngine.Object
        {
            T retVal = (T)this.bundle.LoadAsset<T>(fileName);

            if (retVal != null)
            {
                return retVal;
            }

            int index = fileName.LastIndexOf('/');

            if (index >= 0)
            {
                retVal = (T)this.bundle.LoadAsset<T>(fileName.Substring(index + 1));
            }

            if (retVal != null)
            {
                return retVal;
            }

            throw new ArgumentException("there is no asset named " + fileName +
                                        " available in the bundle from the provided URL: " + this.url);
        }
    }

    /**
     * Reader for resources from a resources folder whose location is defined by a URL.
     * The URL must always be relative to the project workspace
     */
    class PackedResourceLoader : IResourceLoader
    {
        /** */
        private string[] CHECKED_PATHS = { "", "FunctionalSpecification/", "Screens/" };

        /** */
        private string url;

        /**
         * 
         */
        public PackedResourceLoader(string url)
        {
            this.url = url;
        }

        /**
         * 
         */
        public IEnumerator Init()
        {
            return null;
        }

        /**
         * Loads an asset from the workspace. It first tries to load it by combining
         * the give URL and the given file name. If this fails, it tries to load also
         * from url + "FunctionalSpecification/" + fileName and
         * url + "Screens/" + fileName. This is done for backwards compatibility with
         * older prototype configurations.
         */
        public T LoadAsset<T>(string fileName) where T : UnityEngine.Object
        {
            foreach (string path in CHECKED_PATHS)
            {
                string effectiveUrl = url + "/" + path + fileName;

                int index = effectiveUrl.LastIndexOf('.');

                if (index > 0)
                {
                    effectiveUrl = effectiveUrl.Substring(0, index);
                }

                T retVal = (T)Resources.Load<T>(effectiveUrl);

                if (retVal != null)
                {
                    return retVal;
                }
            }

            throw new ArgumentException("there is no asset named " + fileName +
                                        " available in the resources at " + this.url);
        }
    }


    /**
     * Reader for resources from a resources folder whose location is defined by a URL.
     * The URL must always be relative to the project workspace
     */
    class Base64ZipContentResourceLoader : IResourceLoader
    {
        /** */
        private string[] CHECKED_PATHS = { "", "FunctionalSpecification/", "Screens/" };

        /** */
        private ZipArchive archive;

        /**
         * 
         */
        public Base64ZipContentResourceLoader(string url)
        {
            var bytes = Convert.FromBase64String(url);
            this.archive = new ZipArchive(new MemoryStream(bytes));
        }

        /**
         * 
         */
        public IEnumerator Init()
        {
            return null;
        }

        /**
         * Loads an asset from the workspace. It first tries to load it by combining
         * the give URL and the given file name. If this fails, it tries to load also
         * from url + "FunctionalSpecification/" + fileName and
         * url + "Screens/" + fileName. This is done for backwards compatibility with
         * older prototype configurations.
         */
        public T LoadAsset<T>(string fileName) where T : UnityEngine.Object
        {
            foreach (string path in CHECKED_PATHS)
            {
                string effectiveUrl = path + fileName;

                ZipArchiveEntry entry = this.archive.GetEntry(effectiveUrl);

                if (entry != null)
                {
                    using (Stream content = entry.Open())
                    {
                        if (typeof(T) == typeof(TextAsset))
                        {
                            string text = new StreamReader(content).ReadToEnd();
                            return new TextAsset(text) as T;
                        }
                        else if (typeof(T) == typeof(Texture2D))
                        {
                            MemoryStream ms = new MemoryStream();
                            content.CopyTo(ms);
                            byte[] data = ms.ToArray();

                            Texture2D tex = new Texture2D(2, 2);
                            tex.LoadImage(data);
                            
                            return tex as T;
                        }
                        else if (typeof(T) == typeof(AudioClip))
                        {
                            MemoryStream ms = new MemoryStream();
                            content.CopyTo(ms);
                            byte[] data = ms.ToArray();

                            float[] samples = new float[data.Length / 4]; //size of a float is 4 bytes

                            Buffer.BlockCopy(data, 0, samples, 0, data.Length);

                            int channels = 2;
                            int sampleRate = 44100;

                            AudioClip clip = AudioClip.Create("ClipName", samples.Length, channels, sampleRate, false);
                            clip.SetData(samples, 0);

                            return clip as T;
                        }
                    }
                }
            }

            throw new ArgumentException("there is no asset named " + fileName +
                                        " available in the zip content");
        }
    }

}
