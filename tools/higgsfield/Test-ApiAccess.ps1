# Non-generating capability probe using the documented estimate endpoint.
# Reads credentials in memory; prints only status and allowlisted cost fields.
param()
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$taskCredential = $null
foreach ($taskLine in [IO.File]::ReadAllLines((Join-Path $taskRoot '.env'))) {
    if ($taskLine -match '^\s*HIGGSFIELD_API_KEY\s*=\s*(.*?)\s*$') {
        $taskCredential = $Matches[1].Trim().Trim('"').Trim("'")
    }
}
if ($taskCredential -notmatch '^[^:\s]+:[^:\s]+$') {
    throw 'HIGGSFIELD_API_KEY must contain a key ID and secret separated by a colon.'
}
$taskHandler = [Net.Http.HttpClientHandler]::new()
$taskHandler.AllowAutoRedirect = $false
$taskClient = [Net.Http.HttpClient]::new($taskHandler)
$taskClient.Timeout = [TimeSpan]::FromSeconds(30)
$taskRequest = [Net.Http.HttpRequestMessage]::new(
    [Net.Http.HttpMethod]::Post,
    'https://api.higgsfield.ai/estimate/higgsfield-ai/soul/v2/standard'
)
try {
    $taskRequest.Headers.TryAddWithoutValidation(
        'Authorization', ('Key ' + $taskCredential)
    ) | Out-Null
    $taskRequest.Content = [Net.Http.StringContent]::new(
        '{"prompt":"A plain gray ceramic sphere on a neutral background"}',
        [Text.Encoding]::UTF8, 'application/json'
    )
    $taskResponse = $taskClient.SendAsync($taskRequest).GetAwaiter().GetResult()
    $taskResult = [ordered]@{
        Operation = 'Estimate only; no generation submitted'
        HttpStatus = [int]$taskResponse.StatusCode
    }
    if ($taskResponse.IsSuccessStatusCode) {
        $taskBody = $taskResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult() |
            ConvertFrom-Json
        foreach ($taskField in @('credits', 'usd')) {
            $taskCost = 0.0
            if ([double]::TryParse([string]$taskBody.$taskField,
                [Globalization.NumberStyles]::Float,
                [Globalization.CultureInfo]::InvariantCulture, [ref]$taskCost)) {
                $taskResult[$taskField] = $taskCost
            }
        }
    }
    $taskResult | ConvertTo-Json
    if (-not $taskResponse.IsSuccessStatusCode) {
        throw 'The estimate endpoint returned a non-success HTTP status.'
    }
} catch {
    # Do not serialize exceptions, response bodies or request objects: they may
    # contain authorization data. Report safe operation context and rethrow.
    Write-Output ('Higgsfield estimate probe failed: ' + $_.Exception.GetType().Name)
    throw 'Higgsfield estimate probe failed; sensitive details suppressed.'
} finally {
    $taskCredential = $null
    $taskLine = $null
    $Matches = @{}
    $taskRequest.Dispose()
    $taskClient.Dispose()
    $taskHandler.Dispose()
}
