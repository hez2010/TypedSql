# TypedSql

TypedSql は、C# の型システムそのものを実行計画として使う、小さな実験的な SQL ライククエリエンジンです。各クエリは `Where` / `Select` / `Stop` などのノードからなる閉じたジェネリック型に変換され、ホットパスでは仮想呼び出しや式木インタープリタを挟まず、静的メソッドの呼び出しだけでさくっと実行されます。

## なぜ TypedSql か

- **型レベルでの実行計画**: クエリの形状をジェネリック型としてエンコードすることで、JIT から見ると「普通のループ」に近いストレートなコードになります。
- **カラムアクセスの構造体化**: 各カラムは `IColumn<TRow, TValue>` 構造体として定義されます (`DemoSchema.cs` を参照)。フィールドアクセスがインライン展開されやすく、ボクシングも発生しません。
- **リテラルの型持ち上げ**: `WHERE` 句内のリテラルは `Runtime/TypeLiterals.cs` 経由で `ILiteral<T>` 型に持ち上げられ、実行時のパースを省きつつ、コンパイル時に値が見える形にします。
- **ValueString による文字列の最適化**: ホットパスでは参照型ジェネリックを避けるため、内部的には文字列を `ValueString` に正規化しつつ、外からは薄いアダプタ経由で通常の `string` として扱えます。

## クイックスタート

スキーマを一度登録し、クエリをコンパイルしてから、配列に対して実行します:

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

// よくある「社員テーブル」の少し複雑なクエリ例 (US 拠点のエンジニアリングマネージャ)
var query = QueryEngine.Compile<Person, Person>(
	"SELECT * FROM $ WHERE department = 'Engineering' AND isManager = true AND yearsAtCompany >= 5 AND salary > 170000 AND country = 'US'");

