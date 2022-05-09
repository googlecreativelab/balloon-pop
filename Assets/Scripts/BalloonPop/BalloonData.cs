//-----------------------------------------------------------------------
// <copyright file="BalloonData.cs" company="Google">
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
    using System.Collections.Generic;
    using Firebase.Firestore;

    /// <summary>
    /// An interface for a class to confrom to if it wants update about data
    /// that would make visual updates to the balloon
    /// </summary>
    public interface BalloonDataVisualChangesListener {
        void VisualChangeBalloonPopped(DateTimeOffset oldPoppedUntil, DateTimeOffset poppedUntil, string poppedByUserID, string curUserID);
    }

    /// <summary>
    /// BalloonData stores information about a Balloon
    /// </summary>
    [Serializable]
    [FirestoreData]
    public class BalloonData : IEquatable<BalloonData> {

        [FirestoreProperty] public string balloon_id { get; set; }
        [FirestoreProperty] public string user_id { get; set; }
        [FirestoreProperty] public double latitude { get; set; }
        [FirestoreProperty] public double longitude { get; set; }
        [FirestoreProperty] public double altitude { get; set; }
        [FirestoreProperty] public string tile_z12_xy_hash { get; set; }
        [FirestoreProperty] public string tile_z13_xy_hash { get; set; }
        [FirestoreProperty] public string tile_z14_xy_hash { get; set; }
        [FirestoreProperty] public string tile_z15_xy_hash { get; set; }
        [FirestoreProperty] public string tile_z16_xy_hash { get; set; }
        [FirestoreProperty] public string geohash { get; set; }
        [FirestoreProperty] public long created { get; set; }
        [FirestoreProperty] public float balloon_string_length { get; set; }
        [FirestoreProperty] public int num_popped { get; set; }
        [FirestoreProperty] public long popped_until { get; set; }
        [FirestoreProperty] public string last_user_pop { get; set; }
        [FirestoreProperty] public string color { get; set; } // hex

        /// <summary>
        /// The visual changes listener
        /// </summary>
        private BalloonDataVisualChangesListener _VisualsListener = null;

        /// <summary>
        /// Set the listener for the visual changes
        /// </summary>
        /// <param name="listener">Set a new listener</param>
        public void SetVisualChangesListener(BalloonDataVisualChangesListener listener) { 
            _VisualsListener = listener;
        }

        // -------------------------
        // Convenience getters

        /// <summary>
        /// The date when this BalloonData was created
        /// </summary>
        /// <returns>The created date</returns>
        public DateTimeOffset CreatedDate {
            get { return DateTimeOffset.FromUnixTimeMilliseconds(created); } 
        }

        /// <summary>
        /// The date when this BalloonData will inflate next, or will inflate
        /// </summary>
        /// <returns>The popped until date</returns>
        public DateTimeOffset PoppedUntilDate {
            get { return DateTimeOffset.FromUnixTimeMilliseconds(popped_until); } 
        }

        /// <summary>
        /// Get the Color of this balloon
        /// </summary>
        /// <returns>The Color</returns>
        public UnityEngine.Color ColorFromHex {
            get {
                UnityEngine.Color col = UnityEngine.Color.white;
                if ( UnityEngine.ColorUtility.TryParseHtmlString("#09FF0064", out col) ) return col;
                return UnityEngine.Color.white;
            }
        }

        /// <summary>
        /// Get the GeoCoordinate for this BalloonData
        /// </summary>
        /// <returns>A Mercator.GeoCoordinate</returns>
        public Mercator.GeoCoordinate coordinate {
            get { return new Mercator.GeoCoordinate(latitude, longitude); }
        }

        // =====================================

        /// <summary>
        /// Default constructor
        /// </summary>
        public BalloonData() {
            balloon_id = "";
            user_id = "";
            created = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// BalloonData constructor
        /// </summary>
        /// <param name="balloonDict">The balloon data dictionary</param>
        public BalloonData(Dictionary<string, object> balloonDict) {
            UpdatePropertiesWithDict(balloonDict);
        }

        // =====================================
        // IEquatable<BalloonDatas>

        /// <summary>
        /// Get a HashCode for this BalloonData
        /// </summary>
        /// <returns>A hash code</returns>
        public override int GetHashCode() => balloon_id.GetHashCode();

        /// <summary>
        /// Check if this BalloonData is equal to another
        /// </summary>
        /// <param name="obj">The BalloonData to compare to</param>
        /// <returns>True if the passed BalloonData is deemed to be equal to this BalloonData</returns>
        public override bool Equals(object obj) => this.Equals(obj as BalloonData);

        /// <summary>
        /// Check if this BalloonData is equal to another
        /// </summary>
        /// <param name="balloonDat">The BalloonData to compare to</param>
        /// <returns>True if the passed BalloonData is deemed to be equal to this BalloonData</returns>
        public bool Equals(BalloonData b) {
            if (b is null) return false;
            // If run-time types are not exactly the same, return false.
            if (this.GetType() != b.GetType()) return false;

            // Optimization for a common success case.
            if (System.Object.ReferenceEquals(this, b)) return true;

            if (b.balloon_id == null || b.balloon_id.Length < 1 || 
                this.balloon_id == null || this.balloon_id.Length < 1) {
                return false;
            }

            return b.balloon_id == this.balloon_id;
        }
        // -------------------------

        /// <summary>
        /// Should be called the the current user pops a balloon
        /// </summary>
        /// <param name="newPoppedUntil">The datetime when the balloon will re-inflate</param>
        /// <param name="newLastUserPopped">The user that popped this balloon</param>
        /// <param name="newNumPopped">How many times this balloon has been popped</param>
        public void CurrentUserPoppedBalloon(long newPoppedUntil, string newLastUserPopped, int? newNumPopped = null) {
            DateTimeOffset oldPoppedUntil = this.PoppedUntilDate;
            popped_until = newPoppedUntil;
            last_user_pop = newLastUserPopped;
            
            DateTimeOffset newPoppedUntilDate = this.PoppedUntilDate;
            if (newNumPopped != null) num_popped = (int)newNumPopped;

            _VisualsListener.VisualChangeBalloonPopped(oldPoppedUntil, newPoppedUntilDate, this.last_user_pop, newLastUserPopped);
        }

        /// <summary>
        /// Updates this BalloonData with a BalloonDict - should be called from the Balloon class
        /// </summary>
        /// <param name="dict">The balloon data dictionary</param>
        /// <param name="curUserID">ID of the current user</param>
        public void UpdateWithDict(Dictionary<string, object> dict, string curUserID) {
            DateTimeOffset oldPoppedUntil = this.PoppedUntilDate;
            UpdatePropertiesWithDict(dict);
            
            if (popped_until < 1) return; // Balloon has never been popped
            DateTimeOffset newPoppedUntil = this.PoppedUntilDate;
            DateTimeOffset now = DateTimeOffset.Now;

            _VisualsListener.VisualChangeBalloonPopped(oldPoppedUntil, newPoppedUntil, this.last_user_pop, curUserID);
        }
        // -------------------------

        /// <summary>
        /// Update the properties on this BalloonData using the data in the dictionary
        /// that was passed in
        /// </summary>
        /// <param name="dict">The balloon data dictionary</param>
        void UpdatePropertiesWithDict(Dictionary<string, object> dict) {
            // PrintDict(dict);
            object val = null;
            if (dict.TryGetValue("balloon_id", out val)) this.balloon_id = (string)val;
            if (dict.TryGetValue("altitude", out val)) this.altitude = Convert.ToDouble(val);
            if (dict.TryGetValue("user_id", out val)) this.user_id = (string)val;
            if (dict.TryGetValue("last_user_pop", out val)) this.last_user_pop = (string)val;            
            if (dict.TryGetValue("latitude", out val)) this.latitude = (double)val;
            if (dict.TryGetValue("longitude", out val)) this.longitude = (double)val;
            if (dict.TryGetValue("tile_z12_xy_hash", out val)) this.tile_z12_xy_hash = (string)val;
            if (dict.TryGetValue("tile_z13_xy_hash", out val)) this.tile_z13_xy_hash = (string)val;
            if (dict.TryGetValue("tile_z14_xy_hash", out val)) this.tile_z14_xy_hash = (string)val;
            if (dict.TryGetValue("tile_z15_xy_hash", out val)) this.tile_z15_xy_hash = (string)val;
            if (dict.TryGetValue("tile_z16_xy_hash", out val)) this.tile_z16_xy_hash = (string)val;
            if (dict.TryGetValue("geohash", out val)) this.geohash = (string)val;
            if (dict.TryGetValue("created", out val)) this.created = (long)val;
            if (dict.TryGetValue("balloon_string_length", out val)) this.balloon_string_length = (float)Convert.ToDouble(val);
            if (dict.TryGetValue("num_popped", out val)) this.num_popped = Convert.ToInt32(val);
            if (dict.TryGetValue("popped_until", out val)) this.popped_until = Convert.ToInt64(val);
            if (dict.TryGetValue("color", out val)) this.color = (string)val;
        }

        /// <summary>
        /// Print the contents of a dictionary
        /// </summary>
        /// <param name="balloonDict">The balloon data dictionary</param>
        void PrintDict(Dictionary<string, object> balloonDict) {
            foreach (KeyValuePair<string, object> kvp in balloonDict) {
                UnityEngine.Debug.Log ($"Key = {kvp.Key}, Value = {kvp.Value}");
            }
        }

        /// <summary>
        /// A description of the contents of this class
        /// </summary>
        /// <returns>A description string</returns>
        public override string ToString() {
            return $"balloon_id: {balloon_id}, user_id: {user_id}, last_user_pop: {last_user_pop}, "+
            $"latitude: {latitude}, longitude: {longitude}, altitude: {altitude}, " +
            $"tile_z16_xy_hash: {tile_z16_xy_hash}, geohash: {geohash}, created: {created}, " +
            $"num_popped: {num_popped}, popped_until: {popped_until}, color: {color}";
        }
    }

}