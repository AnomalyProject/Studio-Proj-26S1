using UnityEngine;

public class RoomSwitcher : MonoBehaviour
{
    [Header("Room Groups")]
    [Tooltip("Rooms activated when this script is enabled.")]
    [SerializeField] private GameObject[] primaryRooms;

    [Tooltip("Rooms activated when this script is disabled.")]
    [SerializeField] private GameObject[] secondaryRooms;

    private void OnEnable()
    {
        SetRoomsState(primaryRooms, true);
        SetRoomsState(secondaryRooms, false);
    }

    private void OnDisable()
    {
        SetRoomsState(primaryRooms, false);
        SetRoomsState(secondaryRooms, true);
    }
    
    private void SetRoomsState(GameObject[] rooms, bool isActive)
    {
       
        if (rooms == null) return;

        foreach (GameObject room in rooms)
        {
            if (room != null)
            {
                room.SetActive(isActive);
            }
        }
    }
}