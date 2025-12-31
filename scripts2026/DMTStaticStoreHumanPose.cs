
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static OpenCVForUnity.UnityIntegration.Worker.DnnModule.MediaPipePoseEstimator;

// OpenCV Static Store
//
// Data from DMTPoseEstimation.cs
//
// 140 DMT.Pose.DMTStaticStoreHumanPose.poseCounter = detectedPoses;
// 164 DMT.Pose.DMTStaticStoreHumanPose.bodyPose = landmarksScreen; 
// 165 DMT.Pose.DMTStaticStoreHumanPose.confidence = data.Confidence;
// 174 DMT.Pose.DMTStaticStoreHumanPose.bodyPoseWorld
// 
// drawings, etc. in MediaPipePoseEstimator.cs 
//
// usage: DMT.DMTStaticStoreHumanPose.myData = ...


namespace DMT.Pose
{

    public static class DMTStaticStoreHumanPose
    {
        // Image Infos
        private static int _imgWidth = 0;
        private static int _imgHeight = 0;

        // Human Pose Landmarks Points

        private static ScreenLandmark[] _bodyPose_screen;
        private static Vector3[] _landmarks_world;
        private static Vector2[] _landmarks_NDC = new Vector2[39];
        private static Vector2[] _landmarks_screen = new Vector2[39]; // must be initialized 0-39 

        private static int _pose_counter = -1; // number of detected poses, -1 no webcam 
        private static bool _pose_detected = false;
        private static float _confidence; // pose confidence

        private static UnityEngine.Rect _boundingBox = UnityEngine.Rect.zero;
        private static UnityEngine.Rect _boundingBoxNDC = UnityEngine.Rect.zero;

        // special

        private static double _leftAngle = 0.0f;
        private static double _rightAngle = 0.0f;

        // LandMark Body Points Enum

        public enum HumanKeyPoint
        {
            // =========================
            // Kopf / Gesicht
            // =========================
            Nose = 0,    // Nasenspitze
            LeftEyeInner = 1,    // Linkes Auge (innen)
            LeftEye = 2,    // Linkes Auge (Mitte)
            LeftEyeOuter = 3,    // Linkes Auge (außen)
            RightEyeInner = 4,    // Rechtes Auge (innen)
            RightEye = 5,    // Rechtes Auge (Mitte)
            RightEyeOuter = 6,    // Rechtes Auge (außen)
            LeftEar = 7,    // Linkes Ohr
            RightEar = 8,    // Rechtes Ohr
            MouthLeft = 9,    // Mund links
            MouthRight = 10,   // Mund rechts

            // =========================
            // Oberkörper
            // =========================
            LeftShoulder = 11,   // Linke Schulter
            RightShoulder = 12,   // Rechte Schulter
            LeftElbow = 13,   // Linker Ellbogen
            RightElbow = 14,   // Rechter Ellbogen
            LeftWrist = 15,   // Linkes Handgelenk
            RightWrist = 16,   // Rechtes Handgelenk

            // =========================
            // Hände (Hilfspunkte)
            // =========================
            LeftPinky = 17,   // Linker kleiner Finger
            RightPinky = 18,   // Rechter kleiner Finger
            LeftIndex = 19,   // Linker Zeigefinger
            RightIndex = 20,   // Rechter Zeigefinger
            LeftThumb = 21,   // Linker Daumen
            RightThumb = 22,   // Rechter Daumen

            // =========================
            // Unterkörper
            // =========================
            LeftHip = 23,   // Linke Hüfte
            RightHip = 24,   // Rechte Hüfte
            LeftKnee = 25,   // Linkes Knie
            RightKnee = 26,   // Rechtes Knie
            LeftAnkle = 27,   // Linker Knöchel
            RightAnkle = 28,   // Rechter Knöchel

            // =========================
            // Füße
            // =========================
            LeftHeel = 29,   // Linke Ferse
            RightHeel = 30,   // Rechte Ferse
            LeftFootIndex = 31,   // Linke Fußspitze
            RightFootIndex = 32,   // Rechte Fußspitze

