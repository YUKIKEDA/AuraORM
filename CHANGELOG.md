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
- Joins: `InnerJoin`, `LeftJoin`, `RightJoin`, `FullJoin` (not on MySQL / MariaDB), and `CrossJoin`, up to eight tables.
  The nullable side of an outer join gets nullable column sets, so a value type read from it is nullable; a whole row of
  it is `null` when nothing matched. `Table.As("alias")` names a table, and a table used twice needs an alias.
- `GroupBy` with one key or a tuple of up to eight keys, after `From`, a join, or `Where`, and `Having`, which also takes
  the list form of `And` / `Or`. The lambdas after `GroupBy` get the key (`k` itself for one key, `k.Item1` … for a
  tuple) and the column sets, and accept only keys and aggregates, so a column that is not a key does not compile.
- The same expression written more than once in a statement is one parameter: a key with a value in the select list,
  `GROUP BY`, `HAVING`, and `ORDER BY`, and the SQL Server expansion of `IsDistinctFrom` / `IsNotDistinctFrom`.
- On MySQL and MariaDB, a key that is not a column is written as `MIN(key)` inside `HAVING`, because they do not match
  an expression there with the `GROUP BY` key. On SQL Server, `Sum` of `float` is cast back to `real`.
- A `GROUP BY` key without a column, and an `ORDER BY` key that is only a value, throw when the statement is built:
  SQL Server rejects them, and MySQL and MariaDB read a value there as a column position (an `ORDER BY` of a value
  silently ordered by the first column).
- Subqueries: `In` / `NotIn` with a subquery of the value's type, `Exists`, and `Scalar` (null when there is no row), and
  their group forms `GroupScalar` / `GroupExists` for `HAVING` and grouped projections, correlated with the grouped
  statement through `Outer(k)`. A subquery with `OrderBy` and no limit does not compile (CS0619, whose message names the
  fix), nor does a limited subquery in `In` on MySQL and MariaDB (`ILimitInSubqueryDialect`). A subquery of another
  dialect, a subquery in a `GROUP BY` key, a grouped statement's column used in a group subquery without `Outer`, and a
  correlation with an outer table whose name the subquery's table has too (which SQL would read as the inner table)
  throw when the statement is built.
- Set operations: `Union`, `UnionAll`, `Intersect`, and `Except` of statements with the same dialect and result type,
  one operator to a chain, with `OrderBy` by the position of an item (`SetItem`), `Limit`, and `Offset` on the whole
  result, and as subqueries. Mixed operators, a set operation after `OrderBy` or `Limit`, an operand with `OrderBy` or
  `Limit`, and another set operation as an operand fail with CS0619 and the fix; `OrderBy` after a projection that holds a
  whole row throws, and so does a SQLite set operation of more than 500 statements.
- Derived tables and CTEs: `As("name")` on a query whose rows are a `[QueryResult]` type or a `[Table]` row (generated for
  each type) names it; `db.From(t)` and joins write it as a derived table, and `db.With(t, …).From(…)` writes it as a CTE.
  The items inside are named after the members or columns, `t.As("t2")` references it again, and `[QueryResult]` types
  get `Columns` and `NullableColumns`. `As` on expressions or tuples fails with CS0619; a nested `With`, a table with the
  name of a CTE, and a named query of another dialect throw when the statement is built.
- Analyzers in the package: a comparison with a typed null constant (AURA0201) or with a value that may be null
  (AURA0202), which matches no row in SQL, a `NotIn` whose subquery may return null (AURA0203), and a raw SQL hole whose value is not a column type (AURA0301).
- On SQLite, ordering, comparing by size, and `Min` / `Max` / `Sum` / `Avg` of decimals throw when the statement is
  built, because SQLite stores decimals as text. A SQL Server `Limit(0)` and a column taken out of another query's
  lambda throw too.
