using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace fzmnm
{
    [RequireComponent(typeof(Camera))]
    public class Mirror : MonoBehaviour
    {
        Camera mirrorCamera,mainCamera;
        RenderTexture renderTexture;
        Material mirrorMaterial;
        public Vector2 localSize = new Vector2(1,1);//Quad
        public Vector2Int resolution = new Vector2Int(512,512);
        public float farPlane = 10000;
        public float maxUpdateDistance = 100f;
        public RenderTextureFormat renderTextureFormat=RenderTextureFormat.ARGB32;
        public int renderTextureDepth = 16;
        private void Start()
        {
            mainCamera = Camera.main;

            mirrorCamera = GetComponent<Camera>();

            renderTexture = new RenderTexture(resolution.x, resolution.y, renderTextureDepth, renderTextureFormat);
            renderTexture.Create();
            mirrorCamera.targetTexture = renderTexture;

            mirrorMaterial = GetComponent<MeshRenderer>().material;
            mirrorMaterial.mainTexture = renderTexture;
        }
        private void LateUpdate()
        {
            if (resolution.x != renderTexture.width || resolution.y!=renderTexture.height)
            {
                renderTexture = new RenderTexture(resolution.x, resolution.y, renderTextureDepth, renderTextureFormat);
                renderTexture.Create();
                mirrorCamera.targetTexture = renderTexture;
                mirrorMaterial.mainTexture = renderTexture;
            }

            Vector3 mirrorCameraPosition = transform.position + Vector3.Reflect(mainCamera.transform.position - transform.position, transform.forward);
            Quaternion mirrorCameraRotation = Quaternion.LookRotation(-transform.forward, transform.up);//forward is into the mirror
            Quaternion inverseRotation = Quaternion.Inverse(mirrorCameraRotation);

            mirrorCamera.worldToCameraMatrix = Matrix4x4.TRS(mirrorCameraPosition, mirrorCameraRotation, new Vector3(1, 1, -1)).inverse;

            float u = (inverseRotation * (transform.TransformPoint(new Vector3(0, localSize.y / 2, 0)) - mirrorCameraPosition)).y;
            float d = (inverseRotation * (transform.TransformPoint(new Vector3(0, -localSize.y / 2, 0)) - mirrorCameraPosition)).y;
            float l = (inverseRotation * (transform.TransformPoint(new Vector3(localSize.x / 2, 0, 0)) - mirrorCameraPosition)).x;//left in the way of camera view
            float r = (inverseRotation * (transform.TransformPoint(new Vector3(-localSize.x / 2, 0, 0)) - mirrorCameraPosition)).x;
            float n = Vector3.Dot(transform.forward, mirrorCameraPosition - transform.position);
            float f = farPlane;
            if(f>n && n > 0)
            {
                mirrorCamera.nearClipPlane = n;
                mirrorCamera.farClipPlane = f;
                mirrorCamera.projectionMatrix = Matrix4x4.Frustum(r, l, d, u, n, f);
            }

            mirrorCamera.enabled = isActiveAndEnabled && f>n && n > 0 && !isCulled && (transform.position-mainCamera.transform.position).magnitude<maxUpdateDistance;
        }
        bool tmpInvertCulling;
        bool isCulled = false;
        private void OnPreRender()
        {
            tmpInvertCulling = GL.invertCulling;
            GL.invertCulling = !GL.invertCulling;
        }
        private void OnPostRender()
        {
            GL.invertCulling = tmpInvertCulling;
        }
        private void OnBecameInvisible()
        {
            isCulled = true;
        }
        private void OnBecameVisible()
        {
            isCulled = false;
        }
        private void OnDisable()
        {
            mirrorCamera.enabled = false;
        }
    }
}

