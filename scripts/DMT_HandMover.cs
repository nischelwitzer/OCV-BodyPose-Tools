using UnityEngine;
using TMPro;
using OpenCVForUnityExample.DnnModel;
using static UnityEngine.Rendering.DebugUI;

public class DMT_HandMover : MonoBehaviour
{
    public TextMeshProUGUI myText;
    public int posID = 0;
    public bool showDebug = false;

    public int screenWidth = 1920;  // 3840; // 2496
    public int screenHeight = 1080;  // 2160; // 1664

    void Update()
    {
        if (DMT.StaticStore.bodyPose != null)
        {
            Vector2 myShowPos = new Vector2(DMT.StaticStore.bodyPoseNDC[posID].x * screenWidth - screenWidth / 2,
                                         -DMT.StaticStore.bodyPoseNDC[posID].y * screenHeight + screenHeight / 2);

            this.gameObject.transform.position = new Vector3(myShowPos.x, myShowPos.y, 0);

            string myKeyPointName = ((MediaPipePoseEstimatorDMT.KeyPoint)posID).ToString();

            if ((myText != null) && showDebug)
            {
                myText.text = "PoseID[" + posID + "]: "+ myKeyPointName + "\n\n";
                myText.text += "NDC     x:" + DMT.StaticStore.bodyPoseNDC[20].x.ToString("0.000")
                    + " y:" + DMT.StaticStore.bodyPoseNDC[posID].y.ToString("0.000") + "\n\n";
                // + " z:" + DMT.StaticStore.bodyPoseNDC[posID].z.ToString("0.000") + "\n";
                myText.text += "Screen  x:" + DMT.StaticStore.bodyPose[posID].x.ToString("0000").PadLeft(5)
                + " y:" + DMT.StaticStore.bodyPose[posID].y.ToString("0000").PadLeft(5) + "\n";
                // + " z:" + DMT.StaticStore.bodyPose[posID].z.ToString("0000") + "\n";
                myText.text += "Unity   x:" + myShowPos.x.ToString("0000").PadLeft(5)
                    + " y:" + myShowPos.y.ToString("0000").PadLeft(5) + "\n";
            }
        }
    }
}
