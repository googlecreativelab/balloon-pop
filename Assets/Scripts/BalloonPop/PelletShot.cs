//-----------------------------------------------------------------------
// <copyright file="PelletShot.cs" company="Google">
//
// Copyright 2022 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace Google.CreativeLab.BalloonPop 
{
    using UnityEngine;

    [RequireComponent(typeof (Rigidbody))]
    [RequireComponent(typeof (SphereCollider))]
    public class PelletShot : MonoBehaviour
    {
        private Rigidbody _rigidBody = null;
        private SphereCollider _sphereCollider = null;
        
        // private bool _isAirborne = false;
        private float _speed = 0;

        /// <summary>
        /// Start is called before the first frame update
        /// </summary>
        void Start()
        {
            _sphereCollider = GetComponent<SphereCollider>();
            _rigidBody = GetComponent<Rigidbody>();
            _rigidBody.useGravity = true;
            _rigidBody.drag = 1f;
            _rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            _sphereCollider.radius = transform.localScale.x * 3.8f;
        
        }

        /// <summary>
        /// Remove any forces from the Pellet's Rigidbody
        /// </summary>
        public void removeAllForces() {
            if (_rigidBody == null) return;
            _rigidBody.velocity = Vector3.zero;
        }

        /// <summary>
        /// Perform a shot with the provided speedPercent at the Pellet's current rotation
        /// </summary>
        /// <param name="speedPercent">A speed to perform the pellet shot with</param>
        public void ShootWithSpeedAtCurrentRotation(float speedPercent) {
            if (_rigidBody == null) return;

            // _isAirborne = true;
            _speed = 50f * speedPercent;

            Vector3 force = transform.forward * _speed;

            _rigidBody.AddForce(force, ForceMode.Impulse);
        }

    }
}