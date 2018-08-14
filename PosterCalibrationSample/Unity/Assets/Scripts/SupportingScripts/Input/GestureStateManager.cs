// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using UnityEngine;
using UnityEngine.XR.WSA.Input;

namespace PosterAlignment.InputUtilities
{
    public class GestureStateManager : StateManager
    {
        public enum Mode
        {
            Navigation,
            Manipulation
        }

        public ushort ClickCount { get; private set; }

        public override bool IsPressed(UnityEngine.EventSystems.PointerEventData.InputButton button)
        {
            return this.ClickCount > 0;
        }

        public override bool IsReleased(UnityEngine.EventSystems.PointerEventData.InputButton button)
        {
            return this.isReleased;
        }

        private bool isReleased;

        public bool IsCancelled { get; private set; }

        public bool IsCapturingGestures
        {
            get { return (this.activeRecognizer != null) ? this.activeRecognizer.IsCapturingGestures() : false; }
        }

        public Mode GestureMode
        {
            get { return this.recognizerType; }
            set { this.ResetGestureRecognizer(value); }
        }

        public Mode recognizerType = Mode.Manipulation;
        private UnityEngine.XR.WSA.Input.GestureRecognizer activeRecognizer = null;

        private UnityEngine.XR.WSA.Input.GestureRecognizer navigationRecognizer = null;
        private Vector3 navigationPosition = Vector3.zero;

        private UnityEngine.XR.WSA.Input.GestureRecognizer manipulationRecognizer = null;
        private Vector3 manipulationPosition = Vector3.zero;
        private Vector3 manipulationPrevious = Vector3.zero;

        public override Vector2 ScreenPosition { get { return this.screenPosition; } }
        private Vector2 screenPosition;

        public override Vector3 WorldScreenPosition { get { return this.worldScreenPosition; } }
        private Vector3 worldScreenPosition;

        public override Vector2 ScreenDelta { get { return this.screenDelta; } }
        private Vector2 screenDelta;

        public override Vector3 WorldDelta { get { return this.worldDelta; } }
        private Vector3 worldDelta;

        private void Awake()
        {
            // Instantiate the Navigation recognizer
            this.navigationRecognizer = new UnityEngine.XR.WSA.Input.GestureRecognizer();
            this.navigationRecognizer.SetRecognizableGestures(
                UnityEngine.XR.WSA.Input.GestureSettings.Tap |
                UnityEngine.XR.WSA.Input.GestureSettings.DoubleTap |
                UnityEngine.XR.WSA.Input.GestureSettings.NavigationX | UnityEngine.XR.WSA.Input.GestureSettings.NavigationY | UnityEngine.XR.WSA.Input.GestureSettings.NavigationZ);
            this.navigationRecognizer.Tapped += this.Recognizer_Tapped;
            this.navigationRecognizer.NavigationStarted += this.NavigationRecognizer_NavigationStarted;
            this.navigationRecognizer.NavigationUpdated += this.NavigationRecognizer_NavigationUpdated;
            this.navigationRecognizer.NavigationCompleted += this.NavigationRecognizer_NavigationCompleted;
            this.navigationRecognizer.NavigationCanceled += this.NavigationRecognizer_NavigationCanceled;

            // Instantiate the Manipulation Recognizer.
            this.manipulationRecognizer = new UnityEngine.XR.WSA.Input.GestureRecognizer();
            this.manipulationRecognizer.SetRecognizableGestures(
                UnityEngine.XR.WSA.Input.GestureSettings.Tap |
                UnityEngine.XR.WSA.Input.GestureSettings.DoubleTap |
                UnityEngine.XR.WSA.Input.GestureSettings.ManipulationTranslate);
            this.manipulationRecognizer.Tapped += this.Recognizer_Tapped;
            this.manipulationRecognizer.ManipulationStarted += this.ManipulationRecognizer_ManipulationStarted;
            this.manipulationRecognizer.ManipulationUpdated += this.ManipulationRecognizer_ManipulationUpdated;
            this.manipulationRecognizer.ManipulationCompleted += this.ManipulationRecognizer_ManipulationCompleted;
            this.manipulationRecognizer.ManipulationCanceled += this.ManipulationRecognizer_ManipulationCanceled;

            // interaction manager state handling
            InteractionManager.InteractionSourcePressed += InteractionManager_InteractionSourcePressed;
            InteractionManager.InteractionSourceReleased += InteractionManager_InteractionSourceReleased;
        }

