﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Climbing;

namespace Climbing
{
    public class VaultingController : MonoBehaviour
    {
        public ClimbController climbController;
        public ThirdPersonController controller;
        public Animator animator;
        public Vector3 kneeRaycastOrigin;
        public float kneeRaycastLength = 1.0f;
        public float landOffset = 0.2f;
        Vector3 targetPos;
        Vector3 startPos;
        Quaternion startRot;
        bool isVaulting;
        float vaultTime = 0.0f;
        float animLength = 0.0f;
        public bool debug = false;
        public AnimationClip clip;

        private Vector3 leftHandPosition;
        private Quaternion leftHandRotation;
        public string HandAnimVariableName = "HandCurve";
        [Range(0, 1f)] [SerializeField] private float handToIKPositionSpeed = 0.25f;

        // Start is called before the first frame update
        void Start()
        {
            controller = GetComponent<ThirdPersonController>();
            climbController = GetComponent<ClimbController>();
            animator = GetComponent<Animator>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKey(KeyCode.Space) && !isVaulting && !controller.dummy)
                CheckVaultObject();

            if (isVaulting)
            {
                float actualSpeed = Time.deltaTime / animLength;
                vaultTime += actualSpeed;

                if (vaultTime > 1)
                {
                    animator.SetBool("Vault", false);
                    isVaulting = false;
                    controller.EnableController();
                }

                Vector3 dir = targetPos - startPos;
                dir.y = 0;
                Quaternion rot = Quaternion.LookRotation(dir);

                transform.rotation = Quaternion.Lerp(startRot, rot, 0.5f);
                transform.position = Vector3.Lerp(startPos, targetPos, vaultTime);
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!isVaulting)
                return;

            float curve = animator.GetFloat(HandAnimVariableName);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, curve);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPosition);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, curve);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandRotation);
        }

        private void CheckVaultObject()
        {
            RaycastHit hit;
            Vector3 origin = transform.position + kneeRaycastOrigin;
            Vector3 origin2 = origin + transform.forward * kneeRaycastLength;

            if (Physics.Raycast(origin, transform.forward, out hit, kneeRaycastLength))
            {
                RaycastHit hit2;
                if (Physics.Raycast(origin2, Vector3.down, out hit2, 1)) //Ground Hit
                {
                    if (hit.transform.tag == "Vault")
                    {
                        controller.characterAnimation.animator.CrossFade("Vault", 0.2f);
                        isVaulting = true;
                        startPos = transform.position;
                        startRot = transform.rotation;
                        targetPos = hit2.point + (-hit.normal * (hit.transform.localScale.z + landOffset));
                        vaultTime = 0;
                        animLength = clip.length;
                        controller.DisableController();

                        //Calculate Hand Rest Position n Rotation
                        Vector3 left = Vector3.Cross(hit.normal, Vector3.up);
                        leftHandPosition = hit.point + (-hit.normal * (hit.transform.localScale.z / 2));
                        leftHandPosition.y = hit.transform.position.y + hit.transform.localScale.y / 2;
                        leftHandPosition.x += left.x * animator.GetBoneTransform(HumanBodyBones.LeftHand).localPosition.x;
                        leftHandRotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
                    }
                }
            }

            if (debug)
            {
                Debug.DrawLine(origin, origin + transform.forward * kneeRaycastLength);//Forward Raycast
                Debug.DrawLine(hit.point, hit.point + hit.normal, Color.cyan); //Face Normal
                Debug.DrawLine(origin2, origin2 + Vector3.down);//Down Raycast
            }
        }

        private void OnDrawGizmos()
        {
            if (debug && isVaulting)
            {
                Gizmos.DrawSphere(targetPos, 0.08f);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(leftHandPosition, 0.08f);
            }
        }
    }
}