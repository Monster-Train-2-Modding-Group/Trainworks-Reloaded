using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TrainworksReloaded.Base.Class
{
    public class ClassCardStyleDelegator
    {
        internal readonly IDictionary<ClassCardStyle, IDictionary<CardType, Sprite>> ClassCardStyles = new Dictionary<ClassCardStyle, IDictionary<CardType, Sprite>>();

        public void Add(ClassCardStyle classStyle, IDictionary<CardType, Sprite> sprites)
        {
            ClassCardStyles.Add(classStyle, sprites);
        }

        public IDictionary<CardType, Sprite>? GetClassCardStyleSprites(ClassCardStyle classStyle)
        {
            return ClassCardStyles.GetValueOrDefault(classStyle);
        }
    }
}
