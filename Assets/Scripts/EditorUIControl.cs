using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EditorUIControls
{
    public class EditorUIControl : MonoBehaviour
    {
        public enum ControlTypes { Toggle, Slider, InputField }
        [HideInInspector]
        public ControlTypes type;
        public Text label;
    }
}

