using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LanguageTranslator : MonoBehaviour
{
    public static LanguageTranslator Instance;

    [Header("Language Selection")]
    public static string SelectedLanguage = "en";
    public string language = "en"; // Used internally

    [Header("Font Assets")]
    public TMP_FontAsset Noto_Hindi;
    public TMP_FontAsset Noto_Tamil;
    public TMP_FontAsset Noto_Telugu;
    public TMP_FontAsset Noto_Malayalam;
    public TMP_FontAsset Noto_Gujarati;
    public TMP_FontAsset Noto_Bengali;
    public TMP_FontAsset Noto_Urdu;
    public TMP_FontAsset Noto_Kannada;
    public TMP_FontAsset DefaultFont;

    private Dictionary<string, Dictionary<string, string>> translationMap = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Use saved static language
            language = SelectedLanguage;

            LoadTranslations();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadTranslations()
    {
        TextAsset json = Resources.Load<TextAsset>("violations_translation");
        if (json == null)
        {
            Debug.LogError("Could not load violations_translation.json from Resources!");
            return;
        }

        TranslationWrapper wrapper = JsonUtility.FromJson<TranslationWrapper>(json.text);
        if (wrapper == null)
        {
            Debug.LogError("Failed to parse translation JSON!");
            return;
        }

        foreach (TranslationEntry entry in wrapper.entries)
        {
            Dictionary<string, string> langDict = new();
            foreach (LanguageTranslation lt in entry.translations)
            {
                langDict[lt.lang] = lt.text;
            }
            translationMap[entry.message] = langDict;
        }

        Debug.Log("Translations loaded successfully.");
    }

    public void SetLanguage(string langCode)
    {
        SelectedLanguage = langCode;
        language = langCode;
        Debug.Log($"Language updated to: {language}");
    }

    public string Translate(string englishMessage)
    {
        if (string.IsNullOrEmpty(englishMessage)) return "";

        if (translationMap.TryGetValue(englishMessage.Trim(), out var langDict))
        {
            if (langDict.TryGetValue(language, out var result))
                return result;
        }

        return englishMessage; // fallback
    }

    public TMP_FontAsset GetFont()
    {
        return language switch
        {
            "hi" => Noto_Hindi,
            "ta" => Noto_Tamil,
            "te" => Noto_Telugu,
            "ml" => Noto_Malayalam,
            "gu" => Noto_Gujarati,
            "bn" => Noto_Bengali,
            "ur" => Noto_Urdu,
            "kn" => Noto_Kannada,
            _ => DefaultFont
        };
    }

    [System.Serializable]
    public class LanguageTranslation
    {
        public string lang;
        public string text;
    }

    [System.Serializable]
    public class TranslationEntry
    {
        public string message;
        public List<LanguageTranslation> translations;
    }

    [System.Serializable]
    public class TranslationWrapper
    {
        public List<TranslationEntry> entries;
    }
}
