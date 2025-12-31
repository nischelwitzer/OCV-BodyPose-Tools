

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Drawing;

public class DMTPoseDrawStatic : MonoBehaviour
{
    private Texture2D debugTexture;
    private Renderer debugRenderer; // to 3D object
    private Mat debugMatrix;

    public GameObject drawPose;
    private RawImage debugRawImage; // to UI

    // ##########################################################################
    // ## MAIN Pose Detection 
    // ##########################################################################


    IEnumerator Start()
    {

        Debug.Log("DMTPoseDrawStatic::Start and waits for Web-CAM init");
        // yield return new WaitUntil(() => DMT.DMTStaticStoreFace68.faceCounter >= 0);

        while ((DMT.Pose.DMTStaticStoreHumanPose.poseCounter < 0) || (DMT.Pose.DMTStaticStoreHumanPose.imgHeight == 0) || (DMT.Pose.DMTStaticStoreHumanPose.imgWidth == 0)) // wait till cam init and image size known
        {
            yield return null; // ein Frame warten
        }

        GameObject.Find("Main Camera").GetComponent<Camera>().orthographicSize = 540; //  DMT.Pose.DMTStaticStoreHumanPose.imgHeight / 2;

        // yield return new WaitForSeconds(3.0f); // wait for DMT init
        Debug.LogWarning("DMTPoseDrawStatic::Start Image>" + DMT.Pose.DMTStaticStoreHumanPose.imgWidth + "x" + DMT.Pose.DMTStaticStoreHumanPose.imgHeight);

        debugMatrix = new Mat(DMT.Pose.DMTStaticStoreHumanPose.imgHeight, DMT.Pose.DMTStaticStoreHumanPose.imgWidth, CvType.CV_8UC4);
        debugTexture = new Texture2D(DMT.Pose.DMTStaticStoreHumanPose.imgWidth, DMT.Pose.DMTStaticStoreHumanPose.imgHeight, TextureFormat.RGBA32, false); // with Alpha
        if (debugRenderer != null) debugRenderer.GetComponent<Renderer>().material.mainTexture = debugTexture;

        debugRawImage = drawPose.GetComponent<RawImage>();
        debugRawImage.texture = debugTexture;
    }

    IEnumerator Sleep(int waitSec)
    {
        Debug.Log("Pause");
        yield return new WaitForSeconds(waitSec);
    }

    void Update()
    {
        if ((DMT.Pose.DMTStaticStoreHumanPose.poseCounter > 0) && (debugTexture != null))
        {
            drawPose.SetActive(true);

            debugMatrix = new Mat(DMT.Pose.DMTStaticStoreHumanPose.imgHeight, DMT.Pose.DMTStaticStoreHumanPose.imgWidth, CvType.CV_8UC4, new Scalar(0, 0, 0, 0));
            DrawPose();
            DrawBoundingBox();
            DrawNose();

            OpenCVForUnity.UnityIntegration.OpenCVMatUtils.MatToTexture2D(debugMatrix, debugTexture);
            debugRawImage.texture = debugTexture;
        }
        else
            drawPose.SetActive(false);
    }

    private void DrawBoundingBox()
    {
        // Draw Bounding Box
        UnityEngine.Rect drawBB = DMT.Pose.DMTStaticStoreHumanPose.boundingBox;

        Imgproc.rectangle(debugMatrix, new OpenCVForUnity.CoreModule.Point(drawBB.xMin, drawBB.yMin), new OpenCVForUnity.CoreModule.Point(drawBB.xMax, drawBB.yMax), new Scalar(0, 255, 0, 255), 2);
        Imgproc.line(debugMatrix, new OpenCVForUnity.CoreModule.Point(drawBB.xMin, drawBB.yMin), new OpenCVForUnity.CoreModule.Point(drawBB.xMax, drawBB.yMax), new Scalar(255, 255, 0, 255), 1);
        Imgproc.line(debugMatrix, new OpenCVForUnity.CoreModule.Point(drawBB.xMax, drawBB.yMin), new OpenCVForUnity.CoreModule.Point(drawBB.xMin, drawBB.yMax), new Scalar(255, 255, 0, 255), 1);
    }

    private void DrawNose()
    {
        OpenCVForUnity.CoreModule.Point nosePoint = 
            new OpenCVForUnity.CoreModule.Point(DMT.Pose.DMTStaticStoreHumanPose.nosePoint.x, DMT.Pose.DMTStaticStoreHumanPose.nosePoint.y);
        Imgproc.circle(debugMatrix, nosePoint, 6, new Scalar(255, 255, 0, 255), -1);
    }

    private static readonly int[] ConnectPoints = new int[]
    {
        00, 01, 01, 02, 02, 03, 03, 07, 00, 04, 04, 05, 05, 06, 06, 08, 09, 10,
        11, 12, 12, 14, 14, 16, 16, 18, 16, 20, 16, 22,
        11, 13, 13, 15, 15, 21, 15, 17, 15, 19,
        11, 23, 12, 24, 23, 24,
        24, 26, 26, 28, 28, 32, 28, 30, 30, 32,
        23, 25, 25, 27, 27, 29, 27, 31, 29, 31
    };

    void DrawPose()
    {
        OpenCVForUnity.CoreModule.Point newPoint1 = new OpenCVForUnity.CoreModule.Point(0, 0);
        OpenCVForUnity.CoreModule.Point newPoint2 = new OpenCVForUnity.CoreModule.Point(0, 0);
        int run = 0;

        Vector2[] posePoints = DMT.Pose.DMTStaticStoreHumanPose.bodyPoseScreen2D;

        for (run = 0; run < (ConnectPoints.Length / 2); run++)
        {
            newPoint1 = new OpenCVForUnity.CoreModule.Point(posePoints[ConnectPoints[run * 2]].x, posePoints[ConnectPoints[run * 2]].y);
            newPoint2 = new OpenCVForUnity.CoreModule.Point(posePoints[ConnectPoints[run * 2 + 1]].x, posePoints[ConnectPoints[run * 2 + 1]].y);

            Imgproc.line(debugMatrix, newPoint1, newPoint2, new Scalar(0, 0, 255, 255), 2);
        }

        for (run = 0; run < 33; run++)
        {
            Scalar color = new Scalar(0, 0, 255, 255);
            int font = Imgproc.FONT_HERSHEY_SIMPLEX;
            double scale = 0.3d;
            int thickness = 1;
            string text = run.ToString();

            OpenCVForUnity.CoreModule.Point newPoint = new OpenCVForUnity.CoreModule.Point(posePoints[run].x, posePoints[run].y);
            Imgproc.putText(debugMatrix, text, newPoint, font, scale, color, thickness);
            Imgproc.circle(debugMatrix, newPoint, 3, new Scalar(255, 0, 0, 255), -1);
        }

    }
}

