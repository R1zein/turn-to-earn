using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogNPC : MonoBehaviour
{
    public DialogSpeech[] dialogSpeeches;
    public float ToggleDiatnce;
    public GameObject DialogPanel;
    public TMP_Text textSpeech;
    public Image characterImage;
    public TMP_Text characterNameText;
    private DialogSpeech currentDialog;
    private FirstPersonMovement player;
    private int currentDialogIndex = 0;
    void Start()
    {
        player = FindObjectOfType<FirstPersonMovement>();

    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.X)) 
        { 
            if (Vector3.Distance(transform.position, player.transform.position) <= ToggleDiatnce)
            {
                StartDialog();
            }
        }
    }
    private void StartDialog()
    {
        Toggle(true);
        if(currentDialogIndex >= dialogSpeeches.Length)
        {
            Toggle(false);
        }
        else
        {
            currentDialog = dialogSpeeches[currentDialogIndex];
            StartCoroutine(ShowDialog());
        }
    }

    private void Toggle(bool value)
    {
        player.enabled = !value;
        DialogPanel.SetActive(value);
        Time.timeScale = value? 0 : 1;
    }

    IEnumerator ShowDialog()
    {
        int index = 0;
        while (index < currentDialog.CharacterSpeeches.Length)
        {
            CharacterSpeech speech = currentDialog.CharacterSpeeches[index];
            textSpeech.text = speech.Speech;
            characterImage.sprite = speech.CharacterSprite;
            characterNameText.text = speech.CharacterName;

            yield return null;
            yield return new WaitUntil(()=> Input.GetKeyDown(KeyCode.Return));

            if (speech.quest != null)
            {
                Toggle(false);
                yield return null;
                yield return new WaitUntil(() => speech.quest.IsQuestComplited());
                Toggle(true);
            }
            index++;
        }
        
        currentDialogIndex++;
        Toggle(false);
    }



}
