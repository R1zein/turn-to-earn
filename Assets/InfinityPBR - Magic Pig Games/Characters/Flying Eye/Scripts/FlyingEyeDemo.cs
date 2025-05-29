using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InfinityPBR.Demo
{
    public class FlyingEyeDemo : InfinityDemoCharacter
    {
        [Header("Texture Buttons")]
        public Button[] buttons;
        
        public void RandomizeTextures() => buttons[Random.Range(0, buttons.Length)].onClick.Invoke();
    }
}