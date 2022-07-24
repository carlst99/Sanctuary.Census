﻿using Sanctuary.Census.Attributes;
using Sanctuary.Census.Common.Objects.CommonModels;
using System.ComponentModel.DataAnnotations;

namespace Sanctuary.Census.Models.Collections;

/// <summary>
/// Represents a PlanetSide 2 server.
/// </summary>
/// <param name="WorldID">The ID of the server.</param>
/// <param name="Name">The name of the server.</param>
/// <param name="IsLocked">Indicates whether the server is locked.</param>
/// <param name="IsUnprivilegedAccessAllowed">Indicates whether standard accounts are allowed access to the server.</param>
[Collection(PrimaryJoinField = nameof(World.WorldID))]
public record World
(
    [property:Key] uint WorldID,
    LocaleString Name,
    bool IsLocked,
    bool IsUnprivilegedAccessAllowed
);
