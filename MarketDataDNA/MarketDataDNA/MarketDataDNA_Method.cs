using System;
using System.Collections.Generic;
using System.Text;

using ExcelDna.Integration;

public static class MarketDataDNA_Method
{
    [ExcelCommand(MenuName = "&MarketDataDNA_RTDServer", MenuText = "&更新を有効にする")]
    public static void Enable_Update()
    {
    }

    [ExcelFunctionAttribute(Description = "GetData(データソース名,銘柄名,フィールド名)", Name = "GetData")] // GetData(\"銘柄コード\",\"フィールドコード\")
    public static string GetData(string source, string bland, string field)
    {
        object x = XlCall.RTD("MarketDataDNA_RealTimeServer", null, source, bland, field);
        return x.ToString();
    }

    [ExcelFunctionAttribute(Description = "", Name = "GetTime")]
    public static string GetTime()
    {
        return DateTime.Now.ToString();
    }
}