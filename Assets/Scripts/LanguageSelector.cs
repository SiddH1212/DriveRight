using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LanguageSelector : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown languageDropdown;

    private readonly Dictionary<string, string> languageMap = new()
    {
        { "English", "en" },
        { "Hindi", "hi" },
        { "Tamil", "ta" },
        { "Telugu", "te" },
        { "Malayalam", "ml" },
        { "Gujarati", "gu" },
        { "Bengali", "bn" },
        { "Urdu", "ur" },
        { "Kannada", "kn" }
    };

    private void Start()
    {
        languageDropdown.ClearOptions();
        List<string> options = new(languageMap.Keys);
        languageDropdown.AddOptions(options);

        // Set initial selection from the static variable
        string currentCode = LanguageTranslator.SelectedLanguage;
        int index = options.FindIndex(name => languageMap[name] == currentCode);
        languageDropdown.value = index >= 0 ? index : 0;

        // Trigger initial change
        OnLanguageChanged(languageDropdown.value);

        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
    }

    private void OnLanguageChanged(int index)
    {
        string selectedDisplayName = languageDropdown.options[index].text;

        if (languageMap.TryGetValue(selectedDisplayName, out string code))
        {
            if (LanguageTranslator.Instance != null)
            {
                LanguageTranslator.Instance.SetLanguage(code);
            }
            else
            {
                LanguageTranslator.SelectedLanguage = code; // fallback
            }

            Debug.Log($"Selected: {selectedDisplayName} ({code})");
        }
    }
}
