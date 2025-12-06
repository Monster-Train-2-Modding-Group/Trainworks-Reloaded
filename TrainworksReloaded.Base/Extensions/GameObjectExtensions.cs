using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TrainworksReloaded.Base.Extensions
{
    public static class GameObjectExtensions
    {
        public static void CopyPrefabToObject(this GameObject original, GameObject prefab)
        {
            var clonedPrefab = GameObject.Instantiate(prefab);

            foreach (var component in original.GetComponents<Component>())
            {
                if (component is Transform)
                    continue;
                GameObject.Destroy(component);
            }
            original.transform.DestroyAllChildren();
            original.layer = 0;
            int childCount = clonedPrefab.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = clonedPrefab.transform.GetChild(i);
                child.SetParent(original.transform);
            }
            foreach (Component component in clonedPrefab.GetComponents<Component>())
            {
                if (component is Transform)
                    continue;

                Component newComponent = original.AddComponent(component.GetType());
                System.Type componentType = component.GetType();

                foreach (var field in componentType.GetFields())
                {
                    if (field.IsLiteral)
                        continue;

                    field.SetValue(newComponent, field.GetValue(component));
                }
            }
            GameObject.Destroy(clonedPrefab);
        }

    }
}
