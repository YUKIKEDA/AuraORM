# Changelog

All notable changes to AuraORM are listed here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow
[Semantic Versioning](https://semver.org/spec/v2.0.0.html). A 0.x minor
version may break the public API.

## [Unreleased]

AuraORM is being rebuilt from its requirements (`docs/design.ja.md`). No
version has been published to NuGet.

### Added

- Expressions and functions (`using static AuraORM.Sql;`): comparisons, `NULL` handling, logic, arithmetic, strings,
  `CAST`, `CASE`, and aggregates, with the evaluation level (`PerRow` / `PerGroup`) in the type so that aggregates and
  row values cannot be mixed.
- The five dialect types (`SqliteDialect`, `PostgreSqlDialect`, `MySqlDialect`, `MariaDbDialect`, `SqlServerDialect`)
  and their capability interfaces.
- `SqlStatement`: the SQL text and parameters of a statement.
- The source generator, shipped inside the package: `[Table]` records and classes get `Table`, `Columns`,
  `NullableColumns`, and `Insert`; `[QueryResult]` types get `RowProjection` and `GroupProjection`. Schema errors are
  reported as AURA0101 to AURA0111.
