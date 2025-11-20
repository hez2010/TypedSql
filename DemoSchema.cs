using System.Runtime.CompilerServices;
using TypedSql.Runtime;

namespace TypedSql;

internal readonly struct PersonIdColumn : IColumn<Person, int>
{
    public static string Identifier => "id";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Get(in Person row) => row.Id;
}

internal readonly struct PersonNameColumn : IColumn<Person, string>
{
    public static string Identifier => "name";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Get(in Person row) => row.Name;
}

internal readonly struct PersonAgeColumn : IColumn<Person, int>
{
    public static string Identifier => "age";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Get(in Person row) => row.Age;
}

internal readonly struct PersonCityColumn : IColumn<Person, string>
{
    public static string Identifier => "city";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Get(in Person row) => row.City;
}

internal readonly struct PersonSalaryColumn : IColumn<Person, float>
{
    public static string Identifier => "salary";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Get(in Person row) => row.Salary;
}

internal readonly struct PersonDepartmentColumn : IColumn<Person, string>
{
    public static string Identifier => "department";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Get(in Person row) => row.Department;
}

internal readonly struct PersonIsManagerColumn : IColumn<Person, bool>
{
    public static string Identifier => "isManager";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Get(in Person row) => row.IsManager;
}

internal readonly struct PersonYearsAtCompanyColumn : IColumn<Person, int>
{
    public static string Identifier => "yearsAtCompany";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Get(in Person row) => row.YearsAtCompany;
}

internal readonly struct PersonCountryColumn : IColumn<Person, string>
{
    public static string Identifier => "country";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Get(in Person row) => row.Country;
}

internal readonly struct PersonTeamColumn : IColumn<Person, string?>
{
    public static string Identifier => "team";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? Get(in Person row) => row.Team;
}

internal readonly struct PersonLevelColumn : IColumn<Person, string>
{
    public static string Identifier => "level";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Get(in Person row) => row.Level;
}


internal static class DemoSchema
{
    public static readonly IReadOnlyDictionary<string, ColumnMetadata> People = CreatePeople();

    private static IReadOnlyDictionary<string, ColumnMetadata> CreatePeople()
    {
        return new Dictionary<string, ColumnMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            [PersonIdColumn.Identifier] = new(PersonIdColumn.Identifier, typeof(PersonIdColumn), typeof(int)),
            [PersonNameColumn.Identifier] = new(PersonNameColumn.Identifier, typeof(PersonNameColumn), typeof(string)),
            [PersonAgeColumn.Identifier] = new(PersonAgeColumn.Identifier, typeof(PersonAgeColumn), typeof(int)),
            [PersonCityColumn.Identifier] = new(PersonCityColumn.Identifier, typeof(PersonCityColumn), typeof(string)),
            [PersonSalaryColumn.Identifier] = new(PersonSalaryColumn.Identifier, typeof(PersonSalaryColumn), typeof(float)),
            [PersonDepartmentColumn.Identifier] = new(PersonDepartmentColumn.Identifier, typeof(PersonDepartmentColumn), typeof(string)),
            [PersonIsManagerColumn.Identifier] = new(PersonIsManagerColumn.Identifier, typeof(PersonIsManagerColumn), typeof(bool)),
            [PersonYearsAtCompanyColumn.Identifier] = new(PersonYearsAtCompanyColumn.Identifier, typeof(PersonYearsAtCompanyColumn), typeof(int)),
            [PersonCountryColumn.Identifier] = new(PersonCountryColumn.Identifier, typeof(PersonCountryColumn), typeof(string)),
            [PersonTeamColumn.Identifier] = new(PersonTeamColumn.Identifier, typeof(PersonTeamColumn), typeof(string)),
            [PersonLevelColumn.Identifier] = new(PersonLevelColumn.Identifier, typeof(PersonLevelColumn), typeof(string)),
        };
    }
}

public record struct Person(
    int Id,
    string Name,
    int Age,
    string City,
    float Salary,
    string Department,
    bool IsManager,
    int YearsAtCompany,
    string Country,
    string Team,
    string Level);
