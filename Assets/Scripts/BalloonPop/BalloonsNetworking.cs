//-----------------------------------------------------------------------
// <copyright file="BalloonsNetworking.cs" company="Google">
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
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using UnityEngine;

    using Firebase;
    using Firebase.Firestore;
    using Firebase.Auth;
    // ----------------------------

    public struct BalloonChange 
    {
        public Dictionary<string, object> Dict;
        public Firebase.Firestore.DocumentChange.Type ChangeType;
    }

    public class BalloonsNetworking
    {
        static public int MAP_TILE_MONITORING_ZOOM = 16;
        static public double BALLOON_POP_COOLDOWN_SECS = 2.7;
        
        private bool _firebaseReady = false;
        private ListenerRegistration _balloonsNearbyListener = null;
        public Action<bool> FirebaseReadyChangedEvent;

        public bool FirebaseReady { get { return _firebaseReady; } }
        public bool BalloonsNearbyMonitoringActive { get { return _balloonsNearbyListener != null; } }
        // public bool BalloonsNearbyMonitoringActive { get { return false; } }

        private FirebaseApp _firebaseApp;
        private FirebaseFirestore _firebaseDB;
        private FirebaseAuth _firebaseAuth;

        private string _currentUserID = "";
        public string CurrentUserID { get { return _currentUserID; } }

        public BalloonsNetworking() 
        {
            InitFirebase();
        }
        
        /// <summary>
        /// https://firebase.google.com/docs/unity/setup?hl=en&authuser=0#confirm-google-play-version
        /// Initialise Firebase
        /// </summary>
        private void InitFirebase() 
        {
            Debug.Log("InitFirebase");
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
                var dependencyStatus = task.Result;
                if (dependencyStatus == Firebase.DependencyStatus.Available)
                {
                    // Create and hold a reference to your FirebaseApp,
                    // where app is a Firebase.FirebaseApp property of your application class.
                    _firebaseApp = Firebase.FirebaseApp.DefaultInstance;
                    _firebaseDB = FirebaseFirestore.DefaultInstance;
                    _firebaseAuth = FirebaseAuth.DefaultInstance;
                    // _FirebaseFunctions = FirebaseFunctions.DefaultInstance;

                    // Set a flag here to indicate whether Firebase is ready to use by your app.
                    this._firebaseReady = true;
                    CreateOrLoginToAnonymousUser();

                } else {
                    UnityEngine.Debug.LogError(System.String.Format(
                    "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                    // Firebase Unity SDK is not safe to use here.
                }
            });
        }

        // ============================================================
        // ------------------------------------------------------------
        // ============================================================

        //     #    #     # ####### #     #    #       #######  #####  ### #     # 
        //    # #   ##    # #     # ##    #    #       #     # #     #  #  ##    # 
        //   #   #  # #   # #     # # #   #    #       #     # #        #  # #   # 
        //  #     # #  #  # #     # #  #  #    #       #     # #  ####  #  #  #  # 
        //  ####### #   # # #     # #   # #    #       #     # #     #  #  #   # # 
        //  #     # #    ## #     # #    ##    #       #     # #     #  #  #    ## 
        //  #     # #     # ####### #     #    ####### #######  #####  ### #     # 
                                                                        
        /// <summary>
        /// Anonymously login to Firebase to get a UserID.
        /// A simple method to associate balloons with a user without user registration.
        /// </summary>
        private void CreateOrLoginToAnonymousUser() 
        {
            Debug.Log("CreateOrLoginToAnonymousUser");
            _firebaseAuth.SignInAnonymouslyAsync().ContinueWith(task => {
                if (task.IsCanceled) 
                {
                    Debug.LogError("SignInAnonymouslyAsync was canceled.");
                    return;
                }
                if (task.IsFaulted) 
                {
                    Debug.LogError("SignInAnonymouslyAsync encountered an error: " + task.Exception);
                    return;
                }

                Firebase.Auth.FirebaseUser newUser = task.Result;
                Debug.LogFormat("User signed in successfully: {0} ({1})",
                    newUser.DisplayName, newUser.UserId);

                _currentUserID = newUser.UserId;
                // PlayerPrefs.SetString(FIREBASE_USERID_KEY, newUser.UserId);
                // PlayerPrefs.Save();

                Debug.Log("FirebaseReady!");
                this.FirebaseReadyChangedEvent.Invoke(this._firebaseReady);
            });
        }

        // ============================================================
        // ------------------------------------------------------------
        // ============================================================

        /// <summary>
        /// Create a balloon on the global CloudBallons server
        /// </summary>
        /// <param name="callback">Callback to return the response result. Passes null if there was an error.</param>
        public void CreateBalloon(BalloonData balloonData,
                                  Action<BalloonData> callback)
        {
                                    
            CreateBalloonOnFirestore(balloonData, callback);
        }

        /// <summary>
        /// Pop a balloon
        /// </summary>
        /// <param name="balloonDat">The balloon data</param>
        /// <param name="callback">Callback to return the response result.</param>
        public void PopBalloon(BalloonData balloonDat, Action<BalloonData> callback)
        {
            PopBalloonOnFirestore(balloonDat, callback);
        }

        // ============================================================
        // ------------------------------------------------------------
        // ============================================================

        //     #    ######  ######     ######     #    #       #       ####### ####### #     # 
        //    # #   #     # #     #    #     #   # #   #       #       #     # #     # ##    # 
        //   #   #  #     # #     #    #     #  #   #  #       #       #     # #     # # #   # 
        //  #     # #     # #     #    ######  #     # #       #       #     # #     # #  #  # 
        //  ####### #     # #     #    #     # ####### #       #       #     # #     # #   # # 
        //  #     # #     # #     #    #     # #     # #       #       #     # #     # #    ## 
        //  #     # ######  ######     ######  #     # ####### ####### ####### ####### #     # 
                                                                                    
        /// <summary>
        /// When the user creates a balloon, create it on Firestore database
        ///     user_id- should already be set
        ///     latitude- should already be set
        ///     longitude- should already be set
        ///     altitude- should already be set
        ///     balloon_string_length- should already be set
        /// </summary>
        /// <param name="balloonDat">The balloon data</param>
        /// <param name="callback">Callback to return the response result.</param>
        private void CreateBalloonOnFirestore(
            BalloonData balloonData,
            Action<BalloonData> callback)
            {

            CollectionReference balloonsRef = _firebaseDB.Collection("balloons");

            balloonData.tile_z12_xy_hash = Mercator.GetTileAtLatLng(balloonData.latitude, balloonData.longitude, 12).XYHash;
            balloonData.tile_z13_xy_hash = Mercator.GetTileAtLatLng(balloonData.latitude, balloonData.longitude, 13).XYHash;
            balloonData.tile_z14_xy_hash = Mercator.GetTileAtLatLng(balloonData.latitude, balloonData.longitude, 14).XYHash;
            balloonData.tile_z15_xy_hash = Mercator.GetTileAtLatLng(balloonData.latitude, balloonData.longitude, 15).XYHash;
            balloonData.tile_z16_xy_hash = Mercator.GetTileAtLatLng(balloonData.latitude, balloonData.longitude, 16).XYHash;

            // geohash is unused
            balloonData.created = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            balloonData.num_popped = 0;
            balloonData.popped_until = 0;
            balloonData.color = "#ffffff";

            balloonsRef.AddAsync(balloonData).ContinueWith(task => {
                if (task != null && task.Result != null && task.Result.Id != null)
                {
                    balloonData.balloon_id = task.Result.Id;
                }
                callback(balloonData);
            });
        }

        // ============================================================
        // ------------------------------------------------------------
        // ============================================================

        //  ######  ####### ######     ######     #    #       #       ####### ####### #     # 
        //  #     # #     # #     #    #     #   # #   #       #       #     # #     # ##    # 
        //  #     # #     # #     #    #     #  #   #  #       #       #     # #     # # #   # 
        //  ######  #     # ######     ######  #     # #       #       #     # #     # #  #  # 
        //  #       #     # #          #     # ####### #       #       #     # #     # #   # # 
        //  #       #     # #          #     # #     # #       #       #     # #     # #    ## 
        //  #       ####### #          ######  #     # ####### ####### ####### ####### #     # 


        /// <summary>
        /// When the user pops a balloon, update the number of pops and the popped_until date
        /// </summary>
        /// <param name="balloonDat">The balloon data</param>
        /// <param name="callback">Callback to return the response result.</param>
        private void PopBalloonOnFirestore(
            BalloonData balloonData,
            Action<BalloonData> callback)
            {

            string curUserID = this._currentUserID;
            int newNumPopped = balloonData.num_popped + 1;
            long newPoppedUntil = DateTimeOffset.Now
                .AddSeconds(BALLOON_POP_COOLDOWN_SECS)
                .ToUnixTimeMilliseconds();

            if (balloonData == null || balloonData.balloon_id == null || balloonData.balloon_id.Length < 1)
            {
                balloonData.CurrentUserPoppedBalloon(newPoppedUntil, curUserID);
                callback(null);
                Debug.LogError("Cannot pop a null balloonData or balloon_id");
                return;
            }

            DocumentReference balloonRef = _firebaseDB.Collection("balloons").Document(balloonData.balloon_id);
            if (balloonRef == null)
            {
                balloonData.CurrentUserPoppedBalloon(newPoppedUntil, curUserID);                
                callback(null);
                Debug.LogError("Balloon ref is null");
                return;
            }

            _firebaseDB.RunTransactionAsync(transaction =>
            {
                return transaction.GetSnapshotAsync(balloonRef)
                .ContinueWith((Task<DocumentSnapshot> snapshotTask) =>
                {
                    DocumentSnapshot snapshot = snapshotTask.Result;
                    newNumPopped = snapshot.GetValue<int>("num_popped") + 1;
                    newPoppedUntil = DateTimeOffset.Now
                        .AddSeconds(BALLOON_POP_COOLDOWN_SECS)
                        .ToUnixTimeMilliseconds();
                    Dictionary<string, object> updates = new Dictionary<string, object>
                    {
                        { "num_popped", newNumPopped },
                        { "popped_until", newPoppedUntil },
                        { "last_user_pop", curUserID }
                    };
                    transaction.Update(balloonRef, updates);
                });
            }).ContinueWith((Task transactionResultTask) =>
            {

                if (transactionResultTask.IsCompleted && 
                    !transactionResultTask.IsCanceled && 
                    !transactionResultTask.IsFaulted)
                {
                    Debug.Log("Balloon popped");
                    balloonData.CurrentUserPoppedBalloon(newPoppedUntil, curUserID, newNumPopped);
                    callback(balloonData);
                }
                else
                {
                    // Set popped_until locally regardless
                    balloonData.CurrentUserPoppedBalloon(newPoppedUntil, curUserID);
                    Debug.Log("Balloon pop error");
                    callback(null);
                } 
            });
        }                                          

        // ============================================================
        // ------------------------------------------------------------
        // ============================================================

        //  ######  #######    #    #       ####### ### #     # ####### 
        //  #     # #         # #   #          #     #  ##   ## #       
        //  #     # #        #   #  #          #     #  # # # # #       
        //  ######  #####   #     # #          #     #  #  #  # #####   
        //  #   #   #       ####### #          #     #  #     # #       
        //  #    #  #       #     # #          #     #  #     # #       
        //  #     # ####### #     # #######    #    ### #     # ####### 
                                                             

        /// <summary>
        /// Stop listening for changes to the database and nullify the reference to the listener
        /// </summary>
        public void StopMonitoringBalloonsNearby()
        {
            if (_balloonsNearbyListener == null) return;
            _balloonsNearbyListener.Stop();
            _balloonsNearbyListener = null;
        }

        /// <summary>
        /// Get a Firestore query for 9 map tiles corresponding to the user map tile
        /// </summary>
        Query GetBalloonsAroundUserQuery(Mercator.MapTile userMapTile)
        {
            List<string> tileHashList = Mercator.GetSurroundingTileXYHashList(userMapTile, includeCenterTile: true);
            
            CollectionReference balloonsRef = _firebaseDB.Collection("balloons");

            // https://firebase.google.com/docs/firestore/query-data/queries#in_not-in_and_array-contains-any
            // Use the in operator to combine up to 10 equality (==) clauses on the same field with a logical OR. 
            // An in query returns documents where the given field matches any of the comparison values.
            string prop = $"tile_z{MAP_TILE_MONITORING_ZOOM}_xy_hash";
            return balloonsRef.WhereIn(prop, tileHashList);
        }

        /// <summary>
        /// Get realtime updates about balloons nearby
        /// </summary>
        /// <param name="userLatitude">Callback to return the response result.</param>
        /// <param name="userLongitude">Callback to return the response result.</param>
        /// <param name="balloonsNearbyCallback">Callback to return the response result.</param>
        public Mercator.MapTile MonitorBalloonsNearby(double userLatitude, double userLongitude, 
                                                Action<List<BalloonChange>> balloonChangesCallback) 
        {
            StopMonitoringBalloonsNearby();

            // // string uID = _CurrentUserID;
            Mercator.MapTile userMapTile = Mercator.GetTileAtLatLng(userLatitude, userLongitude, MAP_TILE_MONITORING_ZOOM);
            Query query = this.GetBalloonsAroundUserQuery(userMapTile);

            _balloonsNearbyListener = query.Listen(snapshot => {
                // Debug.Log("Balloon change callback");
                List<BalloonChange> balloonDicts = new List<BalloonChange>();

                var changes = snapshot.GetChanges();
                foreach (DocumentChange documentChange in changes)
                {
                    string balloonID = documentChange.Document.Id;
                    Dictionary<string, object> balloonDict = documentChange.Document.ToDictionary();
                    balloonDict["balloon_id"] = balloonID;
                    
                    BalloonChange balloonChange = new BalloonChange();
                    balloonChange.Dict = balloonDict;
                    balloonChange.ChangeType = documentChange.ChangeType;
                    balloonDicts.Add(balloonChange);
                    // Debug.Log($"BalloonID was CHANGED: {balloonID} changeType: {documentChange.ChangeType.ToString()}");
                }
                balloonChangesCallback(balloonDicts);
            });

            return userMapTile;
        }

        // ============================================================
        // ------------------------------------------------------------
        // ============================================================

        //  ######  ####### #       ####### ####### #######    ######     #    #       #       ####### ####### #     #  #####  
        //  #     # #       #       #          #    #          #     #   # #   #       #       #     # #     # ##    # #     # 
        //  #     # #       #       #          #    #          #     #  #   #  #       #       #     # #     # # #   # #       
        //  #     # #####   #       #####      #    #####      ######  #     # #       #       #     # #     # #  #  #  #####  
        //  #     # #       #       #          #    #          #     # ####### #       #       #     # #     # #   # #       # 
        //  #     # #       #       #          #    #          #     # #     # #       #       #     # #     # #    ## #     # 
        //  ######  ####### ####### #######    #    #######    ######  #     # ####### ####### ####### ####### #     #  #####                                                                  

        /// <summary>
        /// Used for debugging- delete all the balloons in the 9 MapTiles that the user inhabits.
        /// A good modification would be to only delete balloons surrounding the user that they have created.
        /// </summary>
        /// <param name="userLatitude">Callback to return the response result.</param>
        public void DeleteBalloonsAroundUser(double userLatitude, double userLongitude, Action<string> completeCallback)
        {
            Debug.Log($"DeleteBalloonsAroundUser... at {userLatitude}, {userLongitude}");

            Mercator.MapTile userMapTile = Mercator.GetTileAtLatLng(userLatitude, userLongitude, MAP_TILE_MONITORING_ZOOM);
            Query query = this.GetBalloonsAroundUserQuery(userMapTile);

            query.GetSnapshotAsync().ContinueWith(task => {
                QuerySnapshot querySnapshot = task.Result;

                WriteBatch batch = _firebaseDB.StartBatch();
                Debug.Log("Deleting " + querySnapshot.Count + " balloons...");
                foreach (DocumentSnapshot documentSnapshot in querySnapshot.Documents)
                {
                    batch.Delete(documentSnapshot.Reference);
                };

                batch.CommitAsync()
                .ContinueWith(localTask => {
                    bool err = localTask.IsCanceled || localTask.IsFaulted || !localTask.IsCompleted;
                    completeCallback(err ? "Error" : null);
                });
            });   
        }
    }
}
