using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace CourseMod.Components.Molecules.Chip
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class Chip : MonoBehaviour
    {
        public TextMeshProUGUI mainText;
        public TextMeshProUGUI subText;

        public void ChangeMainText(string text)
        {
            var isEmpty = string.IsNullOrEmpty(text);

            mainText.gameObject.SetActive(!isEmpty);
            mainText.text = text ?? string.Empty;
        }

        public void ChangeSubText(string text)
        {
            var isEmpty = string.IsNullOrEmpty(text);
            
            subText.gameObject.SetActive(!isEmpty);
            subText.text = text ?? string.Empty;
        }

        public void ChangeText(string mainTextContent, string subTextContent)
        {
            ChangeMainText(mainTextContent);
            ChangeSubText(subTextContent);
        }
    }
}
