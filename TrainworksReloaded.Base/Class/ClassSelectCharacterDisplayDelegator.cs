using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TrainworksReloaded.Base.Class
{
    public class ClassSelectCharacterDisplayDelegator
    {
        internal readonly IDictionary<string, List<GameObject>> ClassCharacterDisplays = new Dictionary<string, List<GameObject>>();

        public void Add(string classID, List<GameObject> characterDisplays)
        {
            ClassCharacterDisplays.Add(classID, characterDisplays);
        }

        public List<GameObject>? GetCharacterDisplays(string classID)
        {
            return ClassCharacterDisplays.GetValueOrDefault(classID);
        }
    }
}
