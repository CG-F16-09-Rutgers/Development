﻿using UnityEngine;
using System.Collections;
using System;

namespace NPC {

    public enum LOCO_STATE {
        IDLE,
        FRONT,
        FORWARD,
        BACKWARDS,
        LEFT,
        RIGHT,
        RUN,
        WALK,
        DUCK,
        GROUND,
        JUMP,
        FALL
    }

    public enum NAV_STATE {
        DISABLED = 0,
        STEERING_NAV,
        NAVMESH_NAV
    }

    [System.Serializable]
    public class NPCBody : MonoBehaviour {

        NavMeshAgent gNavMeshAgent;
        Rigidbody gRigidBody;
        Animator g_Animator;
        CapsuleCollider gCapsuleCollider;
        NPCIKController gIKController;

        private bool g_LookingAround = false;

        private static string g_AnimParamSpeed      = "Speed";
        private static string g_AnimParamDirection  = "Direction";
        private static string g_AnimParamJump       = "Jump";
        
        private static int   SPEED_MOD          =  2;
        private static float MAX_WALK__SPEED    =  1.00f;
        private static float MAX_RUN_SPEED      =  1.00f * SPEED_MOD;
        private static float MIN_WALK_SPEED     =  -1* MAX_WALK__SPEED;
        private static float MIN_RUN_SPEED      =  -1 * MAX_WALK__SPEED;

        private LOCO_STATE g_CurrentStateFwd    = LOCO_STATE.IDLE;
        private LOCO_STATE g_CurrentStateGnd    = LOCO_STATE.GROUND;
        private LOCO_STATE g_CurrentStateDir    = LOCO_STATE.FRONT;
        private LOCO_STATE g_CurrentStateMod    = LOCO_STATE.WALK;

        // This correlate with the parameters from the Animator
        private float g_CurrentSpeed            = 0.0f;
        private float g_CurrentVelocity         = 0.05f;
        private float g_CurrentOrientation      = 0.0f;
        private bool g_Navigating = false;

        #region Properties

        public NAV_STATE Navigation;
        public bool UseCurves;
        public bool IKEnabled;
        public bool UseAnimatorController;

        public bool LookingAround {
            get {
                return g_LookingAround;
            }
        }

        public Transform TargetObject {
            get {
                return gIKController.LOOK_AT_TARGET;
            }
        }

        public Transform Head {
            get {
                return gIKController.Head;
            }
        }
        
        public bool IsIdle {
            get {
                return
                    // We always need to test for a state and a possible active transition
                    g_Animator.GetCurrentAnimatorStateInfo(0).shortNameHash == gHashIdle
                    && g_Animator.GetAnimatorTransitionInfo(0).fullPathHash == 0;

            }
        }
        #endregion

        #region StateHash
        private static int gHashJump = Animator.StringToHash("JumpLoco");
        private static int gHashIdle = Animator.StringToHash("Idle");
        #endregion

        [System.ComponentModel.DefaultValue(1f)]
        private float MaxWalkSpeed { get; set; }

        [System.ComponentModel.DefaultValue(2f)]
        private float MaxRunSpeed { get; set; }

        [System.ComponentModel.DefaultValue(-1f)]
        private float TurnLeftAngle { get; set; }

        [System.ComponentModel.DefaultValue(1f)]
        private float TurnRightAngle { get; set; }

        void Reset() {

            Debug.Log("Initializing NPCBody ... ");
            gNavMeshAgent = gameObject.GetComponent<NavMeshAgent>();
            gRigidBody = gameObject.GetComponent<Rigidbody>();
            g_Animator = gameObject.GetComponent<Animator>();
            gIKController = gameObject.GetComponent<NPCIKController>();
            gCapsuleCollider = gameObject.GetComponent<CapsuleCollider>();
            if (gNavMeshAgent == null) {
                gNavMeshAgent = gameObject.AddComponent<NavMeshAgent>();
                gNavMeshAgent.autoBraking = true;
                gNavMeshAgent.enabled = false;
                Debug.Log("NPCBody requires a NavMeshAgent if navigation is on, adding a default one.");
            }
            if (g_Animator == null || g_Animator.runtimeAnimatorController == null) {
                Debug.Log("NPCBody --> Agent requires an Animator Controller!!! - consider adding the NPCDefaultAnimatorController");
            } else UseAnimatorController = true;
            if(gRigidBody == null) {
                gRigidBody = gameObject.AddComponent<Rigidbody>();
                gRigidBody.useGravity = true;
                gRigidBody.mass = 3;
                gRigidBody.constraints = RigidbodyConstraints.FreezeRotation;
            }
            if(gCapsuleCollider == null) {
                gCapsuleCollider = gameObject.AddComponent<CapsuleCollider>();
                gCapsuleCollider.radius = 0.3f;
                gCapsuleCollider.height = 1.5f;
                gCapsuleCollider.center = new Vector3(0.0f,0.75f,0.0f);
            }
            if(gIKController == null) {
                gIKController = gameObject.AddComponent<NPCIKController>();
            }
        }

        void Start() {
            g_Animator = gameObject.GetComponent<Animator>();
            gIKController = gameObject.GetComponent<NPCIKController>();
            gNavMeshAgent = gameObject.GetComponent<NavMeshAgent>();
            if (Navigation == NAV_STATE.NAVMESH_NAV) gNavMeshAgent.enabled = true;
            if (g_Animator == null || gNavMeshAgent == null || gNavMeshAgent.enabled) UseAnimatorController = false;
            if (gIKController == null) IKEnabled = false;
        }

