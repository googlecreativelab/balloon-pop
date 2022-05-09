//-----------------------------------------------------------------------
// <copyright file="BalloonEarthController.cs" company="Google">
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
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Events;
    using UnityEngine.XR.ARFoundation;
    using UnityEngine.XR.ARSubsystems;
    using Google.XR.ARCoreExtensions;

    /// <summary>
    /// An event called when the number of Balloons changes
    /// </summary>
    [Serializable] public class BalloonCountEvent : UnityEvent <int> { }

    /// <summary>
    /// An event called when the EarthTracking State changes
    /// </summary>
    [Serializable] public class EarthTrackingEvent : UnityEvent <TrackingState> { }

    /// <summary>
    /// A struct that stores an ARGeospatialAnchor
    /// and the Balloon Monobehaviour component
    /// </summary>
    public struct BalloonAnchor {
        public ARGeospatialAnchor anchor;
        public Balloon balloon;
        public BalloonAnchor(ARGeospatialAnchor a, Balloon b) {
            this.anchor = a;
            this.balloon = b;
        }
    }

    /// <summary>
    /// Controls the BalloonPop example.
    /// </summary>
    [RequireComponent(typeof(ARSessionOrigin), typeof(ARAnchorManager))]
    public class BalloonPopController : MonoBehaviour
    {
        /// <summary>
        /// The ARCoreExtensions component used in this scene.
        /// </summary>
        public ARCoreExtensions ARCoreExtensions;

        /// <summary>
        /// The ARSession used in the example.
        /// </summary>
        public ARSession SessionCore;

        /// <summary>
        /// A bool to determine if ARTracking is currently active
        /// </summary>
        private bool _trackingIsActive = false;

        /// <summary>
        /// Assign the Unity Camera in the Editor
        /// </summary>
        public Camera CameraUnity;

        /// <summary>
        /// An object that contains logic to communicate with the server
        /// that manages a database of balloons
        /// </summary>
        private BalloonsNetworking _network;

        /// <summary>
        /// The model used to visualize anchors.
        /// </summary>
        public GameObject AnchorVisObjectPrefab;

        /// <summary>
        /// The last created anchors.
        /// </summary>
        private List<BalloonAnchor> _anchors;

        /// <summary>
        /// An event called when the list of Anchors is changed
        /// </summary>
        public BalloonCountEvent BalloonAnchorCountChanged;

        /// <summary>
        /// How long to wait between each MapTile Check
        /// </summary>
        static float MAP_TILE_MONITORING_CHECK_INTERVAL = 1.0f;

        /// <summary>
        /// The MapTile that is currently being monitored on the network
        /// </summary>
        private Mercator.MapTile? _monitoringMapTile = null;

        /// <summary>
        /// The next time to check if the user has moved to a different map tile
        /// </summary>
        float _nextMapTileCheckTime = float.MaxValue;
        
        /// <summary>
        /// The active ARSessionOrigin
        /// </summary>
        private ARSessionOrigin _sessionOrigin;

        /// <summary>
        /// The active AREarthManager
        /// </summary>
        private AREarthManager _earthManager;

        /// <summary>
        /// The active ARAnchorManager
        /// </summary>
        private ARAnchorManager _anchorManager;

        /// <summary>
        /// The last recorded EarthManager TrackingState
        /// </summary>
        private TrackingState _lastEarthTrackingState;
        
        /// <summary>
        /// An event invoked when the EarthManager TrackingState has changed
        /// </summary>
        public EarthTrackingEvent EarthTrackingStateChanged;

        // --------------------------
        // DEBUG

        /// <summary>
        /// A UI Text object that will receive the Earth status text.
        /// </summary>
        public Text EarthStatusTextUI;

        /// <summary>
        /// A UI Text object that will receive feedback to the user.
        /// </summary>
        public Text FeedbackTextUI;

        /// <summary>
        /// UI element to display <see cref="ARSessionState"/>.
        /// </summary>
        public Text SessionState;

        /// <summary>
        /// UI element to display <see cref="ARGeospatialAnchor"/> status.
        /// </summary>
        public Text EarthAnchorText;
        // --------------------------

#if UNITY_IOS
        /// <summary>
        /// Unity OnEnabled method.
        /// </summary>
        public void OnEnable()
        {
            Debug.Log("Start location services.");
            Input.location.Start();
        }

        /// <summary>
        /// Unity OnDisable method.
        /// </summary>
        public void OnDisable()
        {
            Debug.Log("Stop location services.");
            Input.location.Stop();
        }
#endif // UNITY_IOS

        /// <summary>
        /// The Unity Awake() method.
        /// </summary>
        public void Awake()
        {
            _anchors = new List<BalloonAnchor>();

            UpdateFeedbackText("", error:false);

            _sessionOrigin = GetComponent<ARSessionOrigin>();
            if (_sessionOrigin == null)
            {
                Debug.LogError("Failed to find ARSessionOrigin.");
                UpdateFeedbackText("Failed to find ARSessionOrigin", error:true);
            }

            _anchorManager = GetComponent<ARAnchorManager>();
            if (_anchorManager == null)
            {
                Debug.LogError("Failed to find ARAnchorManager.");
                UpdateFeedbackText("Failed to find ARAnchorManager", error:true);
            }

            GameObject earthManagerGO = new GameObject("AREarthManager", typeof(AREarthManager));
            _earthManager = earthManagerGO.GetComponent<AREarthManager>();
            if (_earthManager == null)
            {
                Debug.LogError("Failed to initialize AREarthManager");
                UpdateFeedbackText("Failed to initialize AREarthManage", error:true);
            }

            _network = new BalloonsNetworking();
            _network.FirebaseReadyChangedEvent += FirebaseReadyStateChanged;

            _lastEarthTrackingState = TrackingState.None;
        }

        /// <summary>
        /// Unity Monobehaviour Start method
        /// </summary>
        void Start() {
            SessionState.gameObject.SetActive(DebugSettings.Shared.DisplayEarthDebug);
            EarthAnchorText.gameObject.SetActive(
                DebugSettings.Shared.DisplayEarthDebug && 
                _earthManager != null);
            FeedbackTextUI.transform.parent.gameObject.SetActive(
                DebugSettings.Shared.DisplayEarthDebug);
        }

        /// <summary>
        /// Called when Firebase is ready
        /// </summary>
        private void FirebaseReadyStateChanged(bool firebaseReady) {
            // Debug.Log($"FirebaseReadyStateChanged: {firebaseReady}");
            if (!firebaseReady) return;

            // Only SetupBalloonMonitoring once EarthTracking has started
            if (Application.isEditor) {
                this.SetupBalloonNearbyMonitoring();
                UpdateFeedbackText($"Started balloon monitoring. Total anchors: {_anchors.Count}", false);
            }
        }

        /// <summary>
        /// Start the ARSession and AR functions
        /// </summary>
        public void SetPlatformActive(bool active)
        {
            _trackingIsActive = active;
            _sessionOrigin.enabled = active;
            SessionCore.gameObject.SetActive(active);
            ARCoreExtensions.gameObject.SetActive(active);
        }

        // ------------------------------------------------------------------        

        /// <summary>
        /// isBuildOrPlay=true; means the user is in Build Mode
        /// </summary>
        public void IsBuildOrPlayChanged(bool isBuildOrPlay, bool shouldAnimate) {
            // _listenForTouches = isBuildOrPlay;
        }

        /// <summary>
        /// Delete all the BalloonAnchors around the user.
        /// Updates Firestore if it is available
        /// </summary>
        public void DeleteAllBalloonsInTheUsersLocalArea() {
            // Make sure we can get an accurate CameraEarthPose
            if (_earthManager == null || _earthManager.EarthTrackingState != TrackingState.Tracking) return;
            
            GeospatialPose geoPose = _earthManager.CameraGeospatialPose;
            double lat = geoPose.Latitude;
            double lng = geoPose.Longitude;
        
            _network.DeleteBalloonsAroundUser(
                lat, lng, 
                (errString) => {
                    Debug.Log("DeleteBalloonsAroundUser Err:" + errString);
                    UpdateFeedbackText($"DeleteBalloonsAroundUser Err:" + errString, 
                        error:errString != null);
                    if (errString != null) return;

                    this.DeleteAllBallonAnchors();
                });
        }

        // =======================================================

        //  #     # ####### ####### #     # ####### ######  #    # 
        //  ##    # #          #    #  #  # #     # #     # #   #  
        //  # #   # #          #    #  #  # #     # #     # #  #   
        //  #  #  # #####      #    #  #  # #     # ######  ###    
        //  #   # # #          #    #  #  # #     # #   #   #  #   
        //  #    ## #          #    #  #  # #     # #    #  #   #  
        //  #     # #######    #     ## ##  ####### #     # #    # 
                                                        
        /// <summary>
        /// Called when there are changes received from Firestore about a Balloon entity
        /// </summary>
        void UpdateOrCreateBalloonChangeFromServer(BalloonChange balloonChange, int changeNumber) {
            object balloonID = "";
            if (!balloonChange.Dict.TryGetValue("balloon_id", out balloonID)) {
                Debug.LogWarning("NO BALLOON ID");
                UpdateFeedbackText($"NO BALLOON ID. Total anchors: {_anchors.Count}", error:true);
                return;
            }

            object lat = "";
            object lng = "";
            if (!balloonChange.Dict.TryGetValue("latitude", out lat) ||
                !balloonChange.Dict.TryGetValue("longitude", out lng)) return;
            Mercator.GeoCoordinate balloonChangeCoord = new Mercator.GeoCoordinate((double)lat, (double)lng);
            
            string bIDStr = (string)balloonID;
            foreach (BalloonAnchor ba in _anchors) {
                // Check if the current user just created this balloon
                bool createdByUser = ba.balloon.Data.user_id == _network.CurrentUserID;
                bool currentUserProbablyJustCreated = 
                    (createdByUser && 
                     ba.balloon.Data.balloon_id.Length < 1 && 
                     ba.balloon.Data.coordinate.GetDistanceTo(balloonChangeCoord) < 0.01);

                if (bIDStr == ba.balloon.Data.balloon_id || currentUserProbablyJustCreated) {
                    // BALLOON FOUND!
                    if (balloonChange.ChangeType == Firebase.Firestore.DocumentChange.Type.Added) {
                        if (!createdByUser) {
                            UpdateFeedbackText($"Balloon Added NOT from curUser. Total anchors: {_anchors.Count}", error:true);
                        }
                        // Balloon ADDED! But we already have it :\
                        // This could happen when the user moves between MapTiles
                    }
                    else if (balloonChange.ChangeType == Firebase.Firestore.DocumentChange.Type.Modified) {
                        // Debug.Log("Balloon Updated");
                        ba.balloon.Data.UpdateWithDict(balloonChange.Dict, _network.CurrentUserID);
                    }
                    else if (balloonChange.ChangeType == Firebase.Firestore.DocumentChange.Type.Removed) {
                        // Debug.Log("Balloon Deleted");
                        Destroy(ba.anchor.gameObject);
                        _anchors.Remove(ba);
                    }
                    
                    return;
                }
            }

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // No balloon was found, so create one
            // - - - - - - - - - - - - - - - - - -
            Debug.Log("Creating Balloon Anchor");
            StartCoroutine(CreateBalloonAnchorCoroutine(balloonChange.Dict, changeNumber));
        }

        /// <summary>
        /// A function to create a BalloonAnchor on a Coroutine.
        /// Used so the game doesn't lag when loading an initial set of 
        /// balloons from Firestore
        /// </summary>
        public IEnumerator CreateBalloonAnchorCoroutine(Dictionary<string, object> balloonDict, int changeNumber) {
            yield return new WaitForSeconds((float)changeNumber * 0.01f);
            
            Debug.Log("Creating Balloon Anchor");
            BalloonData newBalloonDat = new BalloonData(balloonDict);
    
            // Debug.Log("Earth.TrackingState: " + Earth.TrackingState);
            yield return null; // Wait for one frame

            double lat = newBalloonDat.latitude;
            double lng = newBalloonDat.longitude;
            double alt = newBalloonDat.altitude;
            Quaternion anchorRot = Quaternion.AngleAxis(0, new Vector3(0.0f, 1.0f, 0.0f));

            ARGeospatialAnchor newAnchor = _anchorManager.AddAnchor(lat, lng, alt, anchorRot);

            if (newAnchor != null || Application.isEditor)
            {
                yield return null; // Wait for one frame
                BalloonAnchor newBA = CreateNewBalloonAnchor(newAnchor, newBalloonDat);
                newBA.balloon.PerformInflate();
            } else {
                Debug.LogWarning("Anchor not created successfully");                
                UpdateFeedbackText($"Anchor not created successfully. Total anchors: {_anchors.Count}", error:true);
            }
            UpdateFeedbackText($"Created balloon. Total anchors: {_anchors.Count}", false);
        }

        /// <summary>
        /// Setup realtime monitoring on the Firestore database
        /// for changes in balloons around the user location
        /// </summary>
        void SetupBalloonNearbyMonitoring() {
            if (_earthManager == null || _earthManager.EarthTrackingState != TrackingState.Tracking) return;

            Debug.Log("SetupBalloonNearbyMonitoring");
            
            GeospatialPose geoPose = _earthManager.CameraGeospatialPose;

            double lat = geoPose.Latitude;
            double lng = geoPose.Longitude;
            
            this.SetupBalloonNearbyMonitoring(lat, lng);
        }

        /// <summary>
        /// Setup realtime monitoring on the Firestore database
        /// for changes in balloons around given latitude and longitude
        /// </summary>
        void SetupBalloonNearbyMonitoring(double lat, double lng) {

            Mercator.MapTile userTile = Mercator.GetTileAtLatLng(lat, lng, BalloonsNetworking.MAP_TILE_MONITORING_ZOOM);
            List<Mercator.MapTile>userTiles = Mercator.GetSurroundingTileList(userTile, includeCenterTile:true);
            this.DeleteAllBallonAnchorsOutsideMapTiles(userTiles);

            this._nextMapTileCheckTime = Time.time + MAP_TILE_MONITORING_CHECK_INTERVAL;
            this._monitoringMapTile = 
                _network.MonitorBalloonsNearby(lat, lng, 
                (List<BalloonChange> balloonChangeList) => {
                    // Make sure monitoring is active. 
                    // _MonitoringMapTile could be null if monitoring was turned off, 
                    // and an async worker is still sending notifications to listeners
                    if (this._monitoringMapTile == null) return;

                    // UpdateFeedbackText($"Received {balloonChangeList.Count} Balloon changes. Total anchors: {_anchors.Count}", false);
                    int changeNumber = 1;
                    balloonChangeList.ForEach((BalloonChange balloonChange) => {
                        UpdateOrCreateBalloonChangeFromServer(balloonChange, changeNumber);
                        ++changeNumber;
                    });
                });
        }

        /// <summary>
        /// Stop the network Balloon monitoring
        /// </summary>
        void StopBalloonNearbyMonitoring() {
            this._monitoringMapTile = null;
            this._nextMapTileCheckTime = float.MaxValue;
            this._network.StopMonitoringBalloonsNearby();
            // this.DeleteAllBallonAnchors();
        }

        /// <summary>
        /// Called from UpdateBalloonMonitoringState() - which is called from the Unity Update() loop... 
        /// Periodically checks if the MapTile we are monitoring is still the same as the MapTile inhabited by the user.
        /// </summary>
        void UpdateMonitoringMapTile() {
            
            if (_earthManager == null || 
                _earthManager.EarthTrackingState != TrackingState.Tracking || 
                this._monitoringMapTile == null || 
                Time.time > this._nextMapTileCheckTime) return;
            // - - - - - - - - - -
            
            GeospatialPose geoPose = _earthManager.CameraGeospatialPose;
            double lat = geoPose.Latitude;  
            double lng = geoPose.Longitude;

            Mercator.MapTile userTile = Mercator.GetTileAtLatLng(lat, lng, BalloonsNetworking.MAP_TILE_MONITORING_ZOOM);
            if (!userTile.Equals((Mercator.MapTile)this._monitoringMapTile)) {
                // User is in a DIFFERENT MapTile. Restart monitoring!
                UpdateFeedbackText($"User moved to new MapTile. Restarting Monitoring!", error:true);
                
                // Function below will...
                //   nullify this._MonitoringMapTile
                //   stop the networking monitoring
                this.StopBalloonNearbyMonitoring();

                // Function below will set...
                //   this._MonitoringMapTile
                //   this._NextMapTileCheckTime
                //   start the networking monitoring
                this.SetupBalloonNearbyMonitoring(lat, lng);
            } else {
                this._nextMapTileCheckTime = Time.time + MAP_TILE_MONITORING_CHECK_INTERVAL;
            }
        }

        // ------------------------------------------------------------------

        

        // ========================================================

        //     #    #     #  #####  #     # ####### ######   #####  
        //    # #   ##    # #     # #     # #     # #     # #     # 
        //   #   #  # #   # #       #     # #     # #     # #       
        //  #     # #  #  # #       ####### #     # ######   #####  
        //  ####### #   # # #       #     # #     # #   #         # 
        //  #     # #    ## #     # #     # #     # #    #  #     # 
        //  #     # #     #  #####  #     # ####### #     #  #####  

        /// <summary>
        /// Destroy all balloon anchors and clear the list of anchors
        /// </summary>                           
        public void DeleteAllBallonAnchors() {
            for (int i = 0; i < _anchors.Count; i++) {
                Destroy(_anchors[i].anchor.gameObject);
            }
            _anchors.Clear();
            this.BalloonAnchorCountChanged.Invoke(_anchors.Count);
        }

        /// <summary>
        /// Destroy all balloon anchors (and delete them from the list of anchors)
        /// if that do not reside inside a MapTile provided in the list
        /// </summary>
        /// <param name="mapTiles">A list of MapTiles to check if balloons are inside of</param>
        public void DeleteAllBallonAnchorsOutsideMapTiles(List<Mercator.MapTile> mapTiles) {
            int balloonsDeleted = 0;
            for (int i = 0; i < _anchors.Count; i++) {
                BalloonAnchor newBA = _anchors[i];
                // Get the MapTile the current balloon resides inside
                Mercator.MapTile balloonMapTile = Mercator.GetTileAtLatLng(
                    newBA.balloon.Data.coordinate, 
                    BalloonsNetworking.MAP_TILE_MONITORING_ZOOM);
                
                // Check if any MapTile from the mapTiles list is the same as the balloonMaptTile
                bool balloonIsInsideAMapTile = false;
                for (int j = 0; j < mapTiles.Count; j++) {
                    if (!balloonMapTile.Equals(mapTiles[j])) continue; // Keep lookin'
                    balloonIsInsideAMapTile = true;
                    break;
                }

                // If the balloon is inside a map tile, move onto the next balloon...
                if (balloonIsInsideAMapTile) continue;
                // If the balloon resides in none of the MapTiles provided, Delete it.
                Destroy(newBA.anchor.gameObject);
                _anchors.RemoveAt(i);
                // Causing the next for-loop iteration 
                // to have the use the same index as this iteration
                i = i - 1; 
                ++balloonsDeleted;
            }
            if (balloonsDeleted > 0)
            {
                this.BalloonAnchorCountChanged.Invoke(_anchors.Count);
            }
            UpdateFeedbackText($"Deleted {balloonsDeleted} balloons outside MapTiles", error:true);
        }

        public void PlaceAnchorAtCurrentPosition() {
            this.PlaceAnchorAtCurrentPosition(distAheadOfCamera: 3.0);
        }

        /// <summary>
        /// Create a balloon anchor some distance away from the camera's facing angle
        /// </summary>
        /// <param name="distAheadOfCamera">The distance ahead of the camera to place a balloon anchor (default: 3m)</param>
        public void PlaceAnchorAtCurrentPosition(double distAheadOfCamera = 3.0) {
            if (_earthManager == null) return;

            TrackingState trackingState = _earthManager.EarthTrackingState;
            if (trackingState != TrackingState.Tracking)
            {
                UpdateFeedbackText(
                    "Failed to create anchor. EarthManager not tracking.", error:true);
                return;
            }

            GeospatialPose geoPose = _earthManager.CameraGeospatialPose;

            Mercator.GeoCoordinate geoCoord = new Mercator.GeoCoordinate(geoPose.Latitude, geoPose.Longitude, geoPose.Altitude);
            Mercator.GeoCoordinate geoCoordAhead = geoCoord.CalculateDerivedPosition(distAheadOfCamera, geoPose.Heading);

            distAheadOfCamera = geoCoord.GetDistanceTo(geoCoordAhead);

            BalloonData newBDat = new BalloonData();
            newBDat.user_id = _network.CurrentUserID;
            newBDat.latitude = geoCoordAhead.latitude;
            newBDat.longitude = geoCoordAhead.longitude;
            newBDat.altitude = geoPose.Altitude - Balloon.ESTIMATED_CAM_HEIGHT_FROM_FLOOR;
            newBDat.balloon_string_length = UnityEngine.Random.Range(0.9f, 1.5f);

            Quaternion anchorRot = Quaternion.AngleAxis(0, new Vector3(0.0f, 1.0f, 0.0f));

            // - - - - - - - - - - - - - - -
            // Create yourself an ARGeospatialAnchor! :o
            ARGeospatialAnchor newAnchor = _anchorManager.AddAnchor(
                newBDat.latitude, 
                newBDat.longitude, 
                newBDat.altitude, 
                anchorRot);
            // - - - - - - - - - - - - - - -    

            if (newAnchor != null || Application.isEditor)
            {
                BalloonAnchor newBA = CreateNewBalloonAnchor(newAnchor, newBDat);
                newBA.balloon.PerformInflate();

                _network.CreateBalloon(newBDat,
                (BalloonData bDat) => {

                    if (bDat == null || bDat.balloon_id == null || bDat.balloon_id.Length < 1) {
                        Debug.LogError("Firestore error when creating Balloon");
                        UpdateFeedbackText($"Firestore Balloon error! Local balloon was still created...", 
                                        error: true);
                    } else {
                        UpdateFeedbackText($"NEW Balloon sent to server! Total balloons: {_anchors.Count}", 
                                        error: false);
                    }
                });

                // UpdateFeedbackText(string.Format("Balloon #{0} created! " + 
                // "distFwd: {1:0.00}m," + 
                // "LAT/LNG: {2:0.00000}/{3:0.00000} ", 
                // _anchors.Count, distFwd, geoCoordAhead.Latitude, geoCoordAhead.Longitude), false);
            }
            else
            {
                UpdateFeedbackText("Failed to create anchor. Internal error.", error:true);
            }
        }

        /// <summary>
        /// Convenience method to create a balloon object from a prefab, 
        /// set it up and save it to the list of anchors
        /// </summary>
        /// <param name="arAnchor">The GoogleARCore.Anchor that should already be created</param>
        /// <param name="balloonData">The BalloonData to use for the new BalloonAnchor</param>
        private BalloonAnchor CreateNewBalloonAnchor(ARGeospatialAnchor arAnchor, BalloonData balloonData) {

            GameObject balloonGO = Instantiate(AnchorVisObjectPrefab);

            Balloon balloon = balloonGO.GetComponentInChildren<Balloon>();
            balloon.balloonWasPopped.AddListener(this.BalloonWasPopped);
            balloon.SetBalloonData(balloonData);

            BalloonAnchor newBA = new BalloonAnchor(arAnchor, balloon);
            balloonGO.SetActive(false);
            if (!Application.isEditor) {
                balloonGO.transform.SetParent(newBA.anchor.transform, false);
            }
            balloonGO.transform.localPosition = Vector3.zero;
            balloonGO.transform.localScale = Vector3.one;
            balloonGO.SetActive(true);
            this._anchors.Add(newBA);
            this.BalloonAnchorCountChanged.Invoke(_anchors.Count);

            balloon.SetVisibleAfterDelay(true, 0.3f);

            return newBA;
        }

        /// <summary>
        /// Update the height of the balloon anchors so they are 
        /// similar to the user height
        /// </summary>
        void UpdateBallonAnchorYPositionsAndFade(bool adjustFade) {
            if (_anchors.Count < 1) return;

            Vector3 camPosWorld = this.CameraUnity.transform.position;
            _anchors.ForEach((BalloonAnchor ba) => {
                float xzDist = Vector2.Distance(
                    new Vector2(camPosWorld.x, camPosWorld.z), 
                    new Vector2(ba.anchor.transform.position.x, ba.anchor.transform.position.z));
                
                ba.balloon.UpdateBalloonCamYPosFadeAndDistToCamera(camPosWorld.y, xzDist, camPosWorld, adjustFade);
            });
        }

        // ------------------------------------------------------------------

        /// <summary>
        /// Called by a Balloon when it is popped
        /// </summary>
        public void BalloonWasPopped(Balloon b) {
            _network.PopBalloon(b.Data, 
            (BalloonData bDat) => {
                Debug.Log("Balloon was popped network returned: ");
                Debug.Log(bDat);
                if (bDat == null) {
                    UpdateFeedbackText($"Balloon pop error", error:true);
                } else {
                    UpdateFeedbackText($"Balloon pop was sent to the server!", false);
                }
            });
        }

        // ------------------------------------------------------------------

        //  #     # ######  ######     #    ####### ####### 
        //  #     # #     # #     #   # #      #    #       
        //  #     # #     # #     #  #   #     #    #       
        //  #     # ######  #     # #     #    #    #####   
        //  #     # #       #     # #######    #    #       
        //  #     # #       #     # #     #    #    #       
        //   #####  #       ######  #     #    #    ####### 

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            if (!_trackingIsActive) return;

            SessionState.text = "ARSession State: " + ARSession.state;
#if UNITY_IOS
            // Wait for location permission request.
            if (Input.location.status != LocationServiceStatus.Running )
            {
                Debug.LogErrorFormat(
                    "LocationServiceStatus is {0}, Geo Earth won't work correctly at this status.",
                    Input.location.status);
                EarthStateText.text = "LocationServiceStatus: " + Input.location.status;
            }
            else
            {
                // UpdateApplicationLifecycle();
                UpdateEarthStatusText();
                UpdateBalloonMonitoringState();
                UpdateBallonAnchorYPositionsAndFade(DebugSettings.Shared.FadeFarBalloons);
                UpdateEarthAnchorStatus();
            }
#else
            // UpdateApplicationLifecycle();
            UpdateEarthStatusText();
            UpdateBalloonMonitoringState();
            UpdateBallonAnchorYPositionsAndFade(DebugSettings.Shared.FadeFarBalloons);
            UpdateGeospatialAnchorStatus();
#endif // UNITY_IOS

            bool isTracking = _earthManager.EarthTrackingState == TrackingState.Tracking 
                              && ARSession.state == ARSessionState.SessionTracking;
            bool lastStateWasTracking = _lastEarthTrackingState == TrackingState.Tracking;
            if (isTracking != lastStateWasTracking) {
                _lastEarthTrackingState = _earthManager.EarthTrackingState;
                EarthTrackingStateChanged.Invoke(_lastEarthTrackingState);
            }
        }

        /// <summary>
        /// Called from the Unity Update() method, this function is responsible 
        /// for starting balloon pop monitoring, or checking if it needs to be restarted
        /// if the user has moved to a different MapTile
        /// </summary>
        private void UpdateBalloonMonitoringState() {

            if (_earthManager != null && 
                _earthManager.EarthTrackingState == TrackingState.Tracking) {
                // Earth IS tracking...
                if (_network.FirebaseReady && !_network.BalloonsNearbyMonitoringActive) {
                    // Start monitoring for balloons!
                    this.SetupBalloonNearbyMonitoring();
                    UpdateFeedbackText($"Started balloon monitoring. Total anchors: {_anchors.Count}", false);
                } else {
                    // No need to check for a new MapTile if we JUST started monitoring
                    UpdateMonitoringMapTile();
                }
                
            } else {
                // Earth is NOT tracking...
                if (_network.BalloonsNearbyMonitoringActive && !Application.isEditor) {
                    // ... but we are still monitoring for balloons. Let's stop.
                    this.StopBalloonNearbyMonitoring();
                    UpdateFeedbackText($"Stopped balloon monitoring... :. Total anchors: {_anchors.Count}", error:true);
                }
            }
        }

        /// <summary>
        /// Updates the UI with the current status from Earth.
        /// </summary>
        private void UpdateEarthStatusText()
        {
            if (ARSession.state != ARSessionState.SessionTracking || 
                EarthStatusTextUI == null || _earthManager == null)
            {
                EarthStatusTextUI.text = "[ERROR] ARSession.state == " + ARSession.state;
                return;
            }

            // ---------------------------

            FeatureSupported geospatialIsSupported = _earthManager.IsGeospatialModeSupported(
                ARCoreExtensions.ARCoreExtensionsConfig.GeospatialMode);
            if (geospatialIsSupported == FeatureSupported.Unknown)
            {
                EarthStatusTextUI.text = "[ERROR] Geospatial supported is unknown";
                return;
            }
            else if (geospatialIsSupported == FeatureSupported.Unsupported)
            {
                EarthStatusTextUI.text = string.Format(
                    "[ERROR] GeospatialMode {0} is unsupported on this device.",
                    ARCoreExtensions.ARCoreExtensionsConfig.GeospatialMode);
                return;
            }

            EarthState earthState = _earthManager.EarthState;
            if (earthState != EarthState.Enabled)
            {
                EarthStatusTextUI.text = "[ERROR] EarthState: " + earthState;
                return;
            }

            TrackingState trackingState = _earthManager.EarthTrackingState;
            if (trackingState != TrackingState.Tracking)
            {
                EarthStatusTextUI.text = "[ERROR] EarthTrackingState: " + trackingState;
                return;
            }

            // ---------------------------

            switch (trackingState)
            {
                case TrackingState.Tracking:
                    GeospatialPose geoPose = _earthManager.CameraGeospatialPose;
                    EarthStatusTextUI.text = string.Format(
                        "Earth Tracking State:   - TRACKING -\n" +
                        "LAT/LNG: {0:0.00000}, {1:0.00000} (acc: {2:0.000})\n" +
                        "ALTITUDE: {3:0.0}m (acc: {4:0.0}m)\n" +
                        "HEADING:{5:0.0}º (acc: {6:0.0}º)",
                        geoPose.Latitude, geoPose.Longitude,
                        geoPose.HorizontalAccuracy,
                        geoPose.Altitude, geoPose.VerticalAccuracy, 
                        geoPose.Heading, geoPose.HeadingAccuracy);
                    break;
                case TrackingState.Limited:
                    EarthStatusTextUI.text = "Earth Tracking State:   - LIMITED -";
                    break;
                case TrackingState.None:
                    EarthStatusTextUI.text = "Earth Tracking State:   - NONE -";
                    break;
            }
        }

        /// <summary>
        /// Update a label about the EarthAnchor status
        /// </summary>
        private void UpdateGeospatialAnchorStatus()
        {
            int total = _anchors.Count;
            int none = 0;
            int limited = 0;
            int tracking = 0;

            foreach (BalloonAnchor ba in _anchors)
            {
                switch (ba.anchor.trackingState)
                {
                    case TrackingState.None:
                        none++;
                        break;
                    case TrackingState.Limited:
                        limited++;
                        break;
                    case TrackingState.Tracking:
                        tracking++;
                        break;
                }
            }

            EarthAnchorText.text = string.Format(
                "EarthAnchor: {1}{0}" +
                " None: {2}{0}" +
                " Limited: {3}{0}" +
                " Tracking: {4}",
                Environment.NewLine, total, none, limited, tracking);
        }

        /// <summary>
        /// Updates the feedback text.
        /// </summary>
        /// <param name="message">Message string to show.</param>
        /// <param name="error">If true, signifies an error and colors the feedback text red,
        /// otherwise colors the text green.</param>
        private void UpdateFeedbackText(string message, bool error)
        {
            if (error)
            {
                FeedbackTextUI.color = Color.red;
            }
            else
            {
                FeedbackTextUI.color = Color.green;
            }

            FeedbackTextUI.text = message;
        }

        /// <summary>
        /// Get the ARCoreExtensions
        /// </summary>
        public static Google.XR.ARCoreExtensions.ARCoreExtensions GetARCoreExtensions()
        {
            var extensionsGO = GameObject.Find("ARCore Extensions");
            return extensionsGO?.GetComponent<Google.XR.ARCoreExtensions.ARCoreExtensions>();
        }

        //  ######  ####### ######  #     #  #####  
        //  #     # #       #     # #     # #     # 
        //  #     # #       #     # #     # #       
        //  #     # #####   ######  #     # #  #### 
        //  #     # #       #     # #     # #     # 
        //  #     # #       #     # #     # #     # 
        //  ######  ####### ######   #####   #####  

        /// <summary>
        /// Called when settings on the DEBUG SETTINGS GameObject change
        /// </summary>                     
        public void DebugSettingsChanged(DebugSettings debugSettings) {
            // Debug.Log("DebugSettingsChanged");
            FeedbackTextUI.transform.parent.gameObject.SetActive(
                debugSettings.DisplayEarthDebug);
            SessionState.gameObject.SetActive(
                debugSettings.DisplayEarthDebug);
            EarthAnchorText.gameObject.SetActive(
                debugSettings.DisplayEarthDebug);
        }
    }
}