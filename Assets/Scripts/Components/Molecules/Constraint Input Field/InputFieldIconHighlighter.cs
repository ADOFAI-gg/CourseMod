using CourseMod.Components.Atoms.InputField;
using CourseMod.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// ReSharper disable CheckNamespace

namespace CourseMod.Components.Molecules.ConstraintInputField
{
    [ExecuteAlways]
    public class InputFieldIconHighlighter : MonoBehaviour
    {
        public InputFieldStyle inputFieldStyle;
        public TextMeshProUGUI unitText;
        public Image image;
        
        #if UNITY_EDITOR
        private int _lastInputFieldValueLength;
        #endif

        private void Awake()
        {
            inputFieldStyle.inputField.onValueChanged.AddListener(UpdateImageColor);
        }

        private void UpdateImageColor(string value)
        {
            Color color = value.Length > 0
                ? Color.white
                : Color.white.SetAlpha(.6f);
            
            image.color = color;
            if(!unitText) return;
            unitText.color = color;
        }

        public void SetText(string text) => UpdateImageColor(inputFieldStyle.inputField.text = text);

#if UNITY_EDITOR
        private void Update()
        {
            var length = inputFieldStyle.inputField.text.Length;

            if (_lastInputFieldValueLength != length)
            {
                _lastInputFieldValueLength = length;
                UpdateImageColor(inputFieldStyle.inputField.text);
            }
        }
        #endif
    }
}
