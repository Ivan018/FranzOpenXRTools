using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using UnityEditor;

public class PoetryJsonGenerator : MonoBehaviour
{
    [Header("Generator Settings")]
    public bool generateOnStart = false;
    
    private const string k_poetryFolder = "poetry";
    private const string k_outputJsonPath = "poetry/poetry_tangsong.json";

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateJson();
        }
    }

    [ContextMenu("Generate Poetry JSON")]
    public void GenerateJson()
    {
        string folderPath = Path.Combine(Application.streamingAssetsPath, k_poetryFolder);
        
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("Poetry folder not found!");
            return;
        }

        List<PoetryData> poetryList = new List<PoetryData>();
        string[] csvFiles = Directory.GetFiles(folderPath, "*.csv");

        foreach (string csvFile in csvFiles)
        {
            string fileName = Path.GetFileName(csvFile);
            
            if (!fileName.Contains("唐") && !fileName.Contains("宋"))
            {
                continue;
            }

            try
            {
                string[] lines = File.ReadAllLines(csvFile, Encoding.UTF8);
                
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    string[] parts = ParseCsvLine(line);
                    if (parts.Length >= 4)
                    {
                        string dynasty = parts[1].Trim();
                        
                        if (dynasty == "唐" || dynasty == "宋")
                        {
                            poetryList.Add(new PoetryData
                            {
                                title = parts[0].Trim(),
                                dynasty = dynasty,
                                author = parts[2].Trim(),
                                content = parts[3].Trim()
                            });
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to read {fileName}: {e.Message}");
            }
        }

        string outputPath = Path.Combine(Application.streamingAssetsPath, k_outputJsonPath);
        string jsonContent = JsonConvert.SerializeObject(poetryList, Formatting.Indented);
        File.WriteAllText(outputPath, jsonContent, Encoding.UTF8);

        Debug.Log($"Generated JSON with {poetryList.Count} entries to {outputPath}");
    }

    private string[] ParseCsvLine(string line)
    {
        List<string> parts = new List<string>();
        bool inQuotes = false;
        StringBuilder current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                parts.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        
        parts.Add(current.ToString());
        
        return parts.ToArray();
    }

    [System.Serializable]
    public struct PoetryData
    {
        public string title;
        public string dynasty;
        public string author;
        public string content;
    }
}

#if UNITY_EDITOR
public static class PoetryJsonGeneratorMenu
{
    private const string k_poetryFolder = "poetry";
    private const string k_outputJsonPath = "poetry/poetry_tangsong.json";

    [MenuItem("Tools/Generate Poetry JSON")]
    public static void GeneratePoetryJson()
    {
        string folderPath = Path.Combine(Application.streamingAssetsPath, k_poetryFolder);
        
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("Poetry folder not found!");
            EditorUtility.DisplayDialog("Error", "Poetry folder not found!", "OK");
            return;
        }

        List<PoetryJsonGenerator.PoetryData> poetryList = new List<PoetryJsonGenerator.PoetryData>();
        string[] csvFiles = Directory.GetFiles(folderPath, "*.csv");

        foreach (string csvFile in csvFiles)
        {
            string fileName = Path.GetFileName(csvFile);
            
            if (!fileName.Contains("唐") && !fileName.Contains("宋"))
            {
                continue;
            }

            try
            {
                string[] lines = File.ReadAllLines(csvFile, Encoding.UTF8);
                
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    string[] parts = ParseCsvLine(line);
                    if (parts.Length >= 4)
                    {
                        string dynasty = parts[1].Trim();
                        
                        if (dynasty == "唐" || dynasty == "宋")
                        {
                            poetryList.Add(new PoetryJsonGenerator.PoetryData
                            {
                                title = parts[0].Trim(),
                                dynasty = dynasty,
                                author = parts[2].Trim(),
                                content = parts[3].Trim()
                            });
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to read {fileName}: {e.Message}");
            }
        }

        string outputPath = Path.Combine(Application.streamingAssetsPath, k_outputJsonPath);
        string jsonContent = JsonConvert.SerializeObject(poetryList, Formatting.Indented);
        File.WriteAllText(outputPath, jsonContent, Encoding.UTF8);

        Debug.Log($"Generated JSON with {poetryList.Count} entries to {outputPath}");
        EditorUtility.DisplayDialog("Success", $"Generated JSON with {poetryList.Count} entries!\n\nOutput: {outputPath}", "OK");
    }

    private static string[] ParseCsvLine(string line)
    {
        List<string> parts = new List<string>();
        bool inQuotes = false;
        StringBuilder current = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                parts.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        
        parts.Add(current.ToString());
        
        return parts.ToArray();
    }
}
#endif