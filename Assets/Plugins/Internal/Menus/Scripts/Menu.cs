﻿// Menus
// Copyright (c) Daniel Lochner

using System;
using UnityEngine;

namespace DanielLochner.Assets
{
    [RequireComponent(typeof(Animator))]
    public class Menu : MonoBehaviour
    {
        #region Fields
        [SerializeField] private bool isOpen;

        protected Animator animator;
        #endregion

        #region Properties
        public bool IsOpen
        {
            get => isOpen;
            set
            {
                animator?.SetBool("IsOpen", isOpen = value);
            }
        }

        public Action OnOpen { get; set; }
        public Action OnClose { get; set; }
        #endregion

        #region Methods
        protected virtual void Awake()
        {
            animator = GetComponent<Animator>();
        }
        protected virtual void OnEnable()
        {
            animator.SetBool("IsOpenByDefault", IsOpen = IsOpen); // Set to the default value in the inspector.
        }

        public virtual void Open(bool instant = false)
        {
            if (instant)
            {
                animator.Play("Open", 0, 1);
            }

            IsOpen = true;
            OnOpen?.Invoke();
        }
        public virtual void Close(bool instant = false)
        {
            if (!IsOpen) return;

            if (instant)
            {
                animator.Play("Close", 0, 1);
            }

            IsOpen = false;
            OnClose?.Invoke();
        }
        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void OnOpened()
        {
            if (IsOpen)
            {
                OnEndOpen();
            }
            else
            {
                OnBeginClose();
            }
        }
        public void OnClosed()
        {
            if (IsOpen)
            {
                OnBeginOpen();
            }
            else
            {
                OnEndClose();
            }
        }

        public virtual void OnBeginOpen()
        {
        }
        public virtual void OnEndOpen()
        {
        }
        public virtual void OnBeginClose()
        {
        }
        public virtual void OnEndClose()
        {
        }
        #endregion
    }
}