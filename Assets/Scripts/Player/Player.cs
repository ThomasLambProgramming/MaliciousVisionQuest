﻿using System;
using System.Collections;
using Malicious.Interfaces;
using Malicious.Core;

using UnityEngine.InputSystem;
using UnityEngine;

namespace Malicious.Player
{
    public class Player : MonoBehaviour, IPlayerObject
    {
        [SerializeField] private PlayerValues _values = new PlayerValues();
        [SerializeField] private float _iframeTime = 1.5f;
        
        [SerializeField] private GameObject _ModelContainer = null;
        private bool _invincible = false;
        
        private bool _paused = false;
        private void Start()
        {
            _values._rigidbody = GetComponent<Rigidbody>();
            _values._playerAnimator = GetComponent<Animator>();
            
            GameEventManager.GamePauseStart += PauseEnter;
            GameEventManager.GamePauseExit += PauseExit;
        }

        public void OnHackEnter()
        {
            EnableInput();
            _values._currentAnimationVector = Vector2.zero;
            gameObject.SetActive(true);
        }

        public void OnHackExit()
        {
            DisableInput();
            gameObject.SetActive(false);
        }

        public void Tick()
        {
            //Logic
            UpdateAnimator();
            SpinMovement();
        }

        public void FixedTick()
        {
            //Movement and etc
            Movement();
            HackValidCheck();
        }

        private Vector3 _prevVelocity = Vector3.zero;
        private void PauseEnter()
        {
            _values._playerAnimator.enabled = false;
            _values._moveInput = Vector2.zero;
            _values._spinInput = Vector2.zero;
            DisableInput();
            _paused = true;
            _prevVelocity = _values._rigidbody.velocity;
            _values._rigidbody.isKinematic = true;
        }

        private void PauseExit()
        {
            _values._playerAnimator.enabled = true;
            EnableInput();
            _paused = false;
            _values._rigidbody.isKinematic = false;
            _values._rigidbody.velocity = _prevVelocity;
        }

        private IEnumerator IFrame()
        {
                
            float timer = 0;
            int frameCount = 0;
            while (timer < _iframeTime)
            {
                if (_paused)
                {
                    //just to make sure when paused its not in inactive state
                    _ModelContainer.SetActive(true);   
                    yield return null;
                }
                
                frameCount++;
                if (frameCount >= 20)
                {
                    frameCount = 0;
                    _ModelContainer.SetActive(!_ModelContainer.activeInHierarchy);
                }
                timer += Time.deltaTime;
                yield return null;
            }

            _ModelContainer.SetActive(true);
            _invincible = false;
        }

        public void FanLaunch(Vector3 a_force)
        {
            _values._canJump = false;
            _values._hasDoubleJumped = false;
            Vector3 newVel = _values._rigidbody.velocity;
            newVel.x += a_force.x;
            newVel.z += a_force.z;
            //this is to ensure that the fan will always propel the player up (and reset the double jump)
            newVel.y = a_force.y;
            _values._rigidbody.velocity = newVel;
        }
        private void Movement()
        {
            if (_values._moveInput != Vector2.zero)
            {
                //For controller users this will change the max movespeed according to how small their inputs are
                float scaleAmount = _values._moveInput.magnitude;
                
                float currentYAmount = _values._rigidbody.velocity.y;
                
                Vector3 newVel =
                    transform.forward * (_values._moveInput.y * _values._moveSpeed * Time.deltaTime) +
                    transform.right * (_values._moveInput.x * _values._moveSpeed * Time.deltaTime);
                
                //We are checking if the horizontal speed is too great 
                Vector3 tempVelocity = _values._rigidbody.velocity + newVel;
                tempVelocity.y = 0;

                float scaledMaxSpeed = _values._maxSpeed * scaleAmount;
                if (tempVelocity.magnitude > scaledMaxSpeed)
                {
                    tempVelocity = tempVelocity.normalized * scaledMaxSpeed;
                    
                }

                tempVelocity.y = currentYAmount;
                _values._rigidbody.velocity = tempVelocity;
            }
            
            if (Mathf.Abs(_values._moveInput.magnitude) < 0.1f)
            {
                //if we are actually moving 
                if (Mathf.Abs(_values._rigidbody.velocity.x) > 0.2f || Mathf.Abs(_values._rigidbody.velocity.z) > 0.2f)
                {
                    Vector3 newVel = _values._rigidbody.velocity;
                    //takes off 5% of the current vel every physics update so the player can land on a platform without overshooting
                    //because the velocity doesnt stop
                    newVel.z = newVel.z * 0.90f;
                    newVel.x = newVel.x * 0.90f;
                    _values._rigidbody.velocity = newVel;
                }
            }
        }
        private void UpdateAnimator()
        {
            _values._currentAnimationVector = new Vector2(
                Mathf.Lerp(_values._currentAnimationVector.x, 
                    _values._moveInput.x, 
                    _values._animationSwapSpeed * Time.deltaTime),
                Mathf.Lerp(_values._currentAnimationVector.y, 
                    _values._moveInput.y, 
                    _values._animationSwapSpeed * Time.deltaTime));
        
            _values._playerAnimator.SetFloat(_values._xPos, _values._currentAnimationVector.x);
            _values._playerAnimator.SetFloat(_values._yPos, _values._currentAnimationVector.y);
        }
        private void SpinMovement()
        {
            if (_values._spinInput != Vector2.zero)
            {
                transform.Rotate(new Vector3(0, _values._spinInput.x * _values._spinSpeed * Time.deltaTime, 0));
            }
        }
        private void Jump()
        {
            _values._rigidbody.AddForce(_values._jumpForce * Vector3.up);
        }
        public void GroundCheck()
        {
            //Collider[] collisions = Physics.OverlapSphere(groundCheck.position, 0.5f, groundMask);
            //if (collisions.Length > 0)
            //{
            //    foreach (var collider in collisions)
            //    {
            //        if (collider.transform.CompareTag("Ground"))
            //        {
            //            ResetJump();
            //        }
            //    }
            //}
        }
        private void HackValidCheck()
        {
            if (_values._currentHackable != null)
            {
                if (DotCheck(transform, _values._currentHackableObj.transform))
                {
                    _values._canHackable = true;
                    _values._currentHackable.OnHackValid();
                }
                else
                {
                    _values._canHackable = false;
                    _values._currentHackable.OnHackFalse();
                }
            }

            if (_values._currentInteract != null)
            {
                if (DotCheck(transform, _values._currentInteractObj.transform))
                {
                    _values._canInteract = true;
                    _values._currentInteract.OnHackValid();
                }
                else
                {
                    _values._canInteract = false;
                    _values._currentInteract.OnHackFalse();
                }
            }
        }
        private bool DotCheck(Transform a_transform, Transform b_transform)
        {
            Vector3 direction = (b_transform.position - a_transform.position).normalized;
            if (Vector3.Dot(direction, a_transform.forward) > _values._dotAllowance)
            {
                return true;
            }

            return false;
        }

