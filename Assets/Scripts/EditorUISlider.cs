using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace EditorUIControls
{
    public class EditorUISlider : EditorUIControl
    {
        public Slider slider;
        private void Awake()
        {
            type = ControlTypes.Slider;
        }
    }
}
