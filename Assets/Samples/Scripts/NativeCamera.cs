using UnityEngine;
using UnityEngine.UI;
namespace NativeCamera {
    public class NativeCamera : MonoBehaviour
    {
        private NativeCameraComponent Camera;
        public RawImage preview;
        public int id = 0;
        public int width = 640;
        public int height = 480;
        public int fps = 120;
        // Start is called before the first frame update
    private void Start()
        {
            Application.targetFrameRate = 300;
            Camera = new NativeCameraComponent(id, width, height, fps);
            Camera.StartPreview();
            Debug.Log(Camera.ToString());
        }

        // Update is called once per frame
        private void Update()
        {
            Debug.Log(1 / Time.deltaTime);
            Camera.UpdateTexture();
            preview.texture = Camera.PreviewTexture;

        }
        private void OnApplicationQuit()
        {
            Camera.StopPreview();
        }
    }
}