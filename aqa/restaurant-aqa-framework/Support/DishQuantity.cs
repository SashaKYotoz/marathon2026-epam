using System.Globalization;
using System.Text.RegularExpressions;

namespace Restaurant_Aqa_Framework.Support;

public sealed record DishQuantity(int Quantity, string Dish)
{
    public override string ToString() => $"{Quantity.ToString(CultureInfo.InvariantCulture)} x {Dish}";
}

public static partial class DishQuantities
{
    public const string None = "none";

    public static IReadOnlyList<DishQuantity> Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || string.Equals(text.Trim(), None, StringComparison.OrdinalIgnoreCase))
            return [];

        return text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseOne)
            .ToList();
    }

    public static string Describe(IEnumerable<DishQuantity> dishes)
    {
        var lines = dishes
            .GroupBy(dish => dish.Dish, StringComparer.Ordinal)
            .Select(group => new DishQuantity(group.Sum(dish => dish.Quantity), group.Key))
            .OrderBy(dish => dish.Dish, StringComparer.Ordinal)
            .Select(dish => dish.ToString())
            .ToList();

        return lines.Count == 0 ? None : string.Join(", ", lines);
    }

    private static DishQuantity ParseOne(string part)
    {
        var match = DishPattern().Match(part);
        if (!match.Success)
            throw new FormatException($"'{part}' is not of the form '<quantity> x <dish>'.");

        return new DishQuantity(int.Parse(match.Groups["quantity"].Value, CultureInfo.InvariantCulture), match.Groups["dish"].Value.Trim());
    }

    [GeneratedRegex(@"^(?<quantity>\d+)\s*x\s+(?<dish>.+)$")]
    private static partial Regex DishPattern();
}
