using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChainCrafting.Utils
{
    public class Interactable : MonoBehaviour
    {
        private readonly Dictionary<GameInput.Button, Action> inputCallbacks = new();

        public void Update() {
            foreach (GameInput.Button input in inputCallbacks.Keys)
            {
                if (GameInput.GetButtonDown(input) || GameInput.GetButtonHeld(input)) inputCallbacks[input]?.Invoke();
            }
        }

        public void RegisterInput(GameInput.Button button, Action callback)
        {
            if(inputCallbacks.ContainsKey(button)) inputCallbacks[button] += callback;
            else inputCallbacks.Add(button, callback);
        }

        public void UnregisterAction(GameInput.Button button, Action callback)
        {
            if (inputCallbacks.ContainsKey(button)) inputCallbacks[button] -= callback;
        }

        public void UnregisterInput(GameInput.Button button)
        {
            inputCallbacks.Remove(button);
        }
    }
}