            // =========================
            // Zusätzliche / synthetische Landmarks
            // =========================
            HipCenter = 33,   // Mittelpunkt linker & rechter Hüfte
            ShoulderCenter = 34,   // Mittelpunkt linker & rechter Schulter
            Spine = 35,   // Wirbelsäule (Hüfte → Schulter)
            Neck = 36,   // Zwischen SchulterCenter & Kopf
            HeadCenter = 37,   // Mittelpunkt des Kopfes
            BodyCenter = 38    // Gesamter Körperschwerpunkt
        }

        // =====================================================================

        public static int imgWidth // image width
        {
            get { return _imgWidth; }
            set
            {
                _imgWidth = value;
            }
        }

        public static int imgHeight // image height
        {
            get { return _imgHeight; }
            set
            {
                _imgHeight = value;
            }
        }

        // =====================================================================
        // Pose Detection

        // 0-32 Points + 6 berechnete (synthetische Landmarks)
        // (x, y, z, visibility sichtbarkeit, presence sicherheit)

        public static ScreenLandmark[] bodyPose
        {
            get { return _bodyPose_screen; }
            set
            {
                _bodyPose_screen = value;

                BodyPoseScreen2NDC();
                FindBoundingBox();
                ConvertBoundingBox2NDC();

                // Debug.Log("Pose Landmarks updated: " + _pose_counter + " Landmarks: " + _bodyPose_screen.Length);
                
                for (int run = 0; run < _bodyPose_screen.Length; run++) // check all Points
                {
                    ScreenLandmark p = _bodyPose_screen[run];
                    // Debug.Log("Landmark [" + run + "]: (" + p.X + ", " + p.Y + ")");
                    _landmarks_screen[run].x = p.X;
                    _landmarks_screen[run].y = p.Y;
                }
            }
        }

        public static Vector2[] bodyPoseScreen2D
        {
            get { return _landmarks_screen; }
        }

        public static Vector3[] bodyPoseWorld
        {
            get { return _landmarks_world; }
            set
            {
                _landmarks_world = value;
            }
        }

        public static Vector2[] bodyPoseNDC
        {
            get { return _landmarks_NDC; }
        }

        public static int poseCounter // number of detected faces
        {
            get { return _pose_counter; }
            set
            {
                if (_pose_counter != value) // changed
                {
                    _pose_counter = value;
                    Debug.Log("BodyPose Counter changed: <b><color='green'>" + _pose_counter + "</color></b>");
                }
                _pose_detected = (_pose_counter > 0) ? true : false;
                // Debug.Log("Pose Counter updated: " + _pose_counter);
            }
        }

        public static bool poseDetected // is face detected
        {
            get { return _pose_detected; }
        }


        public static float confidence // Body Pose Confidence 
        {
            get { return _confidence; }
            set
            {
                _confidence = value;
            }
        }

        // =====================================================================
        // Bounding Boxes

        public static Vector2 BoundingBoxCenter // main BoundingBoxes
        {
            get { return new Vector2(_boundingBox.x + _boundingBox.width / 2, _boundingBox.y + _boundingBox.height / 2); }
        }

        public static Vector2 BoundingBoxCenterNDC // main BoundingBoxes
        {
            get { return new Vector2(_boundingBoxNDC.x + _boundingBoxNDC.width / 2, _boundingBoxNDC.y + _boundingBoxNDC.height / 2); }
        }

        public static UnityEngine.Rect boundingBox // main BoundingBoxes
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

        // .....................................................................
        // =====================================================================

        private static void BodyPoseScreen2NDC()
        {
            for (int run = 0; run < _bodyPose_screen.Length; run++) // check all Points
            {
                float xNDC = (float)_bodyPose_screen[run].X / (float)imgWidth;
                float yNDC = 1.0f - ((float)_bodyPose_screen[run].Y / (float)imgHeight); // flip Y axis, because screen y=0 is top and NDC y=0 is bottom
                _landmarks_NDC[run] = new Vector2(xNDC, yNDC);
            }
        }

