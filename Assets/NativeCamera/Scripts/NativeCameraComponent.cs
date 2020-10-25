using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System;
using UnityEngine;
using UnityEngine.Android;
using Rect = OpenCVForUnity.CoreModule.Rect;
namespace NativeCamera
{
    public class NativeCameraComponent
    {
        #region Fields.
        private readonly AndroidJavaObject plugin;
        private readonly int width;
        private readonly int height;
        private readonly int id;
        private readonly int fps;
        private bool playing = false;
        #endregion
        #region Properties.
        public Texture2D PreviewTexture { get; private set; }
        #endregion
        #region Methods.
        /// <summary>
        /// Standard constructor: ID->0, RES->640x480, FPS->120
        /// </summary>
        public NativeCameraComponent()
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
            plugin = new AndroidJavaObject("com.example.monocamlib.MONOCAMClass");
            plugin.Call("setContext", context);
            //setup class fields
            this.id = 0;
            this.width = 640;
            this.height = 480;
            this.fps = 120;
            this.PreviewTexture = new Texture2D(this.width, this.height, TextureFormat.RGB24, false);
        }
        /// <summary>
        /// Overload constructor: specify camera parameters
        /// </summary>
        /// <param name="width">Image Width</param>
        /// <param name="height">Image Height</param>
        /// <param name="id">Id of the camera</param>
        /// <param name="fps">Desired fps</param>
        public NativeCameraComponent(int id, int width, int height, int fps)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
            plugin = new AndroidJavaObject("com.example.monocamlib.MONOCAMClass");
            plugin.Call("setContext", context);
            //Setup class fields
            this.id = id;
            this.width = width;
            this.height = height;
            this.fps = fps;
            this.PreviewTexture = new Texture2D(this.width, this.height, TextureFormat.RGB24, false);
        }
        /// <summary>
        /// Start camera preview.
        /// </summary>
        public void StartPreview()
        {
            //TO DO: Add popups requesting permissions.
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Debug.Log("Missing Camera Permissions!");
                return;
            }
            if(this.id >= this.AvailableCams().Length)
            {
                Debug.Log("The required Camera does not exist or is not accessible. Change Camera ID");
                return;
            }
            plugin.Call("initCamera", this.id, this.width, this.height, this.fps);
            this.playing = true;
            Debug.Log("Camera " + this.id + " is playing!");
        }
        /// <summary>
        /// Acquire latest frame from frame buffer.
        /// </summary>
        /// <returns>Rgb Mat frame</returns>
        public Mat AcquireLatestFrame()
        {
            if (!playing)
            {
                Debug.Log("Camera " + this.id + " is not playing!");
                return null;
            }

            sbyte[] srgbImage = plugin.Call<sbyte[]>("getLatestImage");
            byte[] rgbImage = (byte[])(Array)srgbImage;

            if (rgbImage == null)
            {
                Debug.Log("Frame is null");
                return null;
            }
            //Convert from YUV to RGB format
            Mat rgbFrame = GetRGBfromYUVbytes(rgbImage, this.height, this.width);
            return rgbFrame;
        }
        /// <summary>
        /// Acquire a portion of the latest frame in the frame buffer.
        /// </summary>
        /// <param name="roi">Portion of the image you want to acquire.</param>
        /// <returns>Cropped Mat frame.</returns>
        public Mat AcquireLatestFrame(Rect roi)
        {
            if (!playing)
            {
                Debug.Log("Camera " + this.id + " is not playing!");
                return null;
            }
            if (!CheckROI(roi, this.width, this.height))
            {
                Debug.Log("Roi exceeded image boundaries.");
                return null;
            }
            sbyte[] srgbImage = plugin.Call<sbyte[]>("getLatestImage");
            byte[] rgbImage = (byte[])(Array)srgbImage;

            if (rgbImage == null)
            {
                Debug.Log("Frame is null");
                return null;
            }
            //Convert from YUV to RGB format
            Mat rgbFrame = GetRGBfromYUVbytes(rgbImage, this.height, this.width);
            return new Mat(rgbFrame, roi);
        }
        /// <summary>
        /// Acquire latest frame as a Texture2D.
        /// </summary>
        /// <returns>Rgb texture2D frame.</returns>
        public void UpdateTexture()
        {
            if (!playing)
            {
                Debug.Log("Camera " + this.id + " is not playing!");
                return;
            }

            sbyte[] srgbImage = plugin.Call<sbyte[]>("getLatestImage");
            byte[] rgbImage = (byte[])(Array)srgbImage;

            if (rgbImage == null)
            {
                Debug.Log("Frame is null");
                return;
            }
            Mat rgbFrame = GetRGBfromYUVbytes(rgbImage, this.height, this.width);
            Utils.fastMatToTexture2D(rgbFrame, this.PreviewTexture);
        }
        /// <summary>
        /// Get a list of available cameras on the device.
        /// </summary>
        /// <returns>List of available cameras.</returns>
        public string[] AvailableCams()
        {
            string[] acams = plugin.Call<string[]>("getAvailableCams");
            return acams;
        }
        /// <summary>
        /// Close camera session.
        /// </summary>
        public void StopPreview()
        {
            plugin.Call("closeSession");
        }
        public override string ToString()
        {
            return "Camera " + this.id + " opened with properties" + " Resolution: " + this.width + "x" + this.height + " Frame Rate: " + this.fps;
        }
        #endregion

        #region Getters.
        public int GetWidth() => this.width;
        public int GetHeight() => this.height;
        public int GetCameraId() => this.id;
        public int GetCameraFps() => this.fps;
        public bool IsPlaying() => this.playing;
        #endregion

        #region Helper Methods.
        private static bool CheckROI(Rect roi, int imgWidth, int imgHeight)
        {
            //Check boundaries
            if (roi.x < 0 || roi.x > imgWidth && roi.y < 0 && roi.y > imgHeight && roi.x + roi.width > imgWidth && roi.y + roi.height > imgHeight)
            {
                return false;
            }
            return true;
        }
        private static Mat GetRGBfromYUVbytes(byte[] data, int height, int width)
        {
            if (data == null)
            {
                return new Mat();
            }

            Mat Yuv = new Mat(height + height / 2, width, CvType.CV_8UC1);
            Yuv.put(0, 0, data);
            Mat RGB = new Mat();
            Imgproc.cvtColor(Yuv, RGB, Imgproc.COLOR_YUV2RGB_NV21, 3);
            return RGB;
        }
        #endregion
    }

}