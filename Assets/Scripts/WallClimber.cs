using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;

public class WallClimber : MonoBehaviour {

    public float ClimbForce;
    public float SmallesEdge = 0.25f;
    public float CoolDown = 0.15f;
    public float MaxAngle = 30;
    public float ClimbRange = 2f;

    public Climbingsort currentSort;

    public Transform HandTrans;
    public Animator animator;
    public float minDistance;
    public Rigidbody rigid;
    public ThirdPersonUserControl TPUC;
    public ThirdPersonCharacter TPC;
    public Vector3 VerticalHandOffset;
    public Vector3 HorizontalHandOffset;
    public LayerMask SpotLayer;
    public LayerMask CurrentSpotLayer;
    public LayerMask CheckLayersReachable;

    private Vector3 TargetPoint;
    private Vector3 TargetNormal;

    private float lastTime;
    private float BeginDistance;
    private RaycastHit hit;
    private Quaternion oldRotation;


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void Climb()
    {
        if(Time.time - lastTime > CoolDown && currentSort == Climbingsort.Climbing)
        {
            if(Input.GetAxis("Vertical") > 0)
            {
                CheckForSpots(HandTrans.position + transform.rotation * VerticalHandOffset + transform.up * ClimbRange, -transform.up, ClimbRange, CheckingSort.normal);
                //CheckForPlateay();
            }

            if (Input.GetAxis("Vertical") < 0)
            {
                CheckForSpots(HandTrans.position - transform.rotation * (VerticalHandOffset + new Vector3(0,0.3f,0)), -transform.up, ClimbRange, CheckingSort.normal);

                if(currentSort != Climbingsort.ClimbingTowardsPoint)
                {
                    rigid.isKinematic = false;
                    TPUC.enabled = true;
                    currentSort = Climbingsort.Falling;
                    oldRotation = transform.rotation;
                }
            }

            if (Input.GetAxis("Horizontal") != 0)
            {
                CheckForSpots(HandTrans.position - transform.rotation * HorizontalHandOffset, transform.right * Input.GetAxis("Horizontal") - transform.up / 3.5f, ClimbRange / 2, CheckingSort.normal );

                if(currentSort != Climbingsort.ClimbingTowardsPoint)
                    CheckForSpots(HandTrans.position + transform.rotation * HorizontalHandOffset, transform.right * Input.GetAxis("Horizontal") - transform.up / 1.5f, ClimbRange / 3, CheckingSort.normal);

                if (currentSort != Climbingsort.ClimbingTowardsPoint)
                    CheckForSpots(HandTrans.position + transform.rotation * HorizontalHandOffset, transform.right * Input.GetAxis("Horizontal") - transform.up / 6, ClimbRange / 1.5f, CheckingSort.normal);

                if (currentSort != Climbingsort.ClimbingTowardsPoint)
                {
                    int hor = 0;

                    if (Input.GetAxis("Horizontal") < 0)
                        hor = -1;
                    if (Input.GetAxis("Horizontal") > 0)
                        hor = 1;

                    CheckForSpots(HandTrans.position + transform.rotation * HorizontalHandOffset + transform.right * hor * SmallesEdge / 4, transform.forward - transform.up * 2, ClimbRange / 3, CheckingSort.turning);

                    if (currentSort != Climbingsort.ClimbingTowardsPoint)
                        CheckForSpots(HandTrans.position + transform.rotation * HorizontalHandOffset + transform.right * 0.2f, transform.forward - transform.up * 2 + transform.right * hor/1.5f, ClimbRange / 3, CheckingSort.turning);
                }
            }
        }
    }

    public void CheckForSpots(Vector3 SpotLocation, Vector3 dir, float range, CheckingSort sort)
    {
        bool foundSpot = false;

        if(Physics.Raycast(SpotLocation - transform.right * SmallesEdge / 2, dir, out hit, range, SpotLayer))
        {
            if(Vector3.Distance(HandTrans.position, hit.point) > minDistance)
            {
                foundSpot = true;

                FindSpot(hit, sort);
            }
        }

        if (!foundSpot)
        {
            if (Physics.Raycast(SpotLocation + transform.right * SmallesEdge / 2, dir, out hit, range, SpotLayer))
            {
                if (Vector3.Distance(HandTrans.position, hit.point) > minDistance)
                {
                    foundSpot = true;

                    FindSpot(hit, sort);
                }
            }
        }

        if (!foundSpot)
        {
            if (Physics.Raycast(SpotLocation + transform.right * SmallesEdge / 2 + transform.forward * SmallesEdge, dir, out hit, range, SpotLayer))
            {
                if (Vector3.Distance(HandTrans.position, hit.point) > minDistance)
                {
                    foundSpot = true;

                    FindSpot(hit, sort);
                }
            }
        }

        if (!foundSpot)
        {
            if (Physics.Raycast(SpotLocation - transform.right * SmallesEdge / 2 + transform.forward * SmallesEdge, dir, out hit, range, SpotLayer))
            {
                if (Vector3.Distance(HandTrans.position, hit.point) > minDistance)
                {
                    foundSpot = true;

                    FindSpot(hit, sort);
                }
            }
        }
    }

