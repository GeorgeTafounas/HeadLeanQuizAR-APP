using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class HeadLeanQuiz : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] ARFaceManager faceManager;

    [Header("UI Text Elements")]
    [SerializeField] TMP_Text questionText;
    [SerializeField] TMP_Text feedbackText;
    [SerializeField] TMP_Text directionIndicator;

    [Header("UI Background (Optional)")]
    [SerializeField] Image backgroundPanel; 

    [Header("Perfect UI Colors")]
    [SerializeField] Color questionPrimary = new Color(1f, 0.84f, 0f);    // Gold
    [SerializeField] Color questionTextWhite = new Color(0.95f, 0.95f, 0.95f); 
    [SerializeField] Color correctColor = new Color(0.1f, 0.9f, 0.1f);    // Bright Green
    [SerializeField] Color wrongColor = new Color(1f, 0.25f, 0.25f);      // Bright Red
    [SerializeField] Color centerHoldColor = new Color(0.4f, 0.9f, 1f);   // Sky Blue
    [SerializeField] Color selectionFillColor = new Color(1f, 0.6f, 0f);  // Vivid Orange
    [SerializeField] Color panelNeutralColor = new Color(0f, 0f, 0f, 0.6f); // Dark Translucent

    [Header("Sensitivity Settings")]
    [SerializeField] float leanThreshold = 12f; 
    [SerializeField] float dwellTime = 1.3f;   

    float dwellTimer = 0f;
    string currentLean = "NONE";
    int currentQuestion = 0;
    bool isProcessing = false;
    bool waitingForCenter = false;

    string[] questions = {
        "Is 2 + 2 = 4?",
        "Is the sky blue?",
        "Is water wet?",
        "Do birds fly in the sky?"
    };
    bool[] correctAnswers = { true, true, true, true };

    void Start()
    {
        InitializeUI();
    }

    void InitializeUI()
    {
        if (questionText != null)
        {
            // FIX: Replaced the error-prone 'Shrink' mode with AutoSizing
            questionText.enableWordWrapping = false;
            questionText.overflowMode = TextOverflowModes.Overflow;
            questionText.enableAutoSizing = true; 
            questionText.fontSizeMin = 18;
            questionText.fontSizeMax = 60;
            
            UpdateQuestionUI();
        }

        if (backgroundPanel != null) backgroundPanel.color = panelNeutralColor;
        if (feedbackText != null) feedbackText.text = "";
    }

    void Update()
    {
        // Don't detect new movement while showing "Correct/Wrong" or if AR is offline
        if (faceManager == null || isProcessing) return;

        bool faceDetected = false;

        foreach (var face in faceManager.trackables)
        {
            faceDetected = true;
            
            // STABILITY FIX: Use local rotation and normalize to -180/180
            Vector3 angles = face.transform.localRotation.eulerAngles;
            float roll = (angles.z > 180f) ? angles.z - 360f : angles.z;
            float yaw  = (angles.y > 180f) ? angles.y - 360f : angles.y;

            string detectedLean = "NONE";

            // Logic: Roll (Z) is a head tilt. Yaw (Y) is a head turn.
            if      (roll >  leanThreshold) detectedLean = "LEFT";
            else if (roll < -leanThreshold) detectedLean = "RIGHT";
            else if (yaw  < -leanThreshold) detectedLean = "LEFT";
            else if (yaw  >  leanThreshold) detectedLean = "RIGHT";

            HandleLogic(detectedLean);
            break; 
        }

        if (!faceDetected) ResetDetection();
    }

    void HandleLogic(string lean)
    {
        if (waitingForCenter)
        {
            if (lean == "NONE")
            {
                waitingForCenter = false;
                if (feedbackText != null) feedbackText.text = "";
                UpdatePanelColor(panelNeutralColor);
            }
            if (directionIndicator != null)
            {
                directionIndicator.text = "<b>↑ CENTER YOUR HEAD ↑</b>";
                directionIndicator.color = Color.white;
            }
            return;
        }

        if (directionIndicator != null)
        {
            directionIndicator.text = lean == "NONE" ? "<b>HOLD STILL</b>" : $"<b>LEANING: <color=yellow>{lean}</color></b>";
            directionIndicator.color = centerHoldColor;
        }

        if (lean != "NONE" && lean == currentLean)
        {
            dwellTimer += Time.deltaTime;
            float progress = dwellTimer / dwellTime;

            if (feedbackText != null)
            {
                feedbackText.text = $"<b>SELECTING {lean}...</b>";
                feedbackText.color = Color.Lerp(centerHoldColor, selectionFillColor, progress);
                
                // Visual Pulse effect for confirmation
                float pulse = 1.0f + (progress * 0.3f);
                feedbackText.transform.localScale = new Vector3(pulse, pulse, 1);
            }
            UpdatePanelColor(Color.Lerp(panelNeutralColor, selectionFillColor, progress * 0.4f));

            if (dwellTimer >= dwellTime) ConfirmAnswer(lean);
        }
        else
        {
            currentLean = lean;
            dwellTimer = 0f;
            UpdatePanelColor(panelNeutralColor);
            if (feedbackText != null)
            {
                feedbackText.text = "";
                feedbackText.transform.localScale = Vector3.one;
            }
        }
    }

    void ConfirmAnswer(string direction)
    {
        isProcessing = true;
        bool answeredYes = (direction == "LEFT");
        bool correct = (answeredYes == correctAnswers[currentQuestion]);

        Color resultColor = correct ? correctColor : wrongColor;

        if (feedbackText != null)
        {
            feedbackText.color = resultColor;
            feedbackText.text = correct ? "<b>CORRECT! ✅</b>" : "<b>WRONG ❌</b>";
            feedbackText.transform.localScale = new Vector3(1.5f, 1.5f, 1);
        }

        UpdatePanelColor(resultColor);
        if (!correct) StartCoroutine(BlinkText(directionIndicator));

        Invoke(nameof(NextQuestion), 2.2f);
    }

    void NextQuestion()
    {
        currentQuestion = (currentQuestion + 1) % questions.Length;
        UpdateQuestionUI();
        
        isProcessing = false;
        waitingForCenter = true;
        dwellTimer = 0f;
        UpdatePanelColor(panelNeutralColor);
        
        if (feedbackText != null) 
        {
            feedbackText.text = "";
            feedbackText.transform.localScale = Vector3.one;
        }
    }

    void UpdateQuestionUI()
    {
        if (questionText != null)
        {
            string hexGold = ColorUtility.ToHtmlStringRGB(questionPrimary);
            string hexWhite = ColorUtility.ToHtmlStringRGB(questionTextWhite);
            questionText.text = $"<color=#{hexGold}><b>QUESTION:</b></color> <color=#{hexWhite}><b>{questions[currentQuestion]}</b></color>";
        }
    }

    void ResetDetection()
    {
        currentLean = "NONE";
        dwellTimer = 0f;
        if (directionIndicator != null)
            directionIndicator.text = "<color=red><b>FACE LOST</b></color>";
    }

    void UpdatePanelColor(Color col)
    {
        if (backgroundPanel != null)
        {
            col.a = 0.6f; // Keep it translucent
            backgroundPanel.color = col;
        }
    }

    IEnumerator BlinkText(TMP_Text text)
    {
        if (text == null) yield break;
        for (int i = 0; i < 3; i++)
        {
            text.alpha = 0;
            yield return new WaitForSeconds(0.2f);
            text.alpha = 1;
            yield return new WaitForSeconds(0.2f);
        }
    }
}