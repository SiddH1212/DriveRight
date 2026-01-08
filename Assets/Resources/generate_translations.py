import json
import requests
import time

# Load input messages
with open("messages.txt", "r", encoding="utf-8") as f:
    messages = [line.strip() for line in f if line.strip()]

# Target Indian languages and their codes
languages = {
    "hi": "Hindi",
    "ta": "Tamil",
    "te": "Telugu",
    "ml": "Malayalam",
    "gu": "Gujarati",
    "bn": "Bengali",
    "ur": "Urdu",
    "kn": "Kannada"
}

def translate(text, target_lang):
    """Translate using unofficial Google Translate API."""
    url = f"https://translate.googleapis.com/translate_a/single?client=gtx&sl=en&tl={target_lang}&dt=t&q={requests.utils.quote(text)}"
    try:
        response = requests.get(url)
        if response.status_code == 200:
            result = response.json()
            return result[0][0][0]
        else:
            print(f"[!] Failed for {target_lang} | Status Code: {response.status_code}")
            return text
    except Exception as e:
        print(f"[!] Error for {target_lang}: {e}")
        return text

output = {
    "entries": []
}

print(f"Translating {len(messages)} messages into {len(languages)} languages...\n")

for msg in messages:
    translation_list = []
    for lang_code in languages:
        translated = translate(msg, lang_code)
        translation_list.append({
            "lang": lang_code,
            "text": translated
        })
        print(f"{languages[lang_code]}: {translated}")
        # time.sleep(0.1)

    output["entries"].append({
        "message": msg,
        "translations": translation_list
    })
    print("-----------")

# Save to JSON
with open("violations_translation.json", "w", encoding="utf-8") as f:
    json.dump(output, f, ensure_ascii=False, indent=4)

print("\nTranslation complete. Saved to 'violations_translation.json'")
