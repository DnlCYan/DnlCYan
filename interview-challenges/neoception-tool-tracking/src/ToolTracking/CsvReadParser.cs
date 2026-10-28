namespace ToolTracking;

/// <summary>Level 1 - turns the raw reader export (CSV) into validated <see cref="TagRead"/>s.</summary>
public sealed class CsvReadParser
{
    private readonly ReaderConfiguration _configuration;

    public CsvReadParser(ReaderConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Parses the CSV export. Must never throw because of bad input lines - every rejected
    /// line is reported in <see cref="ParseResult.Errors"/>. See README, Level 1, for the rules.
    /// </summary>
    public ParseResult Parse(TextReader csv)
    {
        // TODO: implement Level 1
        throw new NotImplementedException();
    }
}
