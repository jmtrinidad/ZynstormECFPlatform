using System;
using System.Collections.Generic;
using System.Text;

namespace ZynstormECFPlatform.Abstractions.Data;

public interface ISqlGenerator
{
    string SelectPaged(string sql, string orderBy, int page, int resultsPerPage,
        IDictionary<string, object> parameters);
}