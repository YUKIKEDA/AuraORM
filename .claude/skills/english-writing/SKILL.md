---
name: english-writing
description: >-
  Write AuraORM XML docs, inline comments, test names and Preconditions/Steps/Expected remarks,
  exception and diagnostic messages, rules, and the README in English with the repository's terminology.
  Use when writing or editing any of these, when translating a term from the Japanese design,
  or when a review flags wording.
---

# Writing English in AuraORM

Read this before writing. When a review corrects wording, add the general rule here in the same change; do not leave it in chat. The language policy is `.cursor/rules/language.mdc`.

## Sentence shape

- Plain, direct English. Short sentences and common words.
- XML doc elements are full sentences and end with a period (SA1629).
- Summaries start with a verb in the third person: "Returns ...", "Creates ...", "Adds ...".
  - Properties: "Gets ..." or "Gets or sets ..." (SA1623).
  - Constructors: "Initializes a new instance of the <see cref="T"/> class." (SA1642).
  - Boolean members: "Gets a value indicating whether ...".
- Write `<see langword="true"/>`, `<see langword="false"/>`, `<see langword="null"/>` for keywords.
- Refer to types and members with `<see cref="..."/>` and parameters with `<paramref name="..."/>`.
- A builder method's summary says which SQL it adds, in SQL words: "Adds a <c>WHERE</c> clause ...". SQL keywords are upper case inside `<c>`.
- Inline comments explain why, not what the next line does. A reason about a database names the database and the version that behaves so.
- Keep the subject the same across a method name, its summary, and its remarks.

## Test names and remarks

- `{Target}_{Scenario}_{Expected}` in PascalCase.
- Remarks use the headings Preconditions, Steps, Expected (`.cursor/rules/testing.mdc`). Name the actual input and the actual check.

## Exception and diagnostic messages

- One sentence that names the condition that failed, with the same terms as the design and the XML docs.
- A generator or analyzer diagnostic also says what to write instead: "Comparing with null is never true in SQL; use IsNull(column) instead."

## Terminology

The left column is the term in `docs/design.ja.md`. Write the right column in code, docs, and tests.

| Design term | Write |
| --- | --- |
| 方言 | dialect |
| クエリビルダー、段階 | query builder, stage |
| 式 | expression |
| 条件 | condition (a boolean expression) |
| 射影 | projection |
| 行の型 | row type |
| テーブル定義、テーブル参照 | table definition, table reference |
| 別名 | alias |
| 外部結合の NULL 化 | nullable side of an outer join |
| 生 SQL 断片 | raw SQL fragment |
| パラメータ | parameter (never "argument" for a bound value) |
| 識別子のクォート | identifier quoting |
| 生成される SQL | generated SQL |
| 予測可能 | predictable |
| 結果の読み取り | materialization (reading rows into results) |
| ソースジェネレーター | source generator |

Words to avoid:

| Avoid | Write |
| --- | --- |
| "entity" for the row type | "row type" (there is no change tracking) |
| "fails" for throwing | "throws <see cref="InvalidOperationException"/>" (the concrete type) |
| "fast", "zero-cost", "efficient" without a number | the measurement, or nothing |
| "magic", "automatically" for SQL the library adds | the exact SQL, or nothing: the library adds none |

## After a review

1. Before applying a suggested replacement, look for similar sentences elsewhere (`.cursor/rules/similar-findings.mdc`).
2. If the finding is not covered above, add it here as a general rule, not as one sentence.
