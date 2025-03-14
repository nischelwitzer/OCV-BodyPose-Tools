using UnityEngine;

public class OCVShowPoseLayers : MonoBehaviour
{
    [Tooltip("Show blue Mask around Body")]
    public bool showMask = true;
    [Tooltip("Show Pose Skeleton")]
    public bool showSkeleton = true;
    [Tooltip("Show red Dots")]
    public bool showDots = true;
    [Tooltip("Show numbers for Landmarks")]
    public bool showNumbers = true;

    [Tooltip("Show blue full body circle")]
    public bool showCircleFullBody = true;
    [Tooltip("Show yellow upper body circle")]
    public bool showCircleUpperBody = true;
    [Tooltip("Show green face Box")]
    public bool showBoxFace = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DMT.StaticStore.showMaskLayer = showMask;
        DMT.StaticStore.showSkeletonLayer = showSkeleton;
        DMT.StaticStore.showDotsLayer = showDots;
        DMT.StaticStore.showNumbersLayer = showNumbers;

        DMT.StaticStore.showCircleFullBodyLayer = showCircleFullBody;
        DMT.StaticStore.showCircleUpperBodyLayer = showCircleUpperBody;
        DMT.StaticStore.showBoxFaceLayer = showBoxFace;
    }

    // Update is called once per frame
    void Update()
    {
        DMT.StaticStore.showMaskLayer = showMask;
        DMT.StaticStore.showSkeletonLayer = showSkeleton;
        DMT.StaticStore.showDotsLayer = showDots;
        DMT.StaticStore.showNumbersLayer = showNumbers;

        DMT.StaticStore.showCircleFullBodyLayer = showCircleFullBody;
        DMT.StaticStore.showCircleUpperBodyLayer = showCircleUpperBody;
        DMT.StaticStore.showBoxFaceLayer = showBoxFace;
    }
}
