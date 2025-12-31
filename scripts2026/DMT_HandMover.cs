using UnityEngine;
using TMPro;

namespace DMT
{
    public class DMT_HandMover : MonoBehaviour
    {
        [Tooltip("Text to show Information")]
        public TextMeshProUGUI myText;

        [Tooltip("BodyPose Landmark ID [0-32]")]
        public int posID = 0;

        [Tooltip("Show Debugging true|false")]
        public bool showDebug = false;

        public int screenWidth = 1920;  // 3840; // 2496
        public int screenHeight = 1080;  // 2160; // 1664

        void Update()
        {
            if (DMT.Pose.DMTStaticStoreHumanPose.bodyPose != null)
            {
                Vector2 myShowPos = new Vector2(DMT.Pose.DMTStaticStoreHumanPose.bodyPoseNDC[posID].x * screenWidth - screenWidth / 2,
                                             -DMT.Pose.DMTStaticStoreHumanPose.bodyPoseNDC[posID].y * screenHeight + screenHeight / 2);

                this.gameObject.transform.position = new Vector3(myShowPos.x, myShowPos.y, gameObject.transform.position.z);

                string myKeyPointName = ((DMT.Pose.DMTStaticStoreHumanPose.HumanKeyPoint)posID).ToString();

                if ((myText != null) && showDebug)
                {
                    myText.text = "PoseID[" + posID + "]: " + myKeyPointName + "\n\n";
                    myText.text += "NDC     x:" + DMT.Pose.DMTStaticStoreHumanPose.bodyPoseNDC[20].x.ToString("0.000")
                        + " y:" + DMT.Pose.DMTStaticStoreHumanPose.bodyPoseNDC[posID].y.ToString("0.000") + "\n\n";
                    // + " z:" + DMT.StaticStore.bodyPoseNDC[posID].z.ToString("0.000") + "\n";
                    myText.text += "Screen  x:" + DMT.Pose.DMTStaticStoreHumanPose.bodyPose[posID].X.ToString("0000").PadLeft(5)
                    + " y:" + DMT.Pose.DMTStaticStoreHumanPose.bodyPose[posID].Y.ToString("0000").PadLeft(5) + "\n";
                    // + " z:" + DMT.StaticStore.bodyPose[posID].z.ToString("0000") + "\n";
                    myText.text += "Unity   x:" + myShowPos.x.ToString("0000").PadLeft(5)
                        + " y:" + myShowPos.y.ToString("0000").PadLeft(5) + "\n";
                }
            }
        }
    }
}
