using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using TMPro;

public class HeadLeanQuiz : MonoBehaviour
{
    [Header("AR - Drag FaceTracker here")]
    [SerializeField] ARFaceManager faceManager;

    // ── UI (built at runtime) ─────────────────────────────
    TMP_Text titleText;
    TMP_Text questionText;
    TMP_Text feedbackText;
    TMP_Text directionIndicator;
    TMP_Text scoreText;
    TMP_Text streakText;
    TMP_Text progressText;
    TMP_Text counterText;
    TMP_Text hintText;
    Image    titlePanel;
    Image    questionPanel;
    Image    bottomPanel;
    Image    scorePanel;
    Image    counterPanel;

    // ── Colours ───────────────────────────────────────────
    static readonly Color C_GOLD      = new Color(1.00f, 0.85f, 0.10f);
    static readonly Color C_WHITE     = Color.white;
    static readonly Color C_GREEN     = new Color(0.10f, 1.00f, 0.35f);
    static readonly Color C_RED       = new Color(1.00f, 0.25f, 0.25f);
    static readonly Color C_CYAN      = new Color(0.25f, 0.90f, 1.00f);
    static readonly Color C_ORANGE    = new Color(1.00f, 0.55f, 0.10f);
    static readonly Color C_LIGHTBLUE = new Color(0.50f, 0.85f, 1.00f);
    static readonly Color C_AMBER     = new Color(1.00f, 0.65f, 0.00f);
    static readonly Color C_COUNTER   = new Color(0.20f, 0.80f, 1.00f);

    static readonly Color P_DARK      = new Color(0f,    0f,    0f,    0.65f);
    static readonly Color P_BLUE      = new Color(0f,    0.05f, 0.30f, 0.60f);
    static readonly Color P_SCORE     = new Color(0.10f, 0.05f, 0f,    0.70f);
    static readonly Color P_COUNTER   = new Color(0f,    0.20f, 0.40f, 0.75f);

    // ── Game State ────────────────────────────────────────
    float  dwellTimer       = 0f;
    string currentLean      = "";
    int    currentQuestion  = 0;
    bool   answerConfirmed  = false;
    bool   waitingForCenter = false;
    int    score            = 0;
    int    streak           = 0;

    readonly string[] questions = {
        "Is the sun a star?",
        "Do fish live in water?",
        "Is 5 + 5 = 10?",
        "Does a cat say meow?",
        "Is the moon round?",
        "Is 3 bigger than 7?",
        "Do birds have wings?",
        "Is grass green?",
        "Does ice feel cold?",
        "Is 2 x 3 = 6?"
    };

    readonly bool[] answers = {
        true,  // sun is a star
        true,  // fish live in water
        true,  // 5+5=10
        true,  // cat says meow
        true,  // moon is round
        false, // 3 is NOT bigger than 7
        true,  // birds have wings
        true,  // grass is green
        true,  // ice feels cold
        true   // 2x3=6
    };

    [Header("Settings")]
    [SerializeField] float dwellTime = 1.5f;

    // ─────────────────────────────────────────────────────
    // AWAKE — destroy old canvases, build fresh UI
    // ─────────────────────────────────────────────────────
    void Awake()
    {
        // Destroy ALL existing Canvas objects so no "New Text" leftovers
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            Destroy(c.gameObject);

        BuildUI();
    }

    // ─────────────────────────────────────────────────────
    // BUILD UI
    // ─────────────────────────────────────────────────────
    void BuildUI()
    {
        GameObject canvasGO = new GameObject("QuizCanvas");
        Canvas canvas       = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1080, 1920);
        scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight   = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── TOP BAR: Title (centre) ──────────────────────
        titlePanel = MakePanel(canvasGO, "TitlePanel", P_DARK,
            new Vector2(0f, 850f), new Vector2(1080f, 115f));

        titleText = MakeText(titlePanel.gameObject, "TitleText",
            "HEAD LEAN QUIZ", 56f, C_GOLD, FontStyles.Bold,
            Vector2.zero, new Vector2(1060f, 105f));
        titleText.characterSpacing = 8f;

        // ── TOP BAR: Counter (left) ──────────────────────
        counterPanel = MakePanel(canvasGO, "CounterPanel", P_COUNTER,
            new Vector2(-370f, 700f), new Vector2(260f, 80f));
        SetRadius(counterPanel, 12f);

        counterText = MakeText(counterPanel.gameObject, "CounterText",
            "Q  1 / 10", 34f, C_COUNTER, FontStyles.Bold,
            Vector2.zero, new Vector2(250f, 72f));
        counterText.characterSpacing = 3f;

        // ── TOP BAR: Score (right) ───────────────────────
        scorePanel = MakePanel(canvasGO, "ScorePanel", P_SCORE,
            new Vector2(370f, 700f), new Vector2(260f, 80f));
        SetRadius(scorePanel, 12f);

        scoreText = MakeText(scorePanel.gameObject, "ScoreText",
            "SCORE: 0", 34f, C_GOLD, FontStyles.Bold,
            Vector2.zero, new Vector2(250f, 72f));

