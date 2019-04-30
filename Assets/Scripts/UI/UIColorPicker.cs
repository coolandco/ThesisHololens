using HoloToolkit.Examples.ColorPicker;
using System;
using System.Collections;
using System.Collections.Generic;
using ThesisHololens.States;
using UnityEngine;

namespace ThesisHololens.UI
{
    public class UIColorPicker : UIContainer
    {
        GazeableColorPicker colorPickerScript;



        private void Start()
        {
            colorPickerScript =  GetComponentInChildren<GazeableColorPicker>();
            colorPickerScript.OnPickedColor.AddListener(ColorPicked);

        }

        private void ColorPicked(Color color)
        {
            if (!interactionAllowed())
                return;

            float H;
            float S;
            float V;

            //OpenHAB needs HSV
            Color.RGBToHSV(color, out H, out S, out V);

            //Open hab needs Values from 1 - 100/255
            int Hi = (int)(H * 360F);
            int Si = (int)(S * 100F);
            int Vi = (int)(V * 100F);


            //sentds a new color to the mqttCenter as r,g,b
            Debug.Log(adress + "picked color: " + Hi.ToString() + "," + Si.ToString() + "," + Vi.ToString());

            ItemStates.Instance.updateItem(adress, Hi.ToString() + "," + Si.ToString() + "," + Vi.ToString(), true);
        }

        protected override void onManipulationEnd_internal()
        {
           
        }

        protected override void onManipulationStart_internal()
        {
            
        }

        protected override void stateChanged_internal(string state)
        {
            
        }

        private void OnDestroy()
        {
            colorPickerScript.OnPickedColor.RemoveListener(ColorPicked);
        }
    }
}
