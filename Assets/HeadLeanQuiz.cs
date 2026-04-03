using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro;

public class HeadLeanQuiz : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] ARFaceManager faceManager;

    [Header("Quiz UI")]
    [SerializeField] TMP_Text questionText;
    [SerializeField] TMP_Text feedbackText;
    [SerializeField] TMP_Text directionIndicator;

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
        if (questionText != null)
            questionText.text = questions[currentQuestion];
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

            if (waitingForCenter)
            {
                if (lean == "NONE")
                {
                    waitingForCenter = false;
                    answerConfirmed = false;
                    if (feedbackText != null) feedbackText.text = "";
                }
                if (directionIndicator != null)
                    directionIndicator.text = "Return to center...";
                return;
            }

            if (directionIndicator != null)
                directionIndicator.text = lean == "NONE" ? "Hold still" : $"Leaning: {lean}";

            if (lean != "NONE" && lean == currentLean && !answerConfirmed)
            {
                dwellTimer += Time.deltaTime;

                if (feedbackText != null)
                    feedbackText.text = $"Hold... {Mathf.CeilToInt(dwellTime - dwellTimer)}s";

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
                    feedbackText.text = "";
            }
        }

        if (!faceDetected)
        {
            currentLean = "";
            dwellTimer = 0f;
            if (directionIndicator != null)
                directionIndicator.text = "No face detected";
        }
    }

    void ConfirmAnswer(string direction)
    {
        bool answeredYes = (direction == "LEFT");
        bool correct = (answeredYes == correctAnswers[currentQuestion]);

        if (feedbackText != null)
            feedbackText.text = correct ? "✅ Correct!" : "❌ Wrong!";

        Invoke(nameof(NextQuestion), 2f);
    }

    void NextQuestion()
    {
        currentQuestion = (currentQuestion + 1) % questions.Length;
        if (questionText != null)
            questionText.text = questions[currentQuestion];
    }
}