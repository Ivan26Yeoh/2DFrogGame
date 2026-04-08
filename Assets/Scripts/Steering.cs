using System.Collections.Generic;
using UnityEngine;

namespace SteeringCalcs
{
    [System.Serializable]
    public class AvoidanceParams
    {
        public bool Enable;
        public LayerMask ObstacleMask;

    }

    public class Steering
    {
        // PLEASE NOTE:
        // You do not need to edit any of the methods in the HelperMethods region.
        // In Visual Studio, you can collapse the HelperMethods region by clicking the "-" to the left.
        #region HelperMethods

        // Helper method for rotating a vector by an angle (in degrees).
        public static Vector2 rotate(Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;

            return new Vector2(
                v.x * Mathf.Cos(radians) - v.y * Mathf.Sin(radians),
                v.x * Mathf.Sin(radians) + v.y * Mathf.Cos(radians)
            );
        }

        // Converts a desired velocity into a steering force,
        // as will be explained in class (Week 2).
        public static Vector2 DesiredVelToForce(Vector2 desiredVel, Rigidbody2D rb, float accelTime, float maxAccel)
        {
            Vector2 accel = (desiredVel - rb.linearVelocity) / accelTime;

            if (accel.magnitude > maxAccel)
            {
                accel = accel.normalized * maxAccel;
            }

            // F = ma
            return rb.mass * accel;
        }

        // In addition to separation, cohesion and alignment, the flies also have
        // an "anchor" force applied to them while flocking, to keep them within the game arena.
        // This is already implemented for you.
        public static Vector2 GetAnchor(Vector2 currentPos, Vector2 anchorDims)
        {
            Vector2 desiredVel = Vector2.zero;

            if (Mathf.Abs(currentPos.x) > anchorDims.x)
            {
                desiredVel -= new Vector2(currentPos.x, 0.0f);
            }

            if (Mathf.Abs(currentPos.y) > anchorDims.y)
            {
                desiredVel -= new Vector2(0.0f, currentPos.y);
            }

            return desiredVel;
        }

        #endregion

        // These are "parent" steering methods that toggle between obstacle avoidance (XAndAvoid)
        // and no avoidance "XDirect" of each steering behaviour.
        // The avoid methods use GetAvoidanceTarget, to find an target position.
        // You will need to implement GetAvoidanceTarget for the avoidance behaviours to work.
        // Do not need to edit these methods.
        #region ParentSteeringMethods

        // Seek returns a desired velocity to reach a target position, at a set speed.
        // This will cause an overshoot of the target position.
        // Do not edit this.
        public static Vector2 Seek(Vector2 currentPos, Vector2 targetPos, float maxSpeed, AvoidanceParams avoidParams)
        {
            if (avoidParams.Enable)
            {
                return SeekAndAvoid(currentPos, targetPos, maxSpeed, avoidParams);
            }
            else
            {
                return SeekDirect(currentPos, targetPos, maxSpeed);
            }
        }

        // Do not edit this method.
        // To implement obstacle avoidance, the only method you need to edit is GetAvoidanceTarget.
        public static Vector2 SeekAndAvoid(Vector2 currentPos, Vector2 targetPos, float maxSpeed, AvoidanceParams avoidParams)
        {
            targetPos = GetAvoidanceTarget(currentPos, targetPos, avoidParams);
            return SeekDirect(currentPos, targetPos, maxSpeed);
        }

        // Arrvie returns a desired velocity to reach a target position,
        // where the velocity is scaled by the distance to the target to avoid overshooting.
        // Do not edit this.
        public static Vector2 Arrive(Vector2 currentPos, Vector2 targetPos, float radius, float maxSpeed, AvoidanceParams avoidParams)
        {
            if (avoidParams.Enable)
            {
                return ArriveAndAvoid(currentPos, targetPos, radius, maxSpeed, avoidParams);
            }
            else
            {
                return ArriveDirect(currentPos, targetPos, radius, maxSpeed);
            }
        }

        // Do not edit this method.
        // To implement obstacle avoidance, the only method you need to edit is GetAvoidanceTarget.
        public static Vector2 ArriveAndAvoid(Vector2 currentPos, Vector2 targetPos, float radius, float maxSpeed, AvoidanceParams avoidParams)
        {
            targetPos = GetAvoidanceTarget(currentPos, targetPos, avoidParams);
            return ArriveDirect(currentPos, targetPos, radius, maxSpeed);
        }


        // Flee returns a desired velocity to move away from a target position, at a set speed.
        // where the velocity is scaled by the distance to the target to avoid overshooting.
        // Do not edit this.
        public static Vector2 Flee(Vector2 currentPos, Vector2 predatorPos, float maxSpeed, AvoidanceParams avoidParams)
        {
            if (avoidParams.Enable)
            {
                return FleeAndAvoid(currentPos, predatorPos, maxSpeed, avoidParams);
            }
            else
            {
                return FleeDirect(currentPos, predatorPos, maxSpeed);
            }
        }

