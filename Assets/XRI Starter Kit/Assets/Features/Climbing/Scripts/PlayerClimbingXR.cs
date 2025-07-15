using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using XR.Interaction.Toolkit.Samples;

namespace MikeNspired.XRIStarterKit
{
    [AddComponentMenu("XR/Locomotion/Player Climbing XR")]
    public class PlayerClimbingXR : LocomotionProvider
    {
        [Header("References")]
        [SerializeField] private XRInteractionManager xrInteractionManager;
        [SerializeField] private DynamicMoveProvider playerMovement;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private LayerMask checkGroundLayerMask = 1;

        [Header("Climb Speed")]
        [SerializeField] private float oneHandClimbSpeed = 0.6f;
        [SerializeField] private float twoHandClimbSpeed = 1f;

        [Header("Return To Old Location On Previous Hand Release")]
        [SerializeField] private float returnDistance = 0.1f;
        [SerializeField] private AnimationCurve returnToPlayerCurve = AnimationCurve.Linear(1f, 1f, 1f, 0f);
        [SerializeField] private float returnAnimationLength = 0.25f;

        [Header("Launching")]
        [SerializeField] private float launchSpeedMultiplier = 2f;
        [SerializeField] private Vector3 launchVelocityDrag = new Vector3(0.1f, 0.1f, 0.1f);

        private ControllerInputActionManager climbingHand;
        private ControllerInputActionManager previousHand;

        private Vector3 overPosition = Vector3.zero;
        private Vector3 prevLocation = Vector3.zero;
        private float climbSpeed;

        private Vector3 launchVelocity = Vector3.up;
        private bool isClimbing;
        private bool isReturningPlayer;

        private Transform grabbedMovingObject = null;
        private Vector3 lastObjectPos;
        private Quaternion lastObjectRot;

        // ✅ GravityProvider reference
        private GravityProvider gravityProvider;

        private void Start()
        {
            OnValidate();

            // ✅ Cache GravityProvider
            gravityProvider = GetComponent<GravityProvider>();
            if (gravityProvider == null)
            {
                Debug.LogWarning("GravityProvider not found on this object. Please add it for gravity control.");
            }
        }

        private void OnValidate()
        {
            if (!mediator)
                mediator = GetComponent<LocomotionMediator>();

            if (!playerMovement)
                playerMovement = GetComponent<DynamicMoveProvider>();

            if (!xrOrigin)
                xrOrigin = GetComponentInParent<XROrigin>();

            if (!xrInteractionManager)
                xrInteractionManager = FindFirstObjectByType<XRInteractionManager>();

            if (!characterController)
                characterController = FindFirstObjectByType<CharacterController>();
        }

        private void Update()
        {
            if (characterController && characterController.isGrounded && !isClimbing)
            {
                // ✅ Enable gravity when grounded
                if (gravityProvider != null)
                    gravityProvider.enabled = true;

                launchVelocity = Vector3.zero;
                return;
            }

            if (isClimbing || overPosition != Vector3.zero)
            {
                // ✅ Disable gravity during climbing
                if (gravityProvider != null)
                    gravityProvider.enabled = false;

                return;
            }

            // Apply launch motion with drag
            launchVelocity.x /= 1 + launchVelocityDrag.x * Time.deltaTime;
            launchVelocity.y += Physics.gravity.y * Time.deltaTime;
            launchVelocity.z /= 1 + launchVelocityDrag.z * Time.deltaTime;

            characterController?.Move(launchVelocity * Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!isClimbing)
                return;

            if (locomotionState == LocomotionState.Preparing)
                TryStartLocomotionImmediately();

            if (locomotionState == LocomotionState.Moving)
            {
                ApplyMovingObjectDelta();
                Climb();
            }
        }

        #region Public Climb Interface

        public void SetClimbHand(ControllerInputActionManager controller, Transform grabbedObject)
        {
            grabbedMovingObject = grabbedObject;
            lastObjectPos = grabbedObject.position;
            lastObjectRot = grabbedObject.rotation;
            SetClimbHand(controller);
        }

        public void SetClimbHand(ControllerInputActionManager controller)
        {
            ClimbingStarted();

            var stamina = controller.GetComponentInParent<HandReference>().Hand.GetComponent<ClimbingStamina>();
            stamina.Activate();
            stamina.OutOfStamina.AddListener(CancelClimbing);

            prevLocation = xrOrigin.transform.position;

            if (climbingHand)
                previousHand = climbingHand;

            climbingHand = controller;

            AdjustMoveSpeed();
        }

        public void RemoveClimbHand(ControllerInputActionManager controller)
        {
            var stamina = controller.GetComponentInChildren<ClimbingStamina>();
            stamina.Deactivate();
            stamina.OutOfStamina.RemoveListener(CancelClimbing);

            if (climbingHand == controller)
            {
                climbingHand = null;
                if (previousHand)
                {
                    climbingHand = previousHand;
                    previousHand = null;
                    CheckIfReturnToHand();
                }
            }

            if (previousHand == controller)
                previousHand = null;

            AdjustMoveSpeed();

            if (previousHand == null && climbingHand == null)
            {
                grabbedMovingObject = null;
                ClimbingEnded();
            }
        }

