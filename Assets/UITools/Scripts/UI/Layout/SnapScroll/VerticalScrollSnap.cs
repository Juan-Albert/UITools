﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UITools
{
    public class VerticalScrollSnap : ScrollSnapBase, IEndDragHandler
    {
        void Start()
        {
            _isVertical = true;
            _childAnchorPoint = new Vector2(0.5f, 0.5f);
            _screensContainer.localPosition = new Vector3(0f, _screensContainer.localPosition.y, _screensContainer.localPosition.z);
            _currentScreen = startingScreen;
            panelDimensions = gameObject.GetComponent<RectTransform>();
            UpdateLayout();
        }
        
        void Update()
        {
            if (!_lerp && _scroll_rect.velocity == Vector2.zero)
            {
                if (!_settled && !_pointerDown)
                {
                    if (!IsRectSettledOnaPage(_screensContainer.localPosition))
                    {
                        ScrollToClosestElement();
                    }
                }
                return;
            }
            else if (_lerp)
            {
                _screensContainer.localPosition = Vector3.Lerp(_screensContainer.localPosition, _lerp_target, transitionSpeed * Time.deltaTime);
                if (Vector3.Distance(_screensContainer.localPosition, _lerp_target) < 5f) //todo balancear variable y cambiar distance por math abs a una variable
                {
                    _screensContainer.localPosition = _lerp_target;
                    _lerp = false;
                    EndScreenChange();
                }
            }

            CurrentPage = GetPageForPosition(_screensContainer.localPosition);

            //If the container is moving check if it needs to settle on a page
            if (!_pointerDown)
            {
                if (_scroll_rect.velocity.y > 0.01 || _scroll_rect.velocity.y < 0.01)
                {
                    //if the pointer is released and is moving slower than the threshold, then just land on a page
                    if (IsRectMovingSlowerThanThreshold(0))
                    {
                        ScrollToClosestElement();
                    }
                }
            }
        }
        
        private bool IsRectMovingSlowerThanThreshold(float startingSpeed)
        {
            return (_scroll_rect.velocity.y > startingSpeed && _scroll_rect.velocity.y < swipeVelocityThreshold) ||
                                (_scroll_rect.velocity.y < startingSpeed && _scroll_rect.velocity.y > -swipeVelocityThreshold);
        }

        //Todo controlar el spacing y el padding del horizontal layout al calcular las posiciones
        private void DistributePages()
        {
            _screens = _screensContainer.childCount;
            _scroll_rect.verticalNormalizedPosition = 0;

            int _offset = 0;
            float _dimension = 0;
            float currentYPosition = 0;
            var pageStepValue = _childSize = childObjects[0].sizeDelta.y;


            for (int i = 0; i < _screensContainer.transform.childCount; i++)
            {
                RectTransform child = _screensContainer.transform.GetChild(i).gameObject.GetComponent<RectTransform>();
                currentYPosition = _offset + (int)(i * pageStepValue);
                //child.sizeDelta = new Vector2(panelDimensions.width, panelDimensions.height);
                child.anchoredPosition = new Vector2(0f, currentYPosition);
                child.anchorMin = child.anchorMax = child.pivot = _childAnchorPoint;
            }

            _dimension = currentYPosition + _offset * -1;

            //_screensContainer.offsetMax = new Vector2(_dimension, 0f);
        }

        /// <summary>
        /// Add a new child to this Scroll Snap and recalculate it's children
        /// </summary>
        /// <param name="GO">GameObject to add to the ScrollSnap</param>
        public void AddChild(GameObject GO)
        {
            AddChild(GO, false);
        }

        /// <summary>
        /// Add a new child to this Scroll Snap and recalculate it's children
        /// </summary>
        /// <param name="GO">GameObject to add to the ScrollSnap</param>
        /// <param name="WorldPositionStays">Should the world position be updated to it's parent transform?</param>
        public void AddChild(GameObject GO, bool WorldPositionStays)
        {
            _scroll_rect.verticalNormalizedPosition = 0;
            GO.transform.SetParent(_screensContainer, WorldPositionStays);
            InitialiseChildObjects();
            DistributePages();

            SetScrollContainerPosition();
        }

        /// <summary>
        /// Remove a new child to this Scroll Snap and recalculate it's children 
        /// *Note, this is an index address (0-x)
        /// </summary>
        /// <param name="index">Index element of child to remove</param>
        /// <param name="ChildRemoved">Resulting removed GO</param>
        public void RemoveChild(int index, out GameObject ChildRemoved)
        {
            RemoveChild(index, false, out ChildRemoved);
        }

        /// <summary>
        /// Remove a new child to this Scroll Snap and recalculate it's children 
        /// *Note, this is an index address (0-x)
        /// </summary>
        /// <param name="index">Index element of child to remove</param>
        /// <param name="WorldPositionStays">If true, the parent-relative position, scale and rotation are modified such that the object keeps the same world space position, rotation and scale as before</param>
        /// <param name="ChildRemoved">Resulting removed GO</param>
        public void RemoveChild(int index, bool WorldPositionStays, out GameObject ChildRemoved)
        {
            ChildRemoved = null;
            if (index < 0 || index > _screensContainer.childCount)
            {
                return;
            }
            _scroll_rect.verticalNormalizedPosition = 0;

            Transform child = _screensContainer.transform.GetChild(index);
            child.SetParent(null, WorldPositionStays);
            ChildRemoved = child.gameObject;
            InitialiseChildObjects();
            DistributePages();

            if (_currentScreen > _screens - 1)
            {
                CurrentPage = _screens - 1;
            }

            SetScrollContainerPosition();
        }

        /// <summary>
        /// Remove all children from this ScrollSnap
        /// </summary>
        /// <param name="ChildrenRemoved">Array of child GO's removed</param>
        public void RemoveAllChildren(out GameObject[] ChildrenRemoved)
        {
            RemoveAllChildren(false, out ChildrenRemoved);
        }

        /// <summary>
        /// Remove all children from this ScrollSnap
        /// </summary>
        /// <param name="WorldPositionStays">If true, the parent-relative position, scale and rotation are modified such that the object keeps the same world space position, rotation and scale as before</param>
        /// <param name="ChildrenRemoved">Array of child GO's removed</param>
        public void RemoveAllChildren(bool WorldPositionStays, out GameObject[] ChildrenRemoved)
        {
            var _screenCount = _screensContainer.childCount;
            ChildrenRemoved = new GameObject[_screenCount];

            for (int i = _screenCount - 1; i >= 0; i--)
            {
                ChildrenRemoved[i] = _screensContainer.GetChild(i).gameObject;
                ChildrenRemoved[i].transform.SetParent(null, WorldPositionStays);
            }

            _scroll_rect.verticalNormalizedPosition = 0;
            CurrentPage = 0;
            InitialiseChildObjects();
            DistributePages();
        }

        private void SetScrollContainerPosition()
        {
            _scrollStartPosition = _screensContainer.localPosition.y;
            _scroll_rect.verticalNormalizedPosition = (float)(_currentScreen) / (_screens - 1);
            OnCurrentScreenChange(_currentScreen);
        }

        /// <summary>
        /// used for changing / updating between screen resolutions
        /// </summary>
        public void UpdateLayout()
        {
            _lerp = false;
            DistributePages();
            SetScrollContainerPosition();
            OnCurrentScreenChange(_currentScreen);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_childAnchorPoint != Vector2.zero)
            {
                UpdateLayout();
            }
        }

        private void OnEnable()
        {
            InitialiseChildObjects();
            DistributePages();

            if (jumpOnEnable || !restartOnEnable) SetScrollContainerPosition();
            if (restartOnEnable) GoToScreen(startingScreen);
        }

        #region Interfaces
        /// <summary>
        /// Release screen to swipe
        /// </summary>
        /// <param name="eventData"></param>
        public void OnEndDrag(PointerEventData eventData)
        {
            _pointerDown = false;

            if (_scroll_rect.horizontal)
            {
                var distance = Vector3.Distance(_startPosition, _screensContainer.localPosition);
                if (useFastSwipe && distance < panelDimensions.rect.width && distance >= fastSwipeThreshold)//todo cambiar funcion distancia por distancia en una dimension
                {
                    _scroll_rect.velocity = Vector3.zero;
                    if (_startPosition.x - _screensContainer.localPosition.x > 0)
                    {
                        NextScreen();
                    }
                    else
                    {
                        PreviousScreen();
                    }
                }
            }
        }
        #endregion
        
        
    }
}