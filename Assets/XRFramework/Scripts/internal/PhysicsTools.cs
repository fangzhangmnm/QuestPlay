using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fzmnm
{
    public static class PhysicsTools
    {
        static public Vector3 ApplyTensor(Vector3 vector, Vector3 tensor, Quaternion tensorRotation, bool inverse = false)
        {
            vector = Quaternion.Inverse(tensorRotation) * vector;
            if (!inverse)
            { vector.x *= tensor.x; vector.y *= tensor.y; vector.z *= tensor.z; }
            else
            { vector.x /= tensor.x; vector.y /= tensor.y; vector.z /= tensor.z; }
            return tensorRotation * vector;
        }
        public static void SetIgnoreCollision(Rigidbody a, Transform sceneObject, bool ignore)
        {
            if (a != null && sceneObject != null)
                foreach (var ac in a.GetComponentsInChildren<Collider>())
                    if (ac.attachedRigidbody == a)
                        foreach (var bc in sceneObject.GetComponentsInChildren<Collider>())
                            if (bc.attachedRigidbody == null)
                                Physics.IgnoreCollision(ac, bc, ignore);
        }
        public static bool IsGhostCollision(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; ++i)
                if (collision.contacts[i].separation <= Physics.defaultContactOffset)
                    return false;
            return true;
        }
        public static float BoxSphere(Vector3 position1, float radius1, Vector3 position2, Quaternion orientation2, Vector3 halfExtent2)
        {
            position1 = Quaternion.Inverse(orientation2) * (position1 - position2);
            Vector3 closestPoint = new Vector3(Mathf.Clamp(position1.x, -halfExtent2.x, halfExtent2.x),
                Mathf.Clamp(position1.y, -halfExtent2.y, halfExtent2.y),
                Mathf.Clamp(position1.z, -halfExtent2.z, halfExtent2.z));
            return radius1 - (position1 - closestPoint).magnitude;
        }
        public static Vector3 RandomInsideBox(BoxCollider box)
        {
            Vector3 min = box.center - box.size / 2;
            Vector3 max = box.center + box.size / 2;
            Vector3 random = new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), Random.Range(min.z, max.z));
            return box.transform.TransformPoint(random);
        }
    }
}