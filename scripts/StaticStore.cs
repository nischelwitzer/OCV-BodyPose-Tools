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

        private static int _poses = 0; // pose counter

        public static int posesCounter
        {
            get { return _poses; }
            set
            {
                _poses = value;
            }
        }

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
