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
    public void StartDialog(Quest quest)
    {
        DialogPanel.SetActive(true);
        quest.currentIndex++;
        if (quest.currentIndex > quest.dialogData.CharacterSpeeches.Length)
        {
            DialogPanel.SetActive(false);
            return;
        }
        PlayCharacterSpeech(quest.dialogData.CharacterSpeeches[quest.currentIndex - 1]);

    }
    private void PlayCharacterSpeech(CharacterSpeech speech)
    {
        dialogText.text = speech.Speech;
        characterIcon.sprite = speech.CharacterSprite;
        characterNameText.text = speech.CharacterName;
    }
}
