using System.Collections.Generic;
using System.Linq;
using System;
using static SnapshotUtility;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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


#if UNITY_EDITOR
    [InitializeOnLoadMethod()]
#endif
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
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
        IComponentApplier applier = GetRelevantApplier(target.GetType(), field.path);
        if (applier != null) return applier.Apply(target, field);
        return false;
    }

    public static bool IsFieldSupported(Type componentType, string fieldPath)
    {
        IComponentApplier applier = GetRelevantApplier(componentType, fieldPath);
        if (applier != null) return applier.Supports(fieldPath);
        return false;
    }
    public static bool IsFieldIgnored(Type componentType, string fieldPath)
    {
        IComponentApplier applier = GetRelevantApplier(componentType, fieldPath, true);
        if (applier != null) return applier.Ignores(fieldPath);
        return false;
    }

    private static IComponentApplier GetRelevantApplier(Type componentType, string fieldPath, bool ignoring = false)
    {
        List<IComponentApplier> relevantAppliers = new List<IComponentApplier>();
        relevantAppliers = appliers.Values.Where(a => a.TargetType.IsAssignableFrom(componentType)).OrderByDescending(a => GetInheritanceDepth(a.TargetType)).ToList();

        // Find the applier that can deal with the given path and use it
        foreach (IComponentApplier applier in relevantAppliers) if (ignoring ? applier.Ignores(fieldPath) : applier.Supports(fieldPath)) return applier;
        return null;
    }
    private static int GetInheritanceDepth(Type type)
    {
        int depth = 0;
        while (type.BaseType != null)
        {
            depth++;
            type = type.BaseType;
        }
        return depth;
    }
}