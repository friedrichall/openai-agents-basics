#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text;
using Debug = UnityEngine.Debug;

public class GenerateInteractionsWindow : EditorWindow
{
    [MenuItem("Assets/generateinteractions")]
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

        if (GUILayout.Button("Create Interaction Objects"))
        {
            CreateInteractionObjects();
            _currentStep = Step.SelectObjects;
        }
    }

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

        AssetDatabase.Refresh();

        // Trigger Python generator with JSON payload instead of prefab path.
        RunPythonGenerator(jsonPath);

        Debug.Log($"Created JSON export: {jsonPath}.");
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
        string repoRoot = @"C:\Users\fried\Projekte\BA\OpenAI-HelloAgents";
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
