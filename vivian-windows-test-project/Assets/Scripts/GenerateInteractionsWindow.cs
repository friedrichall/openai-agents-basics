#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Debug = UnityEngine.Debug;

public class GenerateInteractionsWindow : EditorWindow
{
    [MenuItem("Assets/Generate Interactions")]
    public static void ShowWindow()
    {
        GetWindow<GenerateInteractionsWindow>(true, "generate interactions");
    }

    private enum Step
    {
        SelectObjects,
        DefineInteractionElements
    }

    private Step _currentStep = Step.SelectObjects;
    private Vector2 _scrollPos;
    private readonly Dictionary<GameObject, bool> _selection = new Dictionary<GameObject, bool>();
    private readonly List<GameObject> _selectedObjects = new List<GameObject>();
    private string _groupName = string.Empty;
    private string _interactionDescription = string.Empty;
    private int _renderWidth = 1024;
    private int _renderHeight = 1024;
    private float _cameraFov = 45f;
    private float _paddingFactor = 1.2f;
    private Color _backgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    private Camera _previewCamera;
    private RenderTexture _previewTexture;

    private const string ViewsFolderName = "views";
    private static readonly ViewDirection[] ViewDirections =
    {
        new ViewDirection("front", Vector3.back),
        new ViewDirection("back", Vector3.forward),
        new ViewDirection("left", Vector3.left),
        new ViewDirection("right", Vector3.right),
        new ViewDirection("top", Vector3.up),
        new ViewDirection("bottom", Vector3.down),
        new ViewDirection("iso_top_left", new Vector3(-1f, 1f, 1f).normalized),
        new ViewDirection("iso_top_right", new Vector3(1f, 1f, 1f).normalized)
    };
    private static readonly Regex SafeNameRegex = new Regex("[^A-Za-z0-9_-]", RegexOptions.Compiled);

    private void OnGUI()
    {
        if (_currentStep == Step.SelectObjects)
        {
            DrawSelectionStep();
        }
        else if (_currentStep == Step.DefineInteractionElements)
        {
            DrawInteractionElementsStep();
        }
    }

    // Generate Interactions - Screen 1
    private void DrawSelectionStep()
    {
        EditorGUILayout.LabelField("GameObjects in Active Scene", EditorStyles.boldLabel);

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            EditorGUILayout.LabelField("No active scene loaded.");
            return;
        }

