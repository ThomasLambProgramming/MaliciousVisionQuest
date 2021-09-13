﻿using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Malicious.Core
{
    public class Player : MonoBehaviour
    {
        #region Variables
        
        //Speed Variables//
        [SerializeField] private float _moveSpeed = 100f;
        [SerializeField] private float _maxSpeed = 4f;
        [SerializeField] private float _spinSpeed = 5f;
        //-------------------------------------//
        
        
        //Animator Variables//
        [SerializeField] private float _animationSwapSpeed = 3f;
        [SerializeField] private Animator _playerAnimator = null;
        private readonly int _animatorRunVariable = Animator.StringToHash("RunAmount");
        private readonly int _jumpingVariable = Animator.StringToHash("Jumping");
        private float _currentRunAmount = 0f;
        private float _prevRunAnimAmount = 0;
        //--------------------------------//
        
        
        //Input Variables//
        private Vector2 _moveInput = Vector2.zero;
        private Vector2 _spinInput = Vector2.zero;
        //-------------------------------------//
        
        
        //Jumping Variables//
        [SerializeField] private float _jumpForce = 10f;
        [SerializeField] private float _additionalGravity = -9.81f;
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField] private Transform _groundCheck = null;
        private bool _canJump = true;
        private bool _hasDoubleJumped = false;
        private bool _holdingJump = false;
        //--------------------------------//
        
        
        //IFrame Variables//
        private bool _isPaused = false;
        private bool _iFrameActive = false;
        [SerializeField] private float _iframeTime = 1.5f;
        [SerializeField] private GameObject _modelContainer = null;
        //--------------------------------//
        
        
        //Misc Variables That couldnt be grouped
        [SerializeField] private Transform _cameraTransform = null;
        private Rigidbody _rigidbody = null;
        private Vector3 _pauseEnterVelocity = Vector3.zero;
        private HackableField _currentHackableField = null;
        //--------------------------------//
        #endregion
        public void SetHackableField(HackableField a_field)
        {
            if (a_field == null)
            {
                _currentHackableField = null;
                return;
            }
            if (_currentHackableField != null)
            {
                float distanceToCurrent =
                    Vector3.SqrMagnitude(transform.position - _currentHackableField.transform.position);
                float distanceToNew =
                    Vector3.SqrMagnitude(transform.position - a_field.transform.position);
                if (distanceToNew < distanceToCurrent)
                    _currentHackableField = a_field;
            }
            else
                _currentHackableField = a_field;
        }
        public HackableField CurrentHackableField() => _currentHackableField;
        public Transform GiveOffset() => _cameraTransform;
        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _playerAnimator = _modelContainer.GetComponent<Animator>();
            _playerAnimator.SetFloat(_animatorRunVariable, 0);
            EnableInput();
            
            GameEventManager.GamePauseStart += PauseEnter;
            GameEventManager.GamePauseExit += PauseExit;

            _cameraTransform = Camera.main.transform;

            GameEventManager.PlayerUpdate += Tick;
            GameEventManager.PlayerFixedUpdate += FixedTick;
            _currentRunAmount = 0;

        }
        private void Tick()
        {
            UpdateAnimator();
        }
        private void FixedTick()
        {
            Movement();
            GroundCheck();
        }
        public void LaunchPlayer(Vector3 a_force)
        {
            _rigidbody.velocity = a_force;
        }
        public void OnHackEnter()
        {
            EnableInput();
            _moveInput = Vector2.zero;
            _spinInput = Vector2.zero;
            GameEventManager.PlayerUpdate += Tick;
            GameEventManager.PlayerFixedUpdate += FixedTick;
            _currentRunAmount = 0;
            _currentHackableField = null;
            gameObject.SetActive(true);
            CameraController.ChangeCamera(ObjectType.Player);
            _heldInputDown = false;
        }
        public void OnHackExit()
        {
            DisableInput();
            _moveInput = Vector2.zero;
            _spinInput = Vector2.zero;
            GameEventManager.PlayerUpdate -= Tick;
            GameEventManager.PlayerFixedUpdate -= FixedTick;
            gameObject.SetActive(false);
            _heldInputDown = false;
        }
        private void Movement()
        {
            if (_moveInput != Vector2.zero)
            {
                //For controller users this will change the max movespeed according to how small their inputs are
                float targetAngle = Mathf.Atan2(_moveInput.x, _moveInput.y) * Mathf.Rad2Deg +
                                    _cameraTransform.rotation.eulerAngles.y;
                
                //Rotate player towards current input
                Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
                transform.rotation =
                    Quaternion.Lerp(transform.rotation, targetRotation, _spinSpeed * Time.deltaTime);
                
                
                float scaleAmount = _moveInput.magnitude;
                
                float currentYAmount = _rigidbody.velocity.y;
                
                Vector3 newVel =
                    _cameraTransform.forward * (_moveInput.y * _moveSpeed * Time.deltaTime) +
                    _cameraTransform.right * (_moveInput.x * _moveSpeed * Time.deltaTime);
                
                //We are checking if the horizontal speed is too great 
                Vector3 tempVelocity = _rigidbody.velocity + newVel;
                tempVelocity.y = 0;

                float scaledMaxSpeed = _maxSpeed * scaleAmount;
                if (tempVelocity.magnitude > scaledMaxSpeed)
                {
                    tempVelocity = tempVelocity.normalized * scaledMaxSpeed;
                }

                tempVelocity.y = currentYAmount;
                _rigidbody.velocity = tempVelocity;
                
            }
            
            if (Mathf.Abs(_moveInput.magnitude) < 0.1f)
            {
                //if we are actually moving 
                if (Mathf.Abs(_rigidbody.velocity.x) > 0.2f || Mathf.Abs(_rigidbody.velocity.z) > 0.2f)
                {
                    Vector3 newVel = _rigidbody.velocity;
                    //takes off 5% of the current vel every physics update so the player can land on a platform without overshooting
                    //because the velocity doesnt stop
                    newVel.z = newVel.z * 0.90f;
                    newVel.x = newVel.x * 0.90f;
                    _rigidbody.velocity = newVel;
                }
            }

            Vector3 tempVel = _rigidbody.velocity;
            tempVel.y = 0;
            if (tempVel.sqrMagnitude < 0.1f)
            {
                _rigidbody.velocity = new Vector3(0, _rigidbody.velocity.y, 0);
            }

            if (!_holdingJump && _canJump == false)
            {
                _rigidbody.velocity = new Vector3(
                    _rigidbody.velocity.x,
                    _rigidbody.velocity.y + _additionalGravity * Time.deltaTime,
                    _rigidbody.velocity.z);
            }
        }
        public Quaternion GiveRotation()
        {
            return transform.rotation;
        }
        public Quaternion GiveCameraRotation()
        {
            return _cameraTransform.rotation;
        }
        #region Pausing
        private void PauseEnter()
        {
            _playerAnimator.enabled = false;
            _moveInput = Vector2.zero;
            DisableInput();
            _isPaused = true;
            _pauseEnterVelocity = _rigidbody.velocity;
            _rigidbody.isKinematic = true;
        }
        public void SetCameraTransform(Transform a_cameraTransform)
        {
            _cameraTransform = a_cameraTransform;
        }
        private void PauseExit()
        {
            _playerAnimator.enabled = true;
            EnableInput();
            _isPaused = false;
            _rigidbody.isKinematic = false;
            _rigidbody.velocity = _pauseEnterVelocity;
        }
        #endregion
        #region Jumping
        private void Jump()
        {
            //the 2 y velocity check is so the player can jump just before the arc of their jump
            if ((_canJump || _hasDoubleJumped == false) && _rigidbody.velocity.y < 2)
            {
                Vector3 prevVel = _rigidbody.velocity;
                prevVel.y = _jumpForce;
                _rigidbody.velocity = prevVel;
                if (_canJump == false)
                    _hasDoubleJumped = true;
                _canJump = false;
            }
        }
        private void GroundCheck()
        {
            //We only want to check when the player is actually falling (slight grace amount for when the player
            //is on the ground)
            if (_canJump)
                return;
            
            if (_rigidbody.velocity.y <= 0.3f)
            {
                Collider[] collisions = Physics.OverlapSphere(_groundCheck.position, 1f, _groundMask);
                if (collisions.Length > 0)
                {
                    bool collisionValid = false;
                    
                    for (int i = 0; i < collisions.Length - 1; i++)
                    {
                        Debug.Log(collisions[i].isTrigger);
                        if (collisions[i].isTrigger == false)
                        {
                            collisionValid = true;
                            
                            
                        }
                    }

                    if (collisionValid)
                    {
                        _canJump = true; 
                        _hasDoubleJumped = false;
                    }
                }
            }
        }
        #endregion
        private void UpdateAnimator()
        {
            Vector3 vel = _rigidbody.velocity;
            vel.y = 0;
            
            float animatorAmount = 0;
            if (vel.magnitude > 0)
                animatorAmount = vel.magnitude / _maxSpeed;

            _prevRunAnimAmount = Mathf.Lerp(_prevRunAnimAmount, animatorAmount, Time.deltaTime * _animationSwapSpeed);
            
            _playerAnimator.SetFloat(_animatorRunVariable, _prevRunAnimAmount);
        }
        #region Collisions
        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.CompareTag("Enemy") && _iFrameActive == false)
            {
                //player hit
                StartCoroutine(IFrame());
            }
        }
        #endregion
        private IEnumerator IFrame()
        {
            _iFrameActive = true;
            float timer = 0;
            int frameCount = 0;
            while (timer < _iframeTime)
            {
                if (_isPaused)
                {
                    //just to make sure when paused its not in inactive state
                    _modelContainer.SetActive(true);   
                    yield return null;
                }
                
                frameCount++;
                if (frameCount >= 20)
                {
                    frameCount = 0;
                    _modelContainer.SetActive(!_modelContainer.activeInHierarchy);
                }
                timer += Time.deltaTime;
                yield return null;
            }

            _modelContainer.SetActive(true);
            _iFrameActive = false;
        }
        #region Input

        private bool _heldInputDown = false;
        private void InteractionInputEnter(InputAction.CallbackContext a_context)
        {
            _heldInputDown = true;
            if (_currentHackableField != null)
            {
                _currentHackableField.HackInputStarted();
            }
        }
        private void InteractionInputExit(InputAction.CallbackContext a_context)
        {
            if (_currentHackableField != null && _heldInputDown)
            {
                _currentHackableField.HackInputStopped();
                _heldInputDown = false;
            }
        }
        private void JumpInputEnter(InputAction.CallbackContext a_context)
        {
            if (!_holdingJump)
            {
                Jump();
                _holdingJump = true;
            }
        }
        private void JumpInputExit(InputAction.CallbackContext a_context) => _holdingJump = false;
        private void PauseInputEnter(InputAction.CallbackContext a_context)
        {
            DisableInput();
            _playerAnimator.enabled = false;
            _moveInput = Vector2.zero;
            _isPaused = true;
            _pauseEnterVelocity = _rigidbody.velocity;
            _rigidbody.isKinematic = true;
        }
        private void PauseInputExit(InputAction.CallbackContext a_context)
        {
            EnableInput();
            _playerAnimator.enabled = true;
            _isPaused = false;
            _rigidbody.isKinematic = false;
            _rigidbody.velocity = _pauseEnterVelocity;
        }
        private void MoveInputEnter(InputAction.CallbackContext a_context)
        {
            _moveInput = a_context.ReadValue<Vector2>();
        }
        private void MoveInputExit(InputAction.CallbackContext a_context)
        {
            _moveInput = Vector2.zero;
        }
        private void CameraInputEnter(InputAction.CallbackContext a_context)
        {
            _spinInput = a_context.ReadValue<Vector2>();
        }
        private void CameraInputExit(InputAction.CallbackContext a_context)
        {
            _spinInput = Vector2.zero;
        }
        private void EnableInput()
        {
            GlobalData.InputManager.Player.Movement.performed += MoveInputEnter;
            GlobalData.InputManager.Player.Movement.canceled += MoveInputExit;
            GlobalData.InputManager.Player.Jump.performed += JumpInputEnter;
            GlobalData.InputManager.Player.Jump.canceled += JumpInputExit;
            GlobalData.InputManager.Player.Camera.performed += CameraInputEnter;
            GlobalData.InputManager.Player.Camera.canceled += CameraInputExit;
            GlobalData.InputManager.Player.Interaction.performed += InteractionInputEnter;
            GlobalData.InputManager.Player.Interaction.canceled += InteractionInputExit;
            //GlobalData.InputManager.Player.Down.performed += DownInputEnter;
            //GlobalData.InputManager.Player.Down.canceled += DownInputExit;
        }
        private void DisableInput()
        {
            GlobalData.InputManager.Player.Movement.performed -= MoveInputEnter;
            GlobalData.InputManager.Player.Movement.canceled -= MoveInputExit;
            GlobalData.InputManager.Player.Jump.performed -= JumpInputEnter;
            GlobalData.InputManager.Player.Jump.canceled -= JumpInputExit;
            GlobalData.InputManager.Player.Camera.performed -= CameraInputEnter;
            GlobalData.InputManager.Player.Camera.canceled -= CameraInputExit;
            GlobalData.InputManager.Player.Interaction.performed -= InteractionInputEnter;
            GlobalData.InputManager.Player.Interaction.canceled -= InteractionInputExit;
            //GlobalData.InputManager.Player.Down.performed -= DownInputEnter;
            //GlobalData.InputManager.Player.Down.canceled -= DownInputExit;
        }
        #endregion
    }
}