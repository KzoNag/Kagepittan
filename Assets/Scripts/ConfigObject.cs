using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif

[System.Serializable]
public class Config
{
    [System.Serializable]
    public class Camera
    {
        public int index = 0;
        public bool flip = false;
        public RectOffset offset;
    }

    [System.Serializable]
    public class Game
    {
        [Range(0, 1)]
        public float excessRate = 0.15f;

        [Range(0, 1)]
        public float goalRate = 0.5f;
    }

    [System.Serializable]
    public class ImageProcess
    {
        [Range(0.0f, 255.0f)]
        public float maskThreshold = 40.0f;

        [Range(0.0f, 255.0f)]
        public float maskMaxVal = 255.0f;

        [Range(1, 5)]
        public int smoothIterationCount = 1;
    }

    public Camera camera = new Camera();
    public Game game = new Game();
    public ImageProcess imageProcess = new ImageProcess();
}

[CreateAssetMenu]
public class ConfigObject : ScriptableObject
{
    public Config config;

    public const string ConfigFileName = "config.json";

    public void LoadConfig()
    {
        var path = Path.Combine(Application.streamingAssetsPath, ConfigFileName);
        var json = File.ReadAllText(path);
        config = JsonUtility.FromJson<Config>(json);
    }

    public void SaveConfig(string path)
    {
        var json = JsonUtility.ToJson(config, true);
        File.WriteAllText(path, json);
    }

#if UNITY_EDITOR
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        var assetPath = "Assets/Data/config.asset";

        var playerDirPath = Path.GetDirectoryName(pathToBuiltProject);
        var playerName = Path.GetFileNameWithoutExtension(pathToBuiltProject);
        var dataDirName = playerName + "_Data";
        var savePath = string.Format("{0}\\{1}\\{2}\\{3}", playerDirPath, dataDirName, "StreamingAssets", ConfigFileName);

        var configObject = AssetDatabase.LoadAssetAtPath<ConfigObject>(assetPath);
        configObject.SaveConfig(savePath);
    }
#endif
}

