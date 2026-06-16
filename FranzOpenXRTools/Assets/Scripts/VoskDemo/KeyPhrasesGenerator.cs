using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

public class KeyPhrasesGenerator : MonoBehaviour
{
    private const string k_poetryJsonPath = "poetry/poetry_tangsong.json";
    private const string k_keyPhrasesPath = "poetry/keyphrases.txt";
    private const int k_maxPhrases = 5000;
    
    [ContextMenu("Generate KeyPhrases")]
    public void GenerateKeyPhrases()
    {
        string jsonPath = Path.Combine(Application.streamingAssetsPath, k_poetryJsonPath);
        
        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"[KeyPhrasesGenerator] Poetry JSON not found: {jsonPath}");
            return;
        }

        try
        {
            Debug.Log("[KeyPhrasesGenerator] Loading poetry data...");
            string jsonContent = File.ReadAllText(jsonPath, Encoding.UTF8);
            List<PoetryData> poetryList = JsonConvert.DeserializeObject<List<PoetryData>>(jsonContent);
            
            Debug.Log($"[KeyPhrasesGenerator] Processing {poetryList.Count} poems...");
            
            HashSet<string> phrases = new HashSet<string>();
            
            foreach (PoetryData poetry in poetryList)
            {
                if (phrases.Count >= k_maxPhrases)
                {
                    break;
                }
                
                string content = poetry.content.Replace(" ", "");
                string[] parts = content.Split(new char[] { '，', '。', '！', '？' }, System.StringSplitOptions.RemoveEmptyEntries);
                
                foreach (string part in parts)
                {
                    string trimmed = part.Trim();
                    if (trimmed.Length >= 4 && trimmed.Length <= 20)
                    {
                        phrases.Add(trimmed);
                    }
                    
                    if (phrases.Count >= k_maxPhrases)
                    {
                        break;
                    }
                }
            }
            
            string outputPath = Path.Combine(Application.streamingAssetsPath, k_keyPhrasesPath);
            string outputContent = string.Join("\n", phrases);
            File.WriteAllText(outputPath, outputContent, Encoding.UTF8);
            
            Debug.Log($"[KeyPhrasesGenerator] Generated {phrases.Count} key phrases to {outputPath}");
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog("KeyPhrases Generated", $"Successfully generated {phrases.Count} key phrases!", "OK");
#endif
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[KeyPhrasesGenerator] Error: {ex.Message}");
        }
    }
    
    [System.Serializable]
    private struct PoetryData
    {
        public string title;
        public string dynasty;
        public string author;
        public string content;
    }
}
