using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

public class PoetryManager : MonoBehaviour
{
    private List<PoetryData> m_poetryList = new List<PoetryData>();
    private Dictionary<char, HashSet<int>> m_invertedIndex = new Dictionary<char, HashSet<int>>();
    private const string k_poetryJsonPath = "poetry/poetry_simplified.json";
    private const string k_poetryFolder = "poetry";
    private const int k_minQueryLength = 4;
    
    private bool m_isLoaded = false;
    private bool m_isLoading = false;
    
    public bool isLoaded => m_isLoaded;
    public event System.Action OnLoaded;

    [System.Serializable]
    private struct PoetryData
    {
        public string title;
        public string dynasty;
        public string author;
        public string content;
    }

    private void Start()
    {
        StartCoroutine(LoadPoetryDataCo());
    }

    private IEnumerator LoadPoetryDataCo()
    {
        string jsonPath = Path.Combine(Application.streamingAssetsPath, k_poetryJsonPath);
        
#if UNITY_ANDROID
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(jsonPath))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.ProtocolError ||
                www.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError($"[PoetryManager] Failed to load JSON: {www.error}");
                yield break;
            }
            
            string jsonContent = www.downloadHandler.text;
            Debug.Log($"[PoetryManager] JSON loaded from Android, size: {jsonContent.Length} chars");
            
            m_poetryList = JsonConvert.DeserializeObject<List<PoetryData>>(jsonContent);
            Debug.Log($"[PoetryManager] Parsed {m_poetryList.Count} poetry entries");
        }
#else
        if (File.Exists(jsonPath))
        {
            try
            {
                Debug.Log("[PoetryManager] Loading JSON file from: " + jsonPath);
                string jsonContent = File.ReadAllText(jsonPath, Encoding.UTF8);
                Debug.Log($"[PoetryManager] JSON loaded, size: {jsonContent.Length} chars");
                
                m_poetryList = JsonConvert.DeserializeObject<List<PoetryData>>(jsonContent);
                Debug.Log($"[PoetryManager] Parsed {m_poetryList.Count} poetry entries");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PoetryManager] Failed to load JSON: {ex.Message}");
                LoadFromCsvFiles();
                yield break;
            }
        }
        else
        {
            Debug.LogWarning($"[PoetryManager] JSON file not found: {jsonPath}");
            LoadFromCsvFiles();
            yield break;
        }
