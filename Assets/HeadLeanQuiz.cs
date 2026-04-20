using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using TMPro;

#pragma warning disable CS0649

public class HeadLeanQuiz : MonoBehaviour
{
    [Header("AR - Drag AR Face Manager here")]
    [SerializeField] private ARFaceManager faceManager;

    private TMP_Text titleText;
    private TMP_Text questionText;
    private TMP_Text feedbackText;
    private TMP_Text directionIndicator;
    private TMP_Text scoreText;
    private TMP_Text streakText;
    private TMP_Text progressText;
    private TMP_Text counterText;
    private TMP_Text hintText;
    private Image    titlePanel;
    private Image    questionPanel;
    private Image    bottomPanel;
    private Image    scorePanel;
    private Image    counterPanel;

    private GameObject endScreenGO;
    private TMP_Text   endTitleText;
    private TMP_Text   endScoreText;
    private TMP_Text   endMessageText;
    private TMP_Text   endStarsText;
    private Image      endPanel;
    private Button     restartButton;

    private static readonly Color C_GOLD      = new Color(1.00f, 0.85f, 0.10f);
    private static readonly Color C_WHITE     = Color.white;
    private static readonly Color C_GREEN     = new Color(0.10f, 1.00f, 0.35f);
    private static readonly Color C_RED       = new Color(1.00f, 0.25f, 0.25f);
    private static readonly Color C_CYAN      = new Color(0.25f, 0.90f, 1.00f);
    private static readonly Color C_ORANGE    = new Color(1.00f, 0.55f, 0.10f);
    private static readonly Color C_LIGHTBLUE = new Color(0.50f, 0.85f, 1.00f);
    private static readonly Color C_AMBER     = new Color(1.00f, 0.65f, 0.00f);
    private static readonly Color C_COUNTER   = new Color(0.20f, 0.80f, 1.00f);

    private static readonly Color P_DARK    = new Color(0.00f, 0.00f, 0.00f, 0.65f);
    private static readonly Color P_BLUE    = new Color(0.00f, 0.05f, 0.30f, 0.60f);
    private static readonly Color P_SCORE   = new Color(0.10f, 0.05f, 0.00f, 0.70f);
    private static readonly Color P_COUNTER = new Color(0.00f, 0.20f, 0.40f, 0.75f);

    private float  dwellTimer       = 0f;
    private string currentLean      = "";
    private int    currentQuestion  = 0;
    private bool   answerConfirmed  = false;
    private bool   waitingForCenter = false;
    private int    score            = 0;
    private int    streak           = 0;
    private bool   gameOver         = false;
    private int    correctCount     = 0;

    [Header("Dwell Settings")]
    [SerializeField] private float dwellTime = 1.5f;

    // true = YES (lean left)   false = NO (lean right)
    // Pattern: T F T F T F T F T F  — perfectly alternating, no run of same answer
    private readonly string[] questions =
    {
        "Is the sun a star?",               // true
        "Does the moon produce its own light?",   // false
        "Do birds have wings?",             // true
        "Is 3 bigger than 7?",              // false
        "Is grass green?",                  // true
        "Do fish breathe air like humans?", // false
        "Is 4 x 3 = 12?",                  // true
        "Does ice feel hot?",               // false
        "Does a cat say meow?",             // true
        "Is the sun a planet?"              // false
    };

    private readonly bool[] answers =
    {
        true,   // Is the sun a star?              → YES
        false,  // Does the moon produce its own light? → NO
        true,   // Do birds have wings?             → YES
        false,  // Is 3 bigger than 7?              → NO
        true,   // Is grass green?                  → YES
        false,  // Do fish breathe air like humans? → NO
        true,   // Is 4 x 3 = 12?                  → YES
        false,  // Does ice feel hot?               → NO
        true,   // Does a cat say meow?             → YES
        false   // Is the sun a planet?             → NO
    };

    private void Awake()
    {
        Canvas[] existingCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
        foreach (Canvas c in existingCanvases)
        {
            if (c != null && c.gameObject != null)
                Destroy(c.gameObject);
        }
        BuildUI();
    }

