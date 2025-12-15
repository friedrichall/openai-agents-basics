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

using UnityEditor;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
public class CreateAssetBundles
{
    [MenuItem("Assets/Build Vivian Prototype Bundles")]
    static public void BuildAllAssetBundles()
    {
        AssignAssetBundleIds();

        string assetBundleDirectory = "Assets/AssetBundles";

        BuildAllAssetBundlesFor(assetBundleDirectory + "/" + EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettings.activeBuildTarget);
    }

    static private void AssignAssetBundleIds()
    {
        Debug.Log("assigning asset bundle names");

        string package = "Packages/de.ugoe.cs.vivian.exampleprototypes/Resources";
        string prototypesPath = Path.GetFullPath(package);

        foreach (string asset in AssetDatabase.FindAssets("", new[] { package }))
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(asset);

            // determine the asset folder name
            int startIndex = package.Length;

            if (!package.EndsWith("/"))
            {
                startIndex++;
            }

            int endIndex = assetPath.IndexOf('/', startIndex);
            string assetFolder = null;

            if (endIndex > 0)
            {
                assetFolder = assetPath.Substring(startIndex, endIndex - startIndex);
            }

            if ((assetFolder != null) && (assetFolder != "Editor")) {
                // assign the asset bundle name which is basically the asset folder name
                AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(asset)).SetAssetBundleNameAndVariant(assetFolder, "");
            }
            else {
                // this asset lies top level or belongs to the editor scripts --> remove any assigned bundle name
                //AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(asset)).SetAssetBundleNameAndVariant(null, null);
            }
        }
    }

    static private void BuildAllAssetBundlesFor(string assetBundleDirectory, BuildTarget target)
    {
        Debug.Log(assetBundleDirectory);

        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }

        
        BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.ForceRebuildAssetBundle | BuildAssetBundleOptions.ChunkBasedCompression, target);    
    }
}
#endif