//Code written by Francesco Semeraro on 02/09/2020
package com.example.monocamlib;

import android.annotation.SuppressLint;
import android.content.Context;
import android.graphics.ImageFormat;
import android.hardware.camera2.CameraAccessException;
import android.hardware.camera2.CameraCaptureSession;
import android.hardware.camera2.CameraDevice;
import android.hardware.camera2.CameraManager;
import android.hardware.camera2.CaptureRequest;
import android.media.Image;
import android.media.ImageReader;
import android.os.SystemClock;
import android.util.Log;
import android.util.Range;
import android.view.Surface;

import java.nio.ByteBuffer;
import java.util.ArrayList;
import java.util.List;

public class MONOCAMClass {
    private String TAG = "MONOCAMPlugin";
    private int  frames      = 0;
    private long initialTime = SystemClock.elapsedRealtimeNanos();
    /*
    Camera2 API variables
     */
    private String cameraId;
    private CameraDevice camera;
    private CameraCaptureSession cameraCaptureSession;
    private int imageWidth = 640;
    private int imageHeight = 480;
    private int frameRate = 120;
    //0 = FRONTCAM, 1 = BACKCAM
    private int chosenCam = 0;

    /*
    Image reader variables
     */
    private ImageReader imageReader;
    private Surface imageReaderSurface;

    /*
    Variables to pass to Unity
     */
    static Context context;
    byte[] bytes;
    List<byte[]> bytesBuffer;
    int bufferSize = 5;
    private long time;

    /*
    Set context from Unity Main Activity
     */
    public void setContext(Context ctx){
        this.context = ctx;
    }

    /*
    Camera callback
     */
    private CameraDevice.StateCallback cameraStateCallback = new CameraDevice.StateCallback() {
        @Override
        public void onOpened(CameraDevice cameraDevice) {
            camera = cameraDevice;
            Log.d(TAG, "Camera Opened!");
            getFrames();
        }

        @Override
        public void onDisconnected(CameraDevice cameraDevice) {
            cameraDevice.close();
            Log.d(TAG, "Camera Closed!");
            camera = null;
        }

        @Override
        public void onError(CameraDevice cameraDevice, int i) {
            cameraDevice.close();
            Log.d(TAG, "Camera Error!");
            camera = null;
        }
    };


    /*
    Cameras capture session callbacks
     */
    private CameraCaptureSession.StateCallback captureSessionStateCallback = new CameraCaptureSession.StateCallback() {
        @Override
        public void onConfigured(CameraCaptureSession session) {
            cameraCaptureSession = session;

            try {
                CaptureRequest.Builder requestBuilder = camera.createCaptureRequest(CameraDevice.TEMPLATE_ZERO_SHUTTER_LAG);
                requestBuilder.addTarget(imageReaderSurface);
                Log.d(TAG, "Repeat Request Set!");
                Range<Integer> fpsRange = Range.create(frameRate , frameRate);
                requestBuilder.set(CaptureRequest.CONTROL_AE_TARGET_FPS_RANGE, fpsRange);
                //requestBuilder.set(CaptureRequest.CONTROL_MODE, CaptureRequest.CONTROL_MODE_OFF);
                //requestBuilder.set(CaptureRequest.CONTROL_VIDEO_STABILIZATION_MODE, CaptureRequest.CONTROL_VIDEO_STABILIZATION_MODE_OFF);
                //requestBuilder.set(CaptureRequest.LENS_OPTICAL_STABILIZATION_MODE, CaptureRequest.LENS_OPTICAL_STABILIZATION_MODE_OFF);
                //requestBuilder.set(CaptureRequest.LENS_FOCUS_DISTANCE, .2f);
                //requestBuilder.set(CaptureRequest.SENSOR_EXPOSURE_TIME, 1000000L*2);
                cameraCaptureSession.setRepeatingRequest(requestBuilder.build(), null, null);

            } catch (CameraAccessException e) {
                e.printStackTrace();
            }
        }

        @Override
        public void onConfigureFailed(CameraCaptureSession cameraCaptureSession) {
            Log.d(TAG, "Configuration Failed!");
        }
    };