        var allObjects = new List<GameObject>();
        foreach (var root in scene.GetRootGameObjects())
        {
            CollectChildren(root, allObjects);
        }

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Select", GUILayout.Width(50));
        EditorGUILayout.LabelField("GameObject");
        EditorGUILayout.LabelField("Active", GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        foreach (var go in allObjects)
        {
            EditorGUILayout.BeginHorizontal();

            bool isSelected = _selection.ContainsKey(go) && _selection[go];
            bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(50));
            if (newSelected != isSelected)
            {
                _selection[go] = newSelected;
            }

            EditorGUILayout.ObjectField(go, typeof(GameObject), true);
            EditorGUILayout.Toggle(go.activeInHierarchy, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        _groupName = EditorGUILayout.TextField("Group Name", _groupName);

        if (GUILayout.Button("Next"))
        {
            PrepareInteractionDefinition();
        }
    }

    
    private void DrawInteractionElementsStep()
    {
        EditorGUILayout.LabelField("Selected Objects", EditorStyles.boldLabel);
        foreach (var go in _selectedObjects)
        {
            EditorGUILayout.LabelField(go.name);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Interaction Description", EditorStyles.boldLabel);
        _interactionDescription = EditorGUILayout.TextArea(_interactionDescription, GUILayout.MinHeight(60));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Render Settings", EditorStyles.boldLabel);
        _renderWidth = Mathf.Clamp(EditorGUILayout.IntField("Width", _renderWidth), 64, 8192);
        _renderHeight = Mathf.Clamp(EditorGUILayout.IntField("Height", _renderHeight), 64, 8192);
        _cameraFov = Mathf.Clamp(EditorGUILayout.FloatField("Field of View", _cameraFov), 1f, 120f);
        _paddingFactor = Mathf.Max(1f, EditorGUILayout.FloatField("Padding Factor", _paddingFactor));
        _backgroundColor = EditorGUILayout.ColorField("Background Color", _backgroundColor);

        if (GUILayout.Button("Create Interaction Objects"))
        {
            CreateInteractionObjects();
            _currentStep = Step.SelectObjects;
        }
    }

    // Generate Interactions - Screen 2
    private void PrepareInteractionDefinition()
    {
        _selectedObjects.Clear();
        foreach (var kv in _selection)
        {
            if (kv.Value)
            {
                _selectedObjects.Add(kv.Key);
            }
        }

        if (_selectedObjects.Count == 0 || string.IsNullOrEmpty(_groupName))
        {
            Debug.LogWarning("No objects selected or group name empty.");
            return;
        }

        _currentStep = Step.DefineInteractionElements;
    }

    private void CreateInteractionObjects()
    {
        string basePath = "Packages/vivian-example-prototypes/Resources";
        string groupPath = Path.Combine(basePath, _groupName);

        // Only ensure the group folder exists; no prefab creation anymore.
        Directory.CreateDirectory(groupPath);

        var topLevel = GetTopLevelOnly(_selectedObjects);
        var sceneExport = BuildSceneExport(topLevel);

        string jsonPath = Path.Combine(groupPath, "scene.json").Replace("\\", "/");
        string json = JsonUtility.ToJson(sceneExport, true);
        // Write without BOM so downstream JSON parsers don't choke on UTF-8 BOM.
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

        // Trigger Python generator with JSON payload instead of prefab path.
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
                width = _renderWidth,
                height = _renderHeight,
                projectionType = "perspective",
                fov = _cameraFov,
                paddingFactor = _paddingFactor
            }
        };
    }

    private RenderResult RenderViews(string groupPath, List<GameObject> topLevel)
    {
        var result = CreateRenderResult(groupPath);
        EnsurePreviewResources();

        try
        {
            foreach (var go in topLevel)
            {
                if (go == null) continue;
                var bounds = CalculateWorldBounds(go);
                if (!bounds.HasValue)
                {
                    Debug.LogWarning($"Skipping render for {go.name}: could not determine bounds.");
                    continue;
                }

                var manifestObject = new RenderedObjectManifest
                {
                    objectName = go.name,
                    stableId = go.GetInstanceID(),
                    views = new List<ManifestView>()
                };

                foreach (var view in ViewDirections)
                {
                    var viewData = RenderViewForObject(go, bounds.Value, view, result.viewsPath);
                    if (viewData != null)
                    {
                        manifestObject.views.Add(viewData);
                        result.imageCount++;
                    }
                }

                result.manifestObjects.Add(manifestObject);
            }
        }
        finally
        {
            CleanupPreviewResources();
        }

        return result;
    }

    private ManifestView RenderViewForObject(GameObject target, Bounds bounds, ViewDirection view, string viewsPath)
    {
        RenderTexture previousRt = RenderTexture.active;
        Texture2D tex = null;
        try
        {
            var cam = EnsurePreviewResources();
            var direction = view.direction.normalized;
            float distance = CalculateCameraDistance(bounds, direction);
            cam.transform.position = bounds.center + direction * distance;
            cam.transform.LookAt(bounds.center);
            cam.backgroundColor = _backgroundColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.fieldOfView = _cameraFov;
            cam.aspect = (float)_renderWidth / Mathf.Max(1, _renderHeight);
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = Mathf.Max(distance * 4f, 100f);
            cam.cullingMask = ~0;

            var rt = GetOrCreateRenderTexture();
            cam.targetTexture = rt;
            RenderTexture.active = rt;

            cam.Render();

            tex = new Texture2D(_renderWidth, _renderHeight, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, _renderWidth, _renderHeight), 0, 0);
            tex.Apply();

            string safeName = SanitizeName(target.name);
            string fileName = $"{safeName}_{view.name}.png";
            string fullPath = Path.Combine(viewsPath, fileName);
            byte[] pngBytes = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, pngBytes);

