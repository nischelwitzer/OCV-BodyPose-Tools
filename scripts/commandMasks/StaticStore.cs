using OpenCVForUnity.CoreModule;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using static OpenCVForUnityExample.DnnModel.MediaPipePoseEstimatorDMT;

// usage: DMT.StaticStore.myData = ...

namespace DMT
{
    public static class StaticStore
    {
        private static double _myData = 0;

        private static ScreenLandmark[] _landmarks_screen; // = data.landmarks_screen;
        private static Vector3[] _landmarks_world;
        private static Vector3[] _landmarks_NDC;

        private static int _poses = 0;    // pose counter
        private static float _confidence; // pose confidence

        private static double _leftAngle = 0.0f;
        private static double _rightAngle = 0.0f;

        private static UnityEngine.Rect _boundingBox = UnityEngine.Rect.zero;
        private static UnityEngine.Rect _boundingBoxNDC = UnityEngine.Rect.zero;

        // =====================================================================

        // PoseEstimationDrawDMT 
        // MediaPipePoseEstimatorDMT
        public static bool ShowMaskLayer { get; set; } // line 277
        // MediaPipePoseEstimatorDMT
        public static bool ShowSkeletonLayer { get; set; } // line 540
        public static bool ShowBoundingBoxLayer { get; set; }

        // MediaPipePoseEstimatorDMT
        public static bool ShowDotsLayer { get; set; } // line 600
        public static bool ShowNumbersLayer { get; set; } // line 600

        // MediaPipePersonDetectorDMT
        // call: PoseEstimationDrawDMT 276
        // draw: MediaPipePersonDetectorDMT visualize 271
        public static bool ShowBoxFaceLayer { get; set; }
        public static bool ShowCircleFullBodyLayer { get; set; } // line 300
        public static bool ShowCircleUpperBodyLayer { get; set; }
        public static bool ShowTextConfidenceLayer { get; set; }

        public static Vector2 BoundingBoxCenter // main BoundingBoxes
        {
            get { return new Vector2(_boundingBox.x + _boundingBox.width / 2, _boundingBox.y + _boundingBox.height / 2); }
        }

        public static Vector2 BoundingBoxCenterNDC // main BoundingBoxes
        {
            get { return new Vector2(_boundingBoxNDC.x + _boundingBoxNDC.width / 2, _boundingBoxNDC.y + _boundingBoxNDC.height / 2); }
        }

        public static UnityEngine.Rect myBoundingBox // main BoundingBoxes
        {
            get { return _boundingBox; }
            set
            {
                UnityEngine.Rect gotData = value;
                _boundingBox = gotData;
            }
        }

        public static UnityEngine.Rect myBoundingBoxNDC // main BoundingBoxes
        {
            get { return _boundingBoxNDC; }
            set
            {
                UnityEngine.Rect gotData = value;
                _boundingBoxNDC = gotData;
            }
        }

        // =====================================================================

        public static float confidence // Body Pose Confidence 
        {
            get { return _confidence; }
            set
            {
                _confidence = value;
            }
        }

        public static int posesCounter // number of detected poses
        {
            get { return _poses; }
            set
            {
                _poses = value;
            }
        }

        // Normalized Device Coordinates (NDC) for body pose
        // calculated in MediaPipePoseEstimatorDMT

        public static Vector3[] bodyPoseNDC
        {
            get { return _landmarks_NDC; }
            set
            {
                _landmarks_NDC = value;
            }
        }

        public static ScreenLandmark[] bodyPose
        {
            get { return _landmarks_screen; }
            set
            {
                _landmarks_screen = value;
            }
        }

        public static Vector3[] bodyPoseWorld
        {
            get { return _landmarks_world; }
            set
            {
                _landmarks_world = value;
            }
        }

        // =====================================================================

        public static double getLeftAngle
        {
            get
            {
                if (_poses == 1)
                {
                    Point centerPoint = new Point(_landmarks_screen[11].x, _landmarks_screen[11].y);
                    Point leftPoint = new Point(_landmarks_screen[19].x, _landmarks_screen[19].y);
                    _leftAngle = Mathf.Atan2((float)leftPoint.y - (float)centerPoint.y,
                        (float)leftPoint.x - (float)centerPoint.x) * 180.0 / Mathf.PI;
                    
                }
                else
                {
                    _leftAngle = 0.0f;
                }
                return _leftAngle;
            }
        }

        public static double getRightAngle
        {
            get
            {
                if (_poses == 1)
                {
                    Point centerPoint = new Point(_landmarks_screen[12].x, _landmarks_screen[12].y);
                    Point leftPoint = new Point(_landmarks_screen[20].x, _landmarks_screen[20].y);
                    _rightAngle = Mathf.Atan2((float)leftPoint.y - (float)centerPoint.y,
                        (float)leftPoint.x - (float)centerPoint.x) * 180.0 / Mathf.PI; //  + 90.0f;
                    
                }
                else
                {
                    _rightAngle = 0.0f;
                }
                return _rightAngle;
            }
        }

        // =====================================================================

        // example usage of getter and setter

        public static double myData
        {
            get { return _myData; }

            set
            {
                double gotData = value;
                if ((gotData >= 0.0f) && (gotData <= 360.0f))
                {
                    _myData = value;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("setter warning for DMT.StaticStore");
                }
            }
        }


    }
}
