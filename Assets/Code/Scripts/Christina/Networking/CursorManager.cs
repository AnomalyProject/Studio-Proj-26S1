using System;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
   private void OnEnable()
   {
      if (GameStateManager.Instance != null)
         GameStateManager.Instance.OnStateChanged += HandleStateChanged;
   }

   private void OnDisable()
   {
      if (GameStateManager.Instance != null)
         GameStateManager.Instance.OnStateChanged -= HandleStateChanged;
   }

   private void HandleStateChanged(GameState previous, GameState next)
   {
      if (next == GameState.InGame)
      {
         Cursor.lockState = CursorLockMode.Locked;
         Cursor.visible = false;
      }
      else
      {
         Cursor.lockState = CursorLockMode.None;
         Cursor.visible = true;
      }
   }
}
