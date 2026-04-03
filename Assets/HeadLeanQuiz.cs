using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro;
using System.Collections;

public class HeadLeanQuiz : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] ARFaceManager faceManager;

    [Header("UI Elements")]
    [SerializeField] TMP_Text questionText;
    [SerializeField] TMP_Text feedbackText;
    [SerializeField] TMP_Text directionIndicator;

    [Header("Accessibility Styles")]
    [SerializeField] Color questionColor = Color.white;
    [SerializeField] Color correctColor = new Color(0.2f, 1f, 0.2f); // Bright Green
    [SerializeField] Color wrongColor = new Color(1f, 0.3f, 0.3f);   // Bright Red
    [SerializeField] Color activeLeanColor = Color.yellow;
    [SerializeField] Color neutralColor = Color.gray;

    [Header("Settings")]
    [SerializeField] float dwellTime = 1.5f;

    float dwellTimer = 0f;
    string currentLean = "";
    int currentQuestion = 0;
    bool answerConfirmed = false;
    bool waitingForCenter = false;

    string[] questions = {
        "Is 2 + 2 = 4?",
        "Is the sky blue?",
        "Is water wet?"
    };
    bool[] correctAnswers = { true, true, true };

    void Start()
    {
        SetupUI();
    }

    void SetupUI()
    {
        if (questionText != null)
        {
            questionText.color = questionColor;
            // Using Rich Text for extra emphasis
            questionText.text = $"<b>QUESTION:</b>\n{questions[currentQuestion]}";
        }
        if (feedbackText != null) feedbackText.text = "";
    }

    void Update()
    {
        if (faceManager == null) return;

        bool faceDetected = false;

        foreach (var face in faceManager.trackables)
        {
            faceDetected = true;
            Vector3 angles = face.transform.rotation.eulerAngles;

            float roll = angles.z > 180f ? angles.z - 360f : angles.z;
            float yaw  = angles.y > 180f ? angles.y - 360f : angles.y;

            string lean = "NONE";
            if      (roll >  10f) lean = "LEFT";
            else if (roll < -10f) lean = "RIGHT";
            else if (yaw  < -15f) lean = "LEFT";
            else if (yaw  >  15f) lean = "RIGHT";

            HandleLogic(lean);
        }

        if (!faceDetected)
        {
            ResetDetection();
        }
    }

    void HandleLogic(string lean)
    {
        if (waitingForCenter)
        {
            if (lean == "NONE")
            {
                waitingForCenter = false;
                answerConfirmed = false;
                if (feedbackText != null) feedbackText.text = "";
            }
            if (directionIndicator != null)
            {
                directionIndicator.color = neutralColor;
                directionIndicator.text = "<b><size=120%>← RETURN TO CENTER →</size></b>";
            }
            return;
        }

        // Display current direction with high visibility
        if (directionIndicator != null)
        {
            if (lean == "NONE")
            {
                directionIndicator.text = "HOLD STILL";
                directionIndicator.color = Color.white;
            }
            else
            {
                directionIndicator.text = $"LEANING: <color=#FFFF00><b>{lean}</b></color>";
                directionIndicator.color = activeLeanColor;
            }
        }

        if (lean != "NONE" && lean == currentLean && !answerConfirmed)
        {
            dwellTimer += Time.deltaTime;

            // Visual "Dwell" progress
            if (feedbackText != null)
            {
                float progress = dwellTimer / dwellTime;
                feedbackText.color = Color.Lerp(Color.white, activeLeanColor, progress);
                feedbackText.text = $"<b>SELECTING {lean}...</b>";
                
                // Add a slight pulse effect for accessibility feedback
                float pulse = 1.0f + (Mathf.PingPong(Time.time * 2, 0.2f));
                feedbackText.transform.localScale = new Vector3(pulse, pulse, 1);
            }

            if (dwellTimer >= dwellTime)
            {
                answerConfirmed = true;
                waitingForCenter = true;
                dwellTimer = 0f;
                ConfirmAnswer(lean);
            }
        }
        else
        {
            currentLean = lean;
            dwellTimer = 0f;
            if (!answerConfirmed && feedbackText != null)
            {
                feedbackText.text = "";
                feedbackText.transform.localScale = Vector3.one;
            }
        }
    }

    void ConfirmAnswer(string direction)
    {
        bool answeredYes = (direction == "LEFT");
        bool correct = (answeredYes == correctAnswers[currentQuestion]);

        if (feedbackText != null)
        {
            feedbackText.transform.localScale = new Vector3(1.5f, 1.5f, 1); // Pop the text
            feedbackText.color = correct ? correctColor : wrongColor;
            feedbackText.text = correct ? "<b>CORRECT! ✅</b>" : "<b>WRONG! ❌</b>";
        }

        Invoke(nameof(NextQuestion), 2.5f);
    }

    void NextQuestion()
    {
        currentQuestion = (currentQuestion + 1) % questions.Length;
        if (questionText != null)
        {
            questionText.text = $"<b>QUESTION:</b>\n{questions[currentQuestion]}";
        }
        if (feedbackText != null)
        {
            feedbackText.text = "";
            feedbackText.transform.localScale = Vector3.one;
        }
    }

    void ResetDetection()
    {
        currentLean = "";
        dwellTimer = 0f;
        if (directionIndicator != null)
        {
            directionIndicator.text = "<color=red>FACE NOT DETECTED</color>";
        }
    }
}