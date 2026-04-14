param(
    [string]$InputPath = "tools/fetched_from_vtdb.json",
    [string]$OutputPath = "tools/grasshopper_tool_catalog.json"
)

$toolTypeMap = @{
    "0" = "ball_nose"
    "1" = "end_mill"
    "2" = "radiused_end_mill"
    "3" = "vbit"
    "4" = "engraving"
    "5" = "radiused_engraving"
    "6" = "through_drill"
    "7" = "form_tool"
    "8" = "diamond_drag"
    "9" = "radiused_flat_engraving"
}

$operationTypeMap = @{
    "ball_nose" = @("3d_finish")
    "end_mill" = @("profile", "pocket")
    "radiused_end_mill" = @("profile", "pocket")
    "vbit" = @("vcarve", "engrave")
    "engraving" = @("engrave")
    "radiused_engraving" = @("engrave")
    "through_drill" = @("drill")
    "form_tool" = @("special")
    "diamond_drag" = @("drag")
    "radiused_flat_engraving" = @("engrave")
}

function Get-DisplayName {
    param($Tool, [string]$ToolTypeName)

    $groupName = [string]$Tool.aspire_group
    if ([string]::IsNullOrWhiteSpace($groupName)) {
        $groupName = [string]$ToolTypeName
    }

    $diameter = [double]$Tool.diameter_mm
    $toolNumber = [int]$Tool.tool_number
    if ($toolNumber -gt 0) {
        return "$groupName ${diameter}mm T$toolNumber"
    }

    return "$groupName ${diameter}mm"
}

$sourceTools = Get-Content -Raw -Path $InputPath | ConvertFrom-Json

$normalizedTools = foreach ($tool in $sourceTools) {
    $toolTypeCode = [string]$tool.tool_type
    $toolTypeName = $toolTypeMap[$toolTypeCode]
    if (-not $toolTypeName) {
        $toolTypeName = "unknown"
    }

    [PSCustomObject]@{
        id = [string]$tool.id
        display_name = Get-DisplayName -Tool $tool -ToolTypeName $toolTypeName
        tool_type = $toolTypeName
        aspire_group = [string]$tool.aspire_group
        diameter_mm = [double]$tool.diameter_mm
        tool_number = [int]$tool.tool_number
        flute_count = [int]$tool.flute_count
        stepdown_mm = [double]$tool.stepdown_mm
        stepover_mm = [double]$tool.stepover_mm
        rpm_recommend = [double]$tool.rpm_recommend
        feed_recommend_mm_per_min = [double]$tool.feed_recommend_mm_per_min
        plunge_recommend_mm_per_min = [double]$tool.plunge_recommend_mm_per_min
        operation_types = @($operationTypeMap[$toolTypeName])
        selector = [PSCustomObject]@{
            id = [string]$tool.id
            tool_type = $toolTypeName
            diameter_mm = [double]$tool.diameter_mm
            tool_number = [int]$tool.tool_number
            aspire_group = [string]$tool.aspire_group
        }
    }
}

$catalog = [PSCustomObject]@{
    catalog_version = 1
    source = $InputPath
    generated_at = (Get-Date).ToString("s")
    tools = @(
        $normalizedTools |
        Sort-Object tool_type, diameter_mm, display_name
    )
}

$catalog | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputPath -Encoding UTF8
Write-Output "Generated $OutputPath"