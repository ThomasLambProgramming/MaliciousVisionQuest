using System;
using System.Collections;
using System.Collections.Generic;
using Malicious.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Malicious.Hackable
{
    public class FlyingEnemy : BasePlayer
    {
        [SerializeField] private List<Vector3> _flightPath = new List<Vector3>();
        [SerializeField] private int _pathIndex = 0;
        private int direction = 1;
        [SerializeField] private float _goNextDistance = 4;
        [SerializeField] private float _maxTurningSpeed = 10f;
        
        [SerializeField] private float _playerMaxTurnSpeed = 0;
        [SerializeField] private float _playerRotateSpeed = 0;

        private GameObject _playerObject = null;
        private float _sqrMaxTurningSpeed = 0;
        // Start is called before the first frame update
        void Start()
        {
            GameEventManager.EnemyFixedUpdate += AiUpdate;
            _rigidbody = GetComponent<Rigidbody>();
            _sqrMaxTurningSpeed = _maxTurningSpeed * _maxTurningSpeed;
            
        }

        void AiUpdate()
        {
            if (_flightPath.Count == 0)
                return;
            
            Vector3 directionToTarget = _flightPath[_pathIndex] - transform.position;
            if (Vector3.SqrMagnitude(directionToTarget) > _goNextDistance)
            {
                Vector3 desiredVelocity = Vector3.Normalize(directionToTarget) * _maxSpeed;
                Vector3 steeringForce = desiredVelocity - _rigidbody.velocity;

                if (steeringForce.sqrMagnitude > _sqrMaxTurningSpeed)
                {
                    steeringForce = steeringForce.normalized * _maxTurningSpeed;
                }

                _rigidbody.velocity += steeringForce * Time.deltaTime;
                
                if (_rigidbody.velocity.magnitude > _maxSpeed)
                {
                    _rigidbody.velocity = _rigidbody.velocity.normalized * _maxSpeed;
                }
                Quaternion lookDirection = Quaternion.LookRotation(_rigidbody.velocity.normalized);
                
                transform.rotation = Quaternion.RotateTowards(transform.rotation, lookDirection, _maxTurningSpeed);
            }
            else
            {
                if (direction == 1)
                {
                    if (_pathIndex < _flightPath.Count - 1)
                        _pathIndex++;
                    else
                    {
                        direction = -1;
                        _pathIndex--;
                    }
                }
                else
                {
                    if (_pathIndex > 0)
                        _pathIndex--;
                    else
                    {
                        direction = 1;
                        _pathIndex++;
                    }
                }
            }
        }

        protected override void Tick()
        {
        }

        protected override void FixedTick()
        {
            if (_playerObject != null && 
                Vector3.SqrMagnitude(transform.position - _playerObject.transform.position) > 9)
            {
                _playerObject.transform.parent = null;
                _playerObject = null;
            }
            if (_moveInput != Vector2.zero)
            {
                if (Mathf.Abs(_moveInput.x) > 0.1f)
                {
                    transform.Rotate(0,_moveInput.x * _playerRotateSpeed * Time.deltaTime,0);

                    if (_rigidbody.velocity.magnitude > 1)
                    {
                        Vector3 desiredVelocity = Vector3.zero;
                        if (_moveInput.y > 0)
                            desiredVelocity = transform.forward * _maxSpeed;
                        else if (_moveInput.y < 0)
                            desiredVelocity = -transform.forward * _maxSpeed;
                        else
                        {
                            Vector3 _rigidBodyVelDirection = _rigidbody.velocity.normalized;
                            float _dotForward = Vector3.Dot(_rigidBodyVelDirection, transform.forward);
                            float _dotBackward = Vector3.Dot(_rigidBodyVelDirection, -transform.forward);

                            if (_dotForward > _dotBackward)
                                desiredVelocity = transform.forward * _maxSpeed;
                            else
                                desiredVelocity = -transform.forward * _maxSpeed;
                        } 
                        
                        
                        Vector3 steeringForce = desiredVelocity - _rigidbody.velocity;

                        if (steeringForce.magnitude > _playerMaxTurnSpeed)
                            steeringForce = steeringForce.normalized * _playerMaxTurnSpeed;

                        _rigidbody.velocity += steeringForce;
                        if (_rigidbody.velocity.magnitude > _maxSpeed)
                        {
                            _rigidbody.velocity = _rigidbody.velocity.normalized * _maxSpeed;
                        }
                    }
                }

                if (Mathf.Abs(_moveInput.y) > 0.1f)
                {
                    _rigidbody.velocity += transform.forward * (_moveInput.y * Time.deltaTime * _maxSpeed);
                    
                    if (_rigidbody.velocity.magnitude > _maxSpeed)
                        _rigidbody.velocity = _rigidbody.velocity.normalized * _maxSpeed;
                }
            }

            if (_moveInput.x == 0)
                _rigidbody.angularVelocity = Vector3.zero;
        }

        public override void OnHackEnter()
        {
            base.OnHackEnter();
            GameEventManager.EnemyFixedUpdate -= AiUpdate;
            CameraController.ChangeCamera(ObjectType.FlyingEnemy, _cameraTransform);
        }

        public override void OnHackExit()
        {
            base.OnHackExit();
            GameEventManager.EnemyFixedUpdate += AiUpdate;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                other.gameObject.transform.parent = transform;
                _playerObject = other.gameObject;
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                other.gameObject.transform.parent = null;
                _playerObject = null;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            foreach (var point in _flightPath)
            {
                Gizmos.DrawSphere(point, 0.3f);
            }
        }
        #endif
    }
}
