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
- `Database<TDialect>` on a `DbDataSource` or a `DbConnection`, and `Transaction<TDialect>` (`BeginTransactionAsync`,
  `InTransactionAsync`).
- Raw SQL through interpolated strings, where values become parameters: fragments (`Raw`, `Raw<T>`, `RawAggregate<T>`,
  `RawHaving`, `Unsafe`, `Identifier`), queries read by column name (`QueryAsync<T>`) or as tuples, scalars, and commands
  (`ExecuteAsync`).
- The SELECT builder on one table: `From(table)`, `Where`, `Select` / `SelectDistinct` (a column, the whole row, a
  tuple of up to 8 items, or a result projection), `OrderBy` with `Asc` / `Desc`, `Limit`, and `Offset`, read with
  `ToListAsync`, `AsAsyncEnumerable`, `First`, `Single`, and their `OrDefault` forms. Clauses come in SQL order, and a
  limit or an offset that a dialect does not accept in that place (such as a SQL Server limit without `ORDER BY`) does
  not compile.
- `Where` takes the list form of `And` / `Or`, which skips null conditions and writes no `WHERE` when none is left, for
  filters whose number is known only at run time.
- The `OrderBy` of a `SelectDistinct` takes only the selected items, named by position (`ORDER BY 1 ASC`), because
  PostgreSQL and SQL Server reject other keys; a distinct whole row has no `OrderBy`.
- `SqlBuilder<TDialect>` builds statements without a connection, for `ToSql()` only; running them does not compile.
- Analyzers in the package: a comparison with a typed null constant (AURA0201) or with a value that may be null
  (AURA0202), which matches no row in SQL, and a raw SQL hole whose value is not a column type (AURA0301).
- On SQLite, ordering, comparing by size, and `Min` / `Max` / `Sum` / `Avg` of decimals throw when the statement is
  built, because SQLite stores decimals as text. A SQL Server `Limit(0)` and a column taken out of another query's
  lambda throw too.
