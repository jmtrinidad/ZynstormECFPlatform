using System.Globalization;

namespace ZynstormECFPlatform.Services.Xml.Excel;

/// <summary>
/// Decimal formatting helper EXCLUSIVE to the Excel certification flow
/// (PruebasDatosEcf / aprobacion-comercial). It lives in this namespace on purpose so it
/// cannot affect how the Production or Simulation XML generators format their values.
///
/// This flow is a 100% orchestrator: the value must reach the XML EXACTLY as it was read
/// from the source Excel — no rounding, no forced decimals, no stripping of trailing zeros.
///
/// The certification Excel stores these cells as text in invariant format (e.g. "1.00",
/// "400000.00"). When such a string is parsed with <c>decimal.Parse</c> the resulting decimal
/// keeps its scale, so emitting it with <c>ToString(InvariantCulture)</c> reproduces the
/// original cell verbatim:
///   "1.00"      -> "1.00"
///   "400000.00" -> "400000.00"
///   "23"        -> "23"
///   "1.5000"    -> "1.5000"
/// Nothing is added or removed.
/// </summary>
internal static class ExcelDecimal
{
    public static string? Verbatim(decimal? value)
        => value?.ToString(CultureInfo.InvariantCulture);
}
