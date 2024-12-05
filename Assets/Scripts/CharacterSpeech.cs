using System;
using UnityEngine;

[Serializable]
public class CharacterSpeech
{
    public string CharacterName;
    public Sprite CharacterSprite;
    [TextArea] public string Speech;
}
