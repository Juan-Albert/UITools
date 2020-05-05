﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UITools
{
    [ExecuteInEditMode]
    public class FlexibleUI : MonoBehaviour
    {

        protected FlexibleUIData skinData;
        
        protected virtual void OnSkinUI()
        {
            
        }

        protected virtual void Awake()
        {
            skinData = Resources.Load("FlexibleUIData") as FlexibleUIData;
            OnSkinUI();
        }
        
#if UNITY_EDITOR
        public virtual void OnValidate()
        {
            Awake();
            OnSkinUI();
        }
#endif
        
    }
}