        public void CancelClimbing()
        {
            ClimbingEnded();

            if (previousHand)
            {
                var prevInteractor = previousHand.GetComponentInChildren<XRBaseInteractor>();
                if (prevInteractor && prevInteractor.interactablesSelected.Count > 0)
                {
                    var selectedInteractable = prevInteractor.interactablesSelected[0];
                    xrInteractionManager.SelectExit(prevInteractor, selectedInteractable);
                    Debug.Log("Released Prev Hand");
                }
            }

            if (climbingHand)
            {
                var climbInteractor = climbingHand.GetComponentInChildren<XRBaseInteractor>();
                if (climbInteractor && climbInteractor.interactablesSelected.Count > 0)
                {
                    var selectedInteractable = climbInteractor.interactablesSelected[0];
                    xrInteractionManager.SelectExit(climbInteractor, selectedInteractable);
                    Debug.Log("Released Climbing Hand");
                }
            }

            climbingHand = null;
            previousHand = null;
            grabbedMovingObject = null;
        }

        public void SetReleasedVelocity(Vector3 controllerVelocityCurrentSmoothedVelocity)
        {
            if (isClimbing)
                return;

            // ✅ Disable gravity for launch
            if (gravityProvider != null)
                gravityProvider.enabled = false;

            launchVelocity = controllerVelocityCurrentSmoothedVelocity * launchSpeedMultiplier;
        }

        #endregion

        #region Internal Climb Logic

        private void ClimbingStarted()
        {
            launchVelocity = Vector3.zero;
            isClimbing = true;

            if (gravityProvider != null)
                gravityProvider.enabled = false;

            if (!isLocomotionActive)
                TryPrepareLocomotion();
        }

        private void ClimbingEnded()
        {
            if (gravityProvider != null)
                gravityProvider.enabled = true;

            isClimbing = false;

            if (isLocomotionActive)
                TryEndLocomotion();

            if (overPosition != Vector3.zero)
                MoveToPositionWhenReleased();

            overPosition = Vector3.zero;
        }

        private void Climb()
        {
            var xrNode = GetClimbingHandNode();
            InputDevices.GetDeviceAtXRNode(xrNode)
                .TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 velocity);

            if (!isReturningPlayer && characterController)
            {
                characterController.Move(transform.rotation * -velocity * (Time.fixedDeltaTime * climbSpeed));
            }
        }

        private XRNode GetClimbingHandNode()
        {
            if (climbingHand == null)
                return XRNode.LeftHand;

            return climbingHand.GetComponentInParent<HandReference>().LeftRight == LeftRight.Left
                ? XRNode.LeftHand
                : XRNode.RightHand;
        }

        private void AdjustMoveSpeed() =>
            climbSpeed = previousHand ? twoHandClimbSpeed : oneHandClimbSpeed;

        private void CheckIfReturnToHand()
        {
            if (Vector3.Distance(xrOrigin.transform.position, prevLocation) >= returnDistance)
                StartCoroutine(ReturnToPrevHandPosition());
        }

        private IEnumerator ReturnToPrevHandPosition()
        {
            isReturningPlayer = true;

            float currentTimer = 0f;
            var startPosition = xrOrigin.transform.position;
            var goalPosition = prevLocation;

            while (currentTimer < returnAnimationLength)
            {
                float t = currentTimer / returnAnimationLength;
                xrOrigin.transform.position =
                    Vector3.Lerp(startPosition, goalPosition, returnToPlayerCurve.Evaluate(t));

                currentTimer += Time.deltaTime;
                yield return null;
            }

            isReturningPlayer = false;
        }

        private void MoveToPositionWhenReleased()
        {
            var heightAdjustment = xrOrigin.transform.up * xrOrigin.CameraInOriginSpaceHeight;
            var cameraDestination = overPosition + heightAdjustment;
            xrOrigin.MoveCameraToWorldLocation(cameraDestination);
        }

        private void ApplyMovingObjectDelta()
        {
            if (grabbedMovingObject == null || characterController == null)
                return;

            Vector3 currentPos = grabbedMovingObject.position;
            Vector3 deltaPos = currentPos - lastObjectPos;
            characterController.Move(deltaPos);
            lastObjectPos = currentPos;
        }

        private void RotateRigAroundPivot(Quaternion deltaRot)
        {
            Vector3 pivot = xrOrigin.Camera.transform.position;
            Vector3 rigPos = xrOrigin.transform.position;
            Vector3 offset = rigPos - pivot;

            offset = deltaRot * offset;
            xrOrigin.transform.position = pivot + offset;
            xrOrigin.transform.rotation = deltaRot * xrOrigin.transform.rotation;
        }

        #endregion
    }
}