        private static UnityEngine.Rect FindBoundingBox()
        {
            Point poseBBMin = new Point(9999, 9999);
            Point poseBBMax = new Point(-9999, -9999);

            for (int run = 0; run < _bodyPose_screen.Length; run++) // check all Points
            {
                Vector2 screen = new Vector2(_bodyPose_screen[run].X, _bodyPose_screen[run].Y);
                Point point = new Point(screen.x, screen.y);
                if (point.x > poseBBMax.x) poseBBMax.x = point.x;
                if (point.y > poseBBMax.y) poseBBMax.y = point.y;
                if (point.x < poseBBMin.x) poseBBMin.x = point.x;
                if (point.y < poseBBMin.y) poseBBMin.y = point.y;
            }

            UnityEngine.Rect myBoundingBox = new UnityEngine.Rect((float)poseBBMin.x, (float)poseBBMin.y,
                    (float)poseBBMax.x - (float)poseBBMin.x, (float)poseBBMax.y - (float)poseBBMin.y);

            _boundingBox = myBoundingBox;
            return myBoundingBox;
        }

        private static UnityEngine.Rect ConvertBoundingBox2NDC()
        {
            UnityEngine.Rect bb = _boundingBox;
            float xNDC = bb.x / imgWidth;

            float yNDC = 1.0f - ((bb.y + bb.height) / imgHeight); // flip Y axis, because bb.y is top-left corner and NDC y=0 is bottom
            float wNDC = bb.width / imgWidth;
            float hNDC = bb.height / imgHeight;
            UnityEngine.Rect myBoundingBoxNDC = new UnityEngine.Rect(xNDC, yNDC, wNDC, hNDC);
            return myBoundingBoxNDC;
        }

        public static Vector2 nosePoint
        {
            get { return new Vector2(_landmarks_screen[0].x, _landmarks_screen[0].y); }
        }

        // .....................................................................
        // =====================================================================
        // special calculations and helpers

        public static string getPoseInfo
        {
            get
            {
                StringBuilder sb = new StringBuilder(1536);

                sb.Append("-----------dmt pose estimator-----------");
                sb.AppendLine();
                sb.AppendFormat("Faces     : {0:F4}", _pose_counter);
                sb.AppendLine();
                sb.AppendFormat("Confidence: {0:F4}", _confidence);
                sb.AppendLine();
                sb.AppendFormat("Person Box: ({0:F3}, {1:F3}, {2:F3}, {3:F3})", _boundingBox.x, _boundingBox.y, _boundingBox.width, _boundingBox.height);
                sb.AppendLine();
                sb.Append("Pose LandmarksScreen: ");
                sb.Append("{");
                for (int i = 0; i < _bodyPose_screen.Length; i++)
                {
                    // ref readonly var p = ref _bodyPose_screen[i];
                    ScreenLandmark p = _bodyPose_screen[i];
                    sb.AppendFormat("(<b>{0:F3}, {1:F3}</b>, {2:F3}, {3:F3}, {4:F3})", p.X, p.Y, p.Z, p.Visibility, p.Presence);
                    if (i < _bodyPose_screen.Length - 1)
                        sb.Append(", ");
                }
                sb.Append("}");
                sb.AppendLine();
                Debug.Log(sb.ToString());
                return sb.ToString();
            }
        }


        public static double getLeftAngle
        {
            get
            {
                if ((_pose_counter == 1) && (_bodyPose_screen != null))
                {
                    if (_bodyPose_screen.Length > 19)
                    {
                        Point centerPoint = new Point(_bodyPose_screen[11].X, _bodyPose_screen[11].Y);
                        Point leftPoint = new Point(_bodyPose_screen[19].X, _bodyPose_screen[19].Y);
                        _leftAngle = Mathf.Atan2((float)leftPoint.y - (float)centerPoint.y,
                            (float)leftPoint.x - (float)centerPoint.x) * 180.0 / Mathf.PI;
                    }
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
                if ((_pose_counter == 1) && (_bodyPose_screen != null))
                {
                    if (_bodyPose_screen.Length > 20)
                    {
                        Point centerPoint = new Point(_bodyPose_screen[12].X, _bodyPose_screen[12].Y);
                        Point leftPoint = new Point(_bodyPose_screen[20].X, _bodyPose_screen[20].Y);
                        _rightAngle = Mathf.Atan2((float)leftPoint.y - (float)centerPoint.y,
                            (float)leftPoint.x - (float)centerPoint.x) * 180.0 / Mathf.PI; //  + 90.0f;
                    }
                }
                else
                {
                    _rightAngle = 0.0f;
                }
                return _rightAngle;
            }
        }

        // =====================================================================
        // .....................................................................

    }

}