    public void FindSpot(RaycastHit h, CheckingSort sort)
    {
        if(Vector3.Angle(h.normal, Vector3.up) < MaxAngle)
        {
            RayInfo ray = new RayInfo();

            if (sort == CheckingSort.normal)
                ray = GetClosestPoint(h.transform, h.point + new Vector3(0, -0.01f,0), transform.forward / 2.5f);
            else if (sort == CheckingSort.turning)
                ray = GetClosestPoint(h.transform, h.point + new Vector3(0, -0.01f, 0), transform.forward / 2.5f - transform.right * Input.GetAxis("Horizontal"));
            else if (sort == CheckingSort.falling)
                ray = GetClosestPoint(h.transform, h.point + new Vector3(0, -0.01f, 0), -transform.forward / 2.5f);

            TargetPoint = ray.point;
            TargetNormal = ray.normal;

            if(ray.CanGoToPoint)
            {
                if(currentSort != Climbingsort.Climbing && currentSort != Climbingsort.ClimbingTowardsPoint)
                {
                    TPUC.enabled = false;
                    rigid.isKinematic = true;
                    TPC.m_IsGrounded = false;
                }
                currentSort = Climbingsort.ClimbingTowardsPoint;
            }
        }
    }

    public RayInfo GetClosestPoint(Transform trans, Vector3 pos, Vector3 dir)
    {
        RayInfo curray = new RayInfo();
        RaycastHit hit2;

        int oldLayer = trans.gameObject.layer;

        // Change this
        trans.gameObject.layer = 14;

        if(Physics.Raycast(pos - dir, dir, out hit2, dir.magnitude * 2, CurrentSpotLayer))
        {
            curray.point = hit2.point;
            curray.normal = hit2.normal;

            if (!Physics.Linecast(HandTrans.position + transform.rotation * new Vector3(0.05f, -0.05f), curray.point + new Vector3(0,0.5f,0), out hit2, CheckLayersReachable))
            {
                if(!Physics.Linecast(curray.point - Quaternion.Euler(new Vector3(0,90,0)) * curray.normal * 0.35f + 0.1f * curray.point, curray.point + Quaternion.Euler(new Vector3(0, 90, 0)) * curray.normal * 0.35f + 0.1f * curray.point, out hit2, CheckLayersReachable))
                {
                    if (!Physics.Linecast(curray.point + Quaternion.Euler(new Vector3(0, 90, 0)) * curray.normal * 0.35f + 0.1f * curray.point, curray.point - Quaternion.Euler(new Vector3(0, 90, 0)) * curray.normal * 0.35f + 0.1f * curray.point, out hit2, CheckLayersReachable))
                    {
                        curray.CanGoToPoint = true;
                    }
                    else
                    {
                        curray.CanGoToPoint = false;
                    }
                }
                else
                {
                    curray.CanGoToPoint = false;
                }
            }
            else
            {
                curray.CanGoToPoint = false;
            }

            trans.gameObject.layer = oldLayer;
            return curray;
        }
        else
        {
            curray.CanGoToPoint = false;
            trans.gameObject.layer = oldLayer;
            return curray;
        }




    }

    public void MoveTowardsPoint()
    {
        transform.position = Vector3.Lerp(transform.position, (TargetPoint - transform.rotation * HandTrans.localPosition), Time.deltaTime * ClimbForce);
        Quaternion lookRotation = Quaternion.LookRotation(-TargetNormal);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * ClimbForce);
        animator.SetBool("OnGround", false);

        float distance = Vector2.Distance(transform.position, (TargetPoint - transform.rotation * HandTrans.localPosition));
        float percent = -9 * (BeginDistance - distance) / BeginDistance;

        animator.SetFloat("Jump", percent);

        if (distance <= 0.01f && currentSort == Climbingsort.ClimbingTowardsPoint)
        {
            transform.position = TargetPoint - transform.rotation * HandTrans.localPosition;
            transform.rotation = lookRotation;

            lastTime = Time.time;
            currentSort = Climbingsort.Climbing;
        }

        if (distance <= 0.25f && currentSort == Climbingsort.ClimbingTowardsPlateau)
        {
            transform.position = TargetPoint - transform.rotation * HandTrans.localPosition;
            transform.rotation = lookRotation;

            lastTime = Time.time;
            currentSort = Climbingsort.Walking;

            rigid.isKinematic = false;
            TPUC.enabled = true;
        }
    }
}

[System.Serializable]
public enum Climbingsort
{
    Walking,
    Jumping,
    Falling,
    Climbing,
    ClimbingTowardsPoint,
    ClimbingTowardsPlateau,
    checkingForClimbStart
}

[System.Serializable]
public class RayInfo
{
    public Vector3 point;
    public Vector3 normal;
    public bool CanGoToPoint;
}

[System.Serializable]
public enum CheckingSort
{
    normal,
    turning,
    falling
}