        //for all materials and other graphical changes when the player can hack
        public void OnHackValid(){}
        public void OnHackFalse(){}
        public ObjectType ReturnType() => ObjectType.Player;

        public OffsetContainer GiveOffset()
        {
            OffsetContainer temp = new OffsetContainer();
            temp._offsetTransform = _values._cameraOffset;
            temp._rigOffset = _values._rigOffset;
            return temp;
        }
        public void SetOffset(Transform a_transform) => _values._cameraOffset = a_transform;
        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.CompareTag("Hackable"))
            {
                if (!_invincible)
                {
                    PlayerController.PlayerControl.PlayerHit();
                    StartCoroutine(IFrame());
                    _invincible = true;
                }
            }
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Hackable"))
            {
                _values._currentInteract = other.GetComponent<IPlayerObject>();
                _values._currentInteractObj = other.gameObject;
            }
            else if (other.gameObject.CompareTag("Interactable"))
            {
                _values._currentHackable = other.GetComponent<IHackable>();
                _values._currentHackableObj = other.gameObject;
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Hackable"))
            {
                //Double check that the object gets set back to original state
                if (_values._currentInteract != null)
                    _values._currentInteract.OnHackFalse();
                
                _values._currentInteract = null;
                _values._currentInteractObj = null;
                _values._canInteract = false;
            }
            else if (other.gameObject.CompareTag("Interactable"))
            {
                //Double check that the object gets set back to original state
                if (_values._currentHackable != null)
                    _values._currentHackable.OnHackFalse();
                
                _values._currentHackable = null;
                _values._currentHackableObj = null;
                _values._canHackable = false;
            }
        }

        private void Interact(InputAction.CallbackContext a_context)
        {
            if (_values._currentInteract != null && _values._canInteract)
            {
                PlayerController.PlayerControl.SwapPlayer(_values._currentInteract);
            }
            else if (_values._currentHackable != null && _values._canHackable)
            {
                _values._currentHackable.Hacked();
            }
        }
        public bool RequiresTruePlayerOffset() => false;
        private void MoveInputEnter(InputAction.CallbackContext a_context) => _values._moveInput = a_context.ReadValue<Vector2>();
        private void MoveInputExit(InputAction.CallbackContext a_context) => _values._moveInput = Vector2.zero;
        private void SpinInputEnter(InputAction.CallbackContext a_context) => _values._spinInput = a_context.ReadValue<Vector2>();
        private void SpinInputExit(InputAction.CallbackContext a_context) => _values._spinInput = Vector2.zero;
        private void JumpInputEnter(InputAction.CallbackContext a_context) => Jump();
        private void JumpInputExit(InputAction.CallbackContext a_context) => _values._holdingJump = false;
            

        private void EnableInput()
        {
            GlobalData.InputManager.Player.Movement.performed += MoveInputEnter;
            GlobalData.InputManager.Player.Movement.canceled += MoveInputExit;
            GlobalData.InputManager.Player.Jump.performed += JumpInputEnter;
            GlobalData.InputManager.Player.Jump.canceled += JumpInputExit;
            GlobalData.InputManager.Player.Camera.performed += SpinInputEnter;
            GlobalData.InputManager.Player.Camera.canceled += SpinInputExit;
            GlobalData.InputManager.Player.Interaction.performed += Interact;
        }

        private void DisableInput()
        {
            GlobalData.InputManager.Player.Movement.performed -= MoveInputEnter;
            GlobalData.InputManager.Player.Movement.canceled -= MoveInputExit;
            GlobalData.InputManager.Player.Jump.performed -= JumpInputEnter;
            GlobalData.InputManager.Player.Jump.canceled -= JumpInputExit;
            GlobalData.InputManager.Player.Camera.performed -= SpinInputEnter;
            GlobalData.InputManager.Player.Camera.canceled -= SpinInputExit;
            GlobalData.InputManager.Player.Interaction.performed -= Interact;
        }
        
    }
}