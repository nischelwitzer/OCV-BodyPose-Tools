using UnityEngine;

public class OCVShowPoseLayers : MonoBehaviour
{
    [Tooltip("Show blue Mask around Body")]
    public bool showMask = true;
    [Tooltip("Show white Pose Skeleton")]
    public bool showSkeleton = true;
    private bool oldSkeleton = true;
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
    private bool oldBoundingBox = true;

    void Start()
    {
        DMT.StaticStore.ShowSkeletonLayer = showSkeleton;
        DMT.StaticStore.ShowBoundingBoxLayer = showBoundingBox;
    }

    void Update()
    {
        DMT.StaticStore.ShowMaskLayer = showMask;
        DMT.StaticStore.ShowDotsLayer = showDots;
        DMT.StaticStore.ShowNumbersLayer = showNumbers;

        DMT.StaticStore.ShowCircleFullBodyLayer = showCircleFullBody;
        DMT.StaticStore.ShowCircleUpperBodyLayer = showCircleUpperBody;
        DMT.StaticStore.ShowBoxFaceLayer = showBoxFace;
        DMT.StaticStore.ShowTextConfidenceLayer = showTextConfidence;

        if (showSkeleton != oldSkeleton) // User Interface changed
        {
            oldSkeleton = showSkeleton;
            DMT.StaticStore.ShowSkeletonLayer = showSkeleton;
        }
        else
            showSkeleton = DMT.StaticStore.ShowSkeletonLayer;

        if (showBoundingBox != oldBoundingBox) // User Interface changed
        {
            oldBoundingBox = showBoundingBox;
            DMT.StaticStore.ShowBoundingBoxLayer = showBoundingBox;
        }
        else
            showBoundingBox = DMT.StaticStore.ShowBoundingBoxLayer;
    }
}
