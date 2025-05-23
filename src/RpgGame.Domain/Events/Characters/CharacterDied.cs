﻿using RpgGame.Domain.Events.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpgGame.Domain.Events.Characters
{

    /// <summary>
    /// Event raised when character dies
    /// </summary>
    public class CharacterDied : DomainEventBase
    {
        public string CharacterName { get; }
        public int Level { get; }
        public string Location { get; }

        public CharacterDied(Guid aggregateId, int version, string characterName, int level, string location) 
            : base(aggregateId, version)
        {
            CharacterName = characterName;
            Level = level;
            Location = location;
        }
    }
}
