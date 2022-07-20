﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson;
using MongoDB.Driver;
using Sanctuary.Census.Abstractions.Database;
using Sanctuary.Census.Common.Objects;
using Sanctuary.Census.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

    private static readonly char[] QueryCommandIdentifier = { 'c', ':' };

    private readonly IMongoContext _mongoContext;
    private readonly IMemoryCache _memoryCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionController"/> class.
    /// </summary>
    /// <param name="mongoContext">The Mongo DB collections context.</param>
    /// <param name="memoryCache">The memory cache.</param>
    public CollectionController(IMongoContext mongoContext, IMemoryCache memoryCache)
    {
        _mongoContext = mongoContext;
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// Gets the available collections.
    /// </summary>
    /// <param name="environment">The environment to retrieve the collections from.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>The available collections.</returns>
    /// <response code="200">Returns the list of collections.</response>
    [HttpGet("get/{environment}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<DataResponse<Datatype>> GetDatatypesAsync(PS2Environment environment, CancellationToken ct = default)
    {
        IReadOnlyList<Datatype> dataTypes = await GetAndCacheDatatypeListAsync(environment, ct);
        return new DataResponse<Datatype>(dataTypes, "datatype", null);
    }

    /// <summary>
    /// Counts the number of available collections.
    /// </summary>
    /// <param name="environment">The environment to retrieve the collections from.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> that can be used to stop the operation.</param>
    /// <returns>The collection count.</returns>
    /// <response code="200">Returns the number of collections.</response>
    [HttpGet("count/{environment}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<CollectionCount> CountDatatypesAsync(PS2Environment environment, CancellationToken ct = default)
        => (await GetAndCacheDatatypeListAsync(environment, ct)).Count;

    [HttpGet("/get/{environment}/{collectionName}")]
    [Produces("application/json", Type = typeof(object))]
    public async Task<DataResponse<BsonDocument>> QueryCollectionAsync
    (
        PS2Environment environment,
        string collectionName,
        [FromQuery] CollectionQueryParameters queryParams
    )
    {
        IMongoDatabase db = _mongoContext.GetDatabase(environment);
        IMongoCollection<BsonDocument> coll = db.GetCollection<BsonDocument>(collectionName);

        ProjectionDefinition<BsonDocument>? projection = Builders<BsonDocument>.Projection
            .Exclude("_id");

        if (queryParams.Show is not null)
        {
            foreach (string value in queryParams.Show.SelectMany(s => s.Split(',')))
                projection = projection.Include(value);
        }

        if (queryParams.Hide is not null)
        {
            foreach (string value in queryParams.Hide.SelectMany(h => h.Split(',')))
                projection = projection.Exclude(value);
        }

        FilterDefinitionBuilder<BsonDocument> filterBuilder = Builders<BsonDocument>.Filter;
        FilterDefinition<BsonDocument> filter = filterBuilder.Empty;
        if (queryParams.HasFields is not null)
        {
            foreach (string value in queryParams.HasFields.SelectMany(s => s.Split(',')))
                filter &= filterBuilder.Ne(value, BsonNull.Value);
        }

        foreach ((string paramName, StringValues paramValues) in HttpContext.Request.Query)
        {
            if (paramName.AsSpan().StartsWith(QueryCommandIdentifier))
                continue;

            foreach (string value in paramValues.SelectMany(s => s.Split(',')))
            {
                if (value.Length == 0)
                    continue;

                if (value[0] is '<' or '[' or '>' or ']' or '^' or '*' or '!' && value.Length < 2)
                    continue;

                // TODO: Need to convert value to correct type
                filter &= value[0] switch {
                    '<' => filterBuilder.Lt(paramName, value[1..]),
                    '[' => filterBuilder.Lte(paramName, value[1..]),
                    '>' => filterBuilder.Gt(paramName, value[1..]),
                    ']' => filterBuilder.Gte(paramName, value[1..]),
                    '^' => filterBuilder.Regex(paramName, value),
                    '*' => filterBuilder.Regex(paramName, value[1..]),
                    '!' => filterBuilder.Ne(paramName, value[1..]),
                    _ => filterBuilder.Eq(paramName, value)
                };
            }
        }

        IAggregateFluent<BsonDocument> aggregate = coll.Aggregate()
            .Match(filter)
            .Project(projection);

        if (queryParams.Sorts is not null)
        {
            foreach (string value in queryParams.Sorts.SelectMany(s => s.Split(',')))
            {
                string[] components = value.Split(':');
                int sortDirection = 1;

                if (components.Length == 2 && components[1] == "-1")
                    sortDirection = -1;

                aggregate = aggregate.Sort(new BsonDocument(components[0], sortDirection));
            }
        }

        BsonArray subPipeline = new()
        {
            new BsonDocument
            (
                "$match",
                new BsonDocument
                (
                    "$expr",
                    new BsonDocument
                    (
                        "$eq",
                        new BsonArray { 0, "$fire_mode_index" }
                    )
                )
            ),
            new BsonDocument
            (
                "$project",
                new BsonDocument("_id", 0)
            ),
            new BsonDocument
            (
                "$lookup",
                new BsonDocument("from", "fire_group")
                    .Add("localField", "fire_group_id")
                    .Add("foreignField", "fire_group_id")
                    .Add("as", "fire_mode_to_fire_group")
            )
        };

        BsonDocument lookup = new
        (
            "$lookup",
            new BsonDocument("from", "fire_group_to_fire_mode")
                .Add("localField", "fire_group_id")
                .Add("foreignField", "fire_group_id")
                .Add("pipeline", subPipeline)
                .Add("as", "fire_group_to_fire_mode")
        );

        aggregate = aggregate.Skip(queryParams.Start)
            .Limit(queryParams.Limit)
            .AppendStage<BsonDocument>(lookup);

        Stopwatch st = new();
        st.Start();
        List<BsonDocument> records = await aggregate.ToListAsync();
        st.Stop();
        return new DataResponse<BsonDocument>
        (
            records,
            collectionName,
            queryParams.ShowTimings ? st.Elapsed : null
        );
    }

    private async ValueTask<IReadOnlyList<Datatype>> GetAndCacheDatatypeListAsync(PS2Environment environment, CancellationToken ct)
    {
        if (_memoryCache.TryGetValue((typeof(Datatype), environment), out List<Datatype>? dataTypes) && dataTypes is not null)
            return dataTypes;

        dataTypes = new List<Datatype>();
        IMongoDatabase db = _mongoContext.GetDatabase(environment);
        IAsyncCursor<string> collNames = await db.ListCollectionNamesAsync(cancellationToken: ct);
        while (await collNames.MoveNextAsync(ct))
        {
            foreach (string collName in collNames.Current)
            {
                IMongoCollection<BsonDocument> coll = db.GetCollection<BsonDocument>(collName);
                long count = await coll.CountDocumentsAsync(new BsonDocument(), null, ct);
                dataTypes.Add(new Datatype(collName, (int)count));
            }
        }

        _memoryCache.Set((typeof(Datatype), environment), dataTypes, TimeSpan.FromHours(1));
        return dataTypes;
    }

    // private RedirectResult GetRedirectToCensusResult()
    // {
    //     string url = "http://census.daybreakgames.com";
    //     if (HttpContext.Items.TryGetValue("ServiceId", out object? serviceId))
    //         url += (string)serviceId!;
    //
    //     return Redirect(url + HttpContext.Request.Path + HttpContext.Request.QueryString);
    // }
}
