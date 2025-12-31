#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule;
using UnityEngine;
using UnityEngine.UI;
using static OpenCVForUnity.UnityIntegration.Worker.DnnModule.MediaPipePoseEstimator;
using static UnityEditor.PlayerSettings.SplashScreen;
// using DMT.Pose;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Pose Estimation MediaPipe Example
    /// An example of using OpenCV dnn module with Human Pose Estimation.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/main/models/pose_estimation_mediapipe
    ///
    /// [Tested Models]
    /// https://github.com/opencv/opencv_zoo/raw/0d619617a8e9a389150d8c76e417451a19468150/models/person_detection_mediapipe/person_detection_mediapipe_2023mar.onnx
    /// https://github.com/opencv/opencv_zoo/raw/0d619617a8e9a389150d8c76e417451a19468150/models/pose_estimation_mediapipe/pose_estimation_mediapipe_2023mar.onnx
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class DMTPoseEstimation : MonoBehaviour
    {
        // Constants
        private readonly byte[] SELECTED_INDICES = {
                        (byte)MediaPipePoseEstimator.KeyPoint.Nose,
                        (byte)MediaPipePoseEstimator.KeyPoint.LeftShoulder,
                        (byte)MediaPipePoseEstimator.KeyPoint.RightShoulder,
                        (byte)MediaPipePoseEstimator.KeyPoint.LeftHip,
                        (byte)MediaPipePoseEstimator.KeyPoint.RightHip,
                        (byte)MediaPipePoseEstimator.KeyPoint.LeftElbow,
                        (byte)MediaPipePoseEstimator.KeyPoint.RightElbow,
                        (byte)MediaPipePoseEstimator.KeyPoint.LeftKnee,
                        (byte)MediaPipePoseEstimator.KeyPoint.RightKnee,
                        (byte)MediaPipePoseEstimator.KeyPoint.LeftWrist,
                        (byte)MediaPipePoseEstimator.KeyPoint.RightWrist,
                        (byte)MediaPipePoseEstimator.KeyPoint.LeftAnkle,
                        (byte)MediaPipePoseEstimator.KeyPoint.RightAnkle,
                    };
        private static readonly string PERSON_DETECTION_MODEL_FILENAME = "OpenCVForUnityExamples/dnn/person_detection_mediapipe_2023mar.onnx";
        private static readonly string POSE_ESTIMATION_MODEL_FILENAME = "OpenCVForUnityExamples/dnn/pose_estimation_mediapipe_2023mar.onnx";

        // Public Fields

        // Private Fields

        private Texture2D _texture;
        private MultiSource2MatHelper _multiSource2MatHelper;
        private Mat _bgrMat;

        private MediaPipePersonDetector _personDetector;
        private MediaPipePoseEstimator _poseEstimator;
        private string _personDetectionModelFilepath;
        private string _poseEstimationModelFilepath;

        private FpsMonitor _fpsMonitor;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private Mat _bgrMatForAsync;
        private Mat _latestDetectedPersons;
        private List<Mat> _latestPoses;
        private List<Mat> _latestMasks;
        private Task _inferenceTask;
        private readonly Queue<Action> _mainThreadQueue = new();
        private readonly object _queueLock = new();

        public GameObject renderImage = null;

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _personDetectionModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(PERSON_DETECTION_MODEL_FILENAME, cancellationToken: _cts.Token);
            _poseEstimationModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(POSE_ESTIMATION_MODEL_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        // ##########################################################################################################
        // Pose Detection and Estimation

        private void Update()
        {
            ProcessMainThreadQueue();

            List<Mat> poses = null;
            Mat pose = null;

            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_personDetector == null || _poseEstimator == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

                    // synchronous execution

                    // Person detector inference
                    using (Mat persons = _personDetector.Detect(_bgrMat))
                    {
                        // tm.stop();
                        // Debug.Log("MediaPipePersonDetector Inference time, ms: " + tm.getTimeMilli());

                        poses = new List<Mat>();

                        // NIS Poses detected 
                        int detectedPoses = persons.rows();

                        DMT.Pose.DMTStaticStoreHumanPose.poseCounter = detectedPoses; // update pose counter

                        // Estimate the pose of each person
                        for (int i = 0; i < persons.rows(); ++i)
                        {

                            // Pose estimator inference
                            using (Mat person = persons.row(i))
                            {
                                pose = _poseEstimator.Estimate(_bgrMat, person, true, useCopyOutput: true); // Estimate pose with mask
                                if (!pose.empty()) poses.Add(pose);
                            }
                        }

                        if (detectedPoses > 0)
                        {
                            PoseEstimationBlazeData data = ToStructuredData(pose);
                            float left = data.X1;
                            float top = data.Y1;
                            float right = data.X2;
                            float bottom = data.Y2;
                            ScreenLandmark[] landmarksScreen = data.GetLandmarksScreenArray();

                            DMT.Pose.DMTStaticStoreHumanPose.bodyPose = landmarksScreen; // update latest pose mat list
                            DMT.Pose.DMTStaticStoreHumanPose.confidence = data.Confidence;

                            // konvertiere Vec3f[] in Vector3[]
                            Vec3f[] srcLandMarks = data.GetLandmarksWorldArray();
                            Vector3[] dstLandMarks = new Vector3[srcLandMarks.Length];
                            for (int i = 0; i < srcLandMarks.Length; i++)
                            {
                                dstLandMarks[i] = new Vector3(srcLandMarks[i].Item1, srcLandMarks[i].Item2, srcLandMarks[i].Item3);
                            }
                            DMT.Pose.DMTStaticStoreHumanPose.bodyPoseWorld = dstLandMarks;

                        }

                        Imgproc.cvtColor(_bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                        //_personDetector.Visualize(rgbaMat, persons, false, true);
                        // foreach (var mask in masks) _poseEstimator.VisualizeMask(rgbaMat, mask, true);
                        
                        // NIS: Bounding box, Skelet and Dots
                        // foreach (Mat myPose in poses) _poseEstimator.Visualize(rgbaMat, myPose, false, true);

                        // if (SkeletonVisualizer != null && SkeletonVisualizer.ShowSkeleton) UpdateSkeleton(poses);

                        persons.Dispose();
                        foreach (Mat myPose in poses) myPose.Dispose();
                    }

                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }

        }

        public virtual PoseEstimationBlazeData ToStructuredData(Mat result)
        {
            if (result != null) result.ThrowIfDisposed();
            if (result.empty())
                return new PoseEstimationBlazeData();
            if (result.rows() < 317)
                throw new ArgumentException("Invalid result matrix. It must have at least 317 rows.");
            if (result.cols() != 1)
                throw new ArgumentException("Invalid result matrix. It must have 1 column.");
            if (!result.isContinuous())
                throw new ArgumentException("result is not continuous.");

            PoseEstimationBlazeData dst = System.Runtime.InteropServices.Marshal.PtrToStructure<PoseEstimationBlazeData>((IntPtr)(result.dataAddr()));

            return dst;
        }

        // ##########################################################################################################

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _personDetector?.Dispose();
            _poseEstimator?.Dispose();

            OpenCVDebug.SetDebugMode(false);

            _cts?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = _multiSource2MatHelper.GetMat();
            Debug.Log("rgbaMat <b><color='yellow'>cols " + rgbaMat.cols() + " rows " + rgbaMat.rows()+"</b></color>");

            // NIS : store image size for DMT
            DMT.Pose.DMTStaticStoreHumanPose.imgWidth = rgbaMat.cols(); 
            DMT.Pose.DMTStaticStoreHumanPose.imgHeight = rgbaMat.rows();

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

            // Set the Texture2D as the main texture of the Renderer component attached to the game object

            // NIS
            // gameObject.GetComponent<Renderer>().material.mainTexture = _texture;
            // renderImage.GetComponent<Renderer>().material.mainTexture = _texture;
            // renderImage.GetComponent<RawImage>().material.mainTexture = _texture;

            RawImage debugRawImage = renderImage.GetComponent<RawImage>();
            debugRawImage.texture = _texture;

            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            // Set the camera's orthographicSize to half of the texture height
            Camera.main.orthographicSize = _texture.height / 2f;

            // Get the camera's aspect ratio
            float cameraAspect = Camera.main.aspect;

            // Get the texture's aspect ratio
            float textureAspect = (float)_texture.width / _texture.height;

            // Calculate imageSizeScale
            float imageSizeScale;
            if (textureAspect > cameraAspect)
            {
                // Calculate the camera width (height is already fixed)
                float cameraWidth = Camera.main.orthographicSize * 2f * cameraAspect;

                // Scale so that the texture width fits within the camera width
                imageSizeScale = cameraWidth / _texture.width;
            }
            else
            {
                // Scale so that the texture height fits within the camera height
                imageSizeScale = 1f; // No scaling needed since height is already fixed
            }
            Debug.Log("imageSizeScale " + imageSizeScale);

            // The calculated imageSizeScale is used to set the scale of the game object on which the texture is displayed.
            transform.localScale = new Vector3(_texture.width * imageSizeScale, _texture.height * imageSizeScale, 1);


            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", rgbaMat.width().ToString());
                _fpsMonitor.Add("height", rgbaMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
            _bgrMatForAsync = new Mat();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (_inferenceTask != null && !_inferenceTask.IsCompleted) _inferenceTask.Wait(500);

            _bgrMat?.Dispose(); _bgrMat = null;

            _bgrMatForAsync?.Dispose(); _bgrMatForAsync = null;
            _latestDetectedPersons?.Dispose(); _latestDetectedPersons = null;
            if (_latestPoses != null)
            {
                foreach (var pose in _latestPoses)
                    pose.Dispose();
                _latestPoses.Clear();
            }
            _latestPoses = null;
            if (_latestMasks != null)
            {
                foreach (var mask in _latestMasks)
                    mask.Dispose();
                _latestMasks.Clear();
            }
            _latestMasks = null;

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            _multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            _multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            _multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            _multiSource2MatHelper.RequestedIsFrontFacing = !_multiSource2MatHelper.RequestedIsFrontFacing;
        }


        // Private Methods
        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            if (string.IsNullOrEmpty(_personDetectionModelFilepath))
            {
                Debug.LogError(PERSON_DETECTION_MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                _personDetector = new MediaPipePersonDetector(_personDetectionModelFilepath, 0.3f, 0.6f, 10); // # usually only one person has good performance
            }

            if (string.IsNullOrEmpty(_poseEstimationModelFilepath))
            {
                Debug.LogError(POSE_ESTIMATION_MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                _poseEstimator = new MediaPipePoseEstimator(_poseEstimationModelFilepath, 0.9f);
            }

            _multiSource2MatHelper.Initialize();
        }

        private void UpdateSkeleton(List<Mat> poses)
        {
            if (poses == null || poses.Count == 0)
                return;

            if (poses.Count > 0 && !poses[0].empty())
            {
                // SkeletonVisualizer.UpdatePose(poses[0]);

                PoseEstimationBlazeData data = _poseEstimator.ToStructuredData(poses[0]);
#if NET_STANDARD_2_1
                ReadOnlySpan<ScreenLandmark> landmarks_screen = data.GetLandmarksScreen();
                ReadOnlySpan<Vec3f> landmarks_world = data.GetLandmarksWorld();
#else
                ScreenLandmark[] landmarks_screen = data.GetLandmarksScreenArray();
                Vec3f[] landmarks_world = data.GetLandmarksWorldArray();
#endif

                Vector2[] imagePoints = new Vector2[SELECTED_INDICES.Length];
                Vector3[] objectPoints = new Vector3[SELECTED_INDICES.Length];

                for (int i = 0; i < SELECTED_INDICES.Length; i++)
                {
                    int index = SELECTED_INDICES[i];
                    ref readonly var landmark_screen = ref landmarks_screen[index];
                    ref readonly var landmark_world = ref landmarks_world[index];
                    imagePoints[i] = new Vector2(landmark_screen.X, landmark_screen.Y);
                    objectPoints[i] = new Vector3(landmark_world.Item1, landmark_world.Item2, landmark_world.Item3);
                }

            }
        }

        private void RunOnMainThread(Action action)
        {
            if (action == null) return;

            lock (_queueLock)
            {
                _mainThreadQueue.Enqueue(action);
            }
        }

        private void ProcessMainThreadQueue()
        {
            while (true)
            {
                Action action = null;
                lock (_queueLock)
                {
                    if (_mainThreadQueue.Count == 0)
                        break;

                    action = _mainThreadQueue.Dequeue();
                }

                try { action?.Invoke(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }
    }
}

#endif
