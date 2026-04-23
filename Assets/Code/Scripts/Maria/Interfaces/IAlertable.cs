using UnityEngine;

public interface IAlertable
{
    void Alert<TTarget>(TTarget alertedBy) where TTarget : MonoBehaviour;
}