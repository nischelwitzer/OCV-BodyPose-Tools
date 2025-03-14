using UnityEngine;

public class OCVShowPoseLayers : MonoBehaviour
{
    public bool showMask = true;
    public bool showSkeleton = true;
    public bool showDots = true;
    public bool showNumbers = true;

    public bool showCircleFullBody = true;
    public bool showCircleUpperBody = true;
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
