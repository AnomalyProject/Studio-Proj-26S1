using UnityEngine;

public class SuicideBomb : MonoBehaviour
{
   
    public void DestroyThisObject()
    {
        Debug.Log("Trigger received!KABOOOOOOM " + gameObject.name);
        Destroy(gameObject);
    }
}