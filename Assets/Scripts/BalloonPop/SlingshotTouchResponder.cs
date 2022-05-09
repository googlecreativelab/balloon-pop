//-----------------------------------------------------------------------
// <copyright file="SlingshotTouchResponder.cs" company="Google">
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
    using UnityEngine.EventSystems;

    [Serializable] public class TouchEvent : UnityEvent <Vector2> { }

    public class SlingshotTouchResponder : MonoBehaviour, 
        IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public TouchEvent touchDown;
        public TouchEvent touchMoved;
        public TouchEvent touchEnded;

        public void OnPointerDown(PointerEventData eventData)
        {
            // Debug.Log("Mouse Down: " + eventData.pointerCurrentRaycast.gameObject.name);
            touchDown.Invoke(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Debug.Log("Dragging");
            touchMoved.Invoke(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Debug.Log("Mouse Up");
            touchEnded.Invoke(eventData.position);
        }
    
    }
}