    private void BuildUI()
    {
        GameObject canvasGO = new GameObject("QuizCanvas");
        Canvas canvas       = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler        = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        titlePanel = MakePanel(canvasGO, "TitlePanel", P_DARK,
            new Vector2(0f, 850f), new Vector2(1080f, 115f));

        titleText = MakeText(titlePanel.gameObject, "TitleText",
            "HEAD LEAN QUIZ",
            56f, C_GOLD, FontStyles.Bold,
            Vector2.zero, new Vector2(1060f, 105f));
        titleText.characterSpacing = 8f;

        counterPanel = MakePanel(canvasGO, "CounterPanel", P_COUNTER,
            new Vector2(-370f, 700f), new Vector2(260f, 80f));

        counterText = MakeText(counterPanel.gameObject, "CounterText",
            "Q  1 / 10",
            34f, C_COUNTER, FontStyles.Bold,
            Vector2.zero, new Vector2(250f, 72f));
        counterText.characterSpacing = 3f;

        scorePanel = MakePanel(canvasGO, "ScorePanel", P_SCORE,
            new Vector2(370f, 700f), new Vector2(260f, 80f));

        scoreText = MakeText(scorePanel.gameObject, "ScoreText",
            "SCORE: 0",
            34f, C_GOLD, FontStyles.Bold,
            Vector2.zero, new Vector2(250f, 72f));

        streakText = MakeText(canvasGO, "StreakText",
            "",
            32f, C_ORANGE, FontStyles.Bold,
            new Vector2(370f, 600f), new Vector2(260f, 55f));

        questionPanel = MakePanel(canvasGO, "QuestionPanel", P_BLUE,
            new Vector2(0f, 380f), new Vector2(1020f, 170f));

        questionText = MakeText(questionPanel.gameObject, "QuestionText",
            questions[0],
            52f, C_WHITE, FontStyles.Bold,
            Vector2.zero, new Vector2(1000f, 158f));

        feedbackText = MakeText(canvasGO, "FeedbackText",
            "",
            50f, C_GREEN, FontStyles.Bold,
            new Vector2(0f, 150f), new Vector2(1020f, 180f));

        bottomPanel = MakePanel(canvasGO, "BottomPanel", P_DARK,
            new Vector2(0f, -650f), new Vector2(1080f, 340f));

        directionIndicator = MakeText(bottomPanel.gameObject, "DirectionText",
            "Hold still",
            46f, C_CYAN, FontStyles.Bold,
            new Vector2(0f, 80f), new Vector2(1040f, 100f));

        progressText = MakeText(bottomPanel.gameObject, "ProgressText",
            "",
            30f, C_LIGHTBLUE, FontStyles.Normal,
            new Vector2(0f, -20f), new Vector2(1040f, 70f));
        progressText.characterSpacing = 1f;

        hintText = MakeText(bottomPanel.gameObject, "HintText",
            "LEAN LEFT = YES                LEAN RIGHT = NO",
            28f, new Color(0.75f, 0.75f, 0.75f, 1f), FontStyles.Normal,
            new Vector2(0f, -115f), new Vector2(1040f, 60f));

        BuildEndScreen(canvasGO);
    }

    private void BuildEndScreen(GameObject canvasGO)
    {
        endScreenGO = new GameObject("EndScreen");
        endScreenGO.transform.SetParent(canvasGO.transform, false);

        endPanel       = endScreenGO.AddComponent<Image>();
        endPanel.color = new Color(0f, 0f, 0.10f, 0.88f);

        RectTransform rt = endScreenGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        endStarsText = MakeText(endScreenGO, "EndStars",
            "",
            90f, C_GOLD, FontStyles.Bold,
            new Vector2(0f, 450f), new Vector2(1000f, 180f));

        endTitleText = MakeText(endScreenGO, "EndTitle",
            "QUIZ COMPLETE!",
            68f, C_GOLD, FontStyles.Bold,
            new Vector2(0f, 250f), new Vector2(1000f, 140f));
        endTitleText.characterSpacing = 6f;

        endScoreText = MakeText(endScreenGO, "EndScore",
            "",
            80f, C_WHITE, FontStyles.Bold,
            new Vector2(0f, 50f), new Vector2(1000f, 160f));

        endMessageText = MakeText(endScreenGO, "EndMessage",
            "",
            46f, C_CYAN, FontStyles.Bold,
            new Vector2(0f, -180f), new Vector2(960f, 300f));

        GameObject btnGO = new GameObject("RestartButton");
        btnGO.transform.SetParent(endScreenGO.transform, false);

        Image btnImage   = btnGO.AddComponent<Image>();
        btnImage.color   = new Color(0.10f, 0.60f, 1.00f, 0.90f);

        RectTransform btnRT    = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin        = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax        = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = new Vector2(0f, -420f);
        btnRT.sizeDelta        = new Vector2(460f, 110f);

        restartButton = btnGO.AddComponent<Button>();
        restartButton.targetGraphic = btnImage;

        ColorBlock cb       = restartButton.colors;
        cb.normalColor      = new Color(0.10f, 0.60f, 1.00f, 0.90f);
        cb.highlightedColor = new Color(0.20f, 0.75f, 1.00f, 1.00f);
        cb.pressedColor     = new Color(0.05f, 0.40f, 0.80f, 1.00f);
        cb.selectedColor    = cb.normalColor;
        cb.fadeDuration     = 0.1f;
        restartButton.colors = cb;

        MakeText(btnGO, "RestartLabel",
            "▶  PLAY AGAIN",
            40f, C_WHITE, FontStyles.Bold,
            Vector2.zero, new Vector2(440f, 100f));

        restartButton.onClick.AddListener(RestartQuiz);

        endScreenGO.SetActive(false);
    }

