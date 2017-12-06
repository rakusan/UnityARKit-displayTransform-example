using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.iOS;

public class QRCodeReader : MonoBehaviour {

#if UNITY_IPHONE && !UNITY_EDITOR

	[DllImport ("__Internal")]
	private static extern void ReadQRCode(long mtlTexPtr);

	[DllImport ("__Internal")]
	private static extern void GetQRCodeCorners(out IntPtr cornersPtr);

	private static float[] GetQRCodeCorners() {
		IntPtr cornersPtr;
		GetQRCodeCorners (out cornersPtr);
		float[] corners = new float[8];
		Marshal.Copy (cornersPtr, corners, 0, 8);
		return corners;
	}

#else

	private static void ReadQRCode(long mtlTexPtr) {
	}

	private static float[] GetQRCodeCorners() {
		return new float[8];
	}

#endif

	[SerializeField]
	private Material material;

	private UnityARSessionNativeInterface arSession;
	private Matrix4x4 displayTransformInverse;
	private Vector3[] corners = new Vector3[4];


	void Start () {
		arSession = UnityARSessionNativeInterface.GetARSessionNativeInterface ();
		UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
	}
	
	private void ARFrameUpdated(UnityARCamera camera) {
		Matrix4x4 tmp = new Matrix4x4 (
			camera.displayTransform.column0,
			camera.displayTransform.column1,
			camera.displayTransform.column2,
			camera.displayTransform.column3
		);
		displayTransformInverse = tmp.inverse;
	}

	void Update () {
		ARTextureHandles handles = arSession.GetARVideoTextureHandles ();
		if (handles.textureY != System.IntPtr.Zero) {
			ReadQRCode (handles.textureY.ToInt64 ());
		}
	}

	private Vector3 VideoTextureToViewportPoint(Vector2 videoTexturePoint) {
		Vector4 column0 = displayTransformInverse.GetColumn(0);
		Vector4 column1 = displayTransformInverse.GetColumn(1);
		float x = column0.x * videoTexturePoint.x + column0.y * videoTexturePoint.y + column0.z;
		float y = column1.x * videoTexturePoint.x + column1.y * videoTexturePoint.y + column1.z;
		return new Vector3 (x, y);
	}

	void OnReadQRCode(string arg) {
		float[] videoTexCorners = GetQRCodeCorners ();

		corners[0] = VideoTextureToViewportPoint(new Vector2 (videoTexCorners [0], videoTexCorners [1]));
		corners[1] = VideoTextureToViewportPoint(new Vector2 (videoTexCorners [2], videoTexCorners [3]));
		corners[2] = VideoTextureToViewportPoint(new Vector2 (videoTexCorners [4], videoTexCorners [5]));
		corners[3] = VideoTextureToViewportPoint(new Vector2 (videoTexCorners [6], videoTexCorners [7]));
	}

	void OnRenderObject() {
		material.SetPass (0);

		GL.PushMatrix ();
		GL.LoadOrtho ();

		GL.Begin (GL.QUADS);

		GL.Vertex (corners[0]);
		GL.Vertex (corners[1]);
		GL.Vertex (corners[3]);
		GL.Vertex (corners[2]);

		GL.End ();
		GL.PopMatrix ();
	}
}
