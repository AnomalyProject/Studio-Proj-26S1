using System.Collections.Generic;
using System.Linq;
using System;
using static SnapshotUtility;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Registry of all <see cref="IComponentApplier"/>s. Used to delegate the application and reversal of <see cref="LevelModification"/>s at runtime.
/// </summary>
public static class ComponentApplierRegistry
{
    public static ObjectGuidRegistry objectGuidRegistry
    {
        get
        {
#if UNITY_EDITOR
            ObjectGuidRegistry registry = Resources.Load<ObjectGuidRegistry>("LevelEditor/ObjectGuidRegistry");
            if (registry == null) ObjectGuidRegistryUtility.GenerateNewRegistry();
#endif
            return Resources.Load<ObjectGuidRegistry>("LevelEditor/ObjectGuidRegistry");
        }
    }

    private static Dictionary<Type, IComponentApplier> appliers = new Dictionary<Type, IComponentApplier>();

    static ComponentApplierRegistry()
    {
        AutoRegisterAllAppliers();
    }

    private static void AutoRegisterAllAppliers()
    {
        IEnumerable<Type> applierTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => typeof(IComponentApplier).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (Type type in applierTypes)
        {
            try
            {
                IComponentApplier instance = (IComponentApplier)Activator.CreateInstance(type);
                appliers[instance.TargetType] = instance;

                //Debug.Log($"Registered applier: {type.Name} -> {instance.TargetType.Name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create applier {type.Name}: {e}");
            }
        }
    }

    public static bool TryApply(Component target, FieldSnapshot field)
    {
        Type targetType = target.GetType();

        // Only exact match OR most specific match
        IComponentApplier best = null;
        foreach (IComponentApplier applier in appliers.Values)
        {
            if (applier.TargetType == targetType) return applier.Apply(target, field);
            if (applier.TargetType.IsInstanceOfType(target)) best = applier;
        }
        return best?.Apply(target, field) ?? false;
    }

    public static bool IsFieldSupported(Type componentType, string fieldPath)
    {
        foreach (KeyValuePair<Type, IComponentApplier> applier in appliers) if (applier.Key.IsAssignableFrom(componentType)) return applier.Value.Supports(fieldPath);
        return false;
    }
}