namespace MockPaste.UI.Popup;

public sealed class HistoryItemViewModel
{
    public required string Value { get; init; }
    public required string CategoryName { get; init; }
    public required string FormatName { get; init; }
    public string DisplayValue => Value.Length > 65 ? Value[..65] + "…" : Value;
    public string SubLabel => $"{CategoryName}  ·  {FormatName}";
}
