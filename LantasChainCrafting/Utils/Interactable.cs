using Nautilus.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChainCrafting.Utils
{
    public class Interactable : HandTarget, IHandTarget
    {
        private event Action<GUIHand> _OnHandHover;
        private readonly Dictionary<(GameInput.Button Button, bool IsOnHold), Action> inputCallbacks = new();

        private bool IsActiveTarget 
        { 
            get
            {
                try 
                {
                    GameObject activeTarget = Player.main.guiHand.GetActiveTarget();
                    bool isParentActive = activeTarget == transform.parent;
                    bool isSelfActive = activeTarget == transform.gameObject;
                    return isParentActive || isSelfActive;
                }
                catch (NullReferenceException ex)
                {
                    Plugin.Logger.LogException(ex);
                    return false;
                }
            }
        }

        public override void Awake() => base.Awake();

        public void Update() 
        {
            if (!IsActiveTarget) return;
            foreach ((GameInput.Button input, _) in inputCallbacks.Keys)
            {
                if (GameInput.GetButtonDown(input)) inputCallbacks[(input, false)]?.Invoke();
                if (GameInput.GetButtonHeld(input)) inputCallbacks[(input, true)]?.Invoke();
            }
        }

        public void RegisterInput(GameInput.Button button, bool isOnHoldAction, Action callback)
        {
            (GameInput.Button Button, bool IsOnHold) buttonInput = (button, isOnHoldAction);
            if (inputCallbacks.ContainsKey(buttonInput)) inputCallbacks[buttonInput] += callback;
            else inputCallbacks.Add(buttonInput, callback);
        }

        public void UnregisterAction(GameInput.Button button, Action callback)
        {
            if (inputCallbacks.ContainsKey((button, false))) inputCallbacks[(button, false)] -= callback;
            if (inputCallbacks.ContainsKey((button, true))) inputCallbacks[(button, true)] -= callback;
        }

        public void UnregisterInput(GameInput.Button button)
        {
            inputCallbacks.Remove((button, false));
            inputCallbacks.Remove((button, true));
        }

        public void RegisterOnHandHover(Action<GUIHand> callback)
        {
            _OnHandHover += callback;
        }

        public void UnregisterOnHandHover(Action<GUIHand> callback)
        {
            _OnHandHover -= callback;
        }

        public void OnHandHover(GUIHand hand)
        {
            _OnHandHover?.Invoke(hand);
        }

        public void OnHandClick(GUIHand hand)
        {
            
        }
    }
}
