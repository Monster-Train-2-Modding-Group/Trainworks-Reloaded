using HarmonyLib;
using System;
using System.Collections.Generic;
using TrainworksReloaded.Base.Extensions;
using TrainworksReloaded.Core.Extensions;
using TrainworksReloaded.Core.Interfaces;
using UnityEngine;

namespace TrainworksReloaded.Base.Class
{
    public class ClassCardStyleFinalizer : IDataFinalizer
    {
        private readonly IModLogger<ClassCardStyleFinalizer> logger;
        private readonly ICache<IDefinition<ClassCardStyle>> cache;
        private readonly IRegister<Sprite> spriteRegister;
        private readonly ClassCardStyleDelegator delegator;

        public ClassCardStyleFinalizer(
            IModLogger<ClassCardStyleFinalizer> logger,
            ICache<IDefinition<ClassCardStyle>> cache,
            IRegister<Sprite> spriteRegister,
            ClassCardStyleDelegator delegator
        )
        {
            this.logger = logger;
            this.cache = cache;
            this.spriteRegister = spriteRegister;
            this.delegator = delegator;
        }

        public void FinalizeData()
        {
            foreach (var definition in cache.GetCacheItems())
            {
                FinalizeTrigger(definition);
            }
            cache.Clear();
        }

        private void FinalizeTrigger(IDefinition<ClassCardStyle> definition)
        {
            var configuration = definition.Configuration;
            var key = definition.Key;
            var classCardStyle = definition.Data;
            var id = definition.Id;

            logger.Log(LogLevel.Debug, $"Finalizing ClassCardStyle {key.GetId(TemplateConstants.ClassCardStyle, definition.Id)}...");

            Dictionary<CardType, Sprite> sprites = [];

            void AddSprite(CardType type, string field)
            {
                var reference = configuration.GetSection(field).ParseReference();
                if (reference != null)
                {
                    if (spriteRegister.TryLookupId(reference.ToId(key, TemplateConstants.Sprite), out var lookup, out var _))
                    {
                        sprites.Add(type, lookup);
                    }
                }
            }

            AddSprite(CardType.Equipment, "equipment_card_frame_sprite");
            AddSprite(CardType.Monster, "unit_card_frame_sprite");
            AddSprite(CardType.TrainRoomAttachment, "room_card_frame_sprite");
            AddSprite(CardType.Spell, "spell_card_frame_sprite");

            delegator.Add(classCardStyle, sprites);
        }
    }
}
