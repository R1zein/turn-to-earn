using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;

public class DialogManager : MonoBehaviour
{
    public GameObject DialogPanel;
    public TMP_Text dialogText;
    public Image characterIcon;
    public TMP_Text characterNameText;
    private int currentDialogIndex = 0;

    public bool StartDialog(Quest quest)
    {
        Toggle(true);
        PlayCharacterSpeech(quest.dialogData.CharacterSpeeches[quest.currentIndex]);
        quest.currentIndex++;
        if (quest.currentIndex >= quest.dialogData.CharacterSpeeches.Length - 1)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    private void Toggle(bool value)
    {
        DialogPanel.SetActive(value);
    }

    private void PlayCharacterSpeech(CharacterSpeech speech)
    {
        dialogText.text = speech.Speech;
        characterIcon.sprite = speech.CharacterSprite;
        characterNameText.text = speech.CharacterName;
    }
}