        /// <summary>
        /// Control all the body's parameters for speed, orientation, etc...
        /// </summary>
        public void UpdateBody() {
            
            if(UseAnimatorController) {
                
                // If accidentally checked
                if (g_Animator == null) {
                    Debug.Log("NPCBody --> No Animator in agent, disabling UseAnimatorController");
                    UseAnimatorController = false;
                    return;
                }

                if (g_Navigating) {
                    // GoTo();    
                }

                // handle mod
                float  forth    = g_CurrentStateFwd == LOCO_STATE.FORWARD ? 1.0f : -1.0f;
                float  orient   = g_CurrentStateDir == LOCO_STATE.RIGHT ? 1.0f : -1.0f;
                bool   duck     = (g_CurrentStateMod == LOCO_STATE.DUCK);
                float  topF     = (g_CurrentStateMod == LOCO_STATE.RUN || g_CurrentSpeed > MAX_WALK__SPEED) 
                    ? MAX_RUN_SPEED : MAX_WALK__SPEED;

                // update forward
                if (g_CurrentStateFwd != LOCO_STATE.IDLE) {
                    if (g_CurrentSpeed > MAX_WALK__SPEED
                        && g_CurrentStateMod == LOCO_STATE.WALK) g_CurrentSpeed -= g_CurrentVelocity;
                    else g_CurrentSpeed = Mathf.Clamp(g_CurrentSpeed + (g_CurrentVelocity * forth), MIN_WALK_SPEED, topF);
                } else {
                    if(g_CurrentSpeed != 0.0f) {
                        float m = g_CurrentVelocity * (g_CurrentSpeed > 0.0f ? -1.0f : 1.0f);
                        float stopDelta = g_CurrentSpeed + m;
                        g_CurrentSpeed = Mathf.Abs(stopDelta) > 0.05f ? stopDelta : 0.0f;
                    }
                }

                // update direction
                if (g_CurrentStateDir != LOCO_STATE.FRONT) {
                    g_CurrentOrientation = Mathf.Clamp(g_CurrentOrientation + (g_CurrentVelocity * orient), -1.0f, 1.0f);
                } else {
                    if (g_CurrentStateDir != 0.0f) {
                        float m = g_CurrentVelocity * (g_CurrentOrientation > 0.0f ? -1.0f : 1.0f);
                        float stopDelta = g_CurrentOrientation + m;
                        g_CurrentOrientation = Mathf.Abs(stopDelta) > 0.05f ? stopDelta : 0.0f;
                    }
                }

                // update ground
                if (g_CurrentStateGnd == LOCO_STATE.JUMP) {
                    g_Animator.SetTrigger(g_AnimParamJump);
                    g_CurrentStateGnd = LOCO_STATE.FALL;
                } else if(g_Animator.GetAnimatorTransitionInfo(0).fullPathHash == 0) {
                    // this is as long as we are not in jump state
                    g_CurrentStateGnd = LOCO_STATE.GROUND;
                }

                // apply curves if needed
                if(UseCurves) {
                    // update curves here
                }
            
                // set animator
            
                g_Animator.SetFloat(g_AnimParamSpeed, g_CurrentSpeed);
                g_Animator.SetFloat(g_AnimParamDirection, g_CurrentOrientation);

                // reset all states until updated again
                g_CurrentStateDir = LOCO_STATE.FRONT;
                g_CurrentStateFwd = LOCO_STATE.IDLE;
                g_CurrentStateMod = LOCO_STATE.WALK;
            }
        }

        #region Affordances
        
        public void Move(LOCO_STATE s) {
            switch (s) {
                case LOCO_STATE.RUN:
                case LOCO_STATE.DUCK:
                case LOCO_STATE.WALK:
                    g_CurrentStateMod = s;
                    break;
                case LOCO_STATE.FORWARD:
                case LOCO_STATE.BACKWARDS:
                case LOCO_STATE.IDLE:
                    g_CurrentStateFwd = s;
                    break;
                case LOCO_STATE.RIGHT:
                case LOCO_STATE.LEFT:
                case LOCO_STATE.FRONT:
                    g_CurrentStateDir = s;
                    break;
                case LOCO_STATE.JUMP:
                    g_CurrentStateGnd = s;
                    break;
                default:
                    Debug.Log("NPCBody --> Invalid direction especified for ModifyMotion");
                    break;
            }
        }

        public void GoTo(Vector3 location) {
            if(Navigation != NAV_STATE.DISABLED) {
                if(Navigation == NAV_STATE.STEERING_NAV) {
                    if(g_Navigating) {
                        // select next point and go towards it
                    } else {
                        // recreate points queue
                    }
                } else {
                    if(gNavMeshAgent != null) {

                        if (!gNavMeshAgent.enabled)
                            gNavMeshAgent.enabled = true;

                        gNavMeshAgent.SetDestination(location);
                    }
                }
            }
        }

        /// <summary>
        /// Used to start and stop looking around
        /// </summary>
        /// <param name="startLooking"></param>
        public void LookAround(bool startLooking) {
            GameObject go;
            if (startLooking && !g_LookingAround) {
                go = new GameObject();
                go.name = "TmpLookAtTarget";
                Func<Vector3, Vector3> pos = np => (np + (1.50f * transform.forward));
                go.transform.position = pos(transform.position);
                go.transform.rotation = transform.rotation;
                go.transform.SetParent(transform);
                LookAt(go.transform);
                g_LookingAround = true;
            } else if (g_LookingAround) {
                go = gIKController.LOOK_AT_TARGET.gameObject;
                LookAt(null);
                DestroyImmediate(go);
                g_LookingAround = false;
            }
        }

        public void LookAt(Transform t) {
            gIKController.LOOK_AT_TARGET = t;
        }

        #endregion
    }

}