        // Do not edit this method.
        // To implement obstacle avoidance, the only method you need to edit is GetAvoidanceTarget.
        public static Vector2 FleeAndAvoid(Vector2 currentPos, Vector2 predatorPos, float maxSpeed, AvoidanceParams avoidParams)
        {
            Vector2 offset = predatorPos - currentPos;
            Vector2 fleeTarget = predatorPos + offset;

            fleeTarget = GetAvoidanceTarget(currentPos, fleeTarget, avoidParams);
            return FleeDirect(currentPos, fleeTarget, maxSpeed);
        }


        #endregion


        // Below are all the methods that you *do* need to edit.
        #region MethodsToImplement

        // Seek returns a desired velocity to reach a target position, at a set speed.
        // This will cause an overshoot of the target position.
        public static Vector2 SeekDirect(Vector2 currentPos, Vector2 targetPos, float maxSpeed)
        {
            return (targetPos - currentPos).normalized * maxSpeed;
        }

        // Arrvie returns a desired velocity to reach a target position,
        // where the velocity is scaled by the distance to the target to avoid overshooting.
        public static Vector2 ArriveDirect(Vector2 currentPos, Vector2 targetPos, float radius, float maxSpeed)
        {
            float d = (targetPos - currentPos).magnitude;
            if (d >= radius)
            {
                return (targetPos - currentPos).normalized * maxSpeed;
            }
            else
            {
                return (targetPos - currentPos).normalized * maxSpeed * (d / radius);
            }
        }

        public static Vector2 FleeDirect(Vector2 currentPos, Vector2 predatorPos, float maxSpeed)
        {
            return (currentPos - predatorPos).normalized * maxSpeed;
        }

        // Find an avoidance target position for the given current and target positions.
        // See the spec for a detailed explanation of how GetAvoidanceTarget is expected to work.
        // You're expected to use Physics2D.CircleCast (https://docs.unity3d.com/ScriptReference/Physics2D.CircleCast.html)
        // You'll also probably want to use the rotate() method declared above.
        public static Vector2 GetAvoidanceTarget(Vector2 currentPos, Vector2 targetPos, AvoidanceParams avoidParams)
        {
            if (!avoidParams.Enable) return targetPos;

            Vector2 currentPos2 = currentPos;
            Vector2 endpointOffset = targetPos - currentPos;
            float length = endpointOffset.magnitude;
            bool pathFound = false;

            RaycastHit2D hit = Physics2D.CircleCast(currentPos, 0.5f, endpointOffset, length, avoidParams.ObstacleMask);
            pathFound = hit ? false : true;

            float rotationAngle = 0.0f;

            while (!pathFound && rotationAngle < 180)
            {
                rotationAngle += 10f;

                //clockwise
                Vector2 rotatedEndpointOffsetLeft = rotate(endpointOffset, rotationAngle);
                hit = Physics2D.CircleCast(currentPos, 0.5f, rotatedEndpointOffsetLeft.normalized, length, avoidParams.ObstacleMask);
                if (!hit)
                {
                    return currentPos + rotatedEndpointOffsetLeft;
                }

                //anti clockwise
                Vector2 rotatedEndpointOffsetRight = rotate(endpointOffset, -rotationAngle);
                hit = Physics2D.CircleCast(currentPos, 0.5f, rotatedEndpointOffsetRight.normalized, length, avoidParams.ObstacleMask);
                if (!hit)
                {
                    return currentPos + rotatedEndpointOffsetRight;
                }
            }

            //no free path
            return targetPos;
        }

        // See the assignment spec for an explanation of this method
        public static Vector2 GetSeparation(Vector2 currentPos, List<Transform> neighbours, float maxSpeed)
        {
            if (neighbours.Count == 0) return Vector2.zero;

            Vector2 sRaw = Vector2.zero;
            for (int i = 0; i < neighbours.Count; i++)
            {
                Vector2 neighbourPos = neighbours[i].position;
                float d = (currentPos - neighbourPos).magnitude;
                sRaw += (currentPos - neighbourPos) / (d * d);
            }

            return sRaw.normalized * maxSpeed;

        }

        // See the assignment spec for an explanation of this method
        public static Vector2 GetCohesion(Vector2 currentPos, List<Transform> neighbours, float maxSpeed)
        {
            if (neighbours.Count == 0) return Vector2.zero;

            Vector2 nAve = Vector2.zero;
            for (int i = 0; i < neighbours.Count; i++)
            {
                nAve += (Vector2)neighbours[i].position;
            }

            nAve /= neighbours.Count;

            Vector2 cRaw = nAve - currentPos;
            return cRaw.normalized * maxSpeed;
        }

        // See the assignment spec for an explanation of this method
        public static Vector2 GetAlignment(List<Transform> neighbours, float maxSpeed)
        {
            if (neighbours.Count == 0) return Vector2.zero;

            Vector2 vAve = Vector2.zero;

            for (int i = 0; i < neighbours.Count; i++)
            {
                vAve += neighbours[i].GetComponent<Rigidbody2D>().linearVelocity;
            }
            vAve /= neighbours.Count;
            return vAve.normalized * maxSpeed;
        }

        #endregion
    }
}
