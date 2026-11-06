namespace BrewLog.Domain.Common;

/// <summary>Діапазон значень із включними межами, наприклад діапазон параметра стилю BJCP.</summary>
public readonly record struct ValueRange(double Min, double Max)
{
    public bool Contains(double value) => value >= Min && value <= Max;

    /// <summary>
    /// Положення значення на шкалі діапазону: 0 — нижня межа, 1 — верхня; поза межами обмежується.
    /// Якщо Min = Max, ділити нема на що: значення в діапазоні стає посередині шкали (0,5).
    /// </summary>
    public double Position(double value)
    {
        if (Max <= Min)
        {
            return value < Min ? 0 : value > Max ? 1 : 0.5;
        }

        return Math.Clamp((value - Min) / (Max - Min), 0, 1);
    }
}
