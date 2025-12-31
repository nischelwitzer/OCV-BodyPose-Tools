using UnityEngine;
using TMPro;

using System.Collections;

namespace DMT
{
    public class DMTPosePointMover : MonoBehaviour
    {
        [Tooltip("Text to show Information")]
        public TextMeshPro myText;

        [Tooltip("BodyPose Landmark ID [0-32]")]
        public int posID = 0;

        [Tooltip("Show Debugging true|false")]
        public bool showDebug = false;

        public int screenWidth = 0; // 1920;  // 3840; // 2496
        public int screenHeight = 0; // 1080;  // 2160; // 1664

        IEnumerator Start()
        {

            Debug.Log("DMTHandMover::Start and waits for imgSize");

            while ((DMT.Pose.DMTStaticStoreHumanPose.poseCounter < 0) || (DMT.Pose.DMTStaticStoreHumanPose.imgHeight == 0) || (DMT.Pose.DMTStaticStoreHumanPose.imgWidth == 0)) // wait till cam init and image size known
            {
                yield return null; // ein Frame warten
            }

            screenWidth = DMT.Pose.DMTStaticStoreHumanPose.imgWidth;
            screenHeight = DMT.Pose.DMTStaticStoreHumanPose.imgHeight;

            Debug.Log("DMTHandMover::Start Image Size got><color='yellow'>" + screenWidth + "x" + screenHeight+"</color>");
        }

        void Update()
        {
            if (DMT.Pose.DMTStaticStoreHumanPose.poseDetected)
            {
                Vector2 myShowPos = new Vector2(DMT.Pose.DMTStaticStoreHumanPose.bodyPoseNDC[posID].x * screenWidth - screenWidth / 2,
                                             +DMT.Pose.DMTStaticStoreHumanPose.bodyPoseNDC[posID].y * screenHeight - screenHeight / 2);

                // Debug.Log("HandMover::Update Move to PosID[<b><color='yellow'>" + posID + "</color></b>] x:" + myShowPos.x + " y:" + myShowPos.y);
                this.gameObject.transform.position = new Vector3(2*myShowPos.x, 2*myShowPos.y, gameObject.transform.position.z);

                string myKeyPointName = ((DMT.Pose.DMTStaticStoreHumanPose.HumanKeyPoint)posID).ToString();

                if ((myText != null) && showDebug)
                {
                    myText.text = "PoseID[" + posID + "]: <b>" + myKeyPointName + "</b>\n";
                    myText.text += "NDC     x:" + DMT.Pose.DMTStaticStoreHumanPose.bodyPoseNDC[posID].x.ToString("0.000")
                        + " y:" + DMT.Pose.DMTStaticStoreHumanPose.bodyPoseNDC[posID].y.ToString("0.000") + "\n";
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
