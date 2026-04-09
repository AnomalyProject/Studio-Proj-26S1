using System.Collections.Generic;
using System.Linq;
using System;
using static SnapshotUtility;
using UnityEngine;

public static class ComponentApplierRegistry
{
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

                Debug.Log($"Registered applier: {type.Name} -> {instance.TargetType.Name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create applier {type.Name}: {e}");
            }
        }
    }

    public static bool TryApply(Component target, FieldSnapshot field)
    {
        foreach (KeyValuePair<Type, IComponentApplier> applier in appliers) if (applier.Key.IsAssignableFrom(target.GetType())) return applier.Value.Apply(target, field);
        return false;
    }
}