using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace UISwitcher {
    public class ModeSwitcher : MonoBehaviour
    {
        [SerializeField] private UISwitcher switcher3;
        [SerializeField] private Text onText;
        [SerializeField] private Text offText;

        void Awake()
        {
            // Initialize toggle from saved mode
            switcher3.SetWithoutNotify(AppMode.UseVR);
            // UpdateLabels(AppMode.UseVR);

            switcher3.OnValueChanged += OnValueChanged3;
        }

        private void OnDestroy()
        {
            switcher3.OnValueChanged -= OnValueChanged3;
        }

        private void OnValueChanged3(bool isOn)
        {
            // Save mode
            AppMode.UseVR = isOn;

            // Update UI
            // UpdateLabels(isOn);

            // Reload SAME scene to reinitialize XR safely
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void UpdateLabels(bool isOn)
        {
            onText.enabled = isOn;
            offText.enabled = !isOn;
        }
    }
}
