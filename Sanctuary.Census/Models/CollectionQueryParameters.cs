﻿using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace Sanctuary.Census.Models;

/// <summary>
/// Represents parameters that can be used when querying a collection.
/// </summary>
public class CollectionQueryParameters
{
    /// <summary>
    /// The position into the results of the query at which
    /// to begin returning records at.
    /// E.g. <c>c:start=75</c>.
    /// </summary>
    [FromQuery(Name = "c:start")]
    public int Start { get; set; }

    /// <summary>
    /// The maximum number of records to return.
    /// E.g. <c>c:limit=10</c>.
    /// </summary>
    [FromQuery(Name = "c:limit")]
    public int Limit { get; set; }

    /// <summary>
    /// The list of fields to include in the result.
    /// E.g. <c>c:show=item_id,name</c>.
    /// </summary>
    [FromQuery(Name = "c:show")]
    public IEnumerable<string>? Show { get; set; }

    /// <summary>
    /// The list of fields to exclude from the result.
    /// E.g. <c>c:hide=description,passive_ability_id</c>.
    /// </summary>
    [FromQuery(Name = "c:hide")]
    public IEnumerable<string>? Hide { get; set; }

    /// <summary>
    /// The fields to sort the query on. Use <c>1</c> to sort in
    /// ascending order, and <c>-1</c> to sort in descending order.
    /// E.g. <c>c:sort=item_id:-1,item_category:1</c>.
    /// </summary>
    [FromQuery(Name = "c:sort")]
    public IEnumerable<string>? Sorts { get; set; }

    /// <summary>
    /// Only include a result if it has these fields.
    /// E.g. <c>c:has=image_path,activatable_ability_id</c>.
    /// </summary>
    [FromQuery(Name = "c:has")]
    public IEnumerable<string>? HasFields { get; set; }

    /// <summary>
    /// Include the time taken to query the database
    /// in the response.
    /// E.g. <c>c:timing=true</c>.
    /// </summary>
    [FromQuery(Name = "c:timing")]
    public bool ShowTimings { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionQueryParameters"/> class.
    /// </summary>
    public CollectionQueryParameters()
    {
        Start = 0;
        Limit = 100;
    }
}