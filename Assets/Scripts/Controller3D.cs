﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller3D : MonoBehaviour {

    public LayerMask collisionMask;
    public LayerMask collisionMaskAround;

    const float skinWidth = .2f;
    public float maxSlopeAngle = 60f;

    public CapsuleCollider coll;
    RaycastOrigins raycastOrigins;
    public CollisionInfo collisions;

    private void Awake()
    {
    }

    private void Start()
    {
        CalculateRaySpacing();
        //print("bounds.size.z = " + coll.bounds.size.z+"bounds.size.y = "+ coll.bounds.size.y);
    }

    public void Move(Vector3 vel)
    {
        UpdateRaycastOrigins();
        collisions.ResetVertical();
        collisions.ResetHorizontal();
        collisions.ResetClimbingSlope();

        if (vel.x != 0)
        {
            XCollisions(ref vel);
        }

        if (vel.z != 0)
        {
            ZCollisions(ref vel);
        }

        if (vel.y != 0)
        {
            VerticalCollisions(ref vel);
        }

        print("vel= " + vel.ToString("F4"));
        transform.Translate(vel,Space.World);
    }

    void XCollisions(ref Vector3 vel)
    {
        float directionX = Mathf.Sign(vel.x);
        float rayLength = Mathf.Abs(vel.x) + skinWidth;
        Vector3 rowsOrigin = directionX == 1 ?raycastOrigins.BottomRFCorner:raycastOrigins.BottomLFCorner;
        bool firstHit = false;
        for (int i = 0; i < horizontalRows; i++)
        {
            Vector3 rowOrigin = rowsOrigin;
            rowOrigin.y = rowsOrigin.y + i * horizontalRowSpacing;
            for (int j = 0; j < horizontalRaysPerRow; j++)
            {
                Vector3 rayOrigin = rowOrigin + Vector3.back * (j * horizontalRaySpacing + vel.y);
                RaycastHit hit;
                Debug.DrawRay(rayOrigin, Vector3.right * directionX * rayLength, Color.red);
                //print("rayOrigin= " + rayOrigin + "; rayLength = " + rayLength+"; BottomCenter.y= "+raycastOrigins.BottomCenter.y+"; min.y = "+coll.bounds.min.y);

                if (Physics.Raycast(rayOrigin, Vector3.right * directionX, out hit, rayLength, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    float slopeAngle = GetSlopeAngle(hit);
                    if (i == 0 && slopeAngle <=maxSlopeAngle)//collision on foot level
                    {
                        if (!firstHit)
                        {
                            float distanceToSlopeStart = 0;
                            /*if(slopeAngle != collisions.slopeAngleOld)
                            {
                                distanceToSlopeStart = hit.distance - skinWidth;
                                vel.x -= distanceToSlopeStart * directionX;
                                print("X : NEW SLOPE, distanceToSlopeStart= " + distanceToSlopeStart);
                            }*/
                            firstHit = true;
                            ClimbSlopeX(ref vel, slopeAngle);
                            //vel.x += distanceToSlopeStart * directionX;
                            print("X COLLISION SLOPE: "+" slopeVertAngle= " + slopeAngle);
                        }
                    }
                    if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle)
                    {
                        vel.x = (hit.distance - skinWidth) * directionX;
                        rayLength = hit.distance;

                        collisions.left = directionX == -1;
                        collisions.right = directionX == 1;
                    }

                }
            }
        }
    }

    void ZCollisions(ref Vector3 vel)
    {
        float directionZ = Mathf.Sign(vel.z);
        float rayLength = Mathf.Abs(vel.z) + skinWidth;
        Vector3 rowsOrigin = directionZ == 1 ? raycastOrigins.BottomLFCorner : raycastOrigins.BottomLBCorner;
        bool firstHit = false;
        for (int i = 0; i < horizontalRows; i++)
        {
            Vector3 rowOrigin = rowsOrigin;
            rowOrigin.y = rowsOrigin.y + i * horizontalRowSpacing;
            for (int j = 0; j < horizontalRaysPerRow; j++)
            {
                Vector3 rayOrigin = rowOrigin + Vector3.right * (j * horizontalRaySpacing + vel.y);
                RaycastHit hit;
                Debug.DrawRay(rayOrigin, Vector3.forward * directionZ * rayLength, Color.red);
                //print("rayOrigin= " + rayOrigin + "; rayLength = " + rayLength+"; BottomCenter.y= "+raycastOrigins.BottomCenter.y+"; min.y = "+coll.bounds.min.y);

                if (Physics.Raycast(rayOrigin, Vector3.forward * directionZ, out hit, rayLength, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    float slopeAngle = GetSlopeAngle(hit);
                    if (i == 0)//collision on foot level
                    {
                        if (!firstHit && slopeAngle <= maxSlopeAngle)
                        {
                            float distanceToSlopeStart = 0;
                            if (slopeAngle != collisions.slopeAngleOld)
                            {
                                distanceToSlopeStart = hit.distance - skinWidth;
                                vel.z -= distanceToSlopeStart * directionZ;
                                print("Z : NEW SLOPE, distanceToSlopeStart= " + distanceToSlopeStart);
                            }
                            firstHit = true;
                            ClimbSlopeZ(ref vel, slopeAngle);
                            vel.z += distanceToSlopeStart * directionZ;
                            print("Z COLLISION SLOPE: " + " slopeVertAngle= " + slopeAngle);
                        }
                    }
                    if(!collisions.climbingSlope || slopeAngle> maxSlopeAngle)
                    {
                        vel.z = (hit.distance - skinWidth) * directionZ;
                        rayLength = hit.distance;

                        collisions.behind = directionZ == -1;
                        collisions.foward = directionZ == 1;
                    }
                }
            }
        }
    }

    float GetSlopeAngle(RaycastHit hit)
    {
        float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
        return slopeAngle;
    }

    void ClimbSlope(ref Vector3 vel, float slopeAngle)
    {
        Vector3 horVel = new Vector3(vel.x, 0, vel.z);
        float moveDistance = Mathf.Abs(horVel.magnitude);
        float climbVelY = Mathf.Sin((slopeAngle * Mathf.Deg2Rad)) * moveDistance;
        if(vel.y> climbVelY)
        {
            print("JUMPING ON SLOPE");
        }
        else
        {
            float velXZ = Mathf.Cos((slopeAngle * Mathf.Deg2Rad) * moveDistance);
            vel = horVel.normalized * velXZ;
            vel.y = climbVelY;
            collisions.below = true;
        }
        
    }

    void ClimbSlopeX(ref Vector3 vel, float slopeAngle)
    {
        Vector3 horVel = new Vector3(vel.x, 0, vel.z);
        float moveDistance = Mathf.Abs(horVel.x);
        float climbVelY = Mathf.Sin((slopeAngle * Mathf.Deg2Rad)) * moveDistance;
        if (vel.y <= climbVelY)
        {
            float velX = Mathf.Cos((slopeAngle * Mathf.Deg2Rad) * moveDistance);
            vel.x = velX;
            vel.y = climbVelY;
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
        else
        {
            print("JUMPING ON SLOPE");
        }
    }
    void ClimbSlopeZ(ref Vector3 vel, float slopeAngle)
    {
        Vector3 horVel = new Vector3(vel.x, 0, vel.z);
        float moveDistance = Mathf.Abs(horVel.z);
        float climbVelY = Mathf.Sin((slopeAngle * Mathf.Deg2Rad)) * moveDistance;
        if (vel.y <= climbVelY)
        {
            float velzZ = Mathf.Cos((slopeAngle * Mathf.Deg2Rad) * moveDistance);
            vel.z = velzZ;
            vel.y = climbVelY;
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
        else
        {
            print("JUMPING ON SLOPE");
        }
    }

    public void AroundCollisions()
    {
        float rayLength = aroundRaycastsLength + skinWidth;
        Vector3 center = raycastOrigins.Center;
        float radius = raycastOrigins.AroundRadius;
        Vector3 circlesOrigin = raycastOrigins.BottomEnd;
        Vector3 circleOrigin = circlesOrigin;
        print("----------NEW SET OF RAYS------------");
        for (int i = 0; i < aroundCircles; i++)
        {
            circleOrigin.y = circlesOrigin.y + (i * aroundCirclesSpacing);
            print("Circle Origin= " + circleOrigin.ToString("F4"));
            for(int j = 0; j < aroundRaysPerCircle; j++)
            {
                float angle = (j * aroundAngleSpacing) * Mathf.Deg2Rad;
                float px = center.x + radius * Mathf.Cos(angle);
                float pz = center.z + radius * Mathf.Sin(angle);
                Vector3 rayCrossPoint = new Vector3(px, circleOrigin.y, pz);
                Vector3 finalDir = (rayCrossPoint - center).normalized;

                RaycastHit hit;
                Debug.DrawRay(center, finalDir * rayLength, Color.red);
                if (Physics.Raycast(center, finalDir, out hit, rayLength, collisionMaskAround, QueryTriggerInteraction.Ignore))
                {
                    collisions.around = true;
                }
            }
        }
    } 

    void HorizontalCollisions(ref Vector3 vel)
    {
        // ---------------------- 3D CAPSULE -------------------
        float directionX = Mathf.Sign(vel.x);
        float directionZ = Mathf.Sign(vel.z);

        Vector3 dirXZ = new Vector3(vel.x, 0, vel.z);
        //PERPENDICULAR VECTOR TO MOVING DIR
        float perpX = 1;
        float perpZ = (-dirXZ.x/dirXZ.z) * perpX;
        Vector3 perpVector = new Vector3(perpX, 0, perpZ).normalized;
        Vector3 rowDir = -perpVector;

        float rayLength = Mathf.Abs(dirXZ.magnitude) + skinWidth;
        //CIRCUMFERENCE POINT THAT INTERSECTS WITH dirXZ THAT CROSSES CENTER
        Vector3 rowsOriginCenter = dirXZ.normalized * (horizontalRadius - skinWidth);
        float rx = rowsOriginCenter.x + raycastOrigins.BottomCenterH.x;
        float rz = rowsOriginCenter.z + raycastOrigins.BottomCenterH.z;
        rowsOriginCenter = new Vector3(rx,raycastOrigins.BottomCenterH.y, rz);
        //CIRCUMFERENCE POINT THAT INTERSECTS WITH PERPENDICULAR VECTOR THAT CROSSES CENTER
        /*float angle = Mathf.Acos(((1 * perpVector.x) + (0 * perpVector.z)) / (1 * perpVector.magnitude)) * Mathf.Rad2Deg;
        float px = raycastOrigins.BottomCenter.x + horizontalRadius * Mathf.Cos(angle);
        float pz = raycastOrigins.BottomCenter.z + horizontalRadius * Mathf.Sin(angle);
        Vector3 rowsOrigin = new Vector3(px, raycastOrigins.BottomCenter.y, pz);*/
        Vector3 rowsOrigin = perpVector * (horizontalRadius);
        rx = rowsOrigin.x + rowsOriginCenter.x;
        rz = rowsOrigin.z + rowsOriginCenter.z;
        rowsOrigin = new Vector3(rx, rowsOriginCenter.y, rz);
        //print("dirXZ=" + dirXZ.ToString("F4") + "; perpVector=" + perpVector + ";rowsOrigin="+rowsOrigin);
        //print("----------NEW SET OF RAYS------------");
        for (int i = 0; i < horizontalRows; i++)
        {
            Vector3 rowOrigin = new Vector3(rowsOrigin.x,rowsOrigin.y+(i * horizontalRowSpacing), rowsOrigin.z);
            //print("horizontalRowSpacing= " + horizontalRowSpacing + "; rowOrigin = " + rowOrigin.ToString("F4"));
            for (int j = 0; j < horizontalRaysPerRow; j++)
            {
                //o=(rowOrigin.x,rowOrigin.z), f=(fx,fz), r=(perpVector2.x,perpVector2.z)
                //(perpVector2.x,perpVector2.z)=(fx-rowOrigin.x,fz-rowOrigin.z)
                Vector3 finalRowDir = rowDir * (j * horizontalRaySpacing);
                float fx = finalRowDir.x + rowOrigin.x;
                float fz = finalRowDir.z + rowOrigin.z;
                Vector3 rayOrigin = new Vector3(fx, rowOrigin.y, fz);

                RaycastHit hit;
                Debug.DrawRay(rayOrigin, dirXZ * rayLength, Color.red);
                //print("rayOrigin= " + rayOrigin + "; rayLength = " + rayLength+"; BottomCenter.y= "+raycastOrigins.BottomCenter.y+"; min.y = "+coll.bounds.min.y);

                if (Physics.Raycast(rayOrigin, dirXZ, out hit, rayLength, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    //print("HIT AGAINST " + hit.collider.gameObject.name);
                    //print("DISTANCE HIT = " + hit.distance);
                    if (hit.distance > (skinWidth))
                    {
                        print("HIT DISTANCE= " + hit.distance);
                        Vector3 aux = dirXZ.normalized * (hit.distance - skinWidth);
                        vel = new Vector3(aux.x, vel.y, aux.z);
                        rayLength = hit.distance;
                    }
                    else
                    {
                        //calculate parallel direction to plane hit and percentage of magnitude, based on incidence angle (90º->0)
                        Vector3 normal = new Vector3(hit.normal.x, 0, hit.normal.z);
                        Vector3 parallel = -normal;
                        //angle
                        float cos = Vector3.Dot(dirXZ, normal);
                        float cosDeg = Mathf.Acos(cos)*Mathf.Rad2Deg;
                        float prop = ((cosDeg-90) / 90);
                        prop = Mathf.Clamp(prop, 0, 1);
                        float finalSpeed = vel.magnitude * prop;
                        vel = new Vector3(parallel.x * finalSpeed, vel.y, parallel.z * finalSpeed);
                        print("HIT ANGLE = "+cosDeg+"; PARALLEL DIR= "+parallel.ToString("F4")+"; FINAL SPEED= "+finalSpeed);
                    }

                    collisions.behind = directionZ == -1;
                    collisions.foward = directionZ == 1;
                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
                //if (i == 0) { break; }
            }
        }
    }

    void VerticalCollisions(ref Vector3 vel)
    {
        // ---------------------- 3D CAPSULE -------------------
        float directionY = Mathf.Sign(vel.y);
        float rayLength = Mathf.Abs(vel.y) + skinWidth;
        float radius = 0f;
        //print("----------NEW SET OF RAYS------------");
        for(int i=0;i< verticalCircles; i++)
        {
            Vector3 circleOrigin = directionY == -1 ? raycastOrigins.BottomCenter : raycastOrigins.TopCenter;
            radius = i * verticalRadiusSpacing;
            //print("i= " + i + "; Radius = " + radius);
                //circleOrigin = new Vector3(circleOrigin.x, circleOrigin.y, circleOrigin.z + (i * verticalRadiusSpacing));

            for(int j=0; j < verticalRaysPerCircle; j++)
            {
                float angle = (j * verticalRayAngleSpacing) * Mathf.Deg2Rad;
                float px = circleOrigin.x + radius * Mathf.Cos(angle);
                float pz = circleOrigin.z + radius * Mathf.Sin(angle);
                Vector3 rayOrigin = new Vector3(px+vel.x, circleOrigin.y, pz+vel.z);

                RaycastHit hit;
                Debug.DrawRay(rayOrigin, Vector3.up * directionY * rayLength, Color.red);
                //print("rayOrigin= " + rayOrigin + "; direction = " + directionY + "; rayLength = " + rayLength+"; BottomCenter.y= "+raycastOrigins.BottomCenter.y+"; min.y = "+coll.bounds.min.y);

                if (Physics.Raycast(rayOrigin, Vector3.up * directionY,out hit, rayLength, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    //print("HIT AGAINST " + hit.collider.gameObject.name);
                    vel.y = (hit.distance - skinWidth) * directionY;
                    rayLength = hit.distance;

                    collisions.below = directionY == -1;
                    collisions.above = directionY == 1;
                }
                //if (i == 0) { break; }
            }
        }
    }

    void UpdateRaycastOrigins()
    {
        Bounds bounds = coll.bounds;
        bounds.Expand(skinWidth * -2);

        raycastOrigins.TopCenter = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        raycastOrigins.TopEnd = new Vector3(bounds.center.x, bounds.max.y, bounds.max.z);
        raycastOrigins.BottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        raycastOrigins.BottomEnd = new Vector3(bounds.center.x, bounds.min.y, bounds.max.z);

        raycastOrigins.BottomCenterH = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        //raycastOrigins.BottomLeft = new Vector3(bounds.min.x, bounds.min.y, bounds.center.z);
        raycastOrigins.BottomLFCorner = new Vector3(bounds.min.x,bounds.min.y,bounds.max.z);
        raycastOrigins.BottomRFCorner = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        raycastOrigins.BottomLBCorner = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);

        raycastOrigins.Center = bounds.center;
        raycastOrigins.AroundRadius = bounds.size.z / 2;

    }

    public int verticalCircles;
    public int verticalRaysPerCircle;
    float verticalRadiusSpacing;
    float verticalRayAngleSpacing;

    public int horizontalRows;
    public int horizontalRaysPerRow;
    float horizontalRowSpacing;
    float horizontalRaySpacing;
    float horizontalRadius;

    public int aroundRaysPerCircle;
    public int aroundCircles;
    public float aroundRaycastsLength = 3f;
    float aroundCirclesSpacing;
    float aroundAngleSpacing;

    void CalculateRaySpacing()
    {
        Bounds bounds = coll.bounds;
        bounds.Expand(skinWidth * -2);
        //-------------------

        verticalCircles = Mathf.Clamp(verticalCircles, 2, int.MaxValue);
        verticalRaysPerCircle = Mathf.Clamp(verticalRaysPerCircle, 3, int.MaxValue);

        verticalRadiusSpacing = (bounds.size.z/2) / (verticalCircles-1);
        verticalRayAngleSpacing = 360 / (verticalRaysPerCircle);

        horizontalRows = Mathf.Clamp(horizontalRows, 2, int.MaxValue);
        horizontalRaysPerRow = Mathf.Clamp(horizontalRaysPerRow, 2, int.MaxValue);

        print("HORIZONTAL ROWS = " + horizontalRows + "; BOUNDS.SIZE.Y = " + bounds.size.y);
        horizontalRowSpacing = bounds.size.y / (horizontalRows - 1);
        horizontalRaySpacing = bounds.size.x / (horizontalRaysPerRow - 1);
        horizontalRadius = Mathf.Abs(bounds.size.x / 2);

        aroundCircles = Mathf.Clamp(aroundCircles, 3, int.MaxValue);
        aroundRaysPerCircle = Mathf.Clamp(aroundRaysPerCircle, 3, int.MaxValue);

        aroundCirclesSpacing = bounds.size.y / (aroundCircles - 1);
        aroundAngleSpacing = 360 / (aroundRaysPerCircle);
    }

    struct RaycastOrigins
    {
        public Vector3 TopCenter, TopEnd;//TopEnd= center x, max y, max z
        public Vector3 BottomCenter, BottomEnd;//TopEnd= center x, min y, max z
        public Vector3 BottomCenterH;
        //public Vector3 BottomLeft;//min x, miny, center z 
        public Vector3 BottomLFCorner, BottomRFCorner;
        public Vector3 BottomLBCorner;

        public Vector3 Center;
        public float AroundRadius;
        
    }


    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;
        public bool foward, behind;
        public bool around;
        public bool climbingSlope;
        public float slopeAngle, slopeAngleOld;

        public void ResetVertical()
        {
            above = below = false;
        }
        public void ResetHorizontal()
        {
            left = right = false;
            foward = behind = false;
        }
        public void ResetAround()
        {
            around = false;
        }
        public void ResetClimbingSlope()
        {
            climbingSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }
}
