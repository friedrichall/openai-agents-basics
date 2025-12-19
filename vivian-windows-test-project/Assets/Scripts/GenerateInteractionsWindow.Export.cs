#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public partial class GenerateInteractionsWindow
{
    private void CreateInteractionObjects()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string basePath = Path.Combine(projectRoot, "Packages", "vivian-example-prototypes", "Resources");
        string groupPath = Path.Combine(basePath, _groupName);

        Directory.CreateDirectory(groupPath);

        var topLevel = GetTopLevelOnly(_selectedObjects);
        var sceneExport = BuildSceneExport(topLevel);

        string jsonPath = Path.Combine(groupPath, "scene.json");
        string json = JsonUtility.ToJson(sceneExport, true);
        var utf8NoBom = new UTF8Encoding(false);
        File.WriteAllText(jsonPath, json, utf8NoBom);

        RenderResult renderResult;
        try
        {
            renderResult = RenderViews(groupPath, topLevel);
        }
        catch (Exception e)
        {
            Debug.LogError($"Rendering failed for group {_groupName}: {e.Message}");
            renderResult = CreateRenderResult(groupPath);
        }

        string manifestPath = WriteViewsManifest(groupPath, renderResult);

        AssetDatabase.Refresh();

        RunPythonGenerator(jsonPath);

        Debug.Log($"Created JSON export: {jsonPath}. Rendered {renderResult.imageCount} images to {renderResult.viewsPath} and manifest: {manifestPath}.");
    }

    private RenderResult CreateRenderResult(string groupPath)
    {
        string viewsPath = Path.Combine(groupPath, ViewsFolderName);
        Directory.CreateDirectory(viewsPath);

        return new RenderResult
        {
            manifestObjects = new List<RenderedObjectManifest>(),
            imageCount = 0,
            viewsPath = viewsPath,
            renderSettings = new RenderSettingsData
            {
                width = RenderWidth,
                height = RenderHeight,
                projectionType = "perspective",
                fov = CameraFov,
                paddingFactor = PaddingFactor
            }
        };
    }

    private string WriteViewsManifest(string groupPath, RenderResult renderResult)
    {
        var manifest = new ViewsManifest
        {
            groupName = _groupName,
            renderSettings = renderResult.renderSettings,
            objects = renderResult.manifestObjects,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        string manifestPath = Path.Combine(groupPath, "views_manifest.json").Replace("\\", "/");
        var utf8NoBom = new UTF8Encoding(false);
        File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true), utf8NoBom);
        return manifestPath;
    }

    private string SanitizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Object";
        string sanitized = SafeNameRegex.Replace(name, "_");
        return string.IsNullOrEmpty(sanitized) ? "Object" : sanitized;
    }

    private SceneExport BuildSceneExport(List<GameObject> topLevel)
    {
        var export = new SceneExport
        {
            groupName = _groupName,
            description = _interactionDescription,
            objects = new List<ExportedObject>()
        };

        foreach (var go in topLevel)
        {
            if (go == null) continue;
            export.objects.Add(SerializeGameObject(go));
        }

        return export;
    }

    private ExportedObject SerializeGameObject(GameObject go)
    {
        var data = new ExportedObject
        {
            name = go.name,
            transform = new SerializableTransform(go.transform),
            mesh = ExtractMesh(go),
            materials = ExtractMaterials(go),
            children = new List<ExportedObject>()
        };

        for (int i = 0; i < go.transform.childCount; i++)
        {
            data.children.Add(SerializeGameObject(go.transform.GetChild(i).gameObject));
        }

        return data;
    }

    private SerializableMesh ExtractMesh(GameObject go)
    {
        var meshFilter = go.GetComponent<MeshFilter>();
        var skinnedMesh = go.GetComponent<SkinnedMeshRenderer>();

        var mesh = meshFilter != null ? meshFilter.sharedMesh : skinnedMesh != null ? skinnedMesh.sharedMesh : null;
        if (mesh == null) return null;

        return new SerializableMesh
        {
            vertices = new List<Vector3>(mesh.vertices),
            normals = new List<Vector3>(mesh.normals),
            uvs = new List<Vector2>(mesh.uv),
            triangles = mesh.triangles
        };
    }

    private List<SerializableMaterial> ExtractMaterials(GameObject go)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null || renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0) return null;

        var result = new List<SerializableMaterial>();
        foreach (var mat in renderer.sharedMaterials)
        {
            if (mat == null) continue;

            var serialized = new SerializableMaterial
            {
                name = mat.name
            };

            if (mat.HasProperty("_Color"))
            {
                serialized.color = mat.color;
            }

            if (mat.mainTexture != null)
            {
                serialized.mainTexture = mat.mainTexture.name;
            }

            result.Add(serialized);
        }

        return result.Count > 0 ? result : null;
    }

    private void RunPythonGenerator(string jsonPath)
    {
        //string repoRoot = @"C:\Users\fried\Projekte\BA\vivian-llm-specgen";
        // Assets path: .../vivian-windows-test-project/Assets
        // Repo root:   .../vivian-llm-specgen
        string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        string scriptPath = Path.Combine(repoRoot, "unityconnector.py");
        if (!File.Exists(scriptPath))
        {
            Debug.LogError($"Script not found at {scriptPath}");
            return;
        }

        string python = Path.Combine(repoRoot, ".venv\\Scripts\\python.exe");
        if (!File.Exists(python))
        {
            Debug.LogError($"Python interpreter not found at {python}");
            return;
        }

        string escapedDesc = _interactionDescription.Replace("\"", "\\\"");
        var args = new List<string>
        {
            $"\"{scriptPath}\"",
            $"\"{_groupName}\"",
            $"\"{escapedDesc}\"",
            $"\"{jsonPath}\""
        };
        foreach (var go in _selectedObjects)
        {
            args.Add($"\"{go.name}\"");
        }

        var psi = new ProcessStartInfo
        {
            FileName = python,
            Arguments = string.Join(" ", args),
            WorkingDirectory = repoRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using (var proc = Process.Start(psi))
            {
                proc.WaitForExit();
                string output = proc.StandardOutput.ReadToEnd();
                if (!string.IsNullOrEmpty(output))
                {
                    Debug.Log(output);
                }
                if (proc.ExitCode != 0)
                {
                    Debug.LogError(proc.StandardError.ReadToEnd());
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to run python script: {e.Message}");
        }
    }
}
#endif
