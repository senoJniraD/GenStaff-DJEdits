using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GSBPGEMG.UI
{
    public class InputPressedRepeater
    {
        public bool pressed;
        public int noOfPresses;
        public double startTime;
        public double nextTime;
        public float repeatRate;

        public static float maxTime = 0.30f;
        public static float minTime = 0.03f;
        public static float changeTime = 3f;
        public static float pressesMultiplierTime = 0f;

        public float? overrideMaxTime;
        public float? overrideMinTime;
        public float? overrideChangeTime;
        public float? overridePressesMultiplierTime;

        public void Update(double totalTime, double elapsedTime, bool inputPressed)
        {
            float maxTime = InputPressedRepeater.maxTime;
            if (overrideMaxTime != null)
                maxTime = (float)overrideMaxTime;

            float minTime = InputPressedRepeater.minTime;
            if (overrideMinTime != null)
                minTime = (float)overrideMinTime;

            float changeTime = InputPressedRepeater.changeTime;
            if (overrideChangeTime != null)
                changeTime = (float)overrideChangeTime;

            float pressesMultiplierTime = InputPressedRepeater.pressesMultiplierTime;
            if (overridePressesMultiplierTime != null)
                pressesMultiplierTime = (float)overridePressesMultiplierTime;

            if (inputPressed == true)
            {
                if (startTime == 0d)
                    startTime = totalTime;
                if (nextTime == 0d || totalTime > nextTime)
                {
                    pressed = true;
                    noOfPresses = 1;
                    if (nextTime > 0d)
                    {
                        noOfPresses += (int)((totalTime - nextTime) / repeatRate);
                        if (pressesMultiplierTime > 0f)
                            noOfPresses = (int)(noOfPresses * Math.Max(1f, (totalTime - startTime) / pressesMultiplierTime));
                    }
                    nextTime = totalTime + repeatRate;
                }
                else
                {
                    pressed = false;
                    noOfPresses = 0;
                }
                repeatRate -= (maxTime - minTime) / (60 * changeTime) * ((float)elapsedTime * 60f);
                repeatRate = Math.Max(repeatRate, minTime);
            }
            else
            {
                Reset();
            }
        }

        public void SetRate(float? maxTime, float? minTime, float? changeTime, float? pressesMultiplierTime)
        {
            overrideMaxTime = maxTime;
            overrideMinTime = minTime;
            overrideChangeTime = changeTime;
            overridePressesMultiplierTime = pressesMultiplierTime;
        }

        public void Reset()
        {
            pressed = false;
            noOfPresses = 0;
            startTime = 0d;
            nextTime = 0d;
            repeatRate = maxTime;
        }
    }
}
