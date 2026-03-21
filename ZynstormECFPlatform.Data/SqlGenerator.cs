using System.Text;
using ZynstormECFPlatform.Abstractions.Data;

namespace ZynstormECFPlatform.Data;

public class SqlGenerator : ISqlGenerator
{
    private static char OpenQuote => '[';

    private static char CloseQuote => ']';

    public string SelectPaged(string sql, string orderBy, int page, int resultsPerPage,
        IDictionary<string, object> parameters)
    {
        if (string.IsNullOrEmpty(orderBy))
            throw new ArgumentNullException(nameof(orderBy), "Sort cannot be null or empty.");

        if (parameters == null) throw new ArgumentNullException(nameof(parameters));

        var innerSql = new StringBuilder(sql);
        innerSql.Append(" ORDER BY " + orderBy);

        var query = GetPagingSql(innerSql.ToString(), page, resultsPerPage, parameters);
        return query;
    }

    protected virtual string GetPagingSql(string sql, int page, int resultsPerPage,
        IDictionary<string, object> parameters)
    {
        //var startValue = page * resultsPerPage + 1;
        var startValue = (page - 1) * resultsPerPage;
        return GetSetSql(sql, page, startValue, resultsPerPage, parameters);
    }

    protected virtual string GetSetSql(string sql, int page, int firstResult, int maxResults,
        IDictionary<string, object> parameters)
    {
        if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));

        if (parameters == null) throw new ArgumentNullException(nameof(parameters));

        var selectIndex = GetSelectEnd(sql) + 1;
        var orderByClause = GetOrderByClause(sql) ?? "ORDER BY CURRENT_TIMESTAMP";

        var projectedColumns = GetColumnNames(sql).Aggregate(new StringBuilder(),
            (sb, s) => (sb.Length == 0 ? sb : sb.Append(", ")).Append(GetColumnName("_proj", s, null!)),
            sb => sb.ToString());
        var newSql = sql
            .Replace(" " + orderByClause, string.Empty)
            .Insert(selectIndex,
                $"ROW_NUMBER() OVER(ORDER BY {orderByClause.Substring(9)}) AS {GetColumnName(null!, "_row_number", null!)}, ");

        var result = string.Format(
            "SELECT TOP({0}) {1} FROM ({2}) [_proj] WHERE {3} > @_pageStartRow AND {3} <= @_pageEndRow ORDER BY {3}",
            maxResults, projectedColumns.Trim(), newSql, GetColumnName("_proj", "_row_number", null!));

        parameters.Add("@_pageStartRow", firstResult);
        parameters.Add("@_pageEndRow", page * maxResults);
        return result;
    }

    protected virtual int GetSelectEnd(string sql)
    {
        if (sql.StartsWith("SELECT DISTINCT", StringComparison.InvariantCultureIgnoreCase)) return 15;

        if (sql.StartsWith("SELECT", StringComparison.InvariantCultureIgnoreCase)) return 6;

        throw new ArgumentException("SQL must be a SELECT statement.", nameof(sql));
    }

    private static string GetOrderByClause(string sql)
    {
        var orderByIndex = sql.LastIndexOf(" ORDER BY ", StringComparison.InvariantCultureIgnoreCase);
        if (orderByIndex == -1) return null!;

        var result = sql.Substring(orderByIndex).Trim();

        var whereIndex = result.IndexOf(" WHERE ", StringComparison.InvariantCultureIgnoreCase);
        return whereIndex == -1 ? result : result.Substring(0, whereIndex).Trim();
    }

    private static int GetFromStart(string sql)
    {
        var selectCount = 0;
        var words = sql.Split(' ');
        var fromIndex = 0;
        foreach (var word in words)
        {
            if (word.Equals("SELECT", StringComparison.InvariantCultureIgnoreCase)) selectCount++;

            if (word.Equals("FROM", StringComparison.InvariantCultureIgnoreCase))
            {
                selectCount--;
                if (selectCount == 0) break;
            }

            fromIndex += word.Length + 1;
        }

        return fromIndex;
    }

    protected virtual IEnumerable<string> GetColumnNames(string sql)
    {
        var start = GetSelectEnd(sql);
        var stop = GetFromStart(sql);
        var columnSql = sql.Substring(start, stop - start).Split(',');
        var result = new List<string>();
        foreach (var c in columnSql)
        {
            var index = c.IndexOf(" AS ", StringComparison.InvariantCultureIgnoreCase);
            if (index > 0)
            {
                result.Add(c.Substring(index + 4).Trim());
                continue;
            }

            var colParts = c.Split('.');
            var col = colParts[colParts.Length - 1];

            if (col.Equals("*"))
            {
                result = new List<string> { "*" };
                break;
            }

            result.Add(col.Trim());
        }

        return result;
    }

    protected virtual string GetColumnName(string prefix, string columnName, string alias)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            throw new ArgumentNullException(nameof(columnName), "columnName cannot be null or empty.");

        var result = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(prefix)) result.AppendFormat(QuoteString(prefix) + ".");

        result.AppendFormat(QuoteString(columnName));

        if (!string.IsNullOrWhiteSpace(alias)) result.AppendFormat(" AS {0}", QuoteString(alias));

        return result.ToString();
    }

    protected virtual string QuoteString(string value)
    {
        if (IsQuoted(value) || value == "*") return value;
        return $"{OpenQuote}{value.Trim()}{CloseQuote}";
    }

    protected virtual bool IsQuoted(string value)
    {
        if (value.Trim()[0] == OpenQuote) return value.Trim().Last() == CloseQuote;

        return false;
    }
}