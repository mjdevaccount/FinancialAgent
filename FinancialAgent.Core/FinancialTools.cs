using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace FinancialAgent.Core;

public class FinancialTools(HttpClient httpClient, string alphaVantageKey)
{
    [KernelFunction, Description("Gets the current stock price for a given ticker symbol")]
    public async Task<string> GetStockPrice(
        [Description("The stock ticker symbol, e.g. AAPL, MSFT, NVDA")] string ticker)
    {
        var url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={ticker}&apikey={alphaVantageKey}";
        var response = await httpClient.GetStringAsync(url);
        var doc = JsonDocument.Parse(response);

        var quote = doc.RootElement.GetProperty("Global Quote");
        var price = quote.GetProperty("05. price").GetString();
        var change = quote.GetProperty("10. change percent").GetString();

        return $"{ticker.ToUpper()} is trading at ${decimal.Parse(price):F2} ({change} today)";
    }

    [KernelFunction, Description("Calculates the percentage return between two prices")]
    public string CalculateReturn(
        [Description("The starting price")] decimal startPrice,
        [Description("The ending price")] decimal endPrice)
    {
        var returnPct = ((endPrice - startPrice) / startPrice) * 100;
        return $"Return: {returnPct:F2}% ({(returnPct >= 0 ? "gain" : "loss")})";
    }

    [KernelFunction, Description("Gets a summary of recent news sentiment for a stock")]
    public async Task<string> GetNewsSentiment(
        [Description("The stock ticker symbol, e.g. AAPL, MSFT, NVDA")] string ticker)
    {
        var url = $"https://www.alphavantage.co/query?function=NEWS_SENTIMENT&tickers={ticker}&apikey={alphaVantageKey}&limit=5";
        var response = await httpClient.GetStringAsync(url);
        var doc = JsonDocument.Parse(response);

        if (!doc.RootElement.TryGetProperty("feed", out var feed))
            return $"No news sentiment data found for {ticker}";

        var articles = feed.EnumerateArray().Take(5).ToList();
        if (!articles.Any())
            return $"No recent news found for {ticker}";

        var summaries = articles.Select(article =>
        {
            var title = article.GetProperty("title").GetString();
            var overallSentiment = article.GetProperty("overall_sentiment_label").GetString();

            var tickerSentiment = article.TryGetProperty("ticker_sentiment", out var ts)
                ? ts.EnumerateArray()
                    .FirstOrDefault(t => t.GetProperty("ticker").GetString() == ticker.ToUpper())
                : default;

            var tickerLabel = tickerSentiment.ValueKind != JsonValueKind.Undefined
                ? tickerSentiment.GetProperty("ticker_sentiment_label").GetString()
                : overallSentiment;

            return $"- {title} [{tickerLabel}]";
        });

        return $"{ticker.ToUpper()} recent news sentiment:\n{string.Join("\n", summaries)}";
    }

    [KernelFunction, Description("Compares two stocks across price and sentiment")]
    public async Task<string> CompareStocks(
        [Description("First stock ticker")] string ticker1,
        [Description("Second stock ticker")] string ticker2)
    {
        var price1 = await GetStockPrice(ticker1);
        var price2 = await GetStockPrice(ticker2);
        var sentiment1 = GetNewsSentiment(ticker1);
        var sentiment2 = GetNewsSentiment(ticker2);

        return $"Comparison:\n{price1}\n{sentiment1}\n\n{price2}\n{sentiment2}";
    }

    [KernelFunction, Description("Gets fundamental data for a stock including P/E ratio, EPS, market cap, 52-week range, analyst target price and dividend yield. Use this to assess valuation and whether a stock may be overvalued or undervalued.")]
    public async Task<string> GetFundamentals(
        [Description("The stock ticker symbol, e.g. AAPL, MSFT, NVDA")] string ticker)
    {
        var url = $"https://www.alphavantage.co/query?function=OVERVIEW&symbol={ticker}&apikey={alphaVantageKey}";
        var response = await httpClient.GetStringAsync(url);
        var doc = JsonDocument.Parse(response);
        var root = doc.RootElement;

        if (!root.TryGetProperty("Symbol", out _))
            return $"No fundamental data found for {ticker}";

        var name = root.GetProperty("Name").GetString();
        var marketCap = root.GetProperty("MarketCapitalization").GetString();
        var pe = root.GetProperty("PERatio").GetString();
        var eps = root.GetProperty("EPS").GetString();
        var high52 = root.GetProperty("52WeekHigh").GetString();
        var low52 = root.GetProperty("52WeekLow").GetString();
        var dividend = root.GetProperty("DividendYield").GetString();
        var analyst = root.GetProperty("AnalystTargetPrice").GetString();
        var sector = root.GetProperty("Sector").GetString();

        var marketCapBn = decimal.TryParse(marketCap, out var mc) ? $"${mc / 1_000_000_000:F1}B" : "N/A";
        var divPct = decimal.TryParse(dividend, out var div) ? $"{div * 100:F2}%" : "None";

        return $"""
        {name} ({ticker.ToUpper()}) — {sector}
        Market Cap: {marketCapBn}
        P/E Ratio: {pe}
        EPS: ${eps}
        52-Week Range: ${low52} — ${high52}
        Dividend Yield: {divPct}
        Analyst Target Price: ${analyst}
        """;
    }

    [KernelFunction, Description("Gets upcoming and historical earnings data including dates and surprise percentages")]
    public async Task<string> GetEarnings(
        [Description("The stock ticker symbol, e.g. AAPL, MSFT, NVDA")] string ticker)
    {
        var url = $"https://www.alphavantage.co/query?function=EARNINGS&symbol={ticker}&apikey={alphaVantageKey}";
        var response = await httpClient.GetStringAsync(url);
        var doc = JsonDocument.Parse(response);

        if (!doc.RootElement.TryGetProperty("annualEarnings", out _))
            return $"No earnings data found for {ticker}";

        // Most recent 4 quarters
        var quarterly = doc.RootElement.GetProperty("quarterlyEarnings")
            .EnumerateArray().Take(4).ToList();

        var history = quarterly.Select(q =>
        {
            var date = q.GetProperty("reportedDate").GetString();
            var estimated = q.GetProperty("estimatedEPS").GetString();
            var actual = q.GetProperty("reportedEPS").GetString();
            var surprise = q.GetProperty("surprisePercentage").GetString();

            var surpriseVal = decimal.TryParse(surprise, out var s) ? s : 0;
            var beat = surpriseVal > 0 ? "Beat" : surpriseVal < 0 ? "Missed" : "Met";

            return $"- {date}: Actual EPS ${actual} vs Est ${estimated} — {beat} by {Math.Abs(surpriseVal):F2}%";
        });

        return $"""
        {ticker.ToUpper()} Earnings History (Last 4 Quarters):
        {string.Join("\n", history)}
        """;
    }
}