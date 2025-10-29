using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TrainworksReloaded.Base.Class
{
    public class ClassAssetsDelegator
    {
        internal readonly IDictionary<string, List<GameObject>> ClassCharacterDisplays = new Dictionary<string, List<GameObject>>();
        internal readonly IDictionary<string, Sprite> ClassCardDraftIcons = new Dictionary<string, Sprite>();

        public void Add(string classID, List<GameObject> characterDisplays)
        {
            ClassCharacterDisplays.Add(classID, characterDisplays);
        }

        public void Add(string classID, Sprite cardDraftIcon)
        {
            ClassCardDraftIcons.Add(classID, cardDraftIcon);
        }

        public List<GameObject>? GetCharacterDisplays(string classID)
        {
            return ClassCharacterDisplays.GetValueOrDefault(classID);
        }

        public Sprite? GetCardDraftIcon(string classID)
        {
            return ClassCardDraftIcons.GetValueOrDefault(classID);
        }
    }
}
