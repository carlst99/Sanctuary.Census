using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Sanctuary.Census.Common.Objects;
using Sanctuary.Census.Json;
using Sanctuary.Census.Models;
using Sanctuary.Census.Models.Collections;
using Sanctuary.Census.Util;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Sanctuary.Census.Controllers;

/// <summary>
/// Returns collection data.
/// </summary>
[ApiController]
[Produces("application/json")]
public class CollectionController : ControllerBase
{
    /// <summary>
    /// The maximum number of elements that may be returned from a query.
    /// </summary>
    public const int MAX_LIMIT = 10000;

    private readonly CollectionsContext _collectionsContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionController"/> class.
    /// </summary>
    /// <param name="collectionsContext">The collections context.</param>
    public CollectionController(CollectionsContext collectionsContext)
    {
        _collectionsContext = collectionsContext;
    }

    /// <summary>
    /// Gets the available collections.
    /// </summary>
    /// <param name="environment">The environment to retrieve the collections of.</param>
    /// <returns>The available collections.</returns>
    /// <response code="200">Returns the list of collections.</response>
    /// <response code="400">If the given environment is invalid.</response>
    [HttpGet("get/{environment}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<DataResponse<Datatype>> GetCollectionInfos(PS2Environment environment)
        => environment is not PS2Environment.PS2
            ? GetInvalidEnvironmentResult()
            : new DataResponse<Datatype>(_collectionsContext.Datatypes, "datatype");

    /// <summary>
    /// Counts the number of a available collections.
    /// </summary>
    /// <param name="environment">The environment to retrieve the collections of.</param>
    /// <returns>The collection count.</returns>
    /// <response code="200">Returns the number of collections.</response>
    /// <response code="400">If the given environment is invalid.</response>
    [HttpGet("count/{environment}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<CollectionCount> CountCollectionInfos(PS2Environment environment) =>
        environment is not PS2Environment.PS2
            ? GetInvalidEnvironmentResult()
            : new ActionResult<CollectionCount>(_collectionsContext.Datatypes.Count);

    /// <summary>
    /// Retrieves data from a collection.
    /// </summary>
    /// <param name="environment">The environment to retrieve the collection from.</param>
    /// <param name="collectionName">The name of the collection to retrieve data from.</param>
    /// <param name="start">The index into the collection at which to start returning elements.</param>
    /// <param name="limit">The maximum number of elements to return from the collection.</param>
    /// <returns>Elements of the collection.</returns>
    /// <response code="200">Returns the elements of the collection.</response>
    /// <response code="400">If the given environment or provided query parameters are invalid.</response>
    /// <response code="404">If the given collection does not exist.</response>
    [HttpGet("get/{environment}/{collectionName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<DataResponse<object>> GetCollection
    (
        PS2Environment environment,
        string collectionName,
        [FromQuery(Name = "c:start")] int start,
        [FromQuery(Name = "c:limit")] int limit = 100
    )
    {
        if (environment is not PS2Environment.PS2)
            return GetInvalidEnvironmentResult();

        if (start < 0)
            return BadRequest("c:start may not be less than zero");

        if (limit is < 1 or > MAX_LIMIT)
            return BadRequest($"c:limit must be between 1 and {MAX_LIMIT}, inclusive");

        uint? id = null;
        foreach ((string paramName, StringValues paramValues) in HttpContext.Request.Query)
        {
            if (paramName.StartsWith("c:"))
                continue;

            if (!paramName.Contains("_id"))
                return BadRequest("Filtering on fields other than the collection's ID field is not currently support");

            if (paramValues.Count != 1)
                return BadRequest("Searching for more than a single ID is not currently supported");

            if (!int.TryParse(paramValues[0], out int maybeId))
                return BadRequest("Failed to parse the ID value");

            id = (uint?)maybeId;
        }

        return collectionName switch {
            "currency" => ConvertCollection(_collectionsContext.Currencies, id, start, limit, "currency"),
            "experience" => ConvertCollection(_collectionsContext.Experiences, id, start, limit, "experience"),
            "faction" => ConvertCollection(_collectionsContext.Factions, id, start, limit, "faction"),
            "fire_group" => ConvertCollection(_collectionsContext.FireGroups, id, start, limit, "fire_group"),
            "fire_group_to_fire_mode" => ConvertCollection(_collectionsContext.FireGroupsToFireModes, id, start, limit, "fire_group_to_fire_mode"),
            "fire_mode_2" => ConvertCollection(_collectionsContext.FireModes, id, start, limit, "fire_mode_2"),
            "fire_mode_to_projectile" => ConvertCollection(_collectionsContext.FireModeToProjectileMap, id, start, limit, "fire_mode_to_projectile"),
            "item" => ConvertCollection(_collectionsContext.Items, id, start, limit, "item"),
            "item_category" => ConvertCollection(_collectionsContext.ItemCategories, id, start, limit, "item_category"),
            "item_to_weapon" => ConvertCollection(_collectionsContext.ItemsToWeapon, id, start, limit, "item_to_weapon"),
            "player_state_group_2" => ConvertCollection(_collectionsContext.PlayerStateGroups, id, start, limit, "player_state_group_2"),
            "profile" => ConvertCollection(_collectionsContext.Profiles, id, start, limit, "profile"),
            "profile_2" => ConvertCollection(_collectionsContext.Profiles, id, start, limit, "profile_2"),
            "projectile" => ConvertCollection(_collectionsContext.Projectiles, id, start, limit, "projectile"),
            "weapon" => ConvertCollection(_collectionsContext.Weapons, id, start, limit, "weapon"),
            "weapon_ammo_slot" => ConvertCollection(_collectionsContext.WeaponAmmoSlots, id, start, limit, "weapon_ammo_slot"),
            "weapon_to_fire_group" => ConvertCollection(_collectionsContext.WeaponToFireGroup, id, start, limit, "weapon_to_fire_group"),
            "world" => ConvertCollection(_collectionsContext.Worlds, id, start, limit, "world"),
            _ => GetRedirectToCensusResult()
        };
    }

    /// <summary>
    /// Retrieves data from a collection.
    /// </summary>
    /// <param name="environment">The environment to retrieve the collection from.</param>
    /// <param name="collectionName">The name of the collection to retrieve data from.</param>
    /// <returns>Elements of the collection.</returns>
    /// <response code="200">Returns the count of the collection.</response>
    /// <response code="400">If the given environment or provided query parameters are invalid.</response>
    /// <response code="404">If the given collection does not exist.</response>
    [HttpGet("count/{environment}/{collectionName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CollectionCount> CountCollection
    (
        PS2Environment environment,
        string collectionName
    )
    {
        if (environment is not PS2Environment.PS2)
            return GetInvalidEnvironmentResult();

        return collectionName switch {
            "currency" => (CollectionCount)_collectionsContext.Currencies.Count,
            "experience" => (CollectionCount)_collectionsContext.Experiences.Count,
            "faction" => (CollectionCount)_collectionsContext.Factions.Count,
            "fire_group" => (CollectionCount)_collectionsContext.FireGroups.Count,
            "fire_group_to_fire_mode" => (CollectionCount)_collectionsContext.FireGroupsToFireModes.Values.SelectMany(f => f).Count(),
            "fire_mode_2" => (CollectionCount)_collectionsContext.FireModes.Count,
            "fire_mode_to_projectile" => (CollectionCount)_collectionsContext.FireModeToProjectileMap.Count,
            "item" => (CollectionCount)_collectionsContext.Items.Count,
            "item_category" => (CollectionCount)_collectionsContext.ItemCategories.Count,
            "item_to_weapon" => (CollectionCount)_collectionsContext.ItemsToWeapon.Count,
            "player_state_group_2" => (CollectionCount)_collectionsContext.PlayerStateGroups.Values.SelectMany(f => f).Count(),
            "profile" => (CollectionCount)_collectionsContext.Profiles.Count,
            "profile_2" => (CollectionCount)_collectionsContext.Profiles.Count,
            "projectile" => (CollectionCount)_collectionsContext.Projectiles.Count,
            "weapon" => (CollectionCount)_collectionsContext.Weapons.Count,
            "weapon_ammo_slot" => (CollectionCount)_collectionsContext.WeaponAmmoSlots.Values.SelectMany(f => f).Count(),
            "weapon_to_fire_group" => (CollectionCount)_collectionsContext.WeaponToFireGroup.Values.SelectMany(f => f).Count(),
            "world" => (CollectionCount)_collectionsContext.Worlds.Count,
            _ => GetRedirectToCensusResult()
        };
    }

    [HttpGet("get/ps2/test")]
    public IEnumerable<ExpandoObject> Test()
    {
        ExpressionDataShaper<Item> shaper = new(new SnakeCaseJsonNamingPolicy());
        Item i = _collectionsContext.Items[6013885];
        Console.WriteLine(shaper.GetValue(i, "item_id"));
        Console.WriteLine(shaper.GetValue(i, "item_id").GetType());

        var xx = _collectionsContext.Items.Values.Where(c => shaper.GetValue(c, "item_id")?.Equals(6013885) == true);
        Console.WriteLine(xx.Count());
        return _collectionsContext.Items.Values
            .Where(c => shaper.GetValue(c, "item_id")?.Equals(6013885) == true)
            .Select(c => shaper.ShapeDataInverted(c, Array.Empty<string>()));

        IEnumerable<object> col1 = _collectionsContext.GetCollectionByName(out string keyPropertyName);

        foreach (object o in col1)
        {
            Currency c = (Currency)o;
            ExpandoObject dyn = shaper.ShapeDataInverted(o, Array.Empty<string>());
            dyn.TryAdd("join", shaper.ShapeDataInverted(o, Array.Empty<string>()));
            //yield return dyn;
        }
    }

    private static DataResponse<object> ConvertCollection<TValue>
    (
        IReadOnlyDictionary<uint, TValue> collection,
        uint? id,
        int start,
        int limit,
        string dataTypeName
    ) where TValue : notnull
    {
        IReadOnlyList<object> filtered = FilterCollection(collection, id, start, limit);
        return new DataResponse<object>(filtered, dataTypeName);
    }

    private static IReadOnlyList<object> FilterCollection<TValue>
    (
        IReadOnlyDictionary<uint, TValue> collection,
        uint? id,
        int start,
        int limit
    ) where TValue : notnull
    {
        if (typeof(TValue).IsGenericType && typeof(TValue).GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
        {
            if (id is not null)
            {
                return !collection.ContainsKey(id.Value)
                    ? Array.Empty<object>()
                    : (IReadOnlyList<object>)collection[id.Value];
            }

            return collection.Values
                .Cast<IReadOnlyList<object>>()
                .SelectMany(o => o)
                .Skip(start)
                .Take(limit)
                .ToList();
        }

        if (id is not null)
        {
            return !collection.ContainsKey(id.Value)
                ? Array.Empty<object>()
                : new object[] { collection[id.Value] };
        }

        List<object> elements2 = collection.Values
            .Skip(start)
            .Take(limit)
            .Cast<object>()
            .ToList();

        return elements2;
    }

    private RedirectResult GetRedirectToCensusResult()
    {
        string url = "http://census.daybreakgames.com";
        if (HttpContext.Items.TryGetValue("ServiceId", out object? serviceId))
            url += (string)serviceId!;

        return Redirect(url + HttpContext.Request.Path + HttpContext.Request.QueryString);
    }

    private BadRequestObjectResult GetInvalidEnvironmentResult()
        => BadRequest("Valid environments are " + PS2Environment.PS2);
}
