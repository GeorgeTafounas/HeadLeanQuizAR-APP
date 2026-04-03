using UnityEngine;
using UnityEngine.XR.ARFoundation;
using TMPro;
using System.Collections;

public class HeadLeanQuiz : MonoBehaviour
{
    [Header("UI Elements (Assign these once)")]
    [SerializeField] TMP_Text questionText;
    [SerializeField] TMP_Text feedbackText;
    [SerializeField] TMP_Text directionIndicator;

    private ARFaceManager faceManager;
    private AudioSource audioSource;

    // Settings
    private float dwellTime = 1.3f;
    private float leanThreshold = 12f;
    
    // State
    float dwellTimer = 0f;
    string currentLean = "NONE";
    int currentQuestion = 0;
    bool isProcessing = false;
    bool waitingForCenter = false;

    string[] questions = {
        "Is 2 + 2 = 4?",
        "Is the sky blue?",
        "Is water wet?",
        "Is Unity fun?"
    };
    bool[] correctAnswers = { true, true, true, true };

    void Awake()
    {
        // AUTO-CONFIG: Find components so you don't have to drag them
        faceManager = FindObjectOfType<ARFaceManager>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // STYLE INJECTION: Make text professional via code
        ApplyGlobalStyles(questionText);
        ApplyGlobalStyles(feedbackText);
        ApplyGlobalStyles(directionIndicator);
    }

    void Start()
    {
        UpdateQuestionUI();
    }

    void ApplyGlobalStyles(TMP_Text text)
    {
        if (text == null) return;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12;
        text.alignment = TextAlignmentOptions.Center;
        
        // Add a Black Outline via code for AR visibility
        text.outlineWidth = 0.25f;
        text.outlineColor = Color.black;
    }

    void Update()
    {
        if (faceManager == null || isProcessing) return;

        bool faceDetected = false;
        foreach (var face in faceManager.trackables)
        {
            faceDetected = true;
            Vector3 angles = face.transform.localRotation.eulerAngles;
            float roll = (angles.z > 180f) ? angles.z - 360f : angles.z;
            float yaw = (angles.y > 180f) ? angles.y - 360f : angles.y;

            string detectedLean = "NONE";
            if      (roll > leanThreshold)  detectedLean = "LEFT";
            else if (roll < -leanThreshold) detectedLean = "RIGHT";
            else if (yaw  < -leanThreshold) detectedLean = "LEFT";
            else if (yaw  > leanThreshold)  detectedLean = "RIGHT";

            HandleLogic(detectedLean);
            break; 
        }
        if (!faceDetected) directionIndicator.text = "<color=red>FINDING FACE...</color>";
    }

    void HandleLogic(string lean)
    {
        if (waitingForCenter)
        {
            if (lean == "NONE") {
                waitingForCenter = false;
                feedbackText.text = "";
            }
            directionIndicator.text = "<b><color=#FFFF00>RETURN TO CENTER</color></b>";
            return;
        }

        // Direction Indicator with Rich Text
        directionIndicator.text = lean == "NONE" ? "<b>READY</b>" : $"<b>LEANING <color=#00FFFF>{lean}</color></b>";

        if (lean != "NONE" && lean == currentLean)
        {
            dwellTimer += Time.deltaTime;
            float progress = dwellTimer / dwellTime;

            // PROCEDURAL PULSE: Text grows and shrinks slightly as you hold
            float pulse = 1.0f + (Mathf.Sin(Time.time * 10f) * 0.05f) + (progress * 0.2f);
            feedbackText.transform.localScale = new Vector3(pulse, pulse, 1);
            
            feedbackText.text = $"<b>HOLDING... {Mathf.RoundToInt(progress * 100)}%</b>";
            feedbackText.color = Color.Lerp(Color.white, Color.yellow, progress);

            if (dwellTimer >= dwellTime) ConfirmAnswer(lean);
        }
        else
        {
            currentLean = lean;
            dwellTimer = 0f;
            feedbackText.text = "";
            feedbackText.transform.localScale = Vector3.one;
        }
    }

    void ConfirmAnswer(string direction)
    {
        isProcessing = true;
        bool answeredYes = (direction == "LEFT");
        bool correct = (answeredYes == correctAnswers[currentQuestion]);

        feedbackText.text = correct ? "<b><color=#00FF00>CORRECT! ✅</color></b>" : "<b><color=#FF0000>WRONG ❌</color></b>";
        
        // Effects
        if (correct) {
            StartCoroutine(VictoryScale(feedbackText.transform));
        } else {
            StartCoroutine(ShakeEffect(feedbackText.transform, 0.3f, 15f));
        }

        Invoke(nameof(NextQuestion), 2.0f);
    }

    void NextQuestion()
    {
        currentQuestion = (currentQuestion + 1) % questions.Length;
        UpdateQuestionUI();
        isProcessing = false;
        waitingForCenter = true;
        dwellTimer = 0f;
    }

    void UpdateQuestionUI()
    {
        questionText.text = $"<color=#FFD700><b>Q:</b></color> {questions[currentQuestion]}";
    }

    // --- CODE-ONLY ANIMATIONS ---

    IEnumerator VictoryScale(Transform target)
    {
        float t = 0;
        while (t < 1) {
            t += Time.deltaTime * 5;
            float s = Mathf.Lerp(1f, 1.8f, t);
            target.localScale = new Vector3(s, s, 1);
            yield return null;
        }
    }

    IEnumerator ShakeEffect(Transform target, float duration, float mag)
    {
        Vector3 startPos = target.localPosition;
        float elapsed = 0;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * mag;
            target.localPosition = startPos + new Vector3(x, 0, 0);
            yield return null;
        }
        target.localPosition = startPos;
    }
}