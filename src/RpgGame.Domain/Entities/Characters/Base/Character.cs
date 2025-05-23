﻿using System;
using System.Reflection;
using RpgGame.Domain.Base;
using RpgGame.Domain.Entities.Characters.Player;
using RpgGame.Domain.Enums;
using RpgGame.Domain.Events.Base;
using RpgGame.Domain.Events.Characters;
using RpgGame.Domain.Interfaces.Characters;

namespace RpgGame.Domain.Entities.Characters.Base
{
    /// <summary>
    /// Base abstract class for all characters in the game
    /// </summary>
    public abstract class Character : DomainEntity, ICharacter
    {

        // Protected fields - encapsulated implementation details
        protected string _name;
        protected int _health;
        protected int _maxHealth;
        protected int _level;
        protected int _strength;
        protected int _defense;

        // Properties - providing a controlled interface
        public string Name => _name;
        public int Health => _health;
        public int MaxHealth => _maxHealth;
        public int Level => _level;
        public bool IsAlive => _health > 0;
        public int Strength => _strength;  // Additional properties that may be used by derived classes
        public int Defense => _defense;

        /// <summary>
        /// Base constructor for all character types
        /// </summary>
        protected Character(string name, int health, int strength, int defense, int level = 1)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Character name cannot be empty", nameof(name));

            if (health <= 0)
                throw new ArgumentException("Health must be greater than zero", nameof(health));

            _name = name;
            _maxHealth = health;
            _health = health;
            _strength = strength;
            _defense = defense;
            _level = level;
        }

        /// <summary>
        /// Protected constructor for event sourcing
        /// </summary>
        protected Character(bool forEventSourcing)
        {
            // Minimal initialization, will be overridden by Apply methods
            _name = "";
            _maxHealth = 1;
            _health = 1;
            _level = 1;
            _strength = 1;
            _defense = 1;
        }