            return new ManifestView
            {
                viewName = view.name,
                file = $"{ViewsFolderName}/{fileName}".Replace("\\", "/"),
                cameraPose = new ManifestPose
                {
                    position = cam.transform.position,
                    rotation = cam.transform.rotation
                },
                lookAt = bounds.center
            };
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to render {target.name} ({view.name}): {e.Message}");
            return null;
        }
        finally
        {
            if (tex != null)
            {
                UnityEngine.Object.DestroyImmediate(tex);
            }
            RenderTexture.active = previousRt;
            if (_previewCamera != null)
            {
                _previewCamera.targetTexture = null;
            }
        }
    }

    private float CalculateCameraDistance(Bounds bounds, Vector3 viewDirection)
    {
        Vector3 size = bounds.size * _paddingFactor;
        float aspect = (float)_renderWidth / Mathf.Max(1, _renderHeight);
        float halfVerticalFov = Mathf.Deg2Rad * _cameraFov * 0.5f;

        float distanceForHeight = (size.y * 0.5f) / Mathf.Tan(halfVerticalFov);
        float horizontalFov = 2f * Mathf.Atan(Mathf.Tan(halfVerticalFov) * aspect);
        float distanceForWidth = (size.x * 0.5f) / Mathf.Tan(horizontalFov * 0.5f);

        Vector3 dirAbs = new Vector3(Mathf.Abs(viewDirection.x), Mathf.Abs(viewDirection.y), Mathf.Abs(viewDirection.z));
        float depthOffset = Vector3.Dot(bounds.extents * _paddingFactor, dirAbs);

        return Mathf.Max(distanceForHeight, distanceForWidth) + depthOffset;
    }

    private Bounds? CalculateWorldBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        Bounds? combined = null;
        foreach (var renderer in renderers)
        {
            if (combined == null)
            {
                combined = renderer.bounds;
            }
            else
            {
                var bounds = combined.Value;
                bounds.Encapsulate(renderer.bounds);
                combined = bounds;
            }
        }

        if (combined.HasValue) return combined;

        // Fallback to a small bounds around the transform if no renderer is present.
        return new Bounds(go.transform.position, Vector3.one * 0.25f);
    }

    private Camera EnsurePreviewResources()
    {
        if (_previewCamera == null)
        {
            var go = new GameObject("InteractionPreviewCamera");
            go.hideFlags = HideFlags.HideAndDontSave;
            _previewCamera = go.AddComponent<Camera>();
            _previewCamera.enabled = false;
        }

        _previewCamera.backgroundColor = _backgroundColor;
        _previewCamera.clearFlags = CameraClearFlags.SolidColor;
        _previewCamera.orthographic = false;
        _previewCamera.fieldOfView = _cameraFov;
        _previewCamera.aspect = (float)_renderWidth / Mathf.Max(1, _renderHeight);
        _previewCamera.targetTexture = GetOrCreateRenderTexture();

        return _previewCamera;
    }

    private RenderTexture GetOrCreateRenderTexture()
    {
        if (_previewTexture != null && (_previewTexture.width != _renderWidth || _previewTexture.height != _renderHeight))
        {
            _previewTexture.Release();
            UnityEngine.Object.DestroyImmediate(_previewTexture);
            _previewTexture = null;
        }

        if (_previewTexture == null)
        {
            _previewTexture = new RenderTexture(_renderWidth, _renderHeight, 24)
            {
                antiAliasing = 4
            };
        }

        return _previewTexture;
    }

    private void CleanupPreviewResources()
    {
        if (_previewTexture != null)
        {
            _previewTexture.Release();
            UnityEngine.Object.DestroyImmediate(_previewTexture);
            _previewTexture = null;
        }

        if (_previewCamera != null)
        {
            UnityEngine.Object.DestroyImmediate(_previewCamera.gameObject);
            _previewCamera = null;
        }
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
        string repoRoot = @"C:\Users\fried\Projekte\BA\vivian-llm-specgen";
        //string scriptPath = Path.Combine("C:\\Users\\Friedrich\\PycharmProjects\\vivian-specgen\\unityconnector.py");
        string scriptPath = Path.Combine(repoRoot, "unityconnector.py");
        if (!File.Exists(scriptPath))
        {
            Debug.LogError($"Script not found at {scriptPath}");
            return;
        }
        // Use the repo venv Python
        string python = Path.Combine(repoRoot, ".venv\\Scripts\\python.exe");
        if (!File.Exists(python))
        {
            Debug.LogError($"Python interpreter not found at {python}");
            return;
        }
        string escapedDesc = _interactionDescription.Replace("\"", "\\\"");
        // group name is the first CLI argument
        var args = new List<string>
        {
            $"\"{scriptPath}\"",
            $"\"{_groupName}\"",
            $"\"{escapedDesc}\"",
            $"\"{jsonPath}\"" // Argument: path to JSON asset representation
        };
        foreach (var go in _selectedObjects)
        {
            args.Add($"\"{go.name}\"");
        }
        var psi = new ProcessStartInfo
        {
            FileName = python,
            Arguments = string.Join(" ", args),
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
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to run python script: {e.Message}");
        }
    }


    private void CollectChildren(GameObject go, List<GameObject> list)
    {
        list.Add(go);
        for (int i = 0; i < go.transform.childCount; i++)
        {
            CollectChildren(go.transform.GetChild(i).gameObject, list);
        }
    }

    // Returns only top-level objects (ignores selected ancestors)
    private static List<GameObject> GetTopLevelOnly(List<GameObject> selected)
    {
        var result = new List<GameObject>();
        var selectedSet = new HashSet<Transform>();
        foreach (var go in selected)
        {
            if (go != null) selectedSet.Add(go.transform);
        }

        foreach (var go in selected)
        {
            if (go == null) continue;
            bool hasSelectedAncestor = false;
            var t = go.transform.parent;
            while (t != null)
            {
                if (selectedSet.Contains(t)) { hasSelectedAncestor = true; break; }
                t = t.parent;
            }
            if (!hasSelectedAncestor) result.Add(go);
        }
        return result;
    }

    private class RenderResult
    {
        public List<RenderedObjectManifest> manifestObjects;
        public int imageCount;
        public string viewsPath;
        public RenderSettingsData renderSettings;
    }

    [System.Serializable]
    private class ViewsManifest
    {
        public string groupName;
        public RenderSettingsData renderSettings;
        public List<RenderedObjectManifest> objects;
        public string timestamp;
    }

    [System.Serializable]
    private class RenderSettingsData
    {
        public int width;
        public int height;
        public string projectionType;
        public float fov;
        public float paddingFactor;
    }

    [System.Serializable]
    private class RenderedObjectManifest
    {
        public string objectName;
        public int stableId;
        public List<ManifestView> views;
    }

    [System.Serializable]
    private class ManifestView
    {
        public string viewName;
        public string file;
        public ManifestPose cameraPose;
        public Vector3 lookAt;
    }

    [System.Serializable]
    private class ManifestPose
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    private class ViewDirection
    {
        public string name;
        public Vector3 direction;

        public ViewDirection(string name, Vector3 direction)
        {
            this.name = name;
            this.direction = direction;
        }
    }

    [System.Serializable]
    private class SceneExport
    {
        public string groupName;
        public string description;
        public List<ExportedObject> objects;
    }

    [System.Serializable]
    private class ExportedObject
    {
        public string name;
        public SerializableTransform transform;
        public SerializableMesh mesh;
        public List<SerializableMaterial> materials;
        public List<ExportedObject> children;
    }

    [System.Serializable]
    private class SerializableTransform
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public SerializableTransform(Transform t)
        {
            position = t.position;
            rotation = t.rotation;
            scale = t.lossyScale;
        }
    }

    [System.Serializable]
    private class SerializableMesh
    {
        public List<Vector3> vertices;
        public List<Vector3> normals;
        public List<Vector2> uvs;
        public int[] triangles;
    }

    [System.Serializable]
    private class SerializableMaterial
    {
        public string name;
        public Color color;
        public string mainTexture;
    }
}
#endif