        // ── Streak (below score) ─────────────────────────
        streakText = MakeText(canvasGO, "StreakText",
            "", 32f, C_ORANGE, FontStyles.Bold,
            new Vector2(370f, 600f), new Vector2(260f, 55f));

        // ── Question panel (moved up vs before) ─────────
        questionPanel = MakePanel(canvasGO, "QuestionPanel", P_BLUE,
            new Vector2(0f, 380f), new Vector2(1020f, 170f));
        SetRadius(questionPanel, 16f);

        questionText = MakeText(questionPanel.gameObject, "QuestionText",
            questions[0], 52f, C_WHITE, FontStyles.Bold,
            Vector2.zero, new Vector2(1000f, 158f));

        // ── Feedback (below question) ────────────────────
        feedbackText = MakeText(canvasGO, "FeedbackText",
            "", 50f, C_GREEN, FontStyles.Bold,
            new Vector2(0f, 150f), new Vector2(1020f, 180f));

        // ── Bottom panel ─────────────────────────────────
        bottomPanel = MakePanel(canvasGO, "BottomPanel", P_DARK,
            new Vector2(0f, -650f), new Vector2(1080f, 340f));

        directionIndicator = MakeText(bottomPanel.gameObject, "DirectionText",
            "Hold still", 46f, C_CYAN, FontStyles.Bold,
            new Vector2(0f, 80f), new Vector2(1040f, 100f));

        progressText = MakeText(bottomPanel.gameObject, "ProgressText",
            "", 30f, C_LIGHTBLUE, FontStyles.Normal,
            new Vector2(0f, -20f), new Vector2(1040f, 70f));
        progressText.characterSpacing = 1f;

