#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
// using TreeEditor;
using UnityEngine;
using OpenCVRange = OpenCVForUnity.CoreModule.Range;
using OpenCVRect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample.DnnModel
{
    /// <summary>
    /// Referring to https://github.com/opencv/opencv_zoo/tree/main/models/pose_estimation_mediapipe
    /// https://developers.google.com/mediapipe/solutions/vision/pose_landmarker
    /// </summary>
    public class MediaPipePoseEstimatorDMT
    {

        // public List<SkeletonBone> skeletonBoneList;
        // ScreenLandmark[] landmarks_screen = data.landmarks_screen;

        float conf_threshold;
        int backend;
        int target;

        Net pose_estimation_net;

        Size input_size = new Size(256, 256);

        // # RoI will be larger so the performance will be better, but preprocess will be slower. Default to 1.
        double PERSON_BOX_PRE_ENLARGE_FACTOR = 1.0;
        double PERSON_BOX_ENLARGE_FACTOR = 1.25;

        public Mat tmpImage;
        Mat tmpRotatedImage;

        Mat mask_warp;
        Mat invert_rotation_mask_32F;

        Mat colorMat;

        public MediaPipePoseEstimatorDMT(string modelFilepath, float confThreshold = 0.5f, int backend = Dnn.DNN_BACKEND_OPENCV, int target = Dnn.DNN_TARGET_CPU)
        {
            // initialize
            if (!string.IsNullOrEmpty(modelFilepath))
            {
                pose_estimation_net = Dnn.readNet(modelFilepath);
            }

            conf_threshold = Mathf.Clamp01(confThreshold);
            this.backend = backend;
            this.target = target;

            pose_estimation_net.setPreferableBackend(this.backend);
            pose_estimation_net.setPreferableTarget(this.target);
        }

        protected virtual Mat preprocess(Mat image, Mat person, out Mat rotated_person_bbox, out double angle, out Mat rotation_matrix, out Mat pad_bias)
        {
            // '''
            // Rotate input for inference.
            // Parameters:
            //   image - input image of BGR channel order
            //   face_bbox - human face bounding box found in image of format[[x1, y1], [x2, y2]] (top - left and bottom - right points)
            //   person_landmarks - 4 landmarks(2 full body points, 2 upper body points) of shape[4, 2]
            // Returns:
            //   rotated_person - rotated person image for inference
            //   rotate_person_bbox - person box of interest range
            //   angle - rotate angle for person
            //   rotation_matrix - matrix for rotation and de - rotation
            //   pad_bias - pad pixels of interest range
            // '''

            // Generate an image with padding added after the squarify process.
            int maxSize = Math.Max(image.width(), image.height());
            int tmpImageSize = (int)(maxSize * 1.5);
            if (tmpImage != null && (tmpImage.width() != tmpImageSize || tmpImage.height() != tmpImageSize))
            {
                tmpImage.Dispose();
                tmpImage = null;
                tmpRotatedImage.Dispose();
                tmpRotatedImage = null;
            }
            if (tmpImage == null)
            {
                tmpImage = new Mat(tmpImageSize, tmpImageSize, image.type(), Scalar.all(0));
                tmpRotatedImage = tmpImage.clone();
            }

            int pad = (tmpImageSize - maxSize) / 2;
            pad_bias = new Mat(2, 1, CvType.CV_32FC1);
            pad_bias.put(0, 0, new float[] { -pad, -pad });

            Mat _tmpImage_roi = new Mat(tmpImage, new OpenCVRect(pad, pad, image.width(), image.height()));
            image.copyTo(_tmpImage_roi);

            // Apply the pad_bias to person_bbox and person_landmarks.
            Mat new_person = person.clone();
            Mat person_bbox_and_landmark = new_person.colRange(new OpenCVRange(0, 12)).reshape(2, 6);
            Core.add(person_bbox_and_landmark, new Scalar(pad, pad), person_bbox_and_landmark);

            // # crop and pad image to interest range
            float[] person_keypoints = new float[8];
            person_bbox_and_landmark.get(2, 0, person_keypoints);
            Point mid_hip_point = new Point(person_keypoints[0], person_keypoints[1]);
            Point full_body_point = new Point(person_keypoints[2], person_keypoints[3]);

            // # get RoI
            Mat full_body_vector = new Mat(1, 1, CvType.CV_32FC2, new Scalar(mid_hip_point.x - full_body_point.x, mid_hip_point.y - full_body_point.y));
            double full_dist = Core.norm(full_body_vector);
            OpenCVRect full_bbox_rect = new OpenCVRect(
                new Point((float)(mid_hip_point.x - full_dist), (float)(mid_hip_point.y - full_dist)),
                new Point((float)(mid_hip_point.x + full_dist), (float)(mid_hip_point.y + full_dist)));

            // # enlarge to make sure full body can be cover
            Point center_bbox = mid_hip_point;
            Point wh_bbox = full_bbox_rect.br() - full_bbox_rect.tl();
            Point new_half_size = wh_bbox * PERSON_BOX_PRE_ENLARGE_FACTOR / 2;
            full_bbox_rect = new OpenCVRect(
                center_bbox - new_half_size,
                center_bbox + new_half_size);

            // Rotate input to have vertically oriented person image
            // compute rotation
            Mat p1 = person_bbox_and_landmark.row(2); // mid_hip_point
            Mat p2 = person_bbox_and_landmark.row(3); // full_body_point
            float[] p1_arr = new float[2];
            p1.get(0, 0, p1_arr);
            float[] p2_arr = new float[2];
            p2.get(0, 0, p2_arr);
            double radians = Math.PI / 2 - Math.Atan2(-(p2_arr[1] - p1_arr[1]), p2_arr[0] - p1_arr[0]);
            radians = radians - 2 * Math.PI * Math.Floor((radians + Math.PI) / (2 * Math.PI));
            angle = Mathf.Rad2Deg * radians;

            // get rotation matrix
            rotation_matrix = Imgproc.getRotationMatrix2D(center_bbox, angle, 1.0);

            // # get landmark bounding box
            Point _rotated_person_bbox_tl = full_bbox_rect.tl();
            Point _rotated_person_bbox_br = full_bbox_rect.br();
            rotated_person_bbox = new Mat(2, 2, CvType.CV_64FC1);
            rotated_person_bbox.put(0, 0, new double[] { _rotated_person_bbox_tl.x, _rotated_person_bbox_tl.y, _rotated_person_bbox_br.x, _rotated_person_bbox_br.y });

            // crop bounding box
            int[] diff = new int[] {
                    Math.Max((int)-_rotated_person_bbox_tl.x, 0),
                    Math.Max((int)-_rotated_person_bbox_tl.y, 0),
                    Math.Max((int)_rotated_person_bbox_br.x - tmpRotatedImage.width(), 0),
                    Math.Max((int)_rotated_person_bbox_br.y - tmpRotatedImage.height(), 0)
                };
            Point tl = new Point(_rotated_person_bbox_tl.x + diff[0], _rotated_person_bbox_tl.y + diff[1]);
            Point br = new Point(_rotated_person_bbox_br.x + diff[2], _rotated_person_bbox_br.y + diff[3]);
            OpenCVRect rotated_person_bbox_rect = new OpenCVRect(tl, br);
            OpenCVRect rotated_image_rect = new OpenCVRect(0, 0, tmpRotatedImage.width(), tmpRotatedImage.height());

            // get rotated image
            OpenCVRect warp_roi_rect = rotated_image_rect.intersect(rotated_person_bbox_rect);
            Mat _tmpImage_warp_roi = new Mat(tmpImage, warp_roi_rect);
            Mat _tmpRotatedImage_warp_roi = new Mat(tmpRotatedImage, warp_roi_rect);
            Point warp_roi_center_palm_bbox = center_bbox - warp_roi_rect.tl();
            Mat warp_roi_rotation_matrix = Imgproc.getRotationMatrix2D(warp_roi_center_palm_bbox, angle, 1.0);
            Imgproc.warpAffine(_tmpImage_warp_roi, _tmpRotatedImage_warp_roi, warp_roi_rotation_matrix, _tmpImage_warp_roi.size());

            // get rotated_person_bbox-size rotated image
            OpenCVRect crop_rect = rotated_image_rect.intersect(
                new OpenCVRect(0, 0, (int)_rotated_person_bbox_br.x - (int)_rotated_person_bbox_tl.x, (int)_rotated_person_bbox_br.y - (int)_rotated_person_bbox_tl.y));
            Mat _tmpImage_crop_roi = new Mat(tmpImage, crop_rect);
            Imgproc.rectangle(_tmpImage_crop_roi, new OpenCVRect(0, 0, _tmpImage_crop_roi.width(), _tmpImage_crop_roi.height()), Scalar.all(0), -1);
            OpenCVRect crop2_rect = rotated_image_rect.intersect(new OpenCVRect(diff[0], diff[1], _tmpRotatedImage_warp_roi.width(), _tmpRotatedImage_warp_roi.height()));
            Mat _tmpImage_crop2_roi = new Mat(tmpImage, crop2_rect);
            if (_tmpRotatedImage_warp_roi.size() == _tmpImage_crop2_roi.size())
                _tmpRotatedImage_warp_roi.copyTo(_tmpImage_crop2_roi);


            Mat blob = Dnn.blobFromImage(_tmpImage_crop_roi, 1.0 / 255.0, input_size, new Scalar(0, 0, 0), true, false, CvType.CV_32F);

            // NCHW => NHWC
            Core.transposeND(blob, new MatOfInt(0, 2, 3, 1), blob);

            new_person.Dispose();

            return blob;
        }

        public virtual List<Mat> infer(Mat image, Mat person, bool mask = false, bool heatmap = false)
        {
            // Preprocess
            Mat rotated_person_bbox;
            double angle;
            Mat rotation_matrix;
            Mat pad_bias;
            Mat input_blob = preprocess(image, person, out rotated_person_bbox, out angle, out rotation_matrix, out pad_bias);

            // Forward
            pose_estimation_net.setInput(input_blob);
            List<Mat> output_blob = new List<Mat>();
            pose_estimation_net.forward(output_blob, pose_estimation_net.getUnconnectedOutLayersNames());

            // Postprocess
            List<Mat> results = new List<Mat>();
            Mat box_landmark_conf = postprocess(output_blob, rotated_person_bbox, angle, rotation_matrix, pad_bias, image.size());
            results.Add(box_landmark_conf);

            if (mask)
            {
                Mat invert_rotation_mask = postprocess_mask(output_blob, rotated_person_bbox, angle, rotation_matrix, pad_bias, image.size());
                results.Add(invert_rotation_mask);
            }
            else
            {
                results.Add(new Mat());
            }

            if (heatmap)
            {
                // # 64*64*39 heatmap: currently only used for refining landmarks, requires sigmod processing before use
                // # TODO: refine landmarks with heatmap. reference: https://github.com/tensorflow/tfjs-models/blob/master/pose-detection/src/blazepose_tfjs/detector.ts#L577-L582
                results.Add(output_blob[3].reshape(1, new int[] { 64, 64, 39 }).clone()); // shape: (1, 64, 64, 39) -> (64, 64, 39)
            }
            else
            {
                results.Add(new Mat());
            }


            input_blob.Dispose();
            for (int i = 0; i < output_blob.Count; i++)
            {
                output_blob[i].Dispose();
            }

            // results[0] = [bbox_coords, landmarks_coords, landmarks_coords_world, conf]
            // results[1] = (optional) [invert_rotation_mask]
            // results[2] = (optional) [heatmap]
            return results;
        }

        protected virtual Mat postprocess(List<Mat> output_blob, Mat rotated_person_bbox, double angle, Mat rotation_matrix, Mat pad_bias, Size img_size)
        {
            Mat landmarks = output_blob[0];
            float conf = (float)output_blob[1].get(0, 0)[0];
            Mat landmarks_world = output_blob[4];

            if (conf < conf_threshold)
                return new Mat();

            landmarks = landmarks.reshape(1, 39); // shape: (1, 195) -> (39, 5)
            landmarks_world = landmarks_world.reshape(1, 39); // shape: (1, 117) -> (39, 3)

            // # recover sigmoid score
            Mat _ladmarls_col3_5 = landmarks.colRange(new OpenCVRange(3, 5));
            sigmoid(_ladmarls_col3_5);

            Mat _ladmarks_col0_3 = landmarks.colRange(new OpenCVRange(0, 3)).clone();

            // transform coords back to the input coords
            double[] rotated_person_bbox_arr = new double[4];
            rotated_person_bbox.get(0, 0, rotated_person_bbox_arr);
            Point _rotated_palm_bbox_tl = new Point(rotated_person_bbox_arr[0], rotated_person_bbox_arr[1]);
            Point _rotated_palm_bbox_br = new Point(rotated_person_bbox_arr[2], rotated_person_bbox_arr[3]);
            Point wh_rotated_person_bbox = _rotated_palm_bbox_br - _rotated_palm_bbox_tl;
            Point scale_factor = new Point(wh_rotated_person_bbox.x / input_size.width, wh_rotated_person_bbox.y / input_size.height);

            Mat _landmarks_39x1_c3 = _ladmarks_col0_3.reshape(3, 39);
            Core.subtract(_landmarks_39x1_c3, new Scalar(input_size.width / 2.0, input_size.height / 2.0, 0.0), _landmarks_39x1_c3);
            double max_scale_factor = Math.Max(scale_factor.x, scale_factor.y);
            Core.multiply(_landmarks_39x1_c3, new Scalar(scale_factor.x, scale_factor.y, max_scale_factor), _landmarks_39x1_c3); //  # depth scaling

            _ladmarks_col0_3.copyTo(landmarks.colRange(new OpenCVRange(0, 3)));

            Mat coords_rotation_matrix = Imgproc.getRotationMatrix2D(new Point(0, 0), angle, 1.0);

            Mat rotated_landmarks = landmarks.clone();
            Mat _a = new Mat(1, 2, CvType.CV_64FC1);
            Mat _b = new Mat(1, 2, CvType.CV_64FC1);
            float[] _a_arr = new float[2];
            double[] _b_arr = new double[6];
            coords_rotation_matrix.get(0, 0, _b_arr);

            for (int i = 0; i < 39; ++i)
            {
                landmarks.get(i, 0, _a_arr);
                _a.put(0, 0, new double[] { _a_arr[0], _a_arr[1] });

                _b.put(0, 0, new double[] { _b_arr[0], _b_arr[3] });
                rotated_landmarks.put(i, 0, new float[] { (float)_a.dot(_b) });
                _b.put(0, 0, new double[] { _b_arr[1], _b_arr[4] });
                rotated_landmarks.put(i, 1, new float[] { (float)_a.dot(_b) });
            }

            Mat rotated_landmarks_world = landmarks_world.clone();
            for (int i = 0; i < 39; ++i)
            {
                landmarks_world.get(i, 0, _a_arr);
                _a.put(0, 0, new double[] { _a_arr[0], _a_arr[1] });

                _b.put(0, 0, new double[] { _b_arr[0], _b_arr[3] });
                rotated_landmarks_world.put(i, 0, new float[] { (float)_a.dot(_b) });
                _b.put(0, 0, new double[] { _b_arr[1], _b_arr[4] });
                rotated_landmarks_world.put(i, 1, new float[] { (float)_a.dot(_b) });
            }

            // invert rotation
            double[] rotation_matrix_arr = new double[6];
            rotation_matrix.get(0, 0, rotation_matrix_arr);
            Mat rotation_component = new Mat(2, 2, CvType.CV_64FC1);
            rotation_component.put(0, 0, new double[] { rotation_matrix_arr[0], rotation_matrix_arr[3], rotation_matrix_arr[1], rotation_matrix_arr[4] });
            Mat translation_component = new Mat(2, 1, CvType.CV_64FC1);
            translation_component.put(0, 0, new double[] { rotation_matrix_arr[2], rotation_matrix_arr[5] });
            Mat inverted_translation = new Mat(2, 1, CvType.CV_64FC1);
            inverted_translation.put(0, 0, new double[] { -rotation_component.row(0).dot(translation_component.reshape(1, 1)), -rotation_component.row(1).dot(translation_component.reshape(1, 1)) });

            Mat inverse_rotation_matrix = new Mat(2, 3, CvType.CV_64FC1);
            rotation_component.copyTo(inverse_rotation_matrix.colRange(new OpenCVRange(0, 2)));
            inverted_translation.copyTo(inverse_rotation_matrix.colRange(new OpenCVRange(2, 3)));

            // get box center
            Mat center = new Mat(3, 1, CvType.CV_64FC1);
            center.put(0, 0, new double[] { (rotated_person_bbox_arr[0] + rotated_person_bbox_arr[2]) / 2.0, (rotated_person_bbox_arr[1] + rotated_person_bbox_arr[3]) / 2.0, 1.0 });
            Mat original_center = new Mat(2, 1, CvType.CV_64FC1);
            original_center.put(0, 0, new double[] { inverse_rotation_matrix.row(0).dot(center.reshape(1, 1)), inverse_rotation_matrix.row(1).dot(center.reshape(1, 1)) });

            Mat _rotated_landmarks_col0_3 = rotated_landmarks.colRange(new OpenCVRange(0, 3)).clone();

            Core.add(_rotated_landmarks_col0_3.reshape(3, 39)
                , new Scalar(original_center.get(0, 0)[0] + pad_bias.get(0, 0)[0], original_center.get(1, 0)[0] + pad_bias.get(1, 0)[0], 0.0)
                , _ladmarks_col0_3.reshape(3, 39));

            _rotated_landmarks_col0_3.copyTo(rotated_landmarks.colRange(new OpenCVRange(0, 3)));
            _rotated_landmarks_col0_3.Dispose();
            _ladmarks_col0_3.copyTo(landmarks.colRange(new OpenCVRange(0, 3)));
            _ladmarks_col0_3.Dispose();

            // get bounding box from rotated_landmarks
            Point[] landmarks_points = new Point[39];
            for (int i = 0; i < 39; ++i)
            {
                landmarks.get(i, 0, _a_arr);
                landmarks_points[i] = new Point(_a_arr[0], _a_arr[1]);
            }
            MatOfPoint points = new MatOfPoint(landmarks_points);
            OpenCVRect bbox = Imgproc.boundingRect(points);
            Point center_bbox = (bbox.tl() + bbox.br()) / 2;
            Point wh_bbox = bbox.br() - bbox.tl();
            Point new_half_size = wh_bbox * PERSON_BOX_ENLARGE_FACTOR / 2;
            bbox = new OpenCVRect(
                center_bbox - new_half_size,
                center_bbox + new_half_size);


            // # [0: 4]: person bounding box found in image of format [x1, y1, x2, y2] (top-left and bottom-right points)
            // # [4: 199]: screen landmarks with format [x1, y1, z1, v1, p1, x2, y2 ... x39, y39, z39, v39, p39], z value is relative to HIP
            // # [199: 316]: world landmarks with format [x1, y1, z1, x2, y2 ... x39, y39, z39], 3D metric x, y, z coordinate
            // # [316]: confidence 
            Mat results = new Mat(317, 1, CvType.CV_32FC1);
            results.put(0, 0, new float[] { (float)bbox.tl().x, (float)bbox.tl().y, (float)bbox.br().x, (float)bbox.br().y });
            Mat results_col4_199_39x5 = results.rowRange(new OpenCVRange(4, 199)).reshape(1, 39);
            landmarks.colRange(new OpenCVRange(0, 5)).copyTo(results_col4_199_39x5);
            Mat results_col199_316_39x3 = results.rowRange(new OpenCVRange(199, 316)).reshape(1, 39);
            rotated_landmarks_world.colRange(new OpenCVRange(0, 3)).copyTo(results_col199_316_39x3);
            results.put(316, 0, new float[] { conf });

            // # 2*2 person bbox: [[x1, y1], [x2, y2]]
            // # 39*5 screen landmarks: 33 keypoints and 6 auxiliary points with [x, y, z, visibility, presence], z value is relative to HIP
            // #    Visibility is probability that a keypoint is located within the frame and not occluded by another bigger body part or another object
            // #    Presence is probability that a keypoint is located within the frame
            // # 39*3 world landmarks: 33 keypoints and 6 auxiliary points with [x, y, z] 3D metric x, y, z coordinate
            // # conf: confidence of prediction
            return results;
        }

        protected virtual Mat postprocess_mask(List<Mat> output_blob, Mat rotated_person_bbox, double angle, Mat rotation_matrix, Mat pad_bias, Size img_size)
        {
            Mat mask = output_blob[2];
            mask = mask.reshape(1, 256); // shape: (1, 256, 256, 1) -> (256, 256)

            if (mask_warp == null)
                mask_warp = new Mat(mask.size(), CvType.CV_32FC1);

            if (invert_rotation_mask_32F == null)
                invert_rotation_mask_32F = new Mat(img_size, CvType.CV_32FC1, new Scalar(0));
            if (invert_rotation_mask_32F.width() != img_size.width || invert_rotation_mask_32F.height() != img_size.height)
            {
                invert_rotation_mask_32F.create(img_size, CvType.CV_32FC1);
                Imgproc.rectangle(invert_rotation_mask_32F, new OpenCVRect(0, 0, invert_rotation_mask_32F.width(), invert_rotation_mask_32F.height()), Scalar.all(0), -1);
            }

            // # invert rotation for mask
            double[] rotated_person_bbox_arr = new double[4];
            rotated_person_bbox.get(0, 0, rotated_person_bbox_arr);
            Point _rotated_person_bbox_tl = new Point(rotated_person_bbox_arr[0] + pad_bias.get(0, 0)[0], rotated_person_bbox_arr[1] + pad_bias.get(1, 0)[0]);
            Point _rotated_person_bbox_br = new Point(rotated_person_bbox_arr[2] + pad_bias.get(0, 0)[0], rotated_person_bbox_arr[3] + pad_bias.get(1, 0)[0]);
            Mat invert_rotation_matrix = Imgproc.getRotationMatrix2D(new Point(mask.width() / 2, mask.height() / 2), -angle, 1.0);
            Imgproc.warpAffine(mask, mask_warp, invert_rotation_matrix, mask.size());

            // create invert_rotation_mask (32F)
            // crop bounding box
            int[] diff = new int[] {
                    Math.Max((int)-_rotated_person_bbox_tl.x, 0),
                    Math.Max((int)-_rotated_person_bbox_tl.y, 0),
                    Math.Max((int)_rotated_person_bbox_br.x - invert_rotation_mask_32F.width(), 0),
                    Math.Max((int)_rotated_person_bbox_br.y - invert_rotation_mask_32F.height(), 0)
                };

            Point wh_rotated_person_bbox = _rotated_person_bbox_br - _rotated_person_bbox_tl;
            Point scale_factor = new Point(wh_rotated_person_bbox.x / input_size.width, wh_rotated_person_bbox.y / input_size.height);
            int x = (int)Math.Round(diff[0] / scale_factor.x);
            int y = (int)Math.Round(diff[1] / scale_factor.y);
            int w = Math.Min((int)Math.Round((wh_rotated_person_bbox.x - diff[0] - diff[2]) / scale_factor.x), mask_warp.width());
            int h = Math.Min((int)Math.Round((wh_rotated_person_bbox.y - diff[1] - diff[3]) / scale_factor.y), mask_warp.height());

            OpenCVRect mask_warp_crop_rect = new OpenCVRect(x, y, w, h);
            Mat _mask_warp_crop_roi = new Mat(mask_warp, mask_warp_crop_rect);
            OpenCVRect rotated_person_bbox_rect = new OpenCVRect(_rotated_person_bbox_tl, _rotated_person_bbox_br);
            OpenCVRect invert_rotation_mask_32F_rect = new OpenCVRect(0, 0, invert_rotation_mask_32F.width(), invert_rotation_mask_32F.height());
            OpenCVRect invert_rotation_mask_32F_crop_rect = invert_rotation_mask_32F_rect.intersect(rotated_person_bbox_rect);
            Mat _invert_rotation_mask_32F_crop_roi = new Mat(invert_rotation_mask_32F, invert_rotation_mask_32F_crop_rect);
            Imgproc.resize(_mask_warp_crop_roi, _invert_rotation_mask_32F_crop_roi,
                new Size(_invert_rotation_mask_32F_crop_roi.width(), _invert_rotation_mask_32F_crop_roi.height()));

            // # binarize mask
            Imgproc.threshold(_invert_rotation_mask_32F_crop_roi, _invert_rotation_mask_32F_crop_roi, 0, 255, Imgproc.THRESH_BINARY);

            // create invert_rotation_mask (8U)
            Mat invert_rotation_mask = new Mat(img_size, CvType.CV_8UC1, new Scalar(0));
            Mat _invert_rotation_mask_crop_roi = new Mat(invert_rotation_mask, invert_rotation_mask_32F_crop_rect);
            _invert_rotation_mask_32F_crop_roi.convertTo(_invert_rotation_mask_crop_roi, CvType.CV_8U);


            // # img_size.width*img_size.height img_height*img_width mask: gray mask, where 255 indicates the full body of a person and 0 means background
            return invert_rotation_mask;
        }

        public virtual void visualize(Mat image, Mat result, bool print_result = false, bool isRGB = false)
        {
            if (image.IsDisposed)
                return;

            if (result.empty() || result.rows() < 317)
                return;

            StringBuilder sb = null;

            if (print_result)
                sb = new StringBuilder(1536);

            Scalar line_color = new Scalar(255, 255, 255, 255);
            Scalar point_color = (isRGB) ? new Scalar(255, 0, 0, 255) : new Scalar(0, 0, 255, 255);
            Scalar text_color = new Scalar(0, 128, 0, 255);

            const int auxiliary_points_num = 6;
            EstimationData data = getData(result);

            float left = data.x1;
            float top = data.y1;
            float right = data.x2;
            float bottom = data.y2;
            ScreenLandmark[] landmarks_screen = data.landmarks_screen;

            // NIS
            // Transfer Data to StaticStore
            DMT.StaticStore.bodyPose = landmarks_screen;
            Vector3[] landmarks_world = data.landmarks_world;
            DMT.StaticStore.bodyPoseWorld = landmarks_world;

            // NIS
            // Screen Body Pose BoundingBox calculation

            Point poseBBMin = new Point(9999, 9999);
            Point poseBBMax = new Point(-9999, -9999);
            for (int run = 0; run < landmarks_screen.Length; run++) // check all Points
            {
                Vector3 screen = new Vector3(landmarks_screen[run].x, landmarks_screen[run].y, landmarks_screen[run].z);
                Point point = new Point(screen.x, screen.y);
                if (point.x > poseBBMax.x) poseBBMax.x = point.x;
                if (point.y > poseBBMax.y) poseBBMax.y = point.y;
                if (point.x < poseBBMin.x) poseBBMin.x = point.x;
                if (point.y < poseBBMin.y) poseBBMin.y = point.y;
            }

            DMT.StaticStore.myBoundingBox = new UnityEngine.Rect((float)poseBBMin.x, (float)poseBBMin.y, 
                    (float)poseBBMax.x - (float)poseBBMin.x, (float)poseBBMax.y - (float)poseBBMin.y);
            DMT.StaticStore.myBoundingBoxNDC = new UnityEngine.Rect(
              (float)poseBBMin.x / image.width(), (float)poseBBMin.y / image.height(),
              ((float)poseBBMax.x - (float)poseBBMin.x) / image.height(),
              ((float)poseBBMax.y - (float)poseBBMin.y) / image.height());

            // draw yellow bounding box
            if (DMT.StaticStore.ShowBoundingBoxLayer)
            {
                Imgproc.rectangle(image, poseBBMin, poseBBMax, new Scalar(255, 255, 0), 10);
                Imgproc.line(image, new Point(poseBBMin.x, poseBBMin.y), new Point(poseBBMax.x, poseBBMax.y), new Scalar(255, 255, 128), 7);
                Imgproc.line(image, new Point(poseBBMax.x, poseBBMin.y), new Point(poseBBMin.x, poseBBMax.y), new Scalar(255, 255, 128), 7);

                // use data directly vom StaticStore
                Imgproc.circle(image, new Point(DMT.StaticStore.BoundingBoxCenter.x, DMT.StaticStore.BoundingBoxCenter.y), 20, new Scalar(255, 255, 0), 15);

            }

            // calc NDC from screen

            Vector3[] landmarks_NDC = new Vector3[landmarks_screen.Length];
            for (int i = 0; i < landmarks_screen.Length; i++)
            {
                Vector3 screen = new Vector3(landmarks_screen[i].x, landmarks_screen[i].y, landmarks_screen[i].z);
                Vector3 ndc;
                ndc.x = (screen.x / image.width());
                ndc.y = (screen.y / image.height());
                ndc.z = screen.z;
                landmarks_NDC[i] = ndc;
            }
            DMT.StaticStore.bodyPoseNDC = landmarks_NDC;

            float confidence = data.confidence;
            DMT.StaticStore.confidence = data.confidence;

            // # draw box
            // nis: green box to blue box

            // nis: blue person box
            // Imgproc.rectangle(image, new Point(left, top), new Point(right, bottom), new Scalar(0, 0, 255, 255), 2);

            // Imgproc.putText(image, confidence.ToString("F3"), new Point(left, top + 12), Imgproc.FONT_HERSHEY_DUPLEX, 2.5, point_color); // red
            // Imgproc.putText(image, score[0].ToString("F4"), new Point(face_box[0], face_box[1] + 12), Imgproc.FONT_HERSHEY_DUPLEX, 1.1, new Scalar(0, 0, 255, 255));

            // nis: confidence vale
            // Imgproc.putText(image, confidence.ToString("F3"), new Point(left+10, bottom-30), Imgproc.FONT_HERSHEY_DUPLEX, 1.7, new Scalar(0, 0, 255, 255));


            // # Draw line between each key points
            draw_lines(landmarks_screen, true, 4);

            // Print results
            // pose
            // confidence: 0,998
            // person box: 1375 291 1749 2161 
            // pose screen landmarks: 1673 590 - 696 1671 560 - 632 1678 561 - 632 1685 561 - 632 1647 558 - 716 1635 557 - 716 1624 557 - 716 1667 576 - 271 1584 573 - 649 1677 625 - 552 1643 623 - 662 1694 785 158 1475 771 - 691 1689 1017 406 1425 1033 - 711 1702 1207 92 1428 1278 - 826 1711 1274 41 1413 1358 - 945 1704 1268 - 81 1442 1354 - 1001 1691 1244 31 1452 1326 - 844 1656 1230 261 1530 1226 - 260 1672 1546 188 1557 1571 - 274 1602 1843 640 1523 1860 179 1562 1889 664 1497 1900 202 1698 1945 370 1613 1974 - 143
            // pose world landmarks: 0,11048 - 0,64826 - 0,20069 0,09731 - 0,68676 - 0,18675 0,09701 - 0,68673 - 0,18850 0,09758 - 0,68683 - 0,18651 0,07627 - 0,68485 - 0,21486 0,07873 - 0,68555 - 0,21477 0,07797 - 0,68468 - 0,21093 0,05552 - 0,67518 - 0,08973 - 0,03641 - 0,66258 - 0,20134 0,09067 - 0,62024 - 0,16906 0,06512 - 0,61500 - 0,20007 0,09564 - 0,47524 0,02813 - 0,13023 - 0,46537 - 0,21355 0,07232 - 0,23077 0,12586 - 0,17943 - 0,18343 - 0,20494 0,08481 - 0,00741 0,06036 - 0,17621 0,07519 - 0,22573 0,08246 0,07962 0,05030 - 0,18962 0,16554 - 0,26012 0,07571 0,07327 0,0006

            if (print_result)
            {
                sb.Append("-----------pose-----------");
                sb.AppendLine();
                sb.AppendFormat("confidence: {0:F3}", confidence);
                sb.AppendLine();
                sb.AppendFormat("person box: {0:F0} {1:F0} {2:F0} {3:F0}", left, top, right, bottom);
                sb.AppendLine();
                sb.Append("pose screen landmarks: ");
                for (int i = 0; i < landmarks_screen.Length - auxiliary_points_num; ++i)
                {
                    sb.AppendFormat("{0}: {1:F0} {2:F0} {3:F0} || ", i, landmarks_screen[i].x, landmarks_screen[i].y, landmarks_screen[i].z);
                }
                sb.AppendLine();
                sb.Append("pose world landmarks: ");
                for (int i = 0; i < landmarks_world.Length - auxiliary_points_num; ++i)
                {
                    sb.AppendFormat("{0}: {1:F5} {2:F5} {3:F5} || ", i, landmarks_world[i].x, landmarks_world[i].y, landmarks_world[i].z);
                }
            }

            if (print_result)
                Debug.Log(sb.ToString());


            void draw_lines(ScreenLandmark[] landmarks, bool is_draw_point = true, int thickness = 2)
            {
                void _draw_by_presence(int idx1, int idx2)
                {
                    if (DMT.StaticStore.ShowSkeletonLayer)
                        if (landmarks[idx1].presence > 0.8 && landmarks[idx2].presence > 0.8)
                        {
                            Imgproc.line(image, new Point(landmarks[idx1].x, landmarks[idx1].y), new Point(landmarks[idx2].x, landmarks[idx2].y), line_color, thickness * 4);
                        }
                }

                // Draw line between each key points
                _draw_by_presence((int)KeyPoint.Nose, (int)KeyPoint.LeftEyeInner);
                _draw_by_presence((int)KeyPoint.LeftEyeInner, (int)KeyPoint.LeftEye);
                _draw_by_presence((int)KeyPoint.LeftEye, (int)KeyPoint.LeftEyeOuter);
                _draw_by_presence((int)KeyPoint.LeftEyeOuter, (int)KeyPoint.LeftEar);
                _draw_by_presence((int)KeyPoint.Nose, (int)KeyPoint.RightEyeInner);
                _draw_by_presence((int)KeyPoint.RightEyeInner, (int)KeyPoint.RightEye);
                _draw_by_presence((int)KeyPoint.RightEye, (int)KeyPoint.RightEyeOuter);
                _draw_by_presence((int)KeyPoint.RightEyeOuter, (int)KeyPoint.RightEar);

                _draw_by_presence((int)KeyPoint.MouthLeft, (int)KeyPoint.MouthRight);

                _draw_by_presence((int)KeyPoint.RightShoulder, (int)KeyPoint.RightElbow);
                _draw_by_presence((int)KeyPoint.RightElbow, (int)KeyPoint.RightWrist);
                _draw_by_presence((int)KeyPoint.RightWrist, (int)KeyPoint.RightThumb);
                _draw_by_presence((int)KeyPoint.RightWrist, (int)KeyPoint.RightPinky);
                _draw_by_presence((int)KeyPoint.RightWrist, (int)KeyPoint.RightIndex);
                _draw_by_presence((int)KeyPoint.RightPinky, (int)KeyPoint.RightIndex);

                _draw_by_presence((int)KeyPoint.LeftShoulder, (int)KeyPoint.LeftElbow);
                _draw_by_presence((int)KeyPoint.LeftElbow, (int)KeyPoint.LeftWrist);
                _draw_by_presence((int)KeyPoint.LeftWrist, (int)KeyPoint.LeftThumb);
                _draw_by_presence((int)KeyPoint.LeftWrist, (int)KeyPoint.LeftIndex);
                _draw_by_presence((int)KeyPoint.LeftWrist, (int)KeyPoint.LeftPinky);
                _draw_by_presence((int)KeyPoint.LeftPinky, (int)KeyPoint.LeftIndex);

                _draw_by_presence((int)KeyPoint.LeftShoulder, (int)KeyPoint.RightShoulder);
                _draw_by_presence((int)KeyPoint.LeftShoulder, (int)KeyPoint.LeftHip);
                _draw_by_presence((int)KeyPoint.LeftHip, (int)KeyPoint.RightHip);
                _draw_by_presence((int)KeyPoint.RightHip, (int)KeyPoint.RightShoulder);

                _draw_by_presence((int)KeyPoint.RightHip, (int)KeyPoint.RightKnee);
                _draw_by_presence((int)KeyPoint.RightKnee, (int)KeyPoint.RightAnkle);
                _draw_by_presence((int)KeyPoint.RightAnkle, (int)KeyPoint.RightHeel);
                _draw_by_presence((int)KeyPoint.RightAnkle, (int)KeyPoint.RightFootIndex);
                _draw_by_presence((int)KeyPoint.RightHeel, (int)KeyPoint.RightFootIndex);

                _draw_by_presence((int)KeyPoint.LeftHip, (int)KeyPoint.LeftKnee);
                _draw_by_presence((int)KeyPoint.LeftKnee, (int)KeyPoint.LeftAnkle);
                _draw_by_presence((int)KeyPoint.LeftAnkle, (int)KeyPoint.LeftFootIndex);
                _draw_by_presence((int)KeyPoint.LeftAnkle, (int)KeyPoint.LeftHeel);
                _draw_by_presence((int)KeyPoint.LeftHeel, (int)KeyPoint.LeftFootIndex);

                // NIS IMA DMT
                // openCV main body points drawing, all 33 points
                // draw numbers and points
                if (is_draw_point)
                {
                    // # z value is relative to HIP, but we use constant to instead
                    for (int i = 0; i < landmarks.Length - auxiliary_points_num; ++i)
                    {
                        if (landmarks[i].presence > 0.8)
                        {
                            if (DMT.StaticStore.ShowDotsLayer)
                                Imgproc.circle(image, new Point(landmarks[i].x, landmarks[i].y), 6, point_color, 3);

                            if (DMT.StaticStore.ShowNumbersLayer)
                            {
                                Imgproc.putText(image, i.ToString(), new Point(landmarks[i].x + 1, landmarks[i].y + 1), Imgproc.FONT_HERSHEY_DUPLEX, 0.8, new Scalar(255, 255, 0, 255));
                                Imgproc.putText(image, i.ToString(), new Point(landmarks[i].x, landmarks[i].y), Imgproc.FONT_HERSHEY_DUPLEX, 0.8, new Scalar(0, 255, 0, 255));
                            }
                        }
                    }
                }
            }
        }

        public virtual void visualize_mask(Mat image, Mat mask, bool isRGB = false)
        {
            if (image.IsDisposed)
                return;

            if (mask.empty())
                return;

            if (image.size() != mask.size() && mask.type() == CvType.CV_8UC1)
                return;

            // NIS
            Scalar color = new Scalar(255, 0, 0, 255);

            Imgproc.Canny(mask, mask, 100, 200);
            Mat kernel = Mat.ones(8, 8, CvType.CV_8UC1);// # expansion edge to 2 pixels
            Imgproc.dilate(mask, mask, kernel, new Point(), 1);

            if (colorMat == null)
                colorMat = new Mat(image.size(), image.type(), color);
            if (colorMat.width() != image.width() || colorMat.height() != image.height())
            {
                colorMat.create(image.size(), image.type());
                Imgproc.rectangle(colorMat, new OpenCVRect(0, 0, colorMat.width(), colorMat.height()), color, -1);
            }

            colorMat.copyTo(image, mask);
        }

        public virtual void dispose()
        {
            if (pose_estimation_net != null)
                pose_estimation_net.Dispose();

            if (tmpImage != null)
                tmpImage.Dispose();

            if (tmpRotatedImage != null)
                tmpRotatedImage.Dispose();

            if (mask_warp != null)
                mask_warp.Dispose();
            if (invert_rotation_mask_32F != null)
                invert_rotation_mask_32F.Dispose();

            mask_warp = null;
            invert_rotation_mask_32F = null;

            if (colorMat != null)
                colorMat.Dispose();

            colorMat = null;
        }

        protected virtual void sigmoid(Mat mat)
        {
            if (mat == null)
                throw new ArgumentNullException("mat");
            if (mat != null)
                mat.ThrowIfDisposed();

            //python: 1 / (1 + np.exp(-x))

            Core.multiply(mat, Scalar.all(-1), mat);
            Core.exp(mat, mat);
            Core.add(mat, Scalar.all(1f), mat);
            using (Mat _mat = new Mat(mat.size(), mat.type(), Scalar.all(1f)))
            {
                Core.divide(_mat, mat, mat);
            }
        }

        public enum KeyPoint
        {
            Nose, LeftEyeInner, LeftEye, LeftEyeOuter, RightEyeInner, RightEye, RightEyeOuter, LeftEar, RightEar,
            MouthLeft, MouthRight,
            LeftShoulder, RightShoulder, LeftElbow, RightElbow, LeftWrist, RightWrist, LeftPinky, RightPinky, LeftIndex, RightIndex, LeftThumb, RightThumb,
            LeftHip, RightHip, LeftKnee, RightKnee, LeftAnkle, RightAnkle, LeftHeel, RightHeel, LeftFootIndex, RightFootIndex
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ScreenLandmark
        {
            public readonly float x;
            public readonly float y;
            public readonly float z;
            public readonly float visibility;
            public readonly float presence;

            // sizeof(ScreenLandmark)
            public const int Size = 5 * sizeof(float);
            public ScreenLandmark(float x, float y, float z, float visibility, float presence)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.visibility = visibility;
                this.presence = presence;
            }

            public override string ToString()
            {
                return "x:" + x.ToString() + " y:" + y.ToString() + " z:" + z.ToString() + " visibility:" + visibility.ToString() + " presence:" + presence.ToString();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct EstimationData
        {
            public readonly float x1;
            public readonly float y1;
            public readonly float x2;
            public readonly float y2;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 39)]
            public readonly ScreenLandmark[] landmarks_screen;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 39)]
            public readonly Vector3[] landmarks_world;

            public readonly float confidence;

            // sizeof(EstimationData)
            public const int Size = 317 * sizeof(float);

            public EstimationData(int x1, int y1, int x2, int y2, float confidence)
            {
                this.x1 = x1;
                this.y1 = y1;
                this.x2 = x2;
                this.y2 = y2;
                this.landmarks_screen = new ScreenLandmark[39];
                this.landmarks_world = new Vector3[39];
                this.confidence = confidence;
            }

            public override string ToString()
            {
                return "x1:" + x1 + " y1:" + y1 + " x2:" + x2 + " y2:" + y2 + " confidence:" + confidence;
            }
        };

        public virtual EstimationData getData(Mat result)
        {
            if (result.empty())
                return new EstimationData();

            EstimationData dst = Marshal.PtrToStructure<EstimationData>((IntPtr)(result.dataAddr()));

            return dst;
        }

        public virtual ScreenLandmark[] getScreenLandmarks(ScreenLandmark[] landmarks)
        {
            // Remove 6 unneeded auxiliary points at the end of the raw landmark array.
            ScreenLandmark[] newArr = new ScreenLandmark[landmarks.Length - 6];
            Array.Copy(landmarks, 0, newArr, 0, newArr.Length);

            return newArr;
        }

        public virtual Vector3[] getWorldLandmarks(Vector3[] landmarks)
        {
            // Remove 6 unneeded auxiliary points at the end of the raw landmark array.
            Vector3[] newArr = new Vector3[landmarks.Length - 6];
            Array.Copy(landmarks, 0, newArr, 0, newArr.Length);

            return newArr;
        }

        public virtual bool[] getKeepLandmarks(ScreenLandmark[] landmarks, float presence = 0.8f)
        {
            bool[] keep = new bool[landmarks.Length];

            for (int i = 0; i < landmarks.Length; ++i)
            {
                keep[i] = landmarks[i].presence > presence;
            }

            return keep;
        }
    }
}
#endif