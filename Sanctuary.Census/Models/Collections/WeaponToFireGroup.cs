namespace Sanctuary.Census.Models.Collections;

/// <summary>
/// Represents a mapping between a <see cref="Weapon"/> and a <see cref="FireGroup"/>.
/// </summary>
/// <param name="WeaponId">The ID of the weapon.</param>
/// <param name="FireGroupId">The ID of the fire group.</param>
/// <param name="FireGroupIndex">The index of the fire group within the mapping list.</param>
public record WeaponToFireGroup
(
    uint WeaponId,
    uint FireGroupId,
    uint FireGroupIndex
);
