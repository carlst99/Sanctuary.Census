# Changelog

Date format: DD/MM/YYYY

## vNext

- Added full support for the `c:join` command.
- Added near-full support for the `c:tree` command. The `start` key is not yet supported.
- Added support for the `c:lang` command.
- Added support for the `c:includeNull` command.
- Restricted `c:limit` to a maximum value of 10000.

## 22/07/2022

- The `count` verb is supported again.
- Added more informative error responses.
- `c:limitPerDB` added as an alias of `c:limit`.
- Fixed `facility_link` and `map_region` being non-queryable.
- Fixed `weapon.heat_overheat_penalty_ms` being set incorrectly

## 21/07/2022

- Added the PTS environment.
- ⚠ Temporarily removed support for the `count` verb.
- Expanded the query interface:
  - Filtering now fully supported.
  - `c:case`.
  - `c:show`.
  - `c:hide`.
  - `c:sort`.
  - `c:has`.
  - `c:timing`.
- Added the following, **manually** updated collections:
  - `facility_link`.
  - `map_region`.
- Removed automatic redirect to DBG Census for unrecognised collections.

## 17/07/2022

- Added the `weapon_to_fire_group` collection.
- Fixed the `count` verb returning incorrect values for certain collections.
- Fixed the `count` verb not redirecting to DBG Census for unknown collections.
- `fire_mode_2.recoil_angle_max` and `fire_mode_2.recoil_angle_min` are no longer nullable.

## 16/07/2022

- Added automatic redirect to DBG Census for unknown collections.
- Initial Release 🎉