		private void OnDestroy()
        {
            if (this.navigationRecognizer != null)
            {
                this.navigationRecognizer.Tapped -= this.Recognizer_Tapped;
                this.navigationRecognizer.NavigationStarted -= this.NavigationRecognizer_NavigationStarted;
                this.navigationRecognizer.NavigationUpdated -= this.NavigationRecognizer_NavigationUpdated;
                this.navigationRecognizer.NavigationCompleted -= this.NavigationRecognizer_NavigationCompleted;
                this.navigationRecognizer.NavigationCanceled -= this.NavigationRecognizer_NavigationCanceled;
            }

            if (this.manipulationRecognizer != null)
            {
                this.manipulationRecognizer.Tapped -= this.Recognizer_Tapped;
                this.manipulationRecognizer.ManipulationStarted -= this.ManipulationRecognizer_ManipulationStarted;
                this.manipulationRecognizer.ManipulationUpdated -= this.ManipulationRecognizer_ManipulationUpdated;
                this.manipulationRecognizer.ManipulationCompleted -= this.ManipulationRecognizer_ManipulationCompleted;
                this.manipulationRecognizer.ManipulationCanceled -= this.ManipulationRecognizer_ManipulationCanceled;
            }

			// interaction manager state handling
			InteractionManager.InteractionSourcePressed -= InteractionManager_InteractionSourcePressed;
			InteractionManager.InteractionSourceReleased -= InteractionManager_InteractionSourceReleased;
		}

		private void OnEnable()
        {
            this.ResetGestureRecognizer(this.GestureMode);
        }

        private void OnDisable()
        {
            this.Transition(null);
        }

        private void ResetGestureRecognizer(Mode type)
        {
            switch (type)
            {
                case Mode.Navigation:
                    this.Transition(this.navigationRecognizer);
                    break;
                case Mode.Manipulation:
                    this.Transition(this.manipulationRecognizer);
                    break;
            }

            this.recognizerType = type;
        }

        private void Transition(UnityEngine.XR.WSA.Input.GestureRecognizer newRecognizer)
        {
            if (newRecognizer != null && this.activeRecognizer == newRecognizer && this.IsCapturingGestures)
            {
                return;
            }

            if (this.activeRecognizer != null)
            {
                if (this.activeRecognizer == newRecognizer)
                {
                    return;
                }

                this.activeRecognizer.CancelGestures();
                this.activeRecognizer.StopCapturingGestures();
            }

            if (newRecognizer != null)
            {
                newRecognizer.StartCapturingGestures();
            }

            this.activeRecognizer = newRecognizer;
        }

        // only used when called via Unity UI
        // use ResetGestureRecognizer
        public void SetGestureMode(int type)
        {
            this.GestureMode = (Mode)Enum.ToObject(typeof(Mode), type);
        }


        private void Recognizer_Tapped(TappedEventArgs args)
        {
			this.ClickCount = (ushort)args.tapCount;
            this.isReleased = true;
        }


        private void NavigationRecognizer_NavigationStarted(NavigationStartedEventArgs args)
        {
			Vector3 position;
			args.sourcePose.TryGetPosition(out position);
			this.ClickCount = 1;
            this.isReleased = false;
            this.navigationPosition = position;
        }

        private void NavigationRecognizer_NavigationUpdated(NavigationUpdatedEventArgs args)
        {
			Vector3 position;
			args.sourcePose.TryGetPosition(out position);
			this.ClickCount = 0;
            this.isReleased = false;
            this.navigationPosition = position;
        }

