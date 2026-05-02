using System;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
   
   private static CursorManager instance;

   private void Awake()
   {
      if (instance != null && instance != this)
      {
         Destroy(gameObject);
         return;
      }

      instance = this;
      DontDestroyOnLoad(gameObject);
   }
   
   private void Start()
   {
      if (GameStateManager.Instance != null)
      {
         ApplyCursorState(GameStateManager.Instance.CurrentState);
      }
   }
   
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
      if (GameStateManager.Instance != null)
      {
         ApplyCursorState(GameStateManager.Instance.CurrentState);
      }
   }
   
   private void ApplyCursorState(GameState state)
   {
      if (state == GameState.InGame)
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