        /// <summary>
        /// Performs a basic attack on the target
        /// </summary>
        public virtual void Attack(ICharacter target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target), "Attack target cannot be null");

            if (!IsAlive)
            {
                OnAttackFailed("Cannot attack while defeated");
                return;
            }

            int damage = CalculateDamage();
            OnBeforeAttack(target, damage);
            target.TakeDamage(damage);
            OnAfterAttack(target, damage);
        }

        /// <summary>
        /// Takes damage from an attack
        /// </summary>
        public virtual void TakeDamage(int damage)
        {
            if (damage < 0)
                throw new ArgumentException("Damage cannot be negative", nameof(damage));

            // Apply defense to reduce incoming damage
            int actualDamage = Math.Max(1, damage - _defense);
            _health = Math.Max(0, _health - actualDamage);

            OnDamageTaken(actualDamage);

            if (!IsAlive)
            {
                OnDeath();
            }
        }

        /// <summary>
        /// Heals the character for the specified amount
        /// </summary>
        public virtual void Heal(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Heal amount cannot be negative", nameof(amount));

            if (!IsAlive)
            {
                OnHealFailed("Cannot heal while defeated");
                return;
            }

            int healthBefore = _health;
            _health = Math.Min(_maxHealth, _health + amount);
            int actualHeal = _health - healthBefore;

            OnHealed(actualHeal);
        }

        /// <summary>
        /// Increases the character's level and improves their stats
        /// </summary>
        public virtual void LevelUp()
        {
            int oldLevel = _level;
            _level++;

            // Base stat improvements
            int healthIncrease = 10;
            int strengthIncrease = 2;
            int defenseIncrease = 1;

            _maxHealth += healthIncrease;
            _health = _maxHealth; // Full heal on level up
            _strength += strengthIncrease;
            _defense += defenseIncrease;

            OnLevelUp(healthIncrease, strengthIncrease, defenseIncrease);

            // Raise domain event
            RaiseDomainEvent((id, version) => new CharacterLeveledUp(
                id,
                version,
                Name,
                oldLevel,
                _level,
                healthIncrease,
                strengthIncrease,
                defenseIncrease
            ));
        }

        // Protected methods (encapsulated implementation)

        /// <summary>
        /// Calculates the damage for an attack
        /// </summary>
        protected virtual int CalculateDamage()
        {
            // Base damage calculation with some randomness
            Random rnd = new Random();
            return _strength + rnd.Next(1, 6);
        }

        // Protected event methods for derived classes to override

        /// <summary>
        /// Called before an attack is executed
        /// </summary>
        protected virtual void OnBeforeAttack(ICharacter target, int damage)
        {
            Console.WriteLine($"{Name} attacks {target.Name} for {damage} damage!");
        }

        /// <summary>
        /// Called after an attack is executed
        /// </summary>
        protected virtual void OnAfterAttack(ICharacter target, int damage)
        {
            // Default implementation does nothing
        }

        /// <summary>
        /// Called when an attack fails
        /// </summary>
        protected virtual void OnAttackFailed(string reason)
        {
            Console.WriteLine($"{Name} cannot attack: {reason}");
        }

        /// <summary>
        /// Called when the character takes damage
        /// </summary>
        protected virtual void OnDamageTaken(int damage)
        {
            Console.WriteLine($"{Name} takes {damage} damage. Health: {_health}/{_maxHealth}");
        }

        /// <summary>
        /// Called when the character is healed
        /// </summary>
        protected virtual void OnHealed(int amount)
        {
            Console.WriteLine($"{Name} heals for {amount} health. Health: {_health}/{_maxHealth}");
        }

        /// <summary>
        /// Called when healing fails
        /// </summary>
        protected virtual void OnHealFailed(string reason)
        {
            Console.WriteLine($"{Name} cannot be healed: {reason}");
        }

        /// <summary>
        /// Called when the character levels up
        /// </summary>
        protected virtual void OnLevelUp(int healthIncrease, int strengthIncrease, int defenseIncrease)
        {
            Console.WriteLine($"{Name} leveled up to level {_level}!");
            Console.WriteLine($"New stats: Health: {_health}/{_maxHealth}, Strength: {_strength}, Defense: {_defense}");
        }

        /// <summary>
        /// Called when the character dies
        /// </summary>
        protected virtual void OnDeath()
        {
            Console.WriteLine($"{Name} has been defeated!");

            // Raise domain event with location info
            RaiseDomainEvent((id, version) => new CharacterDied(
                id,
                version,
                Name,
                Level,
                "Current Location" // Ideally get this from game context
            ));
        }

        /// <summary>
        /// Exports the current state of the character as a domain event
        /// </summary>
        /// <returns></returns>
        public virtual CharacterStateExported ExportState()
        {
            return new CharacterStateExported(
                Id, // Assuming Id is inherited from DomainEntity
                Version, // Assuming Version is inherited from DomainEntity
                Name,
                Health,
                MaxHealth,
                Level,
                Strength,
                Defense,
                IsAlive ? 1 : 0, // Pass the 'isAlive' parameter as required
                GetType().Name
            );
        }

        // Apply methods for event sourcing
        protected virtual void Apply(CharacterLeveledUp @event)
        {
            _name = @event.CharacterName;
            _level = @event.NewLevel;
            // You may need to calculate health, strength, etc. based on level
            _maxHealth = 100 + (@event.NewLevel * 10);
            _health = _maxHealth;
            _strength = 10 + (@event.NewLevel * 2);
            _defense = 5 + (@event.NewLevel);
        }

        protected virtual void Apply(CharacterDied @event)
        {
            _name = @event.CharacterName;
            _level = @event.Level;
            _health = 0;
        }

        /// <summary>
        /// Factory method to create the correct subtype from events
        /// </summary>
        public static Character FromEvents(Guid id, IEnumerable<IDomainEvent> events)
        {
            if (!events.Any())
                return null;

            // Find character creation event
            var creationEvent = events.OfType<CharacterCreatedEvent>().FirstOrDefault();
            if (creationEvent == null)
                throw new InvalidOperationException("Character creation event not found");

            // Create the right subtype
            Character character = creationEvent.CharacterType switch
            {
                CharacterType.Warrior => Warrior.CreateForEventSourcing(),
                CharacterType.Mage => Mage.CreateForEventSourcing(),
                CharacterType.Rogue => Rogue.CreateForEventSourcing(),
                _ => throw new InvalidOperationException($"Unknown character type: {creationEvent.CharacterType}")
            };

            // Set ID
            typeof(DomainEntity).GetProperty("Id")?.SetValue(character, id);

            // Apply events in order
            foreach (var @event in events.OrderBy(e => e.OccurredAt))
            {
                ApplyEventToCharacter(character, @event);
            }

            return character;
        }

        private static string GetCharacterTypeFromEvents(IEnumerable<IDomainEvent> events)
        {
            // Find character type in events
            foreach (var @event in events)
            {
                if (@event is CharacterCreatedEvent createdEvent)
                {
                    return createdEvent.CharacterType.ToString();
                }
            }

            throw new InvalidOperationException("Could not determine character type from events");
        }

        private static void ApplyEventToCharacter(Character character, IDomainEvent @event)
        {
            var eventType = @event.GetType();
            var method = character.GetType().GetMethod("Apply",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { eventType }, null);

            if (method != null)
            {
                method.Invoke(character, new object[] { @event });
            }
        }
    }
}