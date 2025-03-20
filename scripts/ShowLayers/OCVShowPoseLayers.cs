using UnityEngine;

public class OCVShowPoseLayers : MonoBehaviour
{
    [Tooltip("Show blue Mask around Body")]
    public bool showMask = true;
    [Tooltip("Show white Pose Skeleton")]
    public bool showSkeleton = true;
    [Tooltip("Show red Dots")]
    public bool showDots = true;
    [Tooltip("Show text numbers for Landmarks")]
    public bool showNumbers = true;

    [Tooltip("Show blue full body circle")]
    public bool showCircleFullBody = true;
    [Tooltip("Show yellow upper body circle")]
    public bool showCircleUpperBody = true;
    [Tooltip("Show green face Box")]
    public bool showBoxFace = true;
    [Tooltip("Show confidence Value")]
    public bool showTextConfidence = true;
    [Tooltip("Show Bounding Box")]
    public bool showBoundingBox = true;

    // Update is called once per frame
    void Update()
    {
        DMT.StaticStore.ShowMaskLayer = showMask;
        DMT.StaticStore.ShowSkeletonLayer = showSkeleton;
        DMT.StaticStore.ShowDotsLayer = showDots;
        DMT.StaticStore.ShowNumbersLayer = showNumbers;

        DMT.StaticStore.ShowCircleFullBodyLayer  = showCircleFullBody;
        DMT.StaticStore.ShowCircleUpperBodyLayer = showCircleUpperBody;
        DMT.StaticStore.ShowBoxFaceLayer         = showBoxFace;
        DMT.StaticStore.ShowTextConfidenceLayer  = showTextConfidence;
        DMT.StaticStore.ShowBoundingBoxLayer     = showBoundingBox;
    }
}
