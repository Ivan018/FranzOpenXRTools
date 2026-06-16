using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Vosk.APIs;

public class VoskDemo : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Name of the Model")]
    private const string ModelName = "vosk-model-small-cn-0.22";

    [Tooltip("Should the recognizer start automatically")]
    [SerializeField]
    private bool AutoStart = true;

    [Tooltip("The Max number of alternatives that will be processed")]
    [SerializeField]
    private int MaxAlternatives = 1;

    [Tooltip("The index of Microphone to use")]
    [SerializeField]
    private int microphoneIndex = 0;

    [Tooltip("The phrases that will be detected. If left empty, all words will be detected.\nKeywords need to exist in the models dictionary, so some words like \"webview\" are better detected as two more common words \"web view\".")]
    [SerializeField]
    private List<string> KeyPhrases = new List<string>();

    [Tooltip("Maximum number of poetry match results to display")]
    [SerializeField]
    private int MaxPoetryResults = 5;

    [Header("UI")]
    [SerializeField]
    private Text ResultText;

    [SerializeField]
    private Text PoetryResultText;

    private PoetryManager m_poetryManager;

    private void OnEnable()
    {
        VoskASR.OnTranscriptionResult += OnTranscriptionResult;
    }

    private void OnDisable()
    {
        VoskASR.OnTranscriptionResult -= OnTranscriptionResult;
    }
    private void Start()
    {
        for (int i = 0; i < KeyPhrases.Count; i++)
        {
            KeyPhrases[i] = KeyPhrases[i].Trim();
        }

        VoskASR.Init(this, ModelName, AutoStart, MaxAlternatives, microphoneIndex, KeyPhrases);
        
        m_poetryManager = FindObjectOfType<PoetryManager>();
        if (m_poetryManager == null)
        {
            Debug.LogWarning("PoetryManager not found in scene. Creating one.");
            m_poetryManager = gameObject.AddComponent<PoetryManager>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTranscriptionResult(string obj)
    {
#if UNITY_EDITOR
        Debug.Log(obj);
#endif

        RecognitionResult resultJson = JsonConvert.DeserializeObject<RecognitionResult>(obj);
        string transcribedTextUnk = resultJson.alternatives[0].text.Replace("[unk]", " ");
        string transcribedText = transcribedTextUnk.Replace(" ", "");
        ResultText.text += transcribedText;
        Debug.Log(transcribedText);
        if (transcribedText.Length >= 4)
        {
            PerformPoetryMatch(transcribedText);
        }
    }

    private void PerformPoetryMatch(string query)
    {
        if (m_poetryManager == null || !m_poetryManager.isLoaded)
        {
            return;
        }

        List<PoetryMatchResult> results = m_poetryManager.FuzzyMatch(query, MaxPoetryResults);
        
        if (results.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<color=green>诗词匹配结果：</color>");
            
            foreach (PoetryMatchResult result in results)
            {
                sb.AppendLine($"<color=yellow>《{result.title}》</color>");
                sb.AppendLine($"朝代：{result.dynasty}　作者：{result.author}");
                sb.AppendLine($"匹配诗句：{result.matchedLine}");
                sb.AppendLine($"匹配句数：{result.matchedLineCount}句");
                
                if (result.matchedLines != null && result.matchedLines.Count > 0)
                {
                    sb.AppendLine($"匹配的诗句：");
                    foreach (string line in result.matchedLines)
                    {
                        sb.AppendLine($"　{line}");
                    }
                }
                
                sb.AppendLine($"全诗：{result.fullContent}");
                sb.AppendLine();
            }
            
            PoetryResultText.text = sb.ToString();
        }
        else
        {
            PoetryResultText.text = query+"_未找到匹配的诗词";
        }
    }

    private struct RecognitionResult
    {
        public Tag[] alternatives;

        public class Tag
        {
            public float confidence;
            public string text;
        }
    }
}