        hintText = MakeText(bottomPanel.gameObject, "HintText",
            "LEAN LEFT = YES          LEAN RIGHT = NO",
            28f, new Color(0.75f, 0.75f, 0.75f), FontStyles.Normal,
            new Vector2(0f, -115f), new Vector2(1040f, 60f));
    }

    // ─────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────
    Image MakePanel(GameObject parent, string name, Color col, Vector2 pos, Vector2 size)
    {
        GameObject go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image img      = go.AddComponent<Image>();
        img.color      = col;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        return img;
    }

    TMP_Text MakeText(GameObject parent, string name, string content,
                      float size, Color col, FontStyles style, Vector2 pos, Vector2 rectSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        TMP_Text t           = go.AddComponent<TextMeshProUGUI>();
        t.text               = content;
        t.fontSize           = size;
        t.color              = col;
        t.fontStyle          = style;
        t.alignment          = TextAlignmentOptions.Center;
        t.enableWordWrapping = true;
        t.overflowMode       = TextOverflowModes.Overflow;
        t.outlineWidth       = 0.28f;
        t.outlineColor       = new Color32(0, 0, 0, 220);
        RectTransform rt     = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = rectSize;
        return t;
    }

    // Add rounded corners to a panel
    void SetRadius(Image img, float radius)
    {
        // Unity UI doesn't support radius natively without a custom shader,
        // so we simply skip — panels look clean with square corners too
        // Add the UI Rounded Corners package later if needed
    }

    // ─────────────────────────────────────────────────────
    // START
    // ─────────────────────────────────────────────────────
    void Start()
    {
        UpdateScoreUI();
        UpdateCounter();
        StartCoroutine(PulseTitle());
        StartCoroutine(AnimateHint());
    }

    // ─────────────────────────────────────────────────────
    // UPDATE — face tracking
    // ─────────────────────────────────────────────────────
    void Update()
    {
        if (faceManager == null) return;

        bool faceFound = false;

        foreach (var face in faceManager.trackables)
        {
            faceFound = true;

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
                directionIndicator.text  = "Return to center";
                directionIndicator.color = C_AMBER;
                progressText.text        = "";
                if (lean == "NONE")
                {
                    waitingForCenter      = false;
                    answerConfirmed       = false;
                    feedbackText.text     = "";
                    SetDirectionUI("NONE");
                }
                return;
            }

            SetDirectionUI(lean);

            if (lean != "NONE" && lean == currentLean && !answerConfirmed)
            {
                dwellTimer += Time.deltaTime;
                float pct    = dwellTimer / dwellTime;
                int   filled = Mathf.Clamp(Mathf.FloorToInt(pct * 16), 0, 16);
                progressText.text  = "[" + new string('|', filled) +
                                     new string(' ', 16 - filled) +
                                     $"]  {Mathf.FloorToInt(pct * 100)}%";
                progressText.color = lean == "LEFT" ? C_GREEN : C_RED;

                if (dwellTimer >= dwellTime)
                {
                    answerConfirmed  = true;
                    waitingForCenter = true;
                    dwellTimer       = 0f;
                    progressText.text = "";
                    ConfirmAnswer(lean);
                }
            }
            else
            {
                currentLean = lean;
                dwellTimer  = 0f;
                if (!answerConfirmed) progressText.text = "";
            }
        }

        if (!faceFound)
        {
            currentLean              = "";
            dwellTimer               = 0f;
            directionIndicator.text  = "No face detected";
            directionIndicator.color = C_AMBER;
        }
    }

    void SetDirectionUI(string lean)
    {
        switch (lean)
        {
            case "LEFT":
                directionIndicator.text  = "<<  YES  (lean left)";
                directionIndicator.color = C_GREEN;
                break;
            case "RIGHT":
                directionIndicator.text  = "NO  (lean right)  >>";
                directionIndicator.color = C_RED;
                break;
            default:
                directionIndicator.text  = "Hold still";
                directionIndicator.color = C_CYAN;
                break;
        }
    }

    // ─────────────────────────────────────────────────────
    // ANSWER LOGIC
    // ─────────────────────────────────────────────────────
    void ConfirmAnswer(string direction)
    {
        bool answeredYes = direction == "LEFT";
        bool correct     = answeredYes == answers[currentQuestion];

        if (correct)
        {
            streak++;
            int bonus  = streak > 1 ? (streak - 1) * 5 : 0;
            int earned = 10 + bonus;
            score     += earned;
            feedbackText.text  = bonus > 0
                ? $"CORRECT!   +{earned} pts\nSTREAK x{streak}  +{bonus} BONUS!"
                : $"CORRECT!   +{earned} pts";
            feedbackText.color = C_GREEN;
            StartCoroutine(BounceText(feedbackText));
            StartCoroutine(FlashPanel(questionPanel, C_GREEN));
        }
        else
        {
            streak             = 0;
            feedbackText.text  = "WRONG!";
            feedbackText.color = C_RED;
            StartCoroutine(ShakeText(feedbackText));
            StartCoroutine(FlashPanel(questionPanel, C_RED));
        }

        UpdateScoreUI();
        StartCoroutine(BounceText(scoreText));
        Invoke(nameof(NextQuestion), 2.5f);
    }

    void NextQuestion()
    {
        currentQuestion = (currentQuestion + 1) % questions.Length;
        questionText.text = questions[currentQuestion];
        feedbackText.text = "";
        StartCoroutine(FadeInText(questionText));
        UpdateCounter();
    }

    void UpdateScoreUI()
    {
        scoreText.text  = $"SCORE: {score}";
        streakText.text = streak > 1 ? $"STREAK x{streak}!" : "";
    }

    void UpdateCounter()
    {
        // Distinctive counter: shows dots for progress
        int    q        = currentQuestion + 1;
        string dots     = "";
        for (int i = 1; i <= questions.Length; i++)
            dots += i <= currentQuestion ? "● " : (i == q ? "◉ " : "○ ");

        counterText.text = $"Q  {q} / {questions.Length}";

        // Flash counter on question change
        StartCoroutine(BounceText(counterText));
        counterText.color = C_COUNTER;
    }

    // ─────────────────────────────────────────────────────
    // ANIMATIONS
    // ─────────────────────────────────────────────────────
    IEnumerator PulseTitle()
    {
        while (true)
        {
            float t = 0f;
            while (t < 1.4f)
            {
                t += Time.deltaTime * 1.6f;
                float s = 1f + 0.06f * Mathf.Sin(t * Mathf.PI);
                titleText.transform.localScale = Vector3.one * s;
                titleText.color = Color.Lerp(C_GOLD, C_WHITE,
                    0.5f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            titleText.transform.localScale = Vector3.one;
            titleText.color = C_GOLD;
            yield return new WaitForSeconds(3f);
        }
    }

    IEnumerator AnimateHint()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2f;
                hintText.color = new Color(0.75f, 0.75f, 0.75f,
                    Mathf.PingPong(t * 2f, 1f));
                yield return null;
            }
            hintText.color = new Color(0.75f, 0.75f, 0.75f, 1f);
        }
    }

    IEnumerator BounceText(TMP_Text target)
    {
        if (target == null) yield break;
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float s = 1f + 0.28f * Mathf.Sin(t / 0.5f * Mathf.PI);
            target.transform.localScale = Vector3.one * s;
            yield return null;
        }
        target.transform.localScale = Vector3.one;
    }

    IEnumerator ShakeText(TMP_Text target)
    {
        if (target == null) yield break;
        Vector3 origin = target.transform.localPosition;
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float offset = Mathf.Sin(t * 70f) * 16f * (1f - t / 0.5f);
            target.transform.localPosition = origin + new Vector3(offset, 0f, 0f);
            yield return null;
        }
        target.transform.localPosition = origin;
    }

    IEnumerator FadeInText(TMP_Text target)
    {
        if (target == null) yield break;
        Color col = target.color;
        float t   = 0f;
        target.color = new Color(col.r, col.g, col.b, 0f);
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            target.color = new Color(col.r, col.g, col.b, t / 0.4f);
            yield return null;
        }
        target.color = col;
    }

    IEnumerator FlashPanel(Image panel, Color flashCol)
    {
        if (panel == null) yield break;
        Color original = panel.color;
        panel.color    = new Color(flashCol.r, flashCol.g, flashCol.b, 0.50f);
        yield return new WaitForSeconds(0.25f);
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            panel.color = Color.Lerp(
                new Color(flashCol.r, flashCol.g, flashCol.b, 0.50f),
                original, t / 0.4f);
            yield return null;
        }
        panel.color = original;
    }
}