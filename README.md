# AuraORM

A type-safe SQL query builder for .NET, in the spirit of [Drizzle ORM](https://orm.drizzle.team/).

> **Status: being rebuilt.** AuraORM is being redesigned from its requirements. Nothing is published to NuGet yet, and the API described in the design is not implemented yet. Follow the progress in [`docs/roadmap.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/docs/roadmap.md).

[日本語](https://github.com/YUKIKEDA/AuraORM/blob/main/README.ja.md)

## Principles

- **SQL stays SQL.** The API follows SQL clause by clause. Anything valid in SQL that the API can express compiles; raw SQL fragments are available where it cannot, with values still bound as parameters.
- **Types reject only invalid queries.** A column of a table that is not in the query, operands of different types, a clause in the wrong order, a missing required column in `INSERT`, an `UPDATE` or `DELETE` without `WHERE`, a feature the database lacks: these do not compile.
- **Predictable SQL.** One builder call is one SQL clause. No hidden queries, joins, or round trips. Every query shows its SQL and parameters before it runs.
- **Safe for code written by AI agents.** Mistakes surface as compile errors with a stable diagnostic id and a message that names the fix, not as wrong results at run time.
- **NativeAOT and trimming.** No reflection, no expression trees, no dynamic code. Mapping code is generated at compile time.
- **Small.** The runtime library depends only on the .NET base library and `System.Data.Common`. Bring any ADO.NET provider.

## Databases

SQLite, PostgreSQL, MySQL (and MariaDB), and SQL Server.

## Requirements

.NET 8 or later.

## Contributing

See [`CONTRIBUTING.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/CONTRIBUTING.md).

## License

[MIT](https://github.com/YUKIKEDA/AuraORM/blob/main/LICENSE)
