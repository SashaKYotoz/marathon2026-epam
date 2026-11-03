using Microsoft.Playwright;

namespace Restaurant_Aqa_Framework.Core;

public static class StorefrontAvailability
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static string? _failure;
    private static bool _checked;

    public static async Task EnsureReachableAsync(IPlaywright playwright, string baseUrl)
    {
        if (!_checked)
        {
            await Gate.WaitAsync();
            try
            {
                if (!_checked)
                {
                    _failure = await ProbeAsync(playwright, baseUrl);
                    _checked = true;
                }
            }
            finally
            {
                Gate.Release();
            }
        }

        if (_failure is not null)
            throw new InvalidOperationException(_failure);
    }

    private static async Task<string?> ProbeAsync(IPlaywright playwright, string baseUrl)
    {
        await using var request = await playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            Timeout = 10_000,
            IgnoreHTTPSErrors = true,
        });

        try
        {
            var response = await request.GetAsync(baseUrl);
            return response.Status < 500
                ? null
                : $"The storefront at '{baseUrl}' answered {response.Status}. Check the frontend deployment or UI_URL, then rerun the @ui scenarios.";
        }
        catch (PlaywrightException ex)
        {
            return $"The storefront at '{baseUrl}' is not reachable ({ex.Message.Split('\n')[0]}). "
                + "Start the frontend first (cd frontend/restaurant-frontend-app && npm start) or point UI_URL at the deployed CloudFront URL, "
                + "or run only API scenarios with --treenode-filter \"/*/*/*/*[Category=api]\".";
        }
    }
}
