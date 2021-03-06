using Sanctuary.Census.Attributes;
using Sanctuary.Census.Common.Objects.CommonModels;
using System.ComponentModel.DataAnnotations;

namespace Sanctuary.Census.Models.Collections;

/// <summary>
/// Represents currency data.
/// </summary>
/// <param name="CurrencyID">The ID of the currency.</param>
/// <param name="Name">The currency's name.</param>
/// <param name="Description">The currency's description.</param>
/// <param name="IconImageSetID">The image set ID of the currency's icon.</param>
/// <param name="MapIconImageSetID">The image set ID of the currency's map icon.</param>
/// <param name="InventoryCap">The maximum amount of this currency a character may have.</param>
[Collection]
public record Currency
(
    [property:Key] uint CurrencyID,
    LocaleString Name,
    LocaleString? Description,
    uint IconImageSetID,
    uint MapIconImageSetID,
    uint? InventoryCap
);
