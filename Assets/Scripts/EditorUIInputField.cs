using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace EditorUIControls
{
    public class EditorUIInputField : EditorUIControl
    {
        public InputField inputField;
        private void Awake()
        {
            type = ControlTypes.InputField;
        }
    }
}
