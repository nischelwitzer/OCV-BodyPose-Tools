using TMPro;
using UnityEngine;

namespace DMT
{
    public class DMT_ShowAngles : MonoBehaviour
    {
        [Tooltip("Text to show Information")]
        public TextMeshProUGUI myText;

        [Tooltip("Show Debugging true|false")]
        public bool showDebug = false;

        void Update()
        {
            if ((myText != null) && showDebug)
            {
                myText.text  = "Right Angle: " + DMT.StaticStore.getRightAngle.ToString("000.0") + "\n";
                myText.text += "Left Angle:  " + DMT.StaticStore.getLeftAngle.ToString("000.0");
                
                this.transform.rotation = Quaternion.Euler(0, 0, -1.0f * (float)DMT.StaticStore.getRightAngle);
            }
        }
    }
}

