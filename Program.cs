using System.Runtime.CompilerServices;
using TypedSql;
using TypedSql.Runtime;

SchemaRegistry<Person>.Register(DemoSchema.People);

var rows = new[]
{
	new Person(1, "Ada", 34, "Seattle", 180_000f, "Engineering", true, 6, "US", "Runtime", "Senior"),
	new Person(2, "Barbara", 28, "Boston", 150_000f, "Engineering", false, 3, "US", "Compiler", "Mid"),
	new Person(3, "Charles", 44, "Helsinki", 210_000f, "Research", true, 15, "FI", "ML", "Principal"),
	new Person(4, "David", 31, "Palo Alto", 195_000f, "Product", false, 4, "US", "Runtime", "Senior"),
	new Person(5, "Eve", 39, "Seattle", 220_000f, "Product", true, 10, "US", null, "Staff"),
};

Console.WriteLine("Input data:");
foreach (var row in rows)
{
	Console.WriteLine(row);
}

Console.WriteLine();

QueryEngine.CompiledQuery<TSource, TResult> Compile<TSource, TResult>(string query)
{
	var compiledQuery = QueryEngine.Compile<TSource, TResult>(query, supportsAot: !RuntimeFeature.IsDynamicCodeSupported);
	Console.WriteLine($"Compiled query for `{query}`:\n{compiledQuery}");
	return compiledQuery;
}

// 1) Simple city filter
foreach (var name in Compile<Person, string>("SELECT Name FROM $ WHERE city != 'Seattle'").Execute(rows))
{
	Console.WriteLine($" -> {name}");
}

Console.WriteLine();

// 2) Senior, well-paid managers in engineering
foreach (var person in Compile<Person, Person>(
	"SELECT * FROM $ WHERE department = 'Engineering' AND isManager = true AND yearsAtCompany >= 5 AND salary > 170000").Execute(rows))
{
	Console.WriteLine($" -> {person.Name} ({person.City}) [{person.Department}], Years={person.YearsAtCompany}, Level={person.Level}");
}

Console.WriteLine();

// 3) US-based senior+ ICs on Runtime or ML teams
foreach (var name in Compile<Person, string>(
	"SELECT Name FROM $ WHERE country = 'US' AND (team = 'Runtime' OR team = 'ML') AND (level = 'Senior' OR level = 'Staff' OR level = 'Principal')").Execute(rows))
{
	Console.WriteLine($" -> {name}");
}

Console.WriteLine();

// 4) Non-US employees
foreach (var name in Compile<Person, string>("SELECT Name FROM $ WHERE NOT country = 'US'").Execute(rows))
{
	Console.WriteLine($" -> {name}");
}

Console.WriteLine();

// 5) Project to a tuple with richer shape
foreach (var (name, city, department, team, level) in Compile<Person, (string Name, string City, string Department, string? Team, string Level)>(
	"SELECT Name, City, Department, Team, Level FROM $ WHERE salary >= 195000 AND Team != null").Execute(rows))
{
	Console.WriteLine($" -> {name} ({city}) - {department}/{team ?? "Unset"} [{level}]");
}

Console.WriteLine();

// 6) Double negation of predicates
foreach (var name in Compile<Person, string>(
	"SELECT Name FROM $ WHERE NOT NOT country = 'US'").Execute(rows))
{
	Console.WriteLine($" -> {name}");
}

Console.WriteLine();

// 7) De Morgan distribution over OR
foreach (var name in Compile<Person, string>(
	"SELECT Name FROM $ WHERE NOT (team = 'Runtime' OR team = 'ML')").Execute(rows))
{
	Console.WriteLine($" -> {name}");
}

Console.WriteLine();

// 8) Duplicate predicates in AND/OR chains
foreach (var person in Compile<Person, Person>(
	"SELECT * FROM $ WHERE department = 'Engineering' AND department = 'Engineering' AND (city = 'Seattle' OR city = 'Seattle')").Execute(rows))
{
	Console.WriteLine($" -> {person.Name} ({person.City}) [{person.Department}]");
}

Console.WriteLine();
Console.WriteLine("All queries executed.");