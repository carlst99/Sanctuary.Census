using Sanctuary.Census.Json;
using Sanctuary.Census.Models;
using Sanctuary.Census.Models.Collections;
using System;
using System.Collections.Generic;

namespace Sanctuary.Census;

/// <summary>
/// Maintains a store of the DTO collections.
/// </summary>
public class CollectionsContext
{
    /// <summary>
    /// Gets the collection information.
    /// </summary>
    public IReadOnlyList<Datatype> Datatypes { get; set; }

    /// <summary>
    /// Gets the Currency collection, indexed by <see cref="Currency.CurrencyID"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, Currency> Currencies { get; set; }

    /// <summary>
    /// Gets the Experience collection, indexed by <see cref="Experience.ExperienceID"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, Experience> Experiences { get; set; }

    /// <summary>
    /// Gets the Faction collection, indexed by <see cref="Faction.FactionID"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, Faction> Factions { get; set; }

    /// <summary>
    /// Gets the FireGroup collection, indexed by <see cref="FireGroup.FireGroupID"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, FireGroup> FireGroups { get; set; }

    /// <summary>
    /// Gets the FireGroupToFireMode collection, indexed by <see cref="FireGroupToFireMode.FireGroupId"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, IReadOnlyList<FireGroupToFireMode>> FireGroupsToFireModes { get; set; }

    /// <summary>
    /// Gets the FireMode2 collection, indexed by <see cref="FireMode2.FireModeID"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, FireMode2> FireModes { get; set; }

    /// <summary>
    /// Gets the FireModeToProjectile collection, indexed by <see cref="FireModeToProjectile.FireModeID"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, FireModeToProjectile> FireModeToProjectileMap { get; set; }

    /// <summary>
    /// Gets the Item collection, indexed by <see cref="Item.ItemID"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, Item> Items { get; set; }

    /// <summary>
    /// Gets the ItemCategory collection, indexed by <see cref="ItemCategory.ItemCategoryID"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, ItemCategory> ItemCategories { get; set; }

    /// <summary>
    /// Gets the ItemToWeapon collection, indexed by <see cref="ItemToWeapon.ItemId"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, ItemToWeapon> ItemsToWeapon { get; set; }

    /// <summary>
    /// Gets the PlayerStateGroup2 collection, indexed by <see cref="PlayerStateGroup2.PlayerStateGroupId"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, IReadOnlyList<PlayerStateGroup2>> PlayerStateGroups { get; set; }

    /// <summary>
    /// Gets the Profile collection, indexed by <see cref="Profile.ProfileId"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, Profile> Profiles { get; set; }

    /// <summary>
    /// Gets the Projectile collection, indexed by <see cref="Projectile.ProjectileId"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, Projectile> Projectiles { get; set; }

    /// <summary>
    /// Gets the Weapon collection, indexed by <see cref="Weapon.WeaponId"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, Weapon> Weapons { get; set; }

    /// <summary>
    /// Gets the WeaponAmmoSlot collection, indexed by <see cref="Weapon.WeaponId"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, IReadOnlyList<WeaponAmmoSlot>> WeaponAmmoSlots { get; set; }

    /// <summary>
    /// Gets the WeaponToFireGroup collection, indexed by <see cref="Weapon.WeaponId"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, IReadOnlyList<WeaponToFireGroup>> WeaponToFireGroup { get; set; }

    /// <summary>
    /// Gets the World collection, indexed by <see cref="World.WorldID"/>.
    /// </summary>
    public IReadOnlyDictionary<uint, World> Worlds { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionsContext"/> class.
    /// </summary>
    public CollectionsContext()
    {
        Datatypes = new List<Datatype>();
        Currencies = new Dictionary<uint, Currency>();
        Experiences = new Dictionary<uint, Experience>();
        Factions = new Dictionary<uint, Faction>();
        FireGroups = new Dictionary<uint, FireGroup>();
        FireGroupsToFireModes = new Dictionary<uint, IReadOnlyList<FireGroupToFireMode>>();
        FireModes = new Dictionary<uint, FireMode2>();
        FireModeToProjectileMap = new Dictionary<uint, FireModeToProjectile>();
        Items = new Dictionary<uint, Item>();
        ItemCategories = new Dictionary<uint, ItemCategory>();
        ItemsToWeapon = new Dictionary<uint, ItemToWeapon>();
        PlayerStateGroups = new Dictionary<uint, IReadOnlyList<PlayerStateGroup2>>();
        Profiles = new Dictionary<uint, Profile>();
        Projectiles = new Dictionary<uint, Projectile>();
        Weapons = new Dictionary<uint, Weapon>();
        WeaponAmmoSlots = new Dictionary<uint, IReadOnlyList<WeaponAmmoSlot>>();
        WeaponToFireGroup = new Dictionary<uint, IReadOnlyList<WeaponToFireGroup>>();
        Worlds = new Dictionary<uint, World>();
    }

    /// <summary>
    /// Builds the <see cref="Datatypes"/> list.
    /// </summary>
    public void BuildCollectionInfos()
    {
        SnakeCaseJsonNamingPolicy nameConverter = new();
        List<Datatype> collectionInfos = new();
        void AddCollection<T>(IReadOnlyDictionary<uint, T> collection)
        {
            Type tType = typeof(T);
            string typeName = tType.IsGenericType
                ? tType.GenericTypeArguments[0].Name
                : tType.Name;

            Datatype info = new
            (
                nameConverter.ConvertName(typeName),
                collection.Count
            );
            collectionInfos.Add(info);
        }

        AddCollection(Currencies);
        AddCollection(Experiences);
        AddCollection(Factions);
        AddCollection(FireGroups);
        AddCollection(FireGroupsToFireModes);
        AddCollection(FireModes);
        AddCollection(FireModeToProjectileMap);
        AddCollection(Items);
        AddCollection(ItemCategories);
        AddCollection(ItemsToWeapon);
        AddCollection(PlayerStateGroups);
        AddCollection(Profiles);
        collectionInfos.Add(new Datatype("profile_2", Profiles.Count));
        AddCollection(Projectiles);
        AddCollection(Weapons);
        AddCollection(WeaponAmmoSlots);
        AddCollection(WeaponToFireGroup);
        AddCollection(Worlds);

        Datatypes = collectionInfos;
    }

    public IEnumerable<object> GetCollectionByName(out string keyPropertyName)
    {
        keyPropertyName = "currency_id";
        return Currencies.Values;
    }
}