    /*
    Image reader callbacks for camera. called every time a new frame is available
     */
    private ImageReader.OnImageAvailableListener imageReaderListener = new ImageReader.OnImageAvailableListener() {
        @SuppressLint("MissingPermission")
        @Override
        public void onImageAvailable(ImageReader imageReader) {
            Image image = imageReader.acquireLatestImage();

            if(image == null){
                return;
            }
            Log.d(TAG, "Image Acquired!");
            time = image.getTimestamp();

            bytes = YUV_420_888toNV21(image);

            bytesBuffer.add(bytes);
            if(bytesBuffer.size() > bufferSize){
                bytesBuffer.remove(0);
            }
            printFPS();
            image.close();

        }
    };

    /*
    Initialize camera manager and open  camera.
     */
    @SuppressLint("MissingPermission")
    public void initCamera() {
        bytesBuffer = new ArrayList<>();
        CameraManager cameraManager = (CameraManager) context.getSystemService(context.CAMERA_SERVICE);
        imageReader = ImageReader.newInstance(imageWidth, imageHeight, ImageFormat.YUV_420_888, 5);
        imageReader.setOnImageAvailableListener(imageReaderListener, null);


        try {
            cameraId = cameraManager.getCameraIdList()[chosenCam];
            cameraManager.openCamera(cameraId, cameraStateCallback, null);
        } catch (CameraAccessException e) {
            e.printStackTrace();
        }
    }

    //Overload for init camera
    @SuppressLint("MissingPermission")
    public void initCamera(int cam, int width, int height, int fps) {
        bytesBuffer = new ArrayList<>();
        CameraManager cameraManager = (CameraManager) context.getSystemService(context.CAMERA_SERVICE);
        imageReader = ImageReader.newInstance(width, height, ImageFormat.YUV_420_888, 5);
        imageReader.setOnImageAvailableListener(imageReaderListener, null);
        frameRate = fps;
        try {
            cameraId = cameraManager.getCameraIdList()[cam];
            cameraManager.openCamera(cameraId, cameraStateCallback, null);
        } catch (CameraAccessException e) {
            e.printStackTrace();
        }
    }

    /**
     Build the captureSessionRequest and start in repeat.
     */
    public void getFrames() {
        imageReaderSurface = imageReader.getSurface();

        List<Surface> surfaceList = new ArrayList<>();
        surfaceList.add(imageReaderSurface);
        try {
            camera.createCaptureSession(surfaceList, captureSessionStateCallback, null);
        } catch (CameraAccessException e) {
            e.printStackTrace();
        }
    }

    /*
    Function needed to pass bytes and timestamps to Unity.
     */
    public byte[] getLatestImage() {
        return bytesBuffer.size()>0 ? bytesBuffer.get(bytesBuffer.size()-1) : null;
        }

    public void closeSession(){
        camera.close();
        camera = null;
        bytes = null;
        bytesBuffer.clear();
    }

    public String[] getAvailableCams() throws CameraAccessException {
        CameraManager cameraManager = (CameraManager) context.getSystemService(context.CAMERA_SERVICE);
        return cameraManager.getCameraIdList();
    }

    public long getTimestamp(){
        return time;
    }
    /*
    Utils function to convert YUV image to NV21 preformatted byte array
     */
    private static byte[] YUV_420_888toNV21(Image image) {
        byte[] nv21;
        ByteBuffer yBuffer = image.getPlanes()[0].getBuffer();
        ByteBuffer uBuffer = image.getPlanes()[1].getBuffer();
        ByteBuffer vBuffer = image.getPlanes()[2].getBuffer();

        int ySize = yBuffer.remaining();
        int uSize = uBuffer.remaining();
        int vSize = vBuffer.remaining();

        nv21 = new byte[ySize + uSize + vSize];

        //U and V are swapped
        yBuffer.get(nv21, 0, ySize);
        vBuffer.get(nv21, ySize, vSize);
        uBuffer.get(nv21, ySize + vSize, uSize);

        return nv21;
    }
    private void printFPS() {
        frames++;
        if (frames % 1 == 0) {
            long currentTime = SystemClock.elapsedRealtimeNanos();
            long fps = Math.round(frames * 1e9 / (currentTime - initialTime));
            Log.d("Image", "approximately " + fps + " fps");
            frames = 0;
            initialTime = SystemClock.elapsedRealtimeNanos();
        }
    }


}
