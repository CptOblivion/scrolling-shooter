using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace EditorUIControls
{
    public class EditorUIToggle : EditorUIControl
    {
        public Toggle toggle;
        private void Awake()
        {
            type = ControlTypes.Toggle;
        }
    }
}