    private void RestartQuiz()
    {
        StopAllCoroutines();

        currentQuestion  = 0;
        score            = 0;
        streak           = 0;
        correctCount     = 0;
        dwellTimer       = 0f;
        currentLean      = "";
        answerConfirmed  = false;
        waitingForCenter = false;
        gameOver         = false;

        endScreenGO.SetActive(false);

        titlePanel.gameObject.SetActive(true);
        questionPanel.gameObject.SetActive(true);
        bottomPanel.gameObject.SetActive(true);
        scorePanel.gameObject.SetActive(true);
        counterPanel.gameObject.SetActive(true);
        streakText.gameObject.SetActive(true);
        feedbackText.gameObject.SetActive(true);

        questionText.text = questions[0];
        feedbackText.text = "";
        progressText.text = "";

        UpdateScoreUI();
        UpdateCounter();

        StartCoroutine(PulseTitle());
        StartCoroutine(AnimateHint());
    }

    private void Start()
    {
        UpdateScoreUI();
        UpdateCounter();
        StartCoroutine(PulseTitle());
        StartCoroutine(AnimateHint());
    }

    private void Update()
    {
        if (faceManager == null || gameOver) return;

        bool faceFound = false;

        foreach (ARFace face in faceManager.trackables)
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
                    waitingForCenter  = false;
                    answerConfirmed   = false;
                    feedbackText.text = "";
                    SetDirectionUI("NONE");
                }
                return;
            }

            SetDirectionUI(lean);

            if (lean != "NONE" && lean == currentLean && !answerConfirmed)
            {
                dwellTimer += Time.deltaTime;
                float pct   = dwellTimer / dwellTime;
                int  filled = Mathf.Clamp(Mathf.FloorToInt(pct * 16), 0, 16);
                progressText.text  = "[" + new string('|', filled) +
                                     new string(' ', 16 - filled) +
                                     $"]  {Mathf.FloorToInt(pct * 100)}%";
                progressText.color = lean == "LEFT" ? C_GREEN : C_RED;

                if (dwellTimer >= dwellTime)
                {
                    answerConfirmed   = true;
                    waitingForCenter  = true;
                    dwellTimer        = 0f;
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

    private void SetDirectionUI(string lean)
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

    private void ConfirmAnswer(string direction)
    {
        bool answeredYes = direction == "LEFT";
        bool correct     = answeredYes == answers[currentQuestion];

        if (correct)
        {
            correctCount++;
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
            streak            = 0;
            feedbackText.text  = "WRONG!";
            feedbackText.color = C_RED;
            StartCoroutine(ShakeText(feedbackText));
            StartCoroutine(FlashPanel(questionPanel, C_RED));
        }

        UpdateScoreUI();
        StartCoroutine(BounceText(scoreText));
        Invoke(nameof(NextQuestion), 2.5f);
    }

    private void NextQuestion()
    {
        if (currentQuestion >= questions.Length - 1)
        {
            gameOver = true;
            Invoke(nameof(ShowEndScreen), 0.5f);
            return;
        }

        currentQuestion++;
        questionText.text = questions[currentQuestion];
        feedbackText.text = "";
        StartCoroutine(FadeInText(questionText));
        UpdateCounter();
    }

    private void UpdateScoreUI()
    {
        scoreText.text  = $"SCORE: {score}";
        streakText.text = streak > 1 ? $"STREAK x{streak}!" : "";
    }

    private void UpdateCounter()
    {
        counterText.text = $"Q  {currentQuestion + 1} / {questions.Length}";
        StartCoroutine(BounceText(counterText));
    }

    private void ShowEndScreen()
    {
        titlePanel.gameObject.SetActive(false);
        questionPanel.gameObject.SetActive(false);
        bottomPanel.gameObject.SetActive(false);
        scorePanel.gameObject.SetActive(false);
        counterPanel.gameObject.SetActive(false);
        streakText.gameObject.SetActive(false);
        feedbackText.gameObject.SetActive(false);

        float  pct          = (float)correctCount / questions.Length;
        string stars        = "*";
        string message      = "KEEP TRYING!\nEvery attempt makes\nyou stronger!";
        Color  messageColor = C_AMBER;
        endPanel.color      = new Color(0.12f, 0f, 0f, 0.90f);

        if (pct >= 1.0f)
        {
            stars          = "* * * * *";
            message        = "PERFECT SCORE!\nAbsolutely incredible!\nYou are a STAR!";
            messageColor   = C_GOLD;
            endPanel.color = new Color(0.05f, 0.10f, 0f, 0.90f);
        }
        else if (pct >= 0.80f)
        {
            stars          = "* * * *";
            message        = "AMAZING WORK!\nSo close to perfect!\nKeep it up!";
            messageColor   = C_GREEN;
            endPanel.color = new Color(0f, 0.10f, 0f, 0.90f);
        }
        else if (pct >= 0.60f)
        {
            stars          = "* * *";
            message        = "GREAT JOB!\nYou did really well!\nPractice makes perfect!";
            messageColor   = C_CYAN;
            endPanel.color = new Color(0f, 0.05f, 0.15f, 0.90f);
        }
        else if (pct >= 0.40f)
        {
            stars          = "* *";
            message        = "GOOD EFFORT!\nYou are getting there!\nTry again!";
            messageColor   = C_ORANGE;
            endPanel.color = new Color(0.10f, 0.05f, 0f, 0.90f);
        }

        endStarsText.text    = stars;
        endTitleText.text    = "QUIZ COMPLETE!";
        endScoreText.text    = $"SCORE:  {score}  pts\n{correctCount} / {questions.Length}  correct";
        endMessageText.text  = message;
        endMessageText.color = messageColor;

        endScreenGO.SetActive(true);
        StartCoroutine(EndScreenAnimation());
    }

    private IEnumerator EndScreenAnimation()
    {
        Color panelTarget = endPanel.color;
        endPanel.color = new Color(panelTarget.r, panelTarget.g, panelTarget.b, 0f);
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            endPanel.color = new Color(panelTarget.r, panelTarget.g, panelTarget.b,
                Mathf.Lerp(0f, 0.90f, t / 0.6f));
            yield return null;
        }

        endTitleText.transform.localScale = Vector3.zero;
        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(0f, 1f, Mathf.SmoothStep(0f, 1f, t / 0.5f));
            endTitleText.transform.localScale = Vector3.one * s;
            yield return null;
        }
        endTitleText.transform.localScale = Vector3.one;
        yield return new WaitForSeconds(0.2f);

        int displayed = 0;
        while (displayed < score)
        {
            displayed = Mathf.Min(displayed + Mathf.Max(1, score / 40), score);
            endScoreText.text =
                $"SCORE:  {displayed}  pts\n{correctCount} / {questions.Length}  correct";
            yield return new WaitForSeconds(0.03f);
        }
        StartCoroutine(BounceText(endScoreText));
        yield return new WaitForSeconds(0.3f);

        string[] parts = endStarsText.text.Split(' ');
        endStarsText.text = "";
        foreach (string part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;
            endStarsText.text += part + " ";
            StartCoroutine(BounceText(endStarsText));
            yield return new WaitForSeconds(0.22f);
        }
        yield return new WaitForSeconds(0.3f);

        yield return StartCoroutine(FadeInText(endMessageText));

        if (restartButton != null)
        {
            Image btnImg  = restartButton.GetComponent<Image>();
            Color baseCol = btnImg.color;
            btnImg.color  = new Color(baseCol.r, baseCol.g, baseCol.b, 0f);
            t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                btnImg.color = new Color(baseCol.r, baseCol.g, baseCol.b, t / 0.4f);
                yield return null;
            }
            btnImg.color = baseCol;
        }

        StartCoroutine(PulseStars());
    }

    private IEnumerator PulseStars()
    {
        while (true)
        {
            float t = 0f;
            while (t < 1.6f)
            {
                t += Time.deltaTime * 1.4f;
                float s = 1f + 0.10f * Mathf.Sin(t * Mathf.PI);
                endStarsText.transform.localScale = Vector3.one * s;
                endStarsText.color = Color.Lerp(C_GOLD, C_WHITE,
                    0.5f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            endStarsText.transform.localScale = Vector3.one;
            endStarsText.color = C_GOLD;
            yield return new WaitForSeconds(2f);
        }
    }

    private IEnumerator PulseTitle()
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

    private IEnumerator AnimateHint()
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

    private IEnumerator BounceText(TMP_Text target)
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

    private IEnumerator ShakeText(TMP_Text target)
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

    private IEnumerator FadeInText(TMP_Text target)
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

    private IEnumerator FlashPanel(Image panel, Color flashCol)
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

    private Image MakePanel(GameObject parent, string name,
                             Color col, Vector2 pos, Vector2 size)
    {
        GameObject go    = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image img        = go.AddComponent<Image>();
        img.color        = col;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin     = new Vector2(0.5f, 0.5f);
        rt.anchorMax     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        return img;
    }

    private TMP_Text MakeText(GameObject parent, string name, string content,
                               float size, Color col, FontStyles style,
                               Vector2 pos, Vector2 rectSize)
    {
        GameObject go        = new GameObject(name);
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
        rt.anchorMin         = new Vector2(0.5f, 0.5f);
        rt.anchorMax         = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition  = pos;
        rt.sizeDelta         = rectSize;
        return t;
    }
}