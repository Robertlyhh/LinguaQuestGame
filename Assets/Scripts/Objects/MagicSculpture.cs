using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;

public class MagicSculpture : Sign
{

    public int sculptureID;
    public SpriteRenderer spriteRenderer;
    public bool isCompleted = false;
    public BoolValue sculptureCompletion;
    public List<MultipleChoiceQuestion> questionData;
    public List<WordOrderQuestion> wordOrderData;
    public List<FeatureMatchQuestion> featureMatchQuestions;
    public MiniGame_MultipleChoice quizManager;
    public MiniGame_WordOrder wordOrderManager;
    public MiniGame_FeatureMatch featureMatchManager;
    public GameObject PlayerPrefab;
    public string[] preparedDialogs;
    public string[] actualDialogs;
    public bool hasPreparedDialogs = false;
    public BoolValue[] prereqs;
    public enum ChallengeType
    {
        WordOrder,
        MultipleChoice,
        FeatureMatch
    }

    public ChallengeType challengeType;

    public override void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (sculptureCompletion != null)
        {
            isCompleted = sculptureCompletion.runtimeValue;
            if (isCompleted && spriteRenderer != null)
            {
                // Change appearance to indicate completion
                // Optionally, you can also make it semi-transparent if desired:
                // spriteRenderer.color = new Color(0f, 1f, 0f, 0.7f); // green and semi-transparent
                this.gameObject.SetActive(false);
            }
        }
        if (PlayerPrefab == null)
        {
            PlayerPrefab = GameObject.FindGameObjectWithTag("Player");
        }

    }

    public override void Update()
    {

        if (isCompleted)
        {
            return;
        }
        if (dialogActive && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
        {
            if (audioSource != null && interactSound != null)
            {
                audioSource.PlayOneShot(interactSound);
            }
            checkPrereqs();

            if (!dialogBox.activeSelf)
            {
                checkPrereqs();
                dialogBox.SetActive(true);
                currentDialogIndex = 0;
                dialogText.text = actualDialogs.Length > 0 ? actualDialogs[currentDialogIndex] : "";
            }
            else
            {
                checkPrereqs();
                currentDialogIndex++;
                if (currentDialogIndex < actualDialogs.Length)
                {
                    dialogText.text = actualDialogs[currentDialogIndex];
                }
                else
                {
                    dialogBox.SetActive(false);
                    dialogActive = false;
                    currentDialogIndex = 0;
                    if (hasPreparedDialogs)
                    {
                        Debug.Log("All dialogs completed, launching mini-game.");
                        PlayerPrefab.GetComponent<PlayerExploring>().changeState(PlayerState.interact);
                        switch (challengeType)
                        {
                            case ChallengeType.WordOrder:
                                wordOrderManager.Launch(wordOrderData, OnMiniGameCompleted);
                                break;

                            case ChallengeType.MultipleChoice:
                                quizManager.LaunchQuestion(questionData, OnMiniGameCompleted);
                                break;
                            case ChallengeType.FeatureMatch:
                                featureMatchManager.Launch(featureMatchQuestions, OnMiniGameCompleted);
                                break;

                        }
                    }
                }
            }
        }
        else if (dialogActive && Input.GetKeyDown(KeyCode.Escape))
        {
            dialogBox.SetActive(false);
            dialogActive = false;
            currentDialogIndex = 0;
        }
    }

    void OnMiniGameCompleted(bool success)
    {
        if (success)
        {
            isCompleted = true;
            Debug.Log($"Sculpture {sculptureID} completed!");
            if (sculptureCompletion != null)
            {
                sculptureCompletion.runtimeValue = true;
            }
            interactSignal.Raise();
            PlayerPrefab.GetComponent<PlayerExploring>().changeState(PlayerState.walk);
            if (gameObject != null)
            {
                gameObject.SetActive(false);
            }
            // Change appearance to indicate completion
            // Optionally, you can also make it semi-transparent if desired:
            // spriteRenderer.color = new Color(0f, 1f, 0f, 0.7f); // green and semi-transparent
        }
        else
        {
            Debug.Log("Failed mini-game. Try again later.");
        }
        PlayerPrefab.GetComponent<PlayerExploring>().changeState(PlayerState.walk);

    }

    public void checkPrereqs()
    {
        foreach (BoolValue prereq in prereqs)
        {
            if (prereq.runtimeValue == false)
            {
                Debug.Log("Resetting to original dialogs.");
                actualDialogs = dialogs;
                hasPreparedDialogs = false;
                return;
            }
        }
        Debug.Log("All prerequisites met, switching to prepared dialogs.");
        actualDialogs = preparedDialogs;
        hasPreparedDialogs = true;
    }
}
