using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace fzmnm
{
    public static class PhysicsDebugger
    {
        public static Color color=Color.white;
        public static bool CapsuleCast(Vector3 P1, Vector3 P2, float radius, Vector3 dir, out RaycastHit hitInfo, float maxDist, int layers)
        {
            bool rtval = Physics.CapsuleCast(P1, P2, radius, dir, out hitInfo, maxDist, layers);
#if UNITY_EDITOR
            dir = dir.normalized;
            DebugDrawSphere(P1, radius, color);
            DebugDrawSphere(P2, radius, color);
            Debug.DrawLine(P1, P2, color);

            DebugDrawSphere(P1 + maxDist * dir, radius, color);
            Debug.DrawLine(P1, P1 + dir * (maxDist), color);
            DebugDrawSphere(P2 + maxDist * dir, radius, color);
            Debug.DrawLine(P2, P2 + dir * (maxDist), color);

            if (rtval)
            {
                DebugDrawSphere(P1 + hitInfo.distance * dir, radius, Color.red);
                DebugDrawSphere(P2 + hitInfo.distance * dir, radius, Color.red);
                Debug.DrawLine(hitInfo.point, hitInfo.point + hitInfo.normal, Color.red);
            }
#endif
            return rtval;
        }
        public static bool SphereCast(Vector3 origin, float radius, Vector3 dir, out RaycastHit hitInfo, float maxDist, int layers)
        {
            bool rtval = Physics.SphereCast(origin, radius, dir, out hitInfo, maxDist, layers);
#if UNITY_EDITOR
            dir = dir.normalized;
            DebugDrawSphere(origin, radius, color);
            DebugDrawSphere(origin + maxDist * dir, radius, color);
            Debug.DrawLine(origin, origin + dir * (maxDist), color);
            if (rtval)
            {
                DebugDrawSphere(origin + hitInfo.distance * dir, radius, Color.red);
                Debug.DrawLine(hitInfo.point, hitInfo.point + hitInfo.normal, Color.red);
            }
#endif
            return rtval;
        }
        public static void DebugDrawSphere(Vector3 pos, float radius, Color color)
        {
#if UNITY_EDITOR
            for (int i = 0; i < 20; ++i)
            {
                float c = Mathf.Cos(i * Mathf.PI * 2 / 20);
                float s = Mathf.Sin(i * Mathf.PI * 2 / 20);
                float c1 = Mathf.Cos((i + 1) * Mathf.PI * 2 / 20);
                float s1 = Mathf.Sin((i + 1) * Mathf.PI * 2 / 20);
                Debug.DrawLine(pos + new Vector3(radius * c, radius * s, 0), pos + new Vector3(radius * c1, radius * s1, 0), color);
                Debug.DrawLine(pos + new Vector3(radius * c, 0, radius * s), pos + new Vector3(radius * c1, 0, radius * s1), color);
                Debug.DrawLine(pos + new Vector3(0, radius * c, radius * s), pos + new Vector3(0, radius * c1, radius * s1), color);
            }
#endif
        }
        public static void DebugDrawCircle(Vector3 pos, Quaternion rot, float radius, Color color)
        {

#if UNITY_EDITOR
            for (int i = 0; i < 20; ++i)
            {
                float c = Mathf.Cos(i * Mathf.PI * 2 / 20);
                float s = Mathf.Sin(i * Mathf.PI * 2 / 20);
                float c1 = Mathf.Cos((i + 1) * Mathf.PI * 2 / 20);
                float s1 = Mathf.Sin((i + 1) * Mathf.PI * 2 / 20);
                Debug.DrawLine(pos + rot*new Vector3(radius * c, radius * s, 0), pos + rot * new Vector3(radius * c1, radius * s1, 0), color);
            }
#endif
        }
        public static void DebugDrawBox(Vector3 pos,Quaternion rot, Vector3 halfExtents,Color color)
        {
            Vector3 v000 = pos + rot * new Vector3(halfExtents.x, halfExtents.y, halfExtents.z);
            Vector3 v001 = pos + rot * new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
            Vector3 v010 = pos + rot * new Vector3(halfExtents.x, -halfExtents.y, halfExtents.z);
            Vector3 v011 = pos + rot * new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);
            Vector3 v100 = pos + rot * new Vector3(-halfExtents.x, halfExtents.y, halfExtents.z);
            Vector3 v101 = pos + rot * new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
            Vector3 v110 = pos + rot * new Vector3(-halfExtents.x, -halfExtents.y, halfExtents.z);
            Vector3 v111 = pos + rot * new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
            Debug.DrawLine(v000, v001, color);
            Debug.DrawLine(v000, v010, color);
            Debug.DrawLine(v000, v100, color);
            Debug.DrawLine(v011, v010, color);
            Debug.DrawLine(v011, v001, color);
            Debug.DrawLine(v011, v111, color);
            Debug.DrawLine(v101, v100, color);
            Debug.DrawLine(v101, v111, color);
            Debug.DrawLine(v101, v001, color);
            Debug.DrawLine(v110, v111, color);
            Debug.DrawLine(v110, v100, color);
            Debug.DrawLine(v110, v010, color);
        }
        public static void ShowVectorInGame(Vector3 origin, Vector3 vec, float time = 1f)
        {
            if (vec.magnitude > 0.000001f)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.GetComponent<BoxCollider>().enabled = false;
                b.transform.position = origin + vec / 2;
                b.transform.localScale = new Vector3(.01f, .01f, vec.magnitude + .01f);
                b.transform.rotation = Quaternion.LookRotation(vec);
                GameObject.Destroy(b, time);
            }
        }
    }
}