#endif
        
        if (m_poetryList.Count > 0)
        {
            BuildInvertedIndex();
        }
        
        m_isLoaded = true;
        Debug.Log($"[PoetryManager] Loaded {m_poetryList.Count} entries");
        OnLoaded?.Invoke();
    }

    private void LoadFromCsvFiles()
    {
        string folderPath = Path.Combine(Application.streamingAssetsPath, k_poetryFolder);
        
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError("[PoetryManager] Poetry folder not found!");
            return;
        }

        string[] csvFiles = Directory.GetFiles(folderPath, "*.csv");
        int totalLoaded = 0;

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
                            PoetryData data = new PoetryData
                            {
                                title = parts[0].Trim(),
                                dynasty = dynasty,
                                author = parts[2].Trim(),
                                content = parts[3].Trim()
                            };
                            
                            m_poetryList.Add(data);
                            totalLoaded++;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[PoetryManager] Failed to read {fileName}: {ex.Message}");
            }
        }

        Debug.Log($"[PoetryManager] Loaded {totalLoaded} Tang/Song poetry entries from CSV");
        
        if (m_poetryList.Count > 0)
        {
            BuildInvertedIndex();
        }
        
        m_isLoaded = true;
        Debug.Log($"[PoetryManager] Loaded {m_poetryList.Count} entries");
        OnLoaded?.Invoke();
    }

    private void BuildInvertedIndex()
    {
        Debug.Log("[PoetryManager] Building inverted index...");
        m_invertedIndex.Clear();
        
        for (int i = 0; i < m_poetryList.Count; i++)
        {
            string content = m_poetryList[i].content.Replace(" ", "");
            
            foreach (char c in content)
            {
                if (!char.IsLetterOrDigit(c))
                {
                    continue;
                }
                
                if (!m_invertedIndex.ContainsKey(c))
                {
                    m_invertedIndex[c] = new HashSet<int>();
                }
                
                m_invertedIndex[c].Add(i);
            }
        }
        
        Debug.Log($"[PoetryManager] Index built with {m_invertedIndex.Count} characters");
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

    public List<PoetryMatchResult> FuzzyMatch(string query, int maxResults = 1)
    {
        if (!m_isLoaded || m_poetryList.Count == 0)
        {
            Debug.LogWarning($"[PoetryManager] Cannot match: isLoaded={m_isLoaded}, count={m_poetryList.Count}");
            return new List<PoetryMatchResult>();
        }

        List<PoetryMatchResult> results = new List<PoetryMatchResult>();
        string cleanQuery = query.Replace(" ", "").Trim();

        if (string.IsNullOrEmpty(cleanQuery) || cleanQuery.Length < k_minQueryLength)
        {
            Debug.LogWarning($"[PoetryManager] Query too short: '{cleanQuery}'");
            return results;
        }

        Debug.Log($"[PoetryManager] Searching for: '{cleanQuery}' in {m_poetryList.Count} poems");

        PoetryMatchResult bestMatch = default;
        int bestScore = -1;

        for (int i = 0; i < m_poetryList.Count; i++)
        {
            PoetryData poetry = m_poetryList[i];
            string cleanContent = poetry.content.Replace(" ", "");
            
            int exactMatchPos = cleanContent.IndexOf(cleanQuery);
            if (exactMatchPos != -1)
            {
                string matchedLine = ExtractMatchedLine(cleanContent, cleanQuery);
                List<string> matchedLines = ExtractAllMatchedLines(cleanContent, cleanQuery);
                
                PoetryMatchResult match = new PoetryMatchResult
                {
                    title = poetry.title,
                    dynasty = poetry.dynasty,
                    author = poetry.author,
                    fullContent = poetry.content,
                    matchedLine = matchedLine,
                    matchPosition = exactMatchPos,
                    matchedLines = matchedLines,
                    matchedLineCount = matchedLines.Count
                };
                
                Debug.Log($"[PoetryManager] Exact match found: {poetry.title}, matched {matchedLines.Count} lines");
                return new List<PoetryMatchResult> { match };
            }
        }

        for (int i = 0; i < m_poetryList.Count; i++)
        {
            PoetryData poetry = m_poetryList[i];
            string cleanContent = poetry.content.Replace(" ", "");
            
            string matchedSubstring = FindLongestSubstringMatch(cleanContent, cleanQuery);
            if (!string.IsNullOrEmpty(matchedSubstring) && matchedSubstring.Length >= 4)
            {
                int matchLength = matchedSubstring.Length;
                if (matchLength > bestScore)
                {
                    bestScore = matchLength;
                    string matchedLine = ExtractMatchedLine(cleanContent, matchedSubstring);
                    List<string> matchedLines = ExtractAllMatchedLines(cleanContent, cleanQuery);
                    
                    bestMatch = new PoetryMatchResult
                    {
                        title = poetry.title,
                        dynasty = poetry.dynasty,
                        author = poetry.author,
                        fullContent = poetry.content,
                        matchedLine = matchedLine,
                        matchPosition = matchLength,
                        matchedLines = matchedLines,
                        matchedLineCount = matchedLines.Count
                    };
                }
            }
        }

        if (bestScore >= 4)
        {
            Debug.Log($"[PoetryManager] Best match: {bestMatch.title}, score: {bestScore}");
            results.Add(bestMatch);
        }
        else
        {
            Debug.Log($"[PoetryManager] No match found for: '{cleanQuery}'");
        }

        return results;
    }

    private HashSet<int> GetCandidateIndices(string query)
    {
        HashSet<int> candidates = new HashSet<int>();
        
        foreach (char c in query)
        {
            if (!char.IsLetterOrDigit(c))
            {
                continue;
            }
            
            if (m_invertedIndex.TryGetValue(c, out HashSet<int> indices))
            {
                candidates.UnionWith(indices);
            }
        }
        
        return candidates.Count > 0 ? candidates : null;
    }

    private string FindLongestSubstringMatch(string content, string query)
    {
        int maxLength = 0;
        string longestMatch = "";
        int minMatchLength = 4;
        
        for (int i = 0; i <= query.Length - minMatchLength; i++)
        {
            int remainingLength = query.Length - i;
            int searchLength = Mathf.Min(remainingLength, content.Length);
            
            for (int len = Mathf.Max(minMatchLength, maxLength + 1); len <= searchLength; len++)
            {
                string substring = query.Substring(i, len);
                if (content.Contains(substring))
                {
                    maxLength = len;
                    longestMatch = substring;
                }
                else
                {
                    break;
                }
            }
        }

        return maxLength >= minMatchLength ? longestMatch : "";
    }

    private string ExtractMatchedLine(string content, string query)
    {
        int startIndex = content.IndexOf(query);
        if (startIndex == -1)
        {
            return content.Length > 30 ? content.Substring(0, 30) + "..." : content;
        }

        int lineStart = startIndex;
        while (lineStart > 0 && content[lineStart - 1] != '，' && content[lineStart - 1] != '。' && content[lineStart - 1] != '！' && content[lineStart - 1] != '？')
        {
            lineStart--;
        }

        int lineEnd = startIndex + query.Length;
        while (lineEnd < content.Length && content[lineEnd] != '，' && content[lineEnd] != '。' && content[lineEnd] != '！' && content[lineEnd] != '？')
        {
            lineEnd++;
        }

        return content.Substring(lineStart, lineEnd - lineStart);
    }

    private List<string> ExtractAllMatchedLines(string content, string query)
    {
        List<string> matchedLines = new List<string>();
        
        if (string.IsNullOrEmpty(content) || string.IsNullOrEmpty(query))
        {
            return matchedLines;
        }

        string[] separators = { "，", "。", "！", "？" };
        string[] lines = content.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine))
            {
                continue;
            }

            if (query.Contains(trimmedLine) || trimmedLine.Contains(query))
            {
                if (!matchedLines.Contains(trimmedLine))
                {
                    matchedLines.Add(trimmedLine);
                }
            }
            else
            {
                string commonSubstring = FindLongestCommonSubstring(trimmedLine, query);
                if (commonSubstring.Length >= 4)
                {
                    if (!matchedLines.Contains(trimmedLine))
                    {
                        matchedLines.Add(trimmedLine);
                    }
                }
            }
        }

        return matchedLines;
    }

    private string FindLongestCommonSubstring(string str1, string str2)
    {
        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
        {
            return "";
        }

        int maxLength = 0;
        string longestSubstring = "";

        for (int i = 0; i < str1.Length; i++)
        {
            for (int j = 0; j < str2.Length; j++)
            {
                int length = 0;
                while (i + length < str1.Length && j + length < str2.Length && str1[i + length] == str2[j + length])
                {
                    length++;
                }

                if (length > maxLength)
                {
                    maxLength = length;
                    longestSubstring = str1.Substring(i, length);
                }
            }
        }

        return longestSubstring;
    }
}

public struct PoetryMatchResult
{
    public string title;
    public string dynasty;
    public string author;
    public string fullContent;
    public string matchedLine;
    public int matchPosition;
    public List<string> matchedLines;
    public int matchedLineCount;
}
