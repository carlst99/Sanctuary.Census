# Migrating from Census

This document serves as an outline of how well Sanctuary.Census maintains parity with DBG Census.

## Queries

The basic structure of a query is supported. Submitting a service ID is optional, and it's not logged in any way if you do.

Both the `get` and `count` verbs are supported.

Filtering (searching, querying) is fully supported.

Supported commands:

- `c:case`.
- `c:limit` (default `100`, max `10 000`).
- `c:limitPerDB` (overrides `c:limit`).
- `c:join` - unlike DBG Census, all siblings may use child joins. However, a single query is limited to 25 joins total.
- `c:start`.
- `c:show`.
- `c:hide`.
- `c:has`.
- `c:includeNull`.
- `c:lang` - allows a comma-separated list of language codes, rather than a single code.
- `c:sort`.
- `c:tree` - the `start` key is not yet supported.
- `c:timing` - inserts a timestamp indicating the time taken to query the database, rather than the more specific model of the DBG Census.

## Response Shape

The shape of the response document should be exactly the same as DBG Census. Please note however that
number, boolean and `null` values are represented appropriately, rather than as strings.

### Error Responses

Sanctuary.Census uses a different set of error codes, the definitions of which [can be found here](../Sanctuary.Census/Models/QueryErrorCode.cs).
Please note that the error response format is not consistent, although it should match Census for most query-related errors.

## Collections

ℹ️ Please see the [collection model definitions here](../Sanctuary.Census/Models/Collections).

This section lists the Collections provided by Sanctuary.Census, and compares them to the DBG Census.
Many collections also add additional data on top of the DBG Census data, but this is not documented here.

⚠️ If a DBG Census collection is not listed here, it not supported.

### 🌠 Gold Tier Collections

These collections provide the same data as their DBG Census equivalents. The shape is not guaranteed to match,
but is likely to be very similar.

- currency
- experience
- facility_link
- faction
- fire_group
- fire_group_to_fire_mode
- fire_mode_to_projectile
- item_category
- item_to_weapon
- player_state_group_2
- ⚠ profile_2 - please query on `profile` instead, which contains all the data of this collection
- weapon_to_fire_group

### 🌟 Silver Tier Collections

These collections are missing small amounts of data as compared to their DBG Census equivalents, or are shaped differently
in such a way that retrieving certain data may not be immediately obvious.

- item
- map_region
- projectile
- weapon
- weapon_ammo_slot
- world

#### item

Missing the `is_vehicle_weapon` and `is_default_attachment` fields. The former can be replaced by checking
if the `item_category_id` matches or inherits from **item_category** `104 - Vehicle Weapons`.

#### map_region

Missing the `reward_amount` and `reward_currency_id` fields.

#### projectile

Missing the `tether_distance` field.

#### weapon

The `heat_capacity` field has been replaced by the `heat_threshold` field on any fire modes that
are linked to the weapon.

#### weapon_ammo_slot

Missing the `refill_ammo_rate` and `refill_ammo_delay_ms` fields.

#### world

Missing the `state` field. This is partially replaced by the `is_locked` field.

### ⭐ Bronze Tier Collections

These collections are missing significant amounts of data, or are shaped very differently.

- fire_mode
- profile

#### fire_mode

Missing the follow fields:
- `damage_direct_effect_id`
- `lockon_acquire_close_ms`
- `lockon_acquire_far_ms`
- `lockon_acquire_ms`
- `lockon_angle`
- `lockon_lose_ms`
- `lockon_maintain`
- `lockon_radius`
- `lockon_range`
- `lockon_range_close`
- `lockon_range_far`
- `lockon_required`

Of note is that lock-on parameters seem to be calculated dynamically by the game nowadays, using an unknown
algorithm. Hence, it's not surprising that these fields have been un-obtainable and you should consider
the DBG Census equivalents invalid.

#### profile

Missing the `movement_speed`, `backpedal_speed_modifier`, `sprint_speed_modifier` and `strafe_speed_modifier` fields.
