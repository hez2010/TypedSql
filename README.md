# TypedSql

TypedSql is a small experimental SQL-like query engine that leans on the C# type system as its execution plan. Each query turns into a closed generic type built from `Where` / `Select` / `Stop` nodes and runs entirely through static methods, so there’s no virtual dispatch or expression-tree interpretation sitting in the hot path.

## Why TypedSql

- **Execution plan in types**: The query shape is encoded in generic types, so from the JIT’s point of view execution looks like an ordinary straight-line loop instead of a dynamic interpreter.
- **Struct-based column access**: Columns are implemented as `IColumn<TRow, TValue>` structs (see `DemoSchema.cs`), which makes inlining predictable and avoids boxing when reading fields.
- **Lifted literals**: Literals in `WHERE` clauses are turned into `ILiteral<T>` types via `Runtime/TypeLiterals.cs`, so there is no runtime parsing and the JIT can see literal values at compile time.
- **ValueString for strings**: Hot paths avoid reference-type generics by normalizing strings to `ValueString` internally, while callers still see ordinary `string` values through a thin adapter.

## Quick start

Register the schema once, compile a query, and then run it over an array of rows:

```csharp
using TypedSql;
using TypedSql.Runtime;

SchemaRegistry<Person>.Register(DemoSchema.People);

var rows = new[]
{
	new Person(1, "Ada", 34, "Seattle", 180_000f, "Engineering", true, 6, "US", "Runtime", "Senior"),
	new Person(2, "Barbara", 28, "Boston", 150_000f, "Engineering", false, 3, "US", "Compiler", "Mid"),
	new Person(3, "Charles", 44, "Helsinki", 210_000f, "Research", true, 15, "FI", "ML", "Principal"),
	new Person(4, "David", 31, "Palo Alto", 195_000f, "Product", false, 4, "US", "Runtime", "Senior"),
	new Person(5, "Eve", 39, "Seattle", 220_000f, "Product", true, 10, "US", "ML", "Staff"),
};

// Find well‑paid senior engineering managers in the US
var query = QueryEngine.Compile<Person, Person>(
	"SELECT * FROM $ WHERE department = 'Engineering' AND isManager = true AND yearsAtCompany >= 5 AND salary > 170000 AND country = 'US'");

foreach (var person in query.Execute(rows))
{
	Console.WriteLine($" -> {person.Name} ({person.City}) [{person.Department}/{person.Team}] {person.Level}, Years={person.YearsAtCompany}, Manager={person.IsManager}");
}
```

## Architecture Overview
At a high level, TypedSql parses a small SQL subset, compiles it into a chain of generic pipeline nodes, and then runs that pipeline over plain in-memory rows.

- A **schema** describes which columns exist for a given row type.
- The **parser/compiler** translate SQL into a concrete generic pipeline type.
- The **runtime** owns the result buffer and calls the pipeline’s static methods.

If you want to peek under the hood in more detail, check out the **Deep dive** section below.

## Supported SQL Surface

- `SELECT * FROM $`: returns the original row type (e.g., `Person`).
- `SELECT <column> FROM $`: returns a single column (`string`, numeric, etc.).
- `SELECT col1, col2, ... FROM $`: returns multiple columns as a C# tuple `(T1, T2, ...)`.
- `WHERE <column> <op> <literal>`: comparison operators `=`, `!=`, `>`, `<`, `>=`, `<=`.
- Boolean operators: `AND`, `OR`, `NOT`, and grouping with `()`.
- Literals: integers (`42`), floats (`123.45`), booleans (`true` / `false`), single-quoted strings (`'Seattle'` with `''` as the escape) and `null` strings.
- Column identifiers are case-insensitive.
- Easily extensible for new expressions, operators, literals, and columns.

`Program.cs` shows many of these operators in action with a bunch of small example queries.

## Running the demo

```pwsh
dotnet run -c Release
```

## Benchmarks
Compare a simple TypedSql query against equivalent LINQ and handwritten loops over the same in-memory data. Filtering out rows where `City == "Seattle"` and returning the matching `Id` values, produced numbers like these:

| Method   | Mean      | Error     | StdDev    | Gen0   | Code Size | Allocated |
|--------- |----------:|----------:|----------:|-------:|----------:|----------:|
| TypedSql | 10.953 ns | 0.0250 ns | 0.0195 ns | 0.0051 |     111 B |      80 B |
| Linq     | 27.030 ns | 0.1277 ns | 0.1067 ns | 0.0148 |   3,943 B |     232 B |
| Foreach  |  9.429 ns | 0.0417 ns | 0.0326 ns | 0.0046 |     407 B |      72 B |

TypedSql and the handwritten `foreach` loop end up with very similar throughput and allocation, while the LINQ query is noticeably slower and allocates more.

## Extending the schema

To extend the schema, add new `IColumn<TRow, TValue>` implementations to `DemoSchema.cs`, register them in the `People` dictionary, and keep:

```csharp
SchemaRegistry<Person>.Register(DemoSchema.People);
```

in place. Once registered, you can use the new column names in your SQL strings without modifying the engine itself.

## Deep dive

### Schema registration

Rows are simple records/structs such as `Person`. Each column implements `IColumn<TRow, TValue>` and exposes a stable `Identifier`. `SchemaRegistry<TRow>` holds a case-insensitive dictionary of `ColumnMetadata` and uses it to resolve column names to concrete column types and getters at compile time.

`DemoSchema.cs` defines the following columns:

- `PersonIdColumn` / `PersonNameColumn` / `PersonAgeColumn` / `PersonCityColumn` / `PersonSalaryColumn`
- `PersonDepartmentColumn` / `PersonIsManagerColumn` / `PersonYearsAtCompanyColumn`
- `PersonCountryColumn` / `PersonTeamColumn` / `PersonLevelColumn`

They are registered with:

```csharp
SchemaRegistry<Person>.Register(DemoSchema.People);
```

### Parsing and compilation

1. `Runtime/SqlParser.cs` tokenizes and parses a small SQL subset (`SELECT`, `FROM $`, `WHERE`, etc.).
2. `Runtime/SqlCompiler.cs` looks up schema information, constructs literal types, and builds a type-level pipeline using nodes from `Runtime/Pipeline.cs`.
3. When possible, consecutive `Where` and `Select` nodes are fused into a single `WhereSelect` node to reduce passes over the data.
4. The final pipeline type is plugged into `QueryProgram<TRow, TPipeline, TRuntimeResult, TPublicResult>`, whose `Execute` method becomes the single entry point.

### Pipeline nodes

- `Where<TRow, TPredicate, TNext, TResult, TRoot>`: evaluates predicates implemented as `IFilter<TRow>`.
- `Select<TRow, TProjection, ...>`: uses `IProjection<TRow, TMiddle>` implementations (for example, `ColumnProjection<PersonCityColumn, Person, ValueString>`) to produce the next shape in the pipeline.
- `WhereSelect`: a combined filter+projection node used when a `Where` is directly followed by a `Select`.
- `Stop<TResult, TRoot>`: the terminal node, which pushes results into the `QueryRuntime` buffer.

Each node exposes static `Run` / `Process` methods. Once a `QueryProgram` is assembled, running a query is essentially a set of static method calls on nested generic types.

### Runtime execution

- `Runtime/QueryRuntime.cs` owns and manages the result buffer.
- `QueryProgram.Execute` creates the runtime, calls the pipeline’s `Run`, and performs minimal conversion to the public result type (original rows, primitives, strings, or tuples).
- `Runtime/QueryEngine.cs` locates `Execute` via reflection once, converts it to a function pointer (`delegate*`), and subsequent executions call it directly.
