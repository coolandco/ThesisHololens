using HoloToolkit.Examples.InteractiveElements;
using ThesisHololens.States;
using UnityEngine;
using UnityEngine.UI;

namespace ThesisHololens.UI
{
    public class UIText : UIContainer
    {

        private TextMesh text;

        private void Start()
        {
            text = GetComponent<TextMesh>();
            text.text = "";

        }


        protected override void stateChanged_internal(string state)
        {
            text.text = FormatString(state, getHowManyCharsFitinLine());
        }

        protected override void onManipulationEnd_internal()
        {
            text.text = FormatString(text.text, getHowManyCharsFitinLine());
        }

        protected override void onManipulationStart_internal()
        {
            //doNothing
        }

        private int getHowManyCharsFitinLine()
        {
            //TODO: Use Bounds to define the maximum amount of chars in one line

            return 35;
        }



        private string FormatString(string myText,int  maxLineChars )
        {
            int charCount = 0;
            string[] words = myText.Split(' '); //Split the string into seperate words
            string result = "";

            for (var index = 0; index < words.Length; index++)
            {

                var word = words[index].Trim();

                if (index == 0)
                {
                    result = words[0];
                }

                if (index > 0)
                {
                    charCount += word.Length + 1; //+1, because we assume, that there will be a space after every word
                    if (charCount <= maxLineChars)
                    {
                        result += " " + word;
                    }
                    else
                    {
                        charCount = 0;
                        result += "\n " + word;
                    }
                }
            }

            return result;
        }
    }
}
