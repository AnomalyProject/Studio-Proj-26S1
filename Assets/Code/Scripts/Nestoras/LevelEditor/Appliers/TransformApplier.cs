using static SnapshotUtility;
using UnityEngine.Scripting;
using UnityEngine;
using System;

[Preserve] // Avoid stripping type from build
public class TransformApplier : IComponentApplier
{
    public Type TargetType => typeof(Transform);

    public bool Apply(Component target, FieldSnapshot field)
    {
        Transform t = (Transform)target;

        switch (field.path)
        {
            case "m_LocalPosition":
                t.localPosition = field.vector3Value;
                return true;

            case "m_LocalRotation":
                t.localRotation = field.quaternionValue;
                return true;

            case "m_LocalScale":
                t.localScale = field.vector3Value;
                return true;
        }

        return false;
    }
}