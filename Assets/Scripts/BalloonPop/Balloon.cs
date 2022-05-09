//-----------------------------------------------------------------------
// <copyright file="Balloon.cs" company="Google">
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
    using System;
    using System.Collections;
    // using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using ElRaccoone.Tweens;
    using ElRaccoone.Tweens.Core;
    using TMPro;

    /// <summary>
    /// A UnityEvent that passes a balloon
    /// </summary>
    [Serializable] public class BalloonEvent : UnityEvent <Balloon> { }

    /// <summary>
    /// A Balloon component lives inside the GoogleFloorBalloon Prefab
    /// and manages balloon behaviour
    /// </summary>
    public class Balloon : MonoBehaviour, BalloonDataVisualChangesListener
    {
        /// <summary>
        /// The blue color of the cloud particles when a balloon is popped by the current user
        /// </summary>
        static Color BlueCloudColor = new Color(26f/255f, 115f/255f, 232f/255f);

        /// <summary>
        /// The red color of the cloud particles when a balloon is popped by another user
        /// </summary>
        static Color RedCloudColor = new Color(222f/255f, 82f/255f, 70f/255f);
        // ====================================================================
        
        /// <summary>
        /// The Balloon fade start distance (from the user camera)
        /// </summary>
        static float BALLOON_FADE_MIN_DIST_TO_CAM = 10f;

        /// <summary>
        /// The Balloon fade end distance (from the user camera)
        /// </summary>
        static float BALLOON_FADE_MAX_DIST_TO_CAM = 14f;

        /// <summary>
        /// The Balloon fade range
        /// </summary>
        static float DIST_TO_CAM_RANGE = BALLOON_FADE_MAX_DIST_TO_CAM - BALLOON_FADE_MIN_DIST_TO_CAM;

        /// <summary>
        /// The average/estimated height of the user camera
        /// (So a balloon can be placed on the floor using the height of the camera)
        /// </summary>
        static public float ESTIMATED_CAM_HEIGHT_FROM_FLOOR = 1.3f;
        // ====================================================================

        /// <summary>
        /// The root transform the the Balloon
        /// </summary>
        public Transform balloonRoot;
        
        /// <summary>
        /// A Balloon Popped event
        /// </summary>
        [NonSerialized] public BalloonEvent balloonWasPopped = new BalloonEvent();

        /// <summary>
        /// Pointer to the cloud particle system
        /// </summary>
        public ParticleSystem cloudParticles;
        
        /// <summary>
        /// The colliders to detect if a pellet should pop this balloon
        /// </summary>
        private SphereCollider[] _sphereColliders;

        /// <summary>
        /// Pointer to the balloon at the end of the string
        /// </summary>
        public Transform balloonTransform;

        /// <summary>
        /// Pointer to the string/wire holding the balloon to the ground
        /// </summary>
        public Transform stringTransform;
        
        /// <summary>
        /// The radial timer UI showing a countdown until the next balloon inflates
        /// </summary>
        public MeshRenderer inflationTimerRenderer = null;

        /// <summary>
        /// MaterialPropertyBlock for the inflationTimerRenderer
        /// </summary>
        private MaterialPropertyBlock _inflationTimerProps;

        // ====================================================================
        
        /// <summary>
        /// MeshRenderers to fade all of the balloon in and out
        /// </summary>
        public MeshRenderer[] RenderersToFade;

        /// <summary>
        /// MaterialPropertyBlock for the RenderersToFade
        /// </summary>
        private MaterialPropertyBlock _balloonPropBlock;

        /// <summary>
        /// The Base Plate MeshRenderer
        /// </summary>
        public MeshRenderer BasePlateMeshRenderer;

        /// <summary>
        /// The Balloon String MeshRenderer
        /// </summary>
        public MeshRenderer BalloonStringMeshRenderer;
        // ====================================================================

        // ====================================================================
        
        /// <summary>
        /// The next Time.time to inflate the balloon
        /// </summary>
        private float _nextInflateTime = 0;

        /// <summary>
        /// The next duration until inflation should occur
        /// </summary>
        private float _nextCooldownDuration = 0;

        /// <summary>
        /// A flag to store if the balloon is currentl inflated or not
        /// </summary>
        private bool _isInflated = false;
        public bool IsInflated { get { return _isInflated; } }

        /// <summary>
        /// A pointer to the BalloonData object
        /// </summary>
        private BalloonData _Data = null;
        public BalloonData Data { get { return _Data; } }
        // ====================================================================

        /// <summary>
        /// A flag for the debug canvas visibility
        /// </summary>
        private bool _debugCanvasVisible = false;

        /// <summary>
        /// A pointer to the debugText (TextMeshPro)
        /// </summary>
        public TextMeshProUGUI debugText;
        // ====================================================================

        /// <summary>
        /// Unity Awake() method
        /// </summary>
        void Awake()
        {
            balloonWasPopped = new BalloonEvent();

            _Data = new BalloonData();
            _Data.SetVisualChangesListener(this);

            _inflationTimerProps = new MaterialPropertyBlock();
            _balloonPropBlock = new MaterialPropertyBlock();
            SetVisible(false);
        }

        /// <summary>
        /// Unity Awake() method
        /// Start is called before the first frame update
        /// </summary>
        void Start()
        {
            _sphereColliders = GetComponents<SphereCollider>();
            // _meshRend = GetComponent<MeshRenderer>();

            inflationTimerRenderer.transform.localScale = new Vector3 (0, 0, 0);

            _inflationTimerProps.SetFloat("_Frac", 0);
            inflationTimerRenderer.SetPropertyBlock(_inflationTimerProps);

            _balloonPropBlock.SetColor("_Color", Color.white);
            foreach (var rend in RenderersToFade) {
                rend.SetPropertyBlock(_balloonPropBlock);
            }

            if (!Application.isEditor) {
                this.SetBalloonHeight(0.02f);
                this.SetBalloonInflationPercent(0);
            }

            // TESTING
            // StartCoroutine(InflateAfter(1f));
        }

        /// <summary>
        /// Set if the balloon is visible or not
        /// </summary>
        void SetVisible(bool isVisible) {
            foreach (MeshRenderer rend in RenderersToFade) {
                rend.enabled = isVisible;
            }
        }

        /// <summary>
        /// Set if the balloon is visible or not after some delay
        /// </summary>
        public void SetVisibleAfterDelay(bool isVisible, float delay) {
            StartCoroutine(SetVisibleCoroutine(isVisible, delay));
        }
        
        /// <summary>
        /// Set if the balloon is visible or not Coroutine (after delay)
        /// </summary>
        IEnumerator SetVisibleCoroutine(bool isVisible, float delay) {
            yield return new WaitForSeconds(delay);
            this.SetVisible(isVisible);
        }

        /// <summary>
        /// Set the transparency of the balloon
        /// </summary>
        void setBalloonFadeOutAlpha(float alpha) {
            alpha = Mathf.Max(0.3f, alpha);
            _balloonPropBlock.SetColor("_Color", new Color(1, 1, 1, alpha));

            foreach (var rend in RenderersToFade) {
                rend.SetPropertyBlock(_balloonPropBlock);
            }

            _balloonPropBlock.SetColor("_Color", 
                new Color(244f/255f, 129f/255f, 0, alpha)); // Base plate colour
            if (BasePlateMeshRenderer != null) {
                BasePlateMeshRenderer.SetPropertyBlock(_balloonPropBlock);
            }

            Color clampedAlphaColor = new Color(1, 1, 1, Mathf.Min(0.64f, alpha));

            _balloonPropBlock.SetColor("_Color", clampedAlphaColor);
            if (BalloonStringMeshRenderer != null) {
                BalloonStringMeshRenderer.SetPropertyBlock(_balloonPropBlock);
            }

            clampedAlphaColor = new Color(1, 1, 1, Mathf.Min(0.5f, alpha));

            _inflationTimerProps.SetColor("_Color", clampedAlphaColor);
            if (inflationTimerRenderer != null) {
                inflationTimerRenderer.SetPropertyBlock(_inflationTimerProps);
            }
        }

        /// <summary>
        /// Unity Update method
        /// Update is called once per frame
        /// </summary>
        void Update()
        {
            // setBalloonFadeOutAlpha(Mathf.Sin(Time.time));

            // Create a subtle wind effect on the balloons using PerlinNoise
            float scaleMod = 5f + (25f * Mathf.Clamp(Mathf.PerlinNoise(this.transform.position.z, this.transform.position.x), 0, 1f));
            float timeModified = Time.time * 0.2f;
            float rot1 = -0.5f + Mathf.PerlinNoise(this.transform.position.x * 0.1f, timeModified);
            float rot2 = -0.5f + Mathf.PerlinNoise(this.transform.position.z * 0.1f, timeModified);
            this.transform.parent.localRotation = Quaternion.Euler(rot1 * scaleMod, 0, rot2 * scaleMod);

            if (!Application.isEditor) {
                // Make sure the position and scale of the balloon is correct
                Vector3 p = this.balloonRoot.localPosition;
                p.x = 0;
                p.z = 0;
                // Leave yPos alone for the SetBalloonWorldYPos() method
                this.balloonRoot.localPosition = p;
            }

            // balloonRoot.localPosition = Vector3.zero;
            balloonRoot.localScale = Vector3.one;

            if (_isInflated || _nextInflateTime < 1) return;
            if (Time.time > _nextInflateTime) {
                _nextInflateTime = 0;
                _nextCooldownDuration = 0;

                _inflationTimerProps.SetFloat("_Frac", 1.0f);
                inflationTimerRenderer.SetPropertyBlock(_inflationTimerProps);

                this.PerformInflate();
            } else {
                float percent = 1f - Mathf.Max(0, 
                                        Mathf.Min(
                                            1f, (_nextInflateTime - Time.time) / _nextCooldownDuration
                                        )
                                    );

                _inflationTimerProps.SetFloat("_Frac", percent);
                inflationTimerRenderer.SetPropertyBlock(_inflationTimerProps);
            }
        }

        /// <summary>
        /// Update the Balloon YPosition and transparency 
        /// based on the distance from the camera
        /// </summary>
        /// <param name="camYPosWorld">The world y position of the camera</param>
        /// <param name="distToCamera">The distance of this balloon to the camera</param>
        /// <param name="camPos">The world position of the camera</param>
        /// <param name="adjustFade">Should the fade of this Balloon be adjusted?</param>
        public void UpdateBalloonCamYPosFadeAndDistToCamera(
            float camYPosWorld, float distToCamera, Vector3 camPos, bool adjustFade) {

            float distPercent = Mathf.Max(0, Mathf.Min(1f, 
                                    (distToCamera - BALLOON_FADE_MIN_DIST_TO_CAM) / DIST_TO_CAM_RANGE
                                ));
            // distPercent = 0.0 (when the Balloon is close to the camera)
            // distPercent = 1.0 (when the Balloon is far from the camera)
            float contribution = (1f - distPercent);

            if (adjustFade) this.setBalloonFadeOutAlpha(contribution);

            Vector3 p = this.balloonRoot.position;

            // Set the height of the balloon at some percentage between 
            // the camera height and the anchor's natural altitude
            // p.y = contribution * (camYPosWorld - ESTIMATED_CAM_HEIGHT_FROM_FLOOR);

            // Always set the height to the estimated floor height based on the camera
            p.y = (camYPosWorld - ESTIMATED_CAM_HEIGHT_FROM_FLOOR);

            this.balloonRoot.position = p;

            // --------------------------------------------------
            if (!DebugSettings.Shared.DisplayBalloonDebug) return;
            
            GameObject canvasGO = debugText.transform.parent.parent.gameObject;
            if (!_debugCanvasVisible && !canvasGO.activeSelf) {
                _debugCanvasVisible = true;
                StartCoroutine(ShowDebugCanvas(0.1f));
            }

            // UPDATE THE DEBUG TEXT
            float angle = -90f + (-Mathf.Rad2Deg * Mathf.Atan2(camPos.z - debugText.transform.parent.parent.position.z, 
                                            camPos.x - debugText.transform.parent.parent.position.x));
            debugText.transform.parent.parent.rotation = Quaternion.Euler(0, angle, 0);

            float strLength = this._Data != null ? this._Data.balloon_string_length : 0;
            float numPopped = this._Data != null ? this._Data.num_popped : 0;

            debugText.text = string.Format("Dist to cam: {0:00.00}m\nmin:{1}m, max:{2}m\n", distToCamera, BALLOON_FADE_MIN_DIST_TO_CAM, BALLOON_FADE_MAX_DIST_TO_CAM) + 
                             string.Format("Fade:{0:0.0} ", contribution) +
                             string.Format("Rope: {0:0.0}", strLength);
        }

        /// <summary>
        /// Show or hide the Debug canvas on the Balloon
        /// </summary>
        /// <param name="delay">Delay time</param>
        public IEnumerator ShowDebugCanvas(float delay) {
            yield return new WaitForSeconds(delay);
            GameObject canvasGO = debugText.transform.parent.parent.gameObject;
            canvasGO.SetActive(true);
        }

        // ==============================================

        /// <summary>
        /// Set a new BalloonData object
        /// </summary>
        /// <param name="newBalloonData">The new BalloonData object</param>
        public void SetBalloonData(BalloonData newBalloonData) {
            _Data = newBalloonData;
            _Data.SetVisualChangesListener(this);

            // TODO: Update visuals...
            System.DateTimeOffset poppedUntil = _Data.PoppedUntilDate;
            System.DateTimeOffset now = System.DateTimeOffset.Now;
            if (poppedUntil > now) {
                _nextCooldownDuration = Mathf.Max((float)BalloonsNetworking.BALLOON_POP_COOLDOWN_SECS, 
                                                (float)(poppedUntil - System.DateTimeOffset.Now).TotalSeconds);
                _nextInflateTime = Time.time + _nextCooldownDuration;
                _isInflated = false;
                // TODO: Disable Balloon renderer and string

            }

            // Check if the balloon is currently in a popped state...
        }

        // ==============================================
        // BalloonDataVisualChangesListener

        /// <summary>
        /// An notification from the attached BalloonData object
        /// telling the balloon that a change in the data has occured
        /// that requires a visual change
        /// </summary>
        /// <param name="oldPoppedUntil">Old popped until datetime</param>
        /// <param name="poppedUntil">New popped until datetime</param>
        /// <param name="poppedByUserID">New popped-by userID</param>
        /// <param name="curUserID">The current user ID</param>
        public void VisualChangeBalloonPopped(
            System.DateTimeOffset oldPoppedUntil, 
            System.DateTimeOffset poppedUntil, 
            string poppedByUserID, string curUserID) {

            // Debug.Log("VisualChangeBalloonPopped");

            if (poppedByUserID == curUserID) {
                Debug.Log("The balloon was popped by the CURRENT USER!");
            } else {
                PerformPopAsOtherPlayer();
            }

            System.DateTimeOffset nowDate = System.DateTimeOffset.Now;

            if (_isInflated) {
                // IS INFLATED
                if (poppedUntil <= nowDate) {
                    // Debug.Log($"poppedUntil <= nowDate. poppedUntil: {poppedUntil}. nowDate: {nowDate}");
                    // Inlfate NOW, poppedUntil is in the past or NOW
                    _nextInflateTime = 0;
                    _nextCooldownDuration = 0;
                    this.PerformInflate();
                }
            } else {
                // IS POPPED
                if (poppedUntil > nowDate) {
                    // System.TimeSpan durationFromNow = poppedUntil - System.DateTimeOffset.Now;
                    _nextCooldownDuration = Mathf.Max((float)BalloonsNetworking.BALLOON_POP_COOLDOWN_SECS, 
                                                      (float)(poppedUntil - System.DateTimeOffset.Now).TotalSeconds);
                    _nextInflateTime = Time.time + _nextCooldownDuration;
                    // Debug.Log($"poppedUntil > nowDate _nextCooldownDuration: {_nextCooldownDuration}. poppedUntil: {poppedUntil}. nowDate: {nowDate}");
                    animateInflationTimerInOrOut(animIn: true);
                }
            }
        }

        // ==============================================
        // Inflation timer

        /// <summary>
        /// Animate the radial timer UI in or out
        /// </summary>
        /// <param name="animIn">Animate it in or out?</param>
        void animateInflationTimerInOrOut(bool animIn) {
            float finalScale = animIn ? 0.75f : 0;
            Vector3 endScale = new Vector3(finalScale, finalScale, 1f);

            if (inflationTimerRenderer == null) return;

            EaseType ease = animIn ? EaseType.BounceOut : EaseType.ExpoIn;
            inflationTimerRenderer
                .TweenLocalScale(endScale, 0.5f)
                .SetEase(ease);
        }

        // ==============================================

        /// <summary>
        /// Set the height of the balloon as it inflates and rises.
        /// This sets the string height and yPosition of the balloon and ParticleSystem
        /// </summary>
        /// <param name="height">The height </param>
        private void SetBalloonHeight(float height = 2f) {
            Vector3 localPos = this.transform.localPosition;
            localPos.y = height;
            this.transform.localPosition = localPos;

            localPos = this.stringTransform.localScale;
            localPos.y = height * 0.5f;
            this.stringTransform.localScale = localPos;

            localPos = cloudParticles.transform.localPosition;
            localPos.y = height + 0.28f; // 0.28f takes it to the middle of the balloon
            cloudParticles.transform.localPosition = localPos;
        }

        /// <summary>
        /// Set the inflation percent and update the scale of the balloon
        /// </summary>
        /// <param name="percent">The percent to fully inflated</param>
        private void SetBalloonInflationPercent(float percent) {
            this.transform.localScale = Vector3.one * percent;
        }

        /// <summary>
        /// Perform the necessary animations for inflation
        /// </summary>
        private void PerformBalloonInflationAnimation() {

            float finalHeight = _Data.balloon_string_length;
            float animDuration = finalHeight * 2f;
            EaseType ease = EaseType.CubicInOut;
            // Ease ease = Ease.InOutCubic;

            // Balloon Height anim
            Tween<float> balloonTween = this.transform
                                .TweenLocalPositionY(finalHeight, animDuration)
                                .SetEase(ease);

            // Particle System Tween
            Tween<float> psTween = this.cloudParticles
                                .TweenLocalPositionY(finalHeight + 0.28f, animDuration)
                                .SetEase(ease);

            // Balloon string anim
            Tween<float> stringAnim = this.stringTransform
                                .TweenLocalScaleY(finalHeight * 0.5f, animDuration)
                                .SetEase(ease);

            // Balloon scale anim
            float scaleAnim = animDuration * 0.3f;
            this.transform.TweenLocalScale(Vector3.one, scaleAnim);

            StartCoroutine(SetCollidersActive(scaleAnim + 0.3f, true));
        }

        /// <summary>
        /// Inflate this balloon!
        /// </summary>
        public void PerformInflate() {
            if (_isInflated) return;

            animateInflationTimerInOrOut(animIn:false);

            this.SetBalloonHeight(0.02f);
            this.SetBalloonInflationPercent(0);

            this.balloonTransform.gameObject.SetActive(true);
            this.stringTransform.gameObject.SetActive(true);

            PerformBalloonInflationAnimation();

            _isInflated = true;
        }

        /// <summary>
        /// Inflate after delay coroutine
        /// </summary>
        /// <param name="delay">Delay time</param>
        public IEnumerator InflateAfter(float delay) {
            yield return new WaitForSeconds(delay);
            this.PerformInflate();
        }

        /// <summary>
        /// Activate the colliders
        /// </summary>
        /// <param name="delay">Activation delay</param>
        /// <param name="active">Should be active or deactive</param>
        public IEnumerator SetCollidersActive(float delay, bool active) {
            yield return new WaitForSeconds(delay);
            for (int i = 0; i < _sphereColliders.Length; i++) {
                _sphereColliders[i].enabled = true;
            }
        }

        // ############################################

        /// <summary>
        /// Set the colour of the pop clouds
        /// </summary>
        /// <param name="col">The new pop cloud colour</param>
        public void SetCloudsColor(Color col) {
            var system = cloudParticles.main;
            system.startColor = col;
        }

        /// <summary>
        /// Perform a balloon pop from an 'other' player
        /// </summary>
        public void PerformPopAsOtherPlayer() {
            if (!_isInflated) return;

            PerformPopWithColor(Balloon.RedCloudColor);

            _nextCooldownDuration = (float)BalloonsNetworking.BALLOON_POP_COOLDOWN_SECS;
            _nextInflateTime = Time.time + _nextCooldownDuration;

            animateInflationTimerInOrOut(animIn:true);
        }

        /// <summary>
        /// Perform a normal balloon pop from the current user
        /// </summary>
        void PerformPopAsCurrentUser() {
            if (!_isInflated) return;

            PerformPopWithColor(Balloon.BlueCloudColor);
            balloonWasPopped.Invoke(this);
        }

        /// <summary>
        /// Perform a balloon pop
        /// </summary>
        /// <param name="cloudColor">The color of the pop clouds</param>
        void PerformPopWithColor(Color cloudColor) {
            _isInflated = false;

            SetCloudsColor(cloudColor);
            cloudParticles.Play();
            
            StartCoroutine(SetCollidersActive(0,false));
            StartCoroutine(HideBalloonAndString(0.15f));

            // The server will notify us about the nextInflationTime
            // _nextInflateTime = Time.time + CooldownUntilInflation;
            _nextInflateTime = 0;
        }

        /// <summary>
        /// Hides the balloon and string when the pop clouds cover their visibility
        /// </summary>
        /// <param name="delay">Delay until hiding</param>
        public IEnumerator HideBalloonAndString(float delay) {
            yield return new WaitForSeconds(delay);
            this.balloonTransform.gameObject.SetActive(false);
            this.stringTransform.gameObject.SetActive(false);
        }

        /// <summary>
        /// A balloon collider was triggered by a pellet, so pop this balloon!
        /// </summary>
        /// <param name="other">The other collider</param>
        void OnTriggerEnter(Collider other) {
            PerformPopAsCurrentUser();
        }
    }
}