        private void NavigationRecognizer_NavigationCompleted(NavigationCompletedEventArgs args)
        {
			Vector3 position;
			args.sourcePose.TryGetPosition(out position);
			this.ClickCount = 0;
            this.isReleased = true;
            this.IsCancelled = false;
            this.navigationPosition = position;
        }

        private void NavigationRecognizer_NavigationCanceled(NavigationCanceledEventArgs args)
		{
            this.ClickCount = 0;
            this.isReleased = true;
            this.IsCancelled = true;
            this.navigationPosition = Vector3.zero;
        }


        private void ManipulationRecognizer_ManipulationStarted(ManipulationStartedEventArgs args)
        {
			Vector3 position;
			args.sourcePose.TryGetPosition(out position);
            this.ClickCount = 1;
            this.isReleased = false;
            this.manipulationPosition = position;
            this.manipulationPrevious = position;
        }

        private void ManipulationRecognizer_ManipulationUpdated(ManipulationUpdatedEventArgs args)
		{
			Vector3 position;
			args.sourcePose.TryGetPosition(out position);
			this.ClickCount = 0;
            this.isReleased = false;
            this.manipulationPrevious = this.manipulationPosition;
            this.manipulationPosition = position;
        }

        private void ManipulationRecognizer_ManipulationCompleted(ManipulationCompletedEventArgs args)
		{
			Vector3 position;
			args.sourcePose.TryGetPosition(out position);
			this.ClickCount = 0;
            this.isReleased = true;
            this.IsCancelled = false;
            this.manipulationPrevious = this.manipulationPosition;
            this.manipulationPosition = position;
        }

        private void ManipulationRecognizer_ManipulationCanceled(ManipulationCanceledEventArgs args)
		{
            this.ClickCount = 0;
            this.isReleased = true;
            this.IsCancelled = true;
            this.manipulationPrevious = Vector3.zero;
            this.manipulationPosition = Vector3.zero;
        }

        private void InteractionManager_InteractionSourcePressed(InteractionSourcePressedEventArgs args)
		{
            this.ClickCount = 1;
        }

        private void InteractionManager_InteractionSourceReleased(InteractionSourceReleasedEventArgs args)
		{
            this.isReleased = true;
        }

        // IModuleManager
        public override bool IsSupported()
        {
            return (this.activeRecognizer != null);
        }

        public override bool ShouldActivate()
        {
            bool shouldActivate = false;

            if(this.IsCapturingGestures)
            {
                shouldActivate |= this.IsPressed(UnityEngine.EventSystems.PointerEventData.InputButton.Left); // did we click
                shouldActivate |= !this.IsReleased(UnityEngine.EventSystems.PointerEventData.InputButton.Left); // did not release
                shouldActivate |= this.IsCancelled; // did we cancel
            }

            return shouldActivate;
        }

        public override bool ShouldSubmit()
        {
            // TODO: support a voice command for submit
            return false;
        }

        public override bool ShouldCancel()
        {
            return this.IsCancelled;
        }

        public override bool ShouldMove(out Vector2 movement)
        {
            // TODO: support a voice command for next previous / up/down actions
            movement = Vector3.zero;

            return false;
        }

        public override void ResetButtonState(UnityEngine.EventSystems.PointerEventData.InputButton button)
        {
            if (this.isReleased == true && this.ClickCount > 0)
            {
                this.ClickCount = 0;
            }

            this.IsCancelled = false;
            this.isReleased = false;
        }

        public override void ActivateModule()
        {
        }

        public override void DeactivateModule()
        {
        }

        public override void UpdateModule()
        {
            this.screenPosition = StateManager.ScreenCenter;
            this.worldScreenPosition = StateManager.WorldScreenCenter;

            Vector3 delta = Vector3.zero;
            if (this.IsCapturingGestures)
            {
                if (this.GestureMode == Mode.Navigation)
                {
                    delta = this.navigationPosition;
                }
                else
                {
                    delta = this.manipulationPosition - this.manipulationPrevious;
                }
            }

            this.worldDelta = delta;
            this.screenDelta = Camera.main.WorldToScreenPoint(this.worldDelta);
        }
    }
}
