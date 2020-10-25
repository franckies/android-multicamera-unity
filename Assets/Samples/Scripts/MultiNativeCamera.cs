using OpenCVForUnity.CoreModule;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace NativeCamera
{
    public class MultiNativeCamera : MonoBehaviour
    {
        private NativeCameraComponent CameraR;
        private NativeCameraComponent CameraL;
        public RawImage previewR;
        public RawImage previewL;
        public int idR = 0;
        public int idL = 1;
        public int width = 640;
        public int height = 480;
        public int fps = 120;
        // Start is called before the first frame update
        void Start()
        {
            Application.targetFrameRate = 300;
            CameraR = new NativeCameraComponent(idR, width, height, fps);
            CameraR.StartPreview();
            CameraL = new NativeCameraComponent(idL, width, height, fps);
            CameraL.StartPreview();
            Debug.Log(CameraR.ToString());
            Debug.Log(CameraL.ToString());
        }

        // Update is called once per frame
        void Update()
        {
            Debug.Log(1 / Time.deltaTime);
            CameraR.UpdateTexture();
            CameraL.UpdateTexture();
            previewR.texture = CameraR.PreviewTexture;
            previewL.texture = CameraL.PreviewTexture;
        }
        private void OnApplicationQuit()
        {
            CameraR.StopPreview();
            CameraL.StopPreview();
        }
    }
}