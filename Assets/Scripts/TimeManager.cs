using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public TMP_Text timeText;
    public float secondsPerHour;
    public Light directionLight;
    public ReflectionProbe reflectionProbe;
    public TimePeriod[] timePeriods;
    private float timer;
    private int hours;
    private int minutes;



    private void Awake()
    {
        timer = 7 * secondsPerHour;
        Light directionlLight = GameObject.Find("Directional Light").GetComponent<Light>();
        AudioSource audio = GetComponent<AudioSource>();
        foreach (TimePeriod timePeriod in timePeriods)
        {
            timePeriod.InitSettings(secondsPerHour, audio, directionlLight, reflectionProbe);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= secondsPerHour * 24)
        {
            timer = 0;
        }
        hours = (int)(timer / secondsPerHour);
        minutes = (int)((timer % secondsPerHour) / (secondsPerHour / 60));
        timeText.text = $"{hours}:{minutes}";
        foreach (TimePeriod timePeriod in timePeriods)
        {
            timePeriod.ProgressTime(timer);
        }
    }
}