foreach (var person in query.Execute(rows))
{
	Console.WriteLine($" -> {person.Name} ({person.City}) [{person.Department}/{person.Team}] {person.Level}, Years={person.YearsAtCompany}, Manager={person.IsManager}");
}
```

## アーキテクチャ概要

TypedSql は、簡易な SQL をパースしてジェネリックなパイプライン型にコンパイルし、そのパイプラインをプレーンなインメモリ行に対して実行します。

- **スキーマ**: 行型に対してどのカラムが存在するかを表します。
- **パーサー / コンパイラ**: SQL 文字列を具体的なパイプライン型に変換します。
- **ランタイム**: 結果バッファを管理し、パイプラインの静的メソッドを呼び出します。

より詳しい仕組みは、後述の「Deep dive」セクションをのぞいてみてください。

## 対応している SQL サーフェス

- `SELECT * FROM $` : 元の行型 (`Person`) を返す。
- `SELECT <column> FROM $` : 単一カラムを返す (`string` / 数値など)。
- `SELECT col1, col2, ... FROM $` : 複数カラムを C# のタプル `(T1, T2, ...)` として返す。
- `WHERE <column> <op> <literal>` : `=`, `!=`, `>`, `<`, `>=`, `<=`。
- ブール演算子: `AND`, `OR`, `NOT`、および `()` によるグルーピング。
- リテラル: 整数 (`42`)、浮動小数 (`123.45`)、ブール値 (`true` / `false`)、シングルクォート文字列 (`'Seattle'`、エスケープは `''`)、ヌル文字列 (`null`)。
- カラム識別子は大文字小文字を区別しません。
- 新しい式、演算子、リテラル、カラムを簡単に拡張できます。

`Program.cs` では、上記の演算子を一通り使ったクエリ例をいくつも実行しています。

## デモ実行

```pwsh
dotnet run -c Release
```

## ベンチマーク

BenchmarkDotNet で TypedSql のクエリと LINQ や手書きループによる同等の処理を、同じインメモリデータに対して比べて、「`City == "Seattle"` の行を除外し、該当する `Id` を集める」という処理は以下のような結果が出ました:

| Method   | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|--------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| TypedSql | 10.093 ns | 0.2519 ns | 0.3185 ns |  1.21 |    0.05 | 0.0046 |     666 B |      72 B |        1.00 |
| Linq     | 27.449 ns | 0.5885 ns | 0.7442 ns |  3.28 |    0.12 | 0.0127 |   3,769 B |     200 B |        2.78 |
| Foreach  |  8.364 ns | 0.2126 ns | 0.2274 ns |  1.00 |    0.04 | 0.0046 |     409 B |      72 B |        1.00 |

TypedSql と手書きの `foreach` ループは、だいたい同じくらいの速さとメモリ割り当てになっているのに対して、LINQ は少し重めで、実行時間もメモリも多めにかかっています。

## スキーマを拡張するには

`DemoSchema.cs` に新しい `IColumn<TRow, TValue>` 実装を追加し、`People` 辞書に登録したうえで:

```csharp
SchemaRegistry<Person>.Register(DemoSchema.People);
```

を維持したままクエリ文字列に新しいカラム名を使えば、エンジン側の変更なしでそのまま利用できます。

## Deep dive

### スキーマ登録

行は `Person` のような単純な record/struct です。各カラムは `IColumn<TRow, TValue>` を実装し、一意な `Identifier` を公開します。`SchemaRegistry<TRow>` は `ColumnMetadata` の大文字小文字を区別しない辞書を持ち、コンパイル時にカラム名から具体的なカラム型とゲッターを引き当てます。

`DemoSchema.cs` では、次のようなカラムが定義されています。

- `PersonIdColumn` / `PersonNameColumn` / `PersonAgeColumn` / `PersonCityColumn` / `PersonSalaryColumn`
- `PersonDepartmentColumn` / `PersonIsManagerColumn` / `PersonYearsAtCompanyColumn`
- `PersonCountryColumn` / `PersonTeamColumn` / `PersonLevelColumn`

これらを `SchemaRegistry<Person>.Register(DemoSchema.People);` で一括登録します。

### パースとコンパイル

1. `Runtime/SqlParser.cs` が簡易 SQL (`SELECT`, `FROM $`, `WHERE` など) をトークナイズ・構文解析します。
2. `Runtime/SqlCompiler.cs` がスキーマ情報を参照しつつリテラル型を組み立て、`Runtime/Pipeline.cs` のノードからなる型レベルパイプラインを構築します。
3. 可能な場合、連続する `Where` と `Select` は `WhereSelect` にまとめられ、データ走査回数を減らします。
4. 最終的なパイプライン型は `QueryProgram<TRow, TPipeline, TRuntimeResult, TPublicResult>` に差し込まれ、その `Execute` が実行エントリポイントになります。

### パイプラインノード

- `Where<TRow, TPredicate, TNext, TResult, TRoot>`: `IFilter<TRow>` を実装した述語型を評価します。
- `Select<TRow, TProjection, ...>`: `IProjection<TRow, TMiddle>` 実装 (例: `ColumnProjection<PersonCityColumn, Person, ValueString>`) を用いて次のシェイプを生成します。
- `WhereSelect`: 直後の `Select` と結合されたフィルタ兼射影ノードです。
- `Stop<TResult, TRoot>`: 終端ノードで、`QueryRuntime` のバッファに結果を積み上げます。

各ノードは静的な `Run` / `Process` メソッドを公開しており、一度 `QueryProgram` が組み上がると、クエリ実行はネストしたジェネリック型上の静的メソッド呼び出しの組み合わせとして表現されます。

### ランタイム実行

- `Runtime/QueryRuntime.cs` が結果バッファを管理します。
- `QueryProgram.Execute` がランタイムを生成し、パイプラインの `Run` を呼び出し、公開結果型 (元の行/プリミティブ/文字列/タプルなど) への最小限の変換を行います。
- `Runtime/QueryEngine.cs` は一度だけ反射で `Execute` メソッドを取得し、delegate* に変換したうえで以降は直接呼び出します。
