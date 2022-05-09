//-----------------------------------------------------------------------
// <copyright file="Slingshot.cs" company="Google">
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
    using UnityEngine.XR.ARSubsystems;
    using ElRaccoone.Tweens;
    using ElRaccoone.Tweens.Core;

    public class Slingshot : MonoBehaviour
    {

        private Camera _cam;

        private float _slingshotOnscreenYPos = 0;
        public float yDistToOffscreen = 0.35f;
        
        private float _slingshotOnscreenXRot = 0;
        public float xOffscreenRot = 30f;

        private Vector2? _touchDownPos = null;

        private float _pullbackPercent = 0;
        public float pullbackPercent { get { return _pullbackPercent; }}

        private bool _isShooting = false;
        public bool isShooting { get { return _isShooting; }}

        private Vector3 _pelletLocalOrigin;
        public Vector3 pelletLocalOrigin { get { return _pelletLocalOrigin; }}

        public Transform pelletTransform;
        public PelletShot pelletShot;

        public Transform shakeTransform;
        public SkinnedMeshRenderer slingshotRenderer;
        public SpriteRenderer reticleRenderer;

        private Vector3 _originalSlingshotRendLocalPos;
        private float _slingshotLocalYOffset;
        public float slingshotLocalYOffsetMultiplier = -0.13f;

        private Tween<float> _leftRightTween = null;
        private float _leftRightPercentOffset = 0;

        public float distAheadOffset = 0.5f;

        /// <summary>
        /// Unity Awake() Function, called before Start()
        /// </summary>
        void Awake()
        {
            _cam = GetComponentInParent<Camera>();

            _pelletLocalOrigin = pelletTransform.localPosition;
            _slingshotOnscreenYPos = this.transform.localPosition.y;
            _slingshotOnscreenXRot = this.transform.eulerAngles.x;
            
            // TODO: Create a stack of spare shots
            pelletShot = CreatePelletShot(pelletTransform);
            _originalSlingshotRendLocalPos = slingshotRenderer.transform.localPosition;

            slingshotRenderer.enabled = false;
            reticleRenderer.enabled = false;
            pelletTransform.gameObject.SetActive(false);
        }

        /// <summary>
        /// Unity Start() Function, called before the first frame update
        /// </summary>
        void Start()
        {
            this.UpdatePullbackVisuals();
            this.PositionSlingshotInFrontOfCamera();
        }

        // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

        private float _screenAspect = 1f;
        private float _distAhead = 1.84f;

        /// <summary>
        /// Calculates and sets the correct position of the Slingshot 
        /// in front of the camera
        /// </summary>
        private void PositionSlingshotInFrontOfCamera() {
            _screenAspect = (float)Screen.height / (float)Screen.width;
            float perc = _screenAspect / _cam.fieldOfView;
            
            this._distAhead = Mathf.Atan(perc * 100f) - distAheadOffset;

            _slingshotLocalYOffset = _screenAspect * slingshotLocalYOffsetMultiplier + 0.25f;

            Vector3 localPos = this.transform.localPosition;
            localPos.z = this._distAhead * _screenAspect;
            _slingshotOnscreenYPos = this._originalSlingshotRendLocalPos.y + _slingshotLocalYOffset;
            this.transform.localPosition = localPos;
        }

        // ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

        /// <summary>
        /// Creates a PelletShot to be shot into the world
        /// </summary>
        /// <returns>
        /// A new Pellet shot to send into the world
        /// </returns>
        /// <param name="origTransform">The original transform of the Pellet we are duplicating
        static PelletShot CreatePelletShot(Transform origTransform) {
            Transform pelletWorldTransform = Instantiate(origTransform, origTransform.position, origTransform.rotation, origTransform.parent);
            pelletWorldTransform.localScale = origTransform.localScale;
            pelletWorldTransform.parent = null;
            pelletWorldTransform.name = "WORLD PELLET";
            // pelletWorldTransform.gameObject.SetActive(false);
            pelletWorldTransform.gameObject.layer = 7; // "pellet"
            pelletWorldTransform.transform.position = new Vector3(999,999,999);

            return pelletWorldTransform.gameObject.AddComponent<PelletShot>();
        }

        /// <summary>
        /// An event sent from BalloonPopController when EarthTracking changes
        /// </summary>
        /// <param name="newTrackingState">The latest tracking state
        public void EarthAnchorsTrackingStateChanged(TrackingState newTrackingState) {
            bool isVisible = newTrackingState == TrackingState.Tracking;

            slingshotRenderer.enabled = isVisible;
            reticleRenderer.enabled = isVisible;
            pelletTransform.gameObject.SetActive(isVisible);
        }

        //   #####  ####### ####### ####### ### #     #  #####   #####  
        //  #     # #          #       #     #  ##    # #     # #     # 
        //  #       #          #       #     #  # #   # #       #       
        //   #####  #####      #       #     #  #  #  # #  ####  #####  
        //        # #          #       #     #  #   # # #     #       # 
        //  #     # #          #       #     #  #    ## #     # #     # 
        //   #####  #######    #       #    ### #     #  #####   #####  

        /// <summary>
        /// An event sent from the UI when the Play mode changes
        /// isBuildOrPlay=true; means the user is in Build Mode
        /// </summary>
        /// <param name="modeIsBuild">Create balloons mode, or Play mode
        /// <param name="shouldAnimate">Should we animate the slingshot?
        public void IsBuildOrPlayChanged(bool modeIsBuild, bool shouldAnimate) {
            float destY = modeIsBuild 
                            ? _slingshotOnscreenYPos - yDistToOffscreen 
                            : _slingshotOnscreenYPos;

            float destLocalXRot = modeIsBuild ? xOffscreenRot : _slingshotOnscreenXRot;

            if (!modeIsBuild) {
                slingshotRenderer.enabled = true;
                reticleRenderer.enabled = true;
                pelletTransform.gameObject.SetActive(true);
            }

            if (!shouldAnimate) {
                Vector3 destPos = this.transform.localPosition;
                destPos.y = destY;
                this.transform.localPosition = destPos;
                
                Vector3 destRot = this.transform.localEulerAngles;
                destRot.x = destLocalXRot;
                this.transform.localRotation = Quaternion.Euler(destRot);

            } else {
                // ANIMATE
                float animDuration = 0.6f;
                EaseType ease = modeIsBuild ? EaseType.ExpoIn : EaseType.BackOut;

                // Animate the slingshot on/off-screen
                this.transform
                    .TweenLocalPositionY(destY, animDuration)
                    .SetEase(ease);

                // Animate the rotation too...
                ease = modeIsBuild ? EaseType.ExpoIn : EaseType.ExpoOut;
                this.transform.TweenLocalRotationX(destLocalXRot, animDuration)
                // this.transform.TweenLocalRotation(destRot, animDuration)
                    .SetEase(ease);
            }
        }

        // --------------------------------------------

        //  ### #     # ######  #     # ####### 
        //   #  ##    # #     # #     #    #    
        //   #  # #   # #     # #     #    #    
        //   #  #  #  # ######  #     #    #    
        //   #  #   # # #       #     #    #    
        //   #  #    ## #       #     #    #    
        //  ### #     # #        #####     #    

        /// <summary>
        /// Static method to return the slingshot pullback percent from the 
        /// touch y-difference
        /// </summary>
        /// <param name="yDelta">The difference from the touch down position
        private static float PullbackPercentWithYDelta(float yDelta) {
            return Mathf.Max(0, Mathf.Min(1f, yDelta * 0.002f));
        }

        // --------------------------------------------

        /// <summary>
        /// TouchDown callback from SlingshotTouchResponder
        /// </summary>
        /// <param name="pos">Latest touch position
        public void SlingshotUITouchDown(Vector2 pos) {
            if (_isShooting) return;
            _touchDownPos = pos;
        }
        
        /// <summary>
        /// TouchMoved callback from SlingshotTouchResponder
        /// </summary>
        /// <param name="pos">Latest touch position
        public void SlingshotUITouchMoved(Vector2 pos) {
            if (_touchDownPos == null) return;

            Vector2 delta = (Vector2)_touchDownPos - pos;
            
            float percentOfScreen = delta.x / (Screen.width * 0.3f);
            float deltaX = Mathf.Clamp(-percentOfScreen, -1f, 1f);

            // float deltaX = Mathf.Clamp(-delta.x * 0.005f, -1f, 1f);
            this.SetLeftRightAimingPercent(deltaX);
            
            // Holding the SLING
            this.SetPullbackPercent(Slingshot.PullbackPercentWithYDelta(delta.y));
        }
        /// <summary>
        /// TouchEnded callback from SlingshotTouchResponder
        /// </summary>
        /// <param name="pos">Latest touch position
        public void SlingshotUITouchEnded(Vector2 pos) {
            if (_touchDownPos == null) return;

            this.PerformShot();

            _touchDownPos = null;
        }

        // --------------------------------------------

        //  #     # ### ####### #     # 
        //  #     #  #  #       #  #  # 
        //  #     #  #  #       #  #  # 
        //  #     #  #  #####   #  #  # 
        //   #   #   #  #       #  #  # 
        //    # #    #  #       #  #  # 
        //     #    ### #######  ## ##  

        /// <summary>
        /// Update the blendshapes on the slingshot mesh using _pullbackPercent.
        /// Also update the position of the pellet
        /// </summary>
        private void UpdatePullbackVisuals() {
            // Pullback
            this.slingshotRenderer.SetBlendShapeWeight(0, 
                Mathf.Max(0,_pullbackPercent) * 150f);
            // Follow-through
            this.slingshotRenderer.SetBlendShapeWeight(1, 
                -Mathf.Min(0,_pullbackPercent) * 150f);

            // Update the position of the pellet as well
            this.pelletTransform.localPosition = _pelletLocalOrigin 
                + new Vector3(0, 0, -_pullbackPercent * 13.2f);
        }

        /// <summary>
        /// This function should be called to set the pullbackPercent 
        /// of the slingshot
        /// </summary>
        /// <param name="percent">Pullback percent of the slingshot
        private void SetPullbackPercent(float percent) {
            _pullbackPercent = percent;
            
            UpdatePullbackVisuals();

            if (this.pelletTransform.gameObject.activeInHierarchy) return;
            this.pelletTransform.gameObject.SetActive(false);
        }

        /// <summary>
        /// The slingshot can be aimed left and right, this function does that!
        /// </summary>
        /// <param name="percent">A value of -1 aims 100% to the left, +0.5 aims 50% to the right
        private void SetLeftRightAimingPercent(float percent) {
            
            Vector3 eulerRot = this.transform.localRotation.eulerAngles;
            if (_leftRightTween != null) {
                _leftRightTween.Cancel();
                _leftRightTween = null;
                _leftRightPercentOffset = (eulerRot.y > 180f ? eulerRot.y-360f : eulerRot.y) * 0.1f;
                // Debug.Log("_leftRightPercentOffset: " + _leftRightPercentOffset + ", eulerRot: " + eulerRot);
            }
            
            float smoothedPercent = _leftRightPercentOffset +
                                    (Mathf.Sign(-percent) * 
                                    0.5f * (1f - Mathf.Cos(percent * Mathf.PI)));
            
            eulerRot.y = Mathf.Clamp(smoothedPercent, -1f, 1f) * 6f;   
            this.transform.localRotation = Quaternion.Euler(eulerRot);
        }

        /// <summary>
        /// Shoot the slingshot
        /// </summary>
        private void PerformShot() {
            _isShooting = true;
            AnimateShot();

            this.UpdateSlingshotShake(0);
        }

        // @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        // @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@

        //     #    #     # ### #     # 
        //    # #   ##    #  #  ##   ## 
        //   #   #  # #   #  #  # # # # 
        //  #     # #  #  #  #  #  #  # 
        //  ####### #   # #  #  #     # 
        //  #     # #    ##  #  #     # 
        //  #     # #     # ### #     # 
                                
        /// <summary>
        /// The user let go of a pulled back slingshot, so take the shot!
        /// </summary>
        private void AnimateShot() {
            float shotSpeed = Mathf.Max(0.2f, this._pullbackPercent);
            // Debug.Log($"shotSpeed: {shotSpeed}");
            float originalPullback = this._pullbackPercent;

            float destination = -shotSpeed;

            this.TweenValueFloat(destination, duration:0.1f, 
                (float updateVal) => {
                    _pullbackPercent = updateVal;
                    this.UpdatePullbackVisuals();
                }).SetFrom(this._pullbackPercent)
                .SetEaseLinear()
                .SetOnComplete(() => {
                    // SHOOT THE WORLD PELLET
                    this.pelletShot.gameObject.SetActive(true);
                    this.pelletShot.removeAllForces();
                    this.pelletShot.transform.position = this.pelletTransform.position;
                    this.pelletShot.transform.rotation = this.pelletTransform.rotation;
                    this.pelletTransform.gameObject.SetActive(false);

                    this.pelletShot.ShootWithSpeedAtCurrentRotation(shotSpeed * 0.4f);

                    AnimateSlingToRest(shotSpeed);
                });
        }

        /// <summary>
        /// Animate the slingshot to the resting or idle position
        /// </summary>
        /// <param name="shotSpeed">Shot speed
        void AnimateSlingToRest(float shotSpeed) {
            float destination = 0f;
            float duration = shotSpeed * 0.5f;

            this.TweenValueFloat(destination, duration, 
                (float updateVal) => {
                    _pullbackPercent = updateVal;
                    this.UpdatePullbackVisuals();
                }).SetFrom(this._pullbackPercent)
                .SetEaseElasticOut()
                .SetOnComplete(SlingToRestAnimationComplete);
        }

        /// <summary>
        /// Animate the left/right aiming back to rest.
        /// Also reset any variables concerned with shooting and animation
        /// allowing the user to take another shot
        /// </summary>
        private void SlingToRestAnimationComplete() {
            // Ease the Left/Right rotation back
            Vector3 eulerRot = this.transform.localRotation.eulerAngles;
            eulerRot.y = 0;
            // Ease the Left/Right rotation back
            _leftRightTween = this.transform.TweenLocalRotationY(0, duration: 1.6f)
                .SetEaseExpoInOut()
                .SetOnComplete(() => {
                    // Debug.Log("ANIM COMPLETE");
                    _leftRightTween = null;
                    _leftRightPercentOffset = 0;
                });

            this._pullbackPercent = 0;
            this.UpdatePullbackVisuals();
            this.pelletTransform.gameObject.SetActive(true);
            
            _isShooting = false;
        }

        /// <summary>
        /// Update the Slingshot shake, based on the pullbackPercent.
        /// It's hard to hold an elastic, the further it has been pulled.
        /// </summary>
        private void UpdateSlingshotShake(float magnitude) {
            float rotMod = 10f;
            shakeTransform.localRotation = Quaternion.Euler(new Vector3(
                Random.Range(-1f, 1f) * magnitude * rotMod,
                Random.Range(-1f, 1f) * magnitude * rotMod,
                Random.Range(-1f, 1f) * magnitude * rotMod
            ));
        }
        
        // @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        // @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        // @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        // @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@

        // Update is called once per frame
        void Update() {
            if (this._pullbackPercent > 0) this.UpdateSlingshotShake(this._pullbackPercent * 0.01f);
        }
    }
}