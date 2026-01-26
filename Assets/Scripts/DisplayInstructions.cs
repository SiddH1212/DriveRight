using System.Collections;
using UnityEngine;
using TMPro;

public class DisplayInstructions : MonoBehaviour
{
    public GameManagerBase gameManager;
    public Transform player;
    public CanvasGroup instructionGroup; // CanvasGroup on the parent UI object
    public CanvasGroup instructionGroupVR; // CanvasGroup on the parent UI object for VR
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI instructionTextVR;
    public float decisionCheckDistance = 40f;
    private int lookahead = 10;
    private bool turning = false;

    private Coroutine hideCoroutine;
    private float displayDuration = 4f;
    private float fadeDuration = 1f;
    private string currentInstruction = "";

    Vector3 pt1 = Vector3.zero, pt2 = Vector3.zero, pt3 = Vector3.zero;

    void Start()
    {
        if(AppMode.UseVR)
            instructionTextVR.font = LanguageTranslator.Instance.GetFont();
        else
            instructionText.font = LanguageTranslator.Instance.GetFont();
        instructionText.text = "";
        instructionTextVR.text = "";

        if (instructionGroup != null)
            instructionGroup.alpha = 0f;
        if (instructionGroupVR != null)
            instructionGroupVR.alpha = 0f;

        StartCoroutine(ShowInitialInstruction());
    }

    IEnumerator ShowInitialInstruction()
    {
        yield return new WaitForSeconds(0.5f); // slight delay to allow initialization
        UpdateInstruction(forceShow: true);
    }

    public void UpdateInstruction(bool forceShow = false)
    {
        if (turning && Mathf.Abs(Vector3.SignedAngle(player.transform.forward, pt3 - pt2, Vector3.up)) < 10f)
            turning = false;

        string newInstruction = "";

        for (int i = 0; i < gameManager.currentPath.Count; i++)
        {
            if (Vector3.Distance(player.transform.position, gameManager.currentPath[i].Position) > decisionCheckDistance)
                break;

            if (gameManager.currentPath[i].Outgoing.Count > 1)
            {
                newInstruction = GetTurnDirection(
                    player.transform.position,
                    gameManager.currentPath[i].Position,
                    gameManager.currentPath[Mathf.Min(i + lookahead, gameManager.currentPath.Count - 1)].Position
                );
                turning = true;
                break;
            }
        }

        if (!turning)
            newInstruction = LanguageTranslator.Instance.Translate("Continue Straight");

        // 🔒 Prevent flashing — only update if instruction actually changed or forced
        if (!forceShow && newInstruction == currentInstruction)
            return;

        currentInstruction = newInstruction;
        ShowInstructionTemporarily(newInstruction);
    }

    void ShowInstructionTemporarily(string text)
    {
        if(AppMode.UseVR)
            instructionTextVR.text = text;
        else
            instructionText.text = text;

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        // Fade in smoothly
        if(AppMode.UseVR)
            StartCoroutine(FadeCanvasGroup(instructionGroupVR, instructionGroupVR.alpha, 1f, 0.3f));
        else
            StartCoroutine(FadeCanvasGroup(instructionGroup, instructionGroup.alpha, 1f, 0.3f));

        hideCoroutine = StartCoroutine(HideInstructionAfterDelay());
    }

    IEnumerator HideInstructionAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        if(AppMode.UseVR)
            yield return FadeCanvasGroup(instructionGroupVR, instructionGroupVR.alpha, 0f, fadeDuration);
        else
            yield return FadeCanvasGroup(instructionGroup, instructionGroup.alpha, 0f, fadeDuration);
        instructionText.text = "";
        instructionTextVR.text = "";
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    string GetTurnDirection(Vector3 from, Vector3 via, Vector3 to)
    {
        Vector3 dirA = (via - from).normalized;
        Vector3 dirB = (to - via).normalized;
        float angle = Vector3.SignedAngle(dirA, dirB, Vector3.up);
        pt1 = from; pt2 = via; pt3 = to;

        if (Mathf.Abs(angle) < 20f)
            return LanguageTranslator.Instance.Translate("Continue Straight on the next intersection");
        else if (angle > 0f)
            return LanguageTranslator.Instance.Translate("Take the next right");
        else
            return LanguageTranslator.Instance.Translate("Take the next left");
    }
}
