using TMPro;
using UnityEngine;
using Zenject;

// Interim uGUI display of the clock; replaced by a UI Toolkit
// TimeUIController at stage 7 of the architecture plan.
public class ClockView : MonoBehaviour
{
    [SerializeField] private TMP_Text timeText;

    [Inject] private TimeManager timeManager;

    private void OnEnable()
    {
        timeManager.OnClockChanged += Redraw;
    }

    private void OnDisable()
    {
        timeManager.OnClockChanged -= Redraw;
    }

    private void Redraw()
    {
        timeText.text = $"{timeManager.Hours}:{timeManager.Minutes}";
    }
}
