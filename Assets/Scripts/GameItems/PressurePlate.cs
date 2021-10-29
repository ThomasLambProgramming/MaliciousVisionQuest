﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Malicious.GameItems
{
    public class PressurePlate : MonoBehaviour
    {
        [SerializeField] private LayerMask _mask;
        [SerializeField] private UnityEvent _OnEvent;
        [SerializeField] private UnityEvent _OffEvent;

        //This is for checking that objects are still in holding it down
        private List<GameObject> _containedObjects = new List<GameObject>();
        private void OnTriggerEnter(Collider other)
        {
            //the & checks if both masks have the same bit then give a resulting number
            //made out of the bits that both share if the result has any bits similar it will
            //return greater than 0
            if (other.isTrigger)
                return; 
            
            if ((_mask & (1 << other.gameObject.layer)) > 0)
            {
                _containedObjects.Add(other.gameObject);
                if (_containedObjects.Count == 1)
                {
                    //we only want to press it down when its the first object
                    //this is all done so multiple objects can be on it without causing issues
                    //or overlaps
                    _OnEvent?.Invoke();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.isTrigger)
                return;

            if (_containedObjects.Contains(other.gameObject))
            {
                _containedObjects.Remove(other.gameObject);
                if (_containedObjects.Count <= 0)
                {
                    _OffEvent?.Invoke();
                }
            }
        }
        #if UNITY_EDITOR
        [ContextMenu("RUN ON EVENT")]
        public void RunON()
        {
            _OnEvent?.Invoke();
        }
        #endif
    }
}