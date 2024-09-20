using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using UnityEngine.SceneManagement;

public class QuizManager : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        public string question;
        public string[] answers;
        public int correctAnswer;
    }

    [System.Serializable]
    public class QuestionList
    {
        public Question[] questions;
    }

    public Text questionText;
    public Button[] answerButtons;
    public Text resultText;
    public Text timerText;
    public Text scoreText;
    public Button submitButton;
    public GameObject endPanel;
    public Text endScoreText;
    public GameObject categoryPanel;
    public Button[] categoryButtons;
    public Button replayButton;
    public Button backToCategoryButton;
    public Button toggleSoundButton;   // Button to toggle sound
    public AudioSource backgroundMusic; // Background music
    public AudioSource correctSound;    // Sound for correct answer
    public AudioSource wrongSound;      // Sound for wrong answer
    public AudioSource buttonClickSound;// Sound for button click

    //public Color correctColor = Color.green;
    //public Color wrongColor = Color.red;
    //public Color defaultColor = Color.white;
    public Sprite correctSprite,wrongSprite,defaultSprite;
    

    public Color submitActiveColor = Color.green;   // Color when submit is active
    public Color submitInactiveColor = Color.gray;  // Color when submit is inactive

    private List<Question> shuffledQuestions;
    private int currentQuestionIndex;
    private int score;
    private bool hasAnswered;
    private float timeRemaining = 10f;
    private bool isGameActive = false;
    private bool isSoundOn = true; // Sound state: on or off
    private string currentCategory;
    public float typeSpeed = 0.05f;

    private bool isAnswerSelected = false;  // Track if an answer is selected

    void Start()
    {
        score = 0;
        currentQuestionIndex = 0;
        endPanel.SetActive(false);

        categoryPanel.transform.localPosition = new Vector3(0, Screen.height, 0);
        LeanTween.moveLocalY(categoryPanel, 0f, 0.5f).setEaseOutBack();

        submitButton.onClick.AddListener(SubmitAnswer);
        toggleSoundButton.onClick.AddListener(ToggleSound); // Sound toggle listener

        foreach (Button categoryButton in categoryButtons)
        {
            categoryButton.onClick.AddListener(() => SelectCategory(categoryButton.name));
            AddButtonHoverAnimation(categoryButton);
            int buttonIndex = System.Array.IndexOf(categoryButtons, categoryButton);
            categoryButton.transform.localScale = Vector3.zero;
            LeanTween.scale(categoryButton.gameObject, Vector3.one, 0.5f).setEaseOutBounce().setDelay(0.2f * buttonIndex);
        }

        replayButton.onClick.AddListener(ReplayQuiz);
        backToCategoryButton.onClick.AddListener(BackToCategorySelection);
        PlayBackgroundMusic(); // Start background music
    }

    void SelectCategory(string category)
    {
        PlayButtonClickSound(); // Play button click sound
        currentCategory = category;

        LeanTween.moveLocalY(categoryPanel, Screen.height, 0.5f).setEaseInBack().setOnComplete(() => {
            categoryPanel.SetActive(false);
        });

        string jsonFileName = category.ToLower() + "_questions.json";
#if UNITY_ANDROID || UNITY_EDITOR
        StartCoroutine(LoadLocalizedTextOnAndroid(jsonFileName));
#else
        LoadQuestions(jsonFileName);
#endif
        ShuffleQuestions();
        DisplayQuestion();
    }

    void LoadQuestions(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        if (File.Exists(path))
        {
            string jsonText = File.ReadAllText(path);
            QuestionList quizQuestions = JsonUtility.FromJson<QuestionList>(jsonText);
            shuffledQuestions = new List<Question>(quizQuestions.questions);
        }
        else
        {
            Debug.LogError("Questions JSON file not found: " + fileName);
        }
    }


    IEnumerator LoadLocalizedTextOnAndroid(string fileName)
    {
      
        string filePath;// = Path.Combine(Application.streamingAssetsPath, fileName);
        filePath = Path.Combine(Application.streamingAssetsPath + "/", fileName);
        string dataAsJson;
        if (filePath.Contains("://") || filePath.Contains(":///"))
        {
            //debugText.text += System.Environment.NewLine + filePath;
            Debug.Log("UNITY:" + System.Environment.NewLine + filePath);
            UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(filePath);
            yield return www.Send();
            dataAsJson = www.downloadHandler.text;
            QuestionList quizQuestions = JsonUtility.FromJson<QuestionList>(dataAsJson);
            shuffledQuestions = new List<Question>(quizQuestions.questions);
        }
        else
        {
            dataAsJson = File.ReadAllText(filePath);
            QuestionList quizQuestions = JsonUtility.FromJson<QuestionList>(dataAsJson);
            shuffledQuestions = new List<Question>(quizQuestions.questions);
        }
       

        
    }

    void ShuffleQuestions()
    {
        for (int i = 0; i < shuffledQuestions.Count; i++)
        {
            Question temp = shuffledQuestions[i];
            int randomIndex = Random.Range(i, shuffledQuestions.Count);
            shuffledQuestions[i] = shuffledQuestions[randomIndex];
            shuffledQuestions[randomIndex] = temp;
        }
    }

    void AddButtonHoverAnimation(Button btn)
    {
        EventTrigger trigger = btn.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entryEnter.callback.AddListener((eventData) =>
        {
            LeanTween.scale(btn.gameObject, Vector3.one * 1.1f, 0.2f).setEasePunch();
        });
        trigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        entryExit.callback.AddListener((eventData) =>
        {
            LeanTween.scale(btn.gameObject, Vector3.one, 0.2f);
        });
        trigger.triggers.Add(entryExit);
    }

    void DisplayQuestion()
    {
        isAnswerSelected = false;  // Reset answer selection
        UpdateSubmitButtonState(); // Disable submit button until an answer is selected

        if (currentQuestionIndex < shuffledQuestions.Count)
        {
            isGameActive = true;
            hasAnswered = false;
            Question currentQuestion = shuffledQuestions[currentQuestionIndex];

            StartCoroutine(TypeText(currentQuestion.question));

            for (int i = 0; i < answerButtons.Length; i++)
            {
                int index = i;
                answerButtons[i].GetComponentInChildren<Text>().text = currentQuestion.answers[i];
                answerButtons[i].interactable = true;
                answerButtons[i].image.color = Color.white;
                answerButtons[i].image.sprite = defaultSprite;

                LeanTween.scale(answerButtons[i].gameObject, Vector3.one, 0.5f).setFrom(Vector3.zero).setEaseOutBounce();
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() =>
                {
                    SelectAnswer(index);
                });
            }

            resultText.text = "";
            timeRemaining = 10f;
            timerText.text = timeRemaining.ToString("F0");
        }
        else
        {
            EndQuiz();
        }
    }

    IEnumerator TypeText(string textToType)
    {
        questionText.text = "";
        foreach (char letter in textToType.ToCharArray())
        {
            questionText.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    void SelectAnswer(int selectedIndex)
    {
        PlayButtonClickSound(); // Play button click sound

        isAnswerSelected = true; // Mark that an answer has been selected
        UpdateSubmitButtonState(); // Enable submit button since an answer is selected

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i == selectedIndex)
            {
                answerButtons[i].interactable = false;
                answerButtons[i].image.color = Color.gray;
                LeanTween.scale(answerButtons[i].gameObject, Vector3.one * 0.9f, 0.2f).setEaseInOutQuad();
            }
            else
            {
                LeanTween.scale(answerButtons[i].gameObject, Vector3.one, 0.2f).setEaseInOutQuad();
                answerButtons[i].interactable = true;
                answerButtons[i].image.color = Color.white;
                answerButtons[i].image.sprite = defaultSprite;
            }
        }
    }

    public void SubmitAnswer()
    {
        if (!hasAnswered && isAnswerSelected)  // Ensure an answer is selected
        {
            Question currentQuestion = shuffledQuestions[currentQuestionIndex];
            submitButton.interactable = false;
            hasAnswered = true;

            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (!answerButtons[i].interactable)
                {
                    if (i == currentQuestion.correctAnswer)
                    {
                        answerButtons[i].image.sprite = correctSprite;
                        resultText.text = "Correct!";
                        PlayCorrectSound(); // Play correct answer sound
                        LeanTween.scale(answerButtons[i].gameObject, Vector3.one * 1.2f, 0.2f).setEasePunch();
                        score++;
                    }
                    else
                    {
                        answerButtons[i].image.sprite = wrongSprite;
                        resultText.text = "Wrong!";
                        PlayWrongSound(); // Play wrong answer sound
                        LeanTween.rotateZ(answerButtons[i].gameObject, 10f, 0.1f).setLoopPingPong(1);
                    }
                }
            }

            HighlightCorrectAnswer(currentQuestion.correctAnswer);
            scoreText.text = score.ToString();
            Invoke("NextQuestion", 2f);
        }
    }

    void HighlightCorrectAnswer(int correctIndex)
    {
        answerButtons[correctIndex].image.sprite = correctSprite ;
        LeanTween.scale(answerButtons[correctIndex].gameObject, Vector3.one * 1.1f, 0.2f).setEasePunch();
    }

    void NextQuestion()
    {
        currentQuestionIndex++;
        DisplayQuestion();
    }

    void Update()
    {
        if (isGameActive)
        {
            timeRemaining -= Time.deltaTime;
            timerText.text = Mathf.Ceil(timeRemaining).ToString();

            if (timeRemaining <= 0 && !hasAnswered)
            {
                isGameActive = false;
                resultText.text = "Time's up!";
                HighlightCorrectAnswer(shuffledQuestions[currentQuestionIndex].correctAnswer);
                SubmitAnswer();
                submitButton.interactable = false;
                Invoke("NextQuestion", 2f);
            }
        }
    }

    void EndQuiz()
    {
        questionText.text = "Quiz Complete!";
        scoreText.text = "Final Score: " + score.ToString();
        LeanTween.moveLocalY(endPanel, 0f, 0.5f).setFrom(-Screen.height).setEaseOutBounce();
        endPanel.SetActive(true);
        endScoreText.text = "Your Score: " + score.ToString() + "/" + shuffledQuestions.Count;

        foreach (Button btn in answerButtons)
        {
            btn.gameObject.SetActive(false);
        }

        timerText.text = "";
    }

    void ReplayQuiz()
    {
        PlayButtonClickSound(); // Play button click sound
        foreach (Button btn in answerButtons)
        {
            btn.gameObject.SetActive(true);
        }
        endPanel.SetActive(false);
        currentQuestionIndex = 0;
        score = 0;
        ShuffleQuestions();
        DisplayQuestion();
    }

    void BackToCategorySelection()
    {
        PlayButtonClickSound(); // Play button click sound
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void PlayBackgroundMusic()
    {
        if (isSoundOn && backgroundMusic != null)
        {
            backgroundMusic.Play();
        }
    }

    void PlayCorrectSound()
    {
        if (isSoundOn && correctSound != null)
        {
            correctSound.Play();
        }
    }

    void PlayWrongSound()
    {
        if (isSoundOn && wrongSound != null)
        {
            wrongSound.Play();
        }
    }

    void PlayButtonClickSound()
    {
        if (isSoundOn && buttonClickSound != null)
        {
            buttonClickSound.Play();
        }
    }
    public Sprite on, off;
    void ToggleSound()
    {
        toggleSoundButton.GetComponent<Image>().sprite = isSoundOn ? on : off;
        isSoundOn = !isSoundOn;
        if (!isSoundOn)
        {
            backgroundMusic.Stop();
        }
        else
        {
            PlayBackgroundMusic();
        }
    }

    // Method to update the state and color of the submit button
    void UpdateSubmitButtonState()
    {
        submitButton.interactable = isAnswerSelected;
        submitButton.image.color = isAnswerSelected ? submitActiveColor : submitInactiveColor;
    }
}
