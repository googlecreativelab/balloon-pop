//-----------------------------------------------------------------------
// <copyright file="GameplayUI.cs" company="Google">
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
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;
    using TMPro;
    using ElRaccoone.Tweens;
    // using ElRaccoone.Tweens.Core;


    public struct SlingshotSetting 
    {
        public Button btn;
        public bool isActive;
        public string activeStr;
        public string inactiveStr;
    }

    [Serializable] public class PlayModeEvent : UnityEvent <bool, bool> { }
    [Serializable] public class TestingEvent : UnityEvent <int> { }

    public class GameplayUI : MonoBehaviour
    {
        public Button buildPlayButton;
        public Image buildPlayPillCircle;

        public Button clearAllButton;
        public Button placeTargetButton;

        public SlingshotTouchResponder slingshotTouchResponder;

        public SlingshotSetting buildPlay;

        // true=Build
        public bool isBuildOrPlayMode { get { return buildPlay.isActive; } }
        public PlayModeEvent BuildOrPlayChanged;

        public UnityEvent PlaceTargetEvent;
        public UnityEvent ClearAllEvent;
        public TestingEvent TestingEvent;

        private int _visibleBalloons = 0;

        void Awake()
        {
            buildPlay.btn = buildPlayButton;
            buildPlay.isActive = true; // Build
            buildPlay.activeStr = "Build";
            buildPlay.inactiveStr = "Play";
        }

        static void UpdateBtn(SlingshotSetting s)
        {
            Button b = s.btn;

            Image i = b.GetComponent<Image>();
            float lightCol = 170f/255f;
            i.color = s.isActive ? new Color(lightCol, lightCol, 1f) : 
                                new Color(1f, 1f, 150f/255f);

            TextMeshProUGUI tm = b.GetComponentInChildren<TextMeshProUGUI>();
            string text = s.isActive ? s.activeStr : s.inactiveStr;
            tm.SetText(text);
        }

        public void NumberOfVisibleBalloonsChanged(int visibleBalloons) {
            this._visibleBalloons = visibleBalloons;
            clearAllButton.gameObject.SetActive(buildPlay.isActive && this._visibleBalloons > 0);
        }

        // Start is called before the first frame update
        void Start()
        {
            // this.settingsChanged.Invoke(this.settings);

            bool animate = false;
            this.BuildOrPlayChanged.Invoke(buildPlay.isActive, animate);
            // instructionsText.text = buildPlay.isActive ? InstructionsBuild : InstructionsPlay;

            // Object that receives touch events for the slingshot
            slingshotTouchResponder.gameObject.SetActive(!buildPlay.isActive);

            updateBuildPlayUIState();

        }

        void updateBuildPlayUIState()
        {
            // Set the state of the Build Play Toggle
            Image buildBGImg = buildPlay.btn.GetComponent<Image>();
            // i.color = 
            Color newBGCol = buildPlay.isActive ? new Color(218f/255f,220f/255f,224f/255f)  // Grey BG Pill
                                                : new Color(250f/255f,233f/255f,217f/255f); // Blue BG Pill

            Color newCircleCol = buildPlay.isActive ? Color.white
                                                : new Color(250f/255f, 151f/255f, 38f/255f);
            
            float animDur = 0.3f;
            buildBGImg.TweenValueColor(newBGCol, animDur, (Color colUpdate) => {
                buildBGImg.color = colUpdate;
            }).SetFrom(buildBGImg.color);
            
            buildPlayPillCircle.TweenValueColor(newCircleCol, animDur, (Color colUpdate) => {
                buildPlayPillCircle.color = colUpdate;
            }).SetFrom(buildPlayPillCircle.color);
            
            buildPlayPillCircle
                .TweenLocalPositionX(64f * (buildPlay.isActive ? -1f : 1f), animDur)
                .SetEaseExpoInOut();

            placeTargetButton.gameObject.SetActive(buildPlay.isActive);
            clearAllButton.gameObject.SetActive(buildPlay.isActive && this._visibleBalloons > 0);
        }

        public void BuildOrPlayButtonClicked()
        {
            buildPlay.isActive = !buildPlay.isActive;
            SlingshotSetting buildPlaySetting = buildPlay;

            // Change the build/play button to be active or white
            updateBuildPlayUIState();

            // Set the slingshot touch responder to active/inactive
            slingshotTouchResponder.gameObject.SetActive(!buildPlay.isActive);

            bool animate = true;
            this.BuildOrPlayChanged.Invoke(buildPlay.isActive, animate);
        }

        public void PlaceTargetClicked()
        {
            PlaceTargetEvent.Invoke();
        }

        public void ClearAllClicked()
        {
            ClearAllEvent.Invoke();
        }

        // Invisible button will trigger a pop of the Balloon that 
        // was PLACED LAST in the multiplayer colour (RED #de5246)
        public void DebugBtnClicked(int index)
        {
            // Debug.Log("DebugBtnClicked");
            TestingEvent.Invoke(index);
        }
    }
}