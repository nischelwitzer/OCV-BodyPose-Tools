using TMPro;
using UnityEngine;

public class DMT_PoseBodyCheck : MonoBehaviour
{

    public TextMeshProUGUI myBodyPoseCounterText;

    void Update()
    {
        int numBodyPoses = DMT.StaticStore.posesCounter;

        if (myBodyPoseCounterText != null)
        {
            myBodyPoseCounterText.text = "" + DMT.StaticStore.posesCounter;
        }

        if (numBodyPoses > 0)
            this.GetComponent<Renderer>().material.color = new Color(0, 255, 0);
        else
            this.GetComponent<Renderer>().material.color = new Color(255, 0, 0);
    }
}
