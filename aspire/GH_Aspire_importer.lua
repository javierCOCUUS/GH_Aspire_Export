-- VECTRIC LUA SCRIPT

local STOCK_LAYER_NAME = "STOCK"

local TOOL_TYPE_BY_CODE = {
  ["0"] = Tool.BALL_NOSE,
  ["1"] = Tool.END_MILL,
  ["2"] = Tool.RADIUSED_END_MILL,
  ["3"] = Tool.VBIT,
  ["4"] = Tool.ENGRAVING,
  ["5"] = Tool.RADIUSED_ENGRAVING,
  ["6"] = Tool.THROUGH_DRILL,
  ["7"] = Tool.FORM_TOOL,
  ["8"] = Tool.DIAMOND_DRAG,
  ["9"] = Tool.RADIUSED_FLAT_ENGRAVING
}

local TOOL_TYPE_BY_NAME = {
  ball_nose = Tool.BALL_NOSE,
  end_mill = Tool.END_MILL,
  radiused_end_mill = Tool.RADIUSED_END_MILL,
  vbit = Tool.VBIT,
  engraving = Tool.ENGRAVING,
  radiused_engraving = Tool.RADIUSED_ENGRAVING,
  through_drill = Tool.THROUGH_DRILL,
  form_tool = Tool.FORM_TOOL,
  diamond_drag = Tool.DIAMOND_DRAG,
  radiused_flat_engraving = Tool.RADIUSED_FLAT_ENGRAVING,
  drill = Tool.THROUGH_DRILL
}

local DEFAULT_TOOL_SELECTORS = {
  profile = { diameter_mm = 6.0, tool_type = "end_mill" },
  pocket = { diameter_mm = 6.0, tool_type = "end_mill" },
  drill = { diameter_mm = 5.0, tool_type = "through_drill" }
}

local FALLBACK_TOOL_CONFIGS = {
  profile = {
    name = "Profile Tool 6mm",
    tool_type = Tool.END_MILL,
    tool_number = 1,
    dia = 6.0,
    stepdown = 3.0,
    stepover = 2.4,
    feed = 30.0,
    plunge = 10.0,
    spindle = 18000.0
  },
  pocket = {
    name = "Pocket Tool 6mm",
    tool_type = Tool.END_MILL,
    tool_number = 2,
    dia = 6.0,
    stepdown = 3.0,
    stepover = 2.4,
    feed = 30.0,
    plunge = 10.0,
    spindle = 18000.0
  },
  drill = {
    name = "Drill Tool 5mm",
    tool_type = Tool.THROUGH_DRILL,
    tool_number = 3,
    dia = 5.0,
    stepdown = 18.0,
    stepover = 0.0,
    feed = 20.0,
    plunge = 10.0,
    spindle = 8000.0
  }
}

local IMPORT_DIALOG_HTML = [[
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<style>
body { font-family: Segoe UI, Arial, sans-serif; margin: 18px; color: #202124; }
h2 { margin: 0 0 14px 0; font-size: 18px; }
p { margin: 0 0 14px 0; color: #555; }
table { width: 100%; border-collapse: collapse; }
td { padding: 6px 0; vertical-align: middle; }
.label { width: 88px; font-weight: 600; }
.path { width: 100%; }
.path input { width: 100%; box-sizing: border-box; padding: 6px 8px; }
.pick { width: 88px; text-align: right; }
.pick input { width: 78px; }
.actions { margin-top: 14px; display: flex; gap: 10px; align-items: center; }
.actions input { min-width: 110px; padding: 6px 10px; }
.status { margin-top: 12px; padding: 10px 12px; background: #f4f6f8; border: 1px solid #d7dce1; color: #334; font-size: 12px; }
.hint { margin-top: 12px; font-size: 12px; color: #666; }
</style>
</head>
<body>
  <h2>GH Aspire Import</h2>
  <p>Selecciona el JSON del job, el DXF de geometria y el catalogo de herramientas.</p>
  <table>
    <tr>
      <td class="label">JSON</td>
      <td class="path"><input type="text" id="JsonPath"></td>
      <td class="pick"><input class="FilePicker" id="PickJson" name="PickJson" type="button" value="Elegir"></td>
    </tr>
    <tr>
      <td class="label">DXF</td>
      <td class="path"><input type="text" id="DxfPath"></td>
      <td class="pick"><input class="FilePicker" id="PickDxf" name="PickDxf" type="button" value="Elegir"></td>
    </tr>
    <tr>
      <td class="label">Tools</td>
      <td class="path"><input type="text" id="ToolsPath"></td>
      <td class="pick"><input class="FilePicker" id="PickTools" name="PickTools" type="button" value="Elegir"></td>
    </tr>
  </table>
  <div class="actions">
    <input class="LuaButton" id="ValidateSelection" name="ValidateSelection" type="button" value="Validar">
  </div>
  <div class="status"><span id="ValidationStatus">Pendiente de validacion.</span></div>
  <div class="hint">Si el DXF se deja vacio se intentara usar el mismo nombre base que el JSON.</div>
</body>
</html>
]]

local function message(txt)
  DisplayMessageBox(tostring(txt))
end

local function trim(s)
  if s == nil then return "" end
  return (tostring(s):gsub("^%s+", ""):gsub("%s+$", ""))
end

local function upper_trim(s)
  return string.upper(trim(s or ""))
end

local function lower_trim(s)
  return string.lower(trim(s or ""))
end

local function file_exists(path)
  local f = io.open(path, "r")
  if f ~= nil then
    f:close()
    return true
  end
  return false
end

local function read_all_text(path)
  local f = io.open(path, "r")
  if f == nil then
    return nil
  end
  local txt = f:read("*a")
  f:close()
  return txt
end

local function write_all_text(path, txt)
  local f = io.open(path, "w")
  if f == nil then
    return false
  end
  f:write(txt)
  f:close()
  return true
end

local function normalize_path(path)
  local s = trim(path)
  s = s:gsub("/", "\\")
  s = s:gsub("\\+", "\\")
  return s
end

local function dirname(path)
  local s = normalize_path(path)
  return s:match("^(.*)\\[^\\]+$") or s
end

local function join_path(base, child)
  if child == nil or child == "" then
    return normalize_path(base)
  end
  if child:match("^%a:[\\/]") then
    return normalize_path(child)
  end
  local b = normalize_path(base)
  if b:sub(-1) == "\\" then
    return normalize_path(b .. child)
  end
  return normalize_path(b .. "\\" .. child)
end

local function ensure_directory(path)
  local dir = normalize_path(path)
  if dir == "" then
    return false
  end

  os.execute('mkdir "' .. dir .. '" >nul 2>nul')

  local probe_path = join_path(dir, "__dir_probe__.tmp")
  local probe = io.open(probe_path, "w")
  if probe == nil then
    return false
  end

  probe:close()
  os.remove(probe_path)
  return true
end

local function replace_extension(path, new_ext)
  local base = path:gsub("%.[^%.\\/:]+$", "")
  return base .. new_ext
end

local function escape_json_string(s)
  local out = tostring(s or "")
  out = out:gsub("\\", "\\\\")
  out = out:gsub('"', '\\"')
  out = out:gsub("\r", "\\r")
  out = out:gsub("\n", "\\n")
  out = out:gsub("\t", "\\t")
  return out
end

local function encode_string_map(data)
  local ordered_keys = { "json_path", "dxf_path", "tools_path" }
  local lines = { "{" }

  for i = 1, #ordered_keys do
    local key = ordered_keys[i]
    local suffix = i < #ordered_keys and "," or ""
    table.insert(lines, '  "' .. key .. '": "' .. escape_json_string(data[key] or "") .. '"' .. suffix)
  end

  table.insert(lines, "}")
  return table.concat(lines, "\n")
end

local function load_import_settings(path)
  if not file_exists(path) then
    return {}
  end

  local txt = read_all_text(path)
  if txt == nil or txt == "" then
    return {}
  end

  local data = nil
  local ok, err = pcall(function()
    data = decode_json(txt)
  end)

  if not ok or type(data) ~= "table" then
    return {}
  end

  return data
end

local function save_import_settings(path, data)
  local parent_dir = dirname(path)
  ensure_directory(parent_dir)
  return write_all_text(path, encode_string_map(data))
end

local function get_script_dir(script_path)
  local p = trim(script_path)
  if p == "" then
    local src = debug and debug.getinfo and debug.getinfo(1, "S").source or ""
    if src:sub(1, 1) == "@" then
      p = src:sub(2)
    end
  end

  p = normalize_path(p)

  if p:match("%.lua$") then
    return dirname(p)
  end

  return p
end

local function get_settings_path(script_dir)
  local appdata = normalize_path(os.getenv("APPDATA") or "")
  if appdata ~= "" then
    local settings_dir = join_path(appdata, "GH_Aspire_Export")
    if ensure_directory(settings_dir) then
      return join_path(settings_dir, "GH_Aspire_importer.settings.json")
    end
  end

  return join_path(script_dir, "GH_Aspire_importer.settings.json")
end

local function skip_ws(text, idx)
  while idx <= #text do
    local c = text:sub(idx, idx)
    if c ~= " " and c ~= "\n" and c ~= "\r" and c ~= "\t" then
      break
    end
    idx = idx + 1
  end
  return idx
end

local function decode_error(text, idx, msg)
  error("JSON error at position " .. tostring(idx) .. ": " .. tostring(msg) .. "\n" .. text)
end

local function parse_json_string(text, idx)
  idx = idx + 1
  local out = {}

  while idx <= #text do
    local c = text:sub(idx, idx)
    if c == '"' then
      return table.concat(out), idx + 1
    end

    if c == "\\" then
      local esc = text:sub(idx + 1, idx + 1)
      if esc == '"' or esc == "\\" or esc == "/" then
        table.insert(out, esc)
        idx = idx + 2
      elseif esc == "b" then
        table.insert(out, "\b")
        idx = idx + 2
      elseif esc == "f" then
        table.insert(out, "\f")
        idx = idx + 2
      elseif esc == "n" then
        table.insert(out, "\n")
        idx = idx + 2
      elseif esc == "r" then
        table.insert(out, "\r")
        idx = idx + 2
      elseif esc == "t" then
        table.insert(out, "\t")
        idx = idx + 2
      elseif esc == "u" then
        local hex = text:sub(idx + 2, idx + 5)
        if not hex:match("^%x%x%x%x$") then
          decode_error(text, idx, "invalid unicode escape")
        end
        local code = tonumber(hex, 16)
        if code < 128 then
          table.insert(out, string.char(code))
        else
          table.insert(out, "?")
        end
        idx = idx + 6
      else
        decode_error(text, idx, "invalid escape")
      end
    else
      table.insert(out, c)
      idx = idx + 1
    end
  end

  decode_error(text, idx, "unterminated string")
end

local function parse_json_number(text, idx)
  local start_idx = idx
  local c = text:sub(idx, idx)
  if c == "-" then
    idx = idx + 1
  end

  while text:sub(idx, idx):match("%d") do
    idx = idx + 1
  end

  if text:sub(idx, idx) == "." then
    idx = idx + 1
    while text:sub(idx, idx):match("%d") do
      idx = idx + 1
    end
  end

  local exp = text:sub(idx, idx)
  if exp == "e" or exp == "E" then
    idx = idx + 1
    local sign = text:sub(idx, idx)
    if sign == "+" or sign == "-" then
      idx = idx + 1
    end
    while text:sub(idx, idx):match("%d") do
      idx = idx + 1
    end
  end

  local num = tonumber(text:sub(start_idx, idx - 1))
  if num == nil then
    decode_error(text, start_idx, "invalid number")
  end
  return num, idx
end

local parse_json_value

local function parse_json_array(text, idx)
  local arr = {}
  idx = skip_ws(text, idx + 1)
  if text:sub(idx, idx) == "]" then
    return arr, idx + 1
  end

  while idx <= #text do
    local val
    val, idx = parse_json_value(text, idx)
    table.insert(arr, val)
    idx = skip_ws(text, idx)
    local c = text:sub(idx, idx)
    if c == "]" then
      return arr, idx + 1
    end
    if c ~= "," then
      decode_error(text, idx, "expected ',' or ']' in array")
    end
    idx = skip_ws(text, idx + 1)
  end

  decode_error(text, idx, "unterminated array")
end

local function parse_json_object(text, idx)
  local obj = {}
  idx = skip_ws(text, idx + 1)
  if text:sub(idx, idx) == "}" then
    return obj, idx + 1
  end

  while idx <= #text do
    if text:sub(idx, idx) ~= '"' then
      decode_error(text, idx, "expected string key")
    end
    local key
    key, idx = parse_json_string(text, idx)
    idx = skip_ws(text, idx)
    if text:sub(idx, idx) ~= ":" then
      decode_error(text, idx, "expected ':' after key")
    end
    idx = skip_ws(text, idx + 1)
    local val
    val, idx = parse_json_value(text, idx)
    obj[key] = val
    idx = skip_ws(text, idx)
    local c = text:sub(idx, idx)
    if c == "}" then
      return obj, idx + 1
    end
    if c ~= "," then
      decode_error(text, idx, "expected ',' or '}' in object")
    end
    idx = skip_ws(text, idx + 1)
  end

  decode_error(text, idx, "unterminated object")
end

parse_json_value = function(text, idx)
  idx = skip_ws(text, idx)
  local c = text:sub(idx, idx)

  if c == '"' then
    return parse_json_string(text, idx)
  elseif c == "{" then
    return parse_json_object(text, idx)
  elseif c == "[" then
    return parse_json_array(text, idx)
  elseif c == "-" or c:match("%d") then
    return parse_json_number(text, idx)
  elseif text:sub(idx, idx + 3) == "true" then
    return true, idx + 4
  elseif text:sub(idx, idx + 4) == "false" then
    return false, idx + 5
  elseif text:sub(idx, idx + 3) == "null" then
    return nil, idx + 4
  end

  decode_error(text, idx, "unexpected token")
end

local function decode_json(text)
  local value, idx = parse_json_value(text, 1)
  idx = skip_ws(text, idx)
  if idx <= #text then
    decode_error(text, idx, "trailing content")
  end
  return value
end

local function to_number(v, default_value)
  local n = tonumber(v)
  if n == nil then
    return default_value
  end
  return n
end

local function mm_to_job_units(value_mm, in_mm)
  if not in_mm then
    return value_mm / 25.4
  end
  return value_mm
end

local function mm_per_min_to_mm_per_sec(value)
  return (to_number(value, 0.0) or 0.0) / 60.0
end

local function lookup_tool_type(value)
  if type(value) == "number" then
    return TOOL_TYPE_BY_CODE[tostring(math.floor(value))]
  end
  if value == nil then
    return nil
  end
  local txt = lower_trim(value):gsub("%s+", "_")
  return TOOL_TYPE_BY_NAME[txt] or TOOL_TYPE_BY_CODE[txt]
end

local function format_tool_name(tool_entry, fallback_name)
  if tool_entry == nil then
    return fallback_name
  end

  local group_name = trim(tool_entry.aspire_group)
  local diameter_mm = to_number(tool_entry.diameter_mm, 0.0)
  local tool_number = to_number(tool_entry.tool_number, 0)

  if group_name == "" then
    group_name = trim(tool_entry.name)
  end

  if group_name == "" then
    group_name = fallback_name
  end

  local base = group_name .. " " .. tostring(diameter_mm) .. "mm"
  if tool_number > 0 then
    base = base .. " T" .. tostring(tool_number)
  end
  return base
end

local function clone_selector(selector)
  local out = {}
  if selector ~= nil then
    for k, v in pairs(selector) do
      out[k] = v
    end
  end
  return out
end

local function selector_has_values(selector)
  if selector == nil then
    return false
  end

  for _, v in pairs(selector) do
    if v ~= nil and trim(v) ~= "" then
      return true
    end
  end

  return false
end

local function merge_selector_values(target, source)
  if type(source) ~= "table" then
    return
  end

  for k, v in pairs(source) do
    target[k] = v
  end
end

local function resolve_selector_for_operation(job_data, op)
  local selector = clone_selector(DEFAULT_TOOL_SELECTORS[op.type] or {})

  if type(job_data) == "table" and type(job_data.tool_defaults) == "table" then
    merge_selector_values(selector, job_data.tool_defaults[op.type])
  end

  if type(op.tool) == "table" then
    merge_selector_values(selector, op.tool)
  end

  return selector
end

local function tool_matches_selector(tool_entry, selector)
  if selector == nil then
    return false
  end

  if selector.id ~= nil and tostring(tool_entry.id) ~= tostring(selector.id) then
    return false
  end

  if selector.tool_number ~= nil and to_number(tool_entry.tool_number, -1) ~= to_number(selector.tool_number, -2) then
    return false
  end

  local selector_tool_type = lookup_tool_type(selector.tool_type)
  if selector_tool_type ~= nil then
    local entry_tool_type = lookup_tool_type(tool_entry.tool_type)
    if entry_tool_type ~= selector_tool_type then
      return false
    end
  end

  if selector.aspire_group ~= nil and lower_trim(tool_entry.aspire_group) ~= lower_trim(selector.aspire_group) then
    return false
  end

  if selector.diameter_mm ~= nil then
    local entry_dia = to_number(tool_entry.diameter_mm, -1)
    local selector_dia = to_number(selector.diameter_mm, -2)
    if math.abs(entry_dia - selector_dia) > 0.001 then
      return false
    end
  end

  return true
end

local function score_tool(tool_entry, selector)
  local score = 0
  if selector.id ~= nil and tostring(tool_entry.id) == tostring(selector.id) then
    score = score + 1000
  end
  if selector.tool_number ~= nil and to_number(tool_entry.tool_number, -1) == to_number(selector.tool_number, -2) then
    score = score + 150
  end
  if selector.diameter_mm ~= nil and math.abs(to_number(tool_entry.diameter_mm, 0) - to_number(selector.diameter_mm, 0)) < 0.001 then
    score = score + 100
  end
  if selector.tool_type ~= nil and lookup_tool_type(tool_entry.tool_type) == lookup_tool_type(selector.tool_type) then
    score = score + 75
  end
  if selector.aspire_group ~= nil and lower_trim(tool_entry.aspire_group) == lower_trim(selector.aspire_group) then
    score = score + 20
  end
  return score
end

local function find_tool_in_database(tool_db, selector)
  if type(tool_db) ~= "table" then
    return nil
  end

  local best_tool = nil
  local best_score = -1

  for i = 1, #tool_db do
    local entry = tool_db[i]
    if type(entry) == "table" and tool_matches_selector(entry, selector) then
      local score = score_tool(entry, selector)
      if score > best_score then
        best_tool = entry
        best_score = score
      end
    end
  end

  return best_tool
end

local function build_fallback_tool(op, in_mm)
  local cfg = FALLBACK_TOOL_CONFIGS[op.type] or FALLBACK_TOOL_CONFIGS.profile
  local tool = Tool(cfg.name, cfg.tool_type)
  tool.InMM = in_mm
  tool.ToolNumber = cfg.tool_number
  tool.ToolDia = mm_to_job_units(cfg.dia, in_mm)
  tool.Stepdown = mm_to_job_units(cfg.stepdown, in_mm)
  tool.Stepover = mm_to_job_units(cfg.stepover, in_mm)
  tool.RateUnits = Tool.MM_SEC
  tool.FeedRate = cfg.feed
  tool.PlungeRate = cfg.plunge
  tool.SpindleSpeed = cfg.spindle
  tool:UpdateParameters()
  return tool, cfg.name .. " (fallback)"
end

local function build_tool_from_database(op, tool_entry, in_mm)
  local tool_type = lookup_tool_type(tool_entry.tool_type)
  if tool_type == nil then
    return nil, "Tipo de herramienta no soportado en tools JSON"
  end

  local tool_name = format_tool_name(tool_entry, op.name)
  local tool = Tool(tool_name, tool_type)
  tool.InMM = in_mm
  tool.ToolNumber = to_number(tool_entry.tool_number, 1)
  tool.ToolDia = mm_to_job_units(to_number(tool_entry.diameter_mm, 0.0), in_mm)
  tool.Stepdown = mm_to_job_units(to_number(tool_entry.stepdown_mm, 1.0), in_mm)
  tool.Stepover = mm_to_job_units(to_number(tool_entry.stepover_mm, 0.0), in_mm)
  tool.RateUnits = Tool.MM_SEC
  tool.FeedRate = mm_per_min_to_mm_per_sec(tool_entry.feed_recommend_mm_per_min)
  tool.PlungeRate = mm_per_min_to_mm_per_sec(tool_entry.plunge_recommend_mm_per_min)
  tool.SpindleSpeed = to_number(tool_entry.rpm_recommend, 12000.0)
  tool:UpdateParameters()
  return tool, tool_name
end

local function build_tool_for_operation(job_data, op, tool_db, in_mm)
  local selector = resolve_selector_for_operation(job_data, op)
  local entry = find_tool_in_database(tool_db, selector)
  if entry ~= nil then
    local tool, tool_name_or_error = build_tool_from_database(op, entry, in_mm)
    if tool ~= nil then
      return tool, tool_name_or_error
    end
  end

  if selector_has_values(selector) then
    return build_fallback_tool(op, in_mm)
  end

  return build_fallback_tool(op, in_mm)
end

local function parse_job_json(json_text)
  local raw = decode_json(json_text)
  if type(raw) ~= "table" then
    error("El JSON del job debe ser un objeto")
  end

  raw.material = raw.material or {}
  raw.operations = raw.operations or {}

  return {
    job_name = raw.job_name or "job",
    units = raw.units or "mm",
    origin = raw.origin or "BOTTOM_LEFT",
    material = {
      thickness = to_number(raw.material.thickness, 18.0),
      z_zero = raw.material.z_zero or "material_top"
    },
    tool_defaults = raw.tool_defaults or {},
    operations = raw.operations
  }
end

local function parse_tool_database(json_text)
  local raw = decode_json(json_text)
  if type(raw) ~= "table" then
    error("El JSON de herramientas debe ser un array")
  end
  return raw
end

local function collect_dialog_selection(dialog)
  local json_path = normalize_path(dialog:GetTextField("JsonPath"))
  local dxf_path = normalize_path(dialog:GetTextField("DxfPath"))
  local tools_path = normalize_path(dialog:GetTextField("ToolsPath"))

  if dxf_path == "" and json_path ~= "" then
    dxf_path = replace_extension(json_path, ".dxf")
    dialog:UpdateTextField("DxfPath", dxf_path)
  end

  return {
    json_path = json_path,
    dxf_path = dxf_path,
    tools_path = tools_path
  }
end

local function validate_selected_inputs(selection)
  if trim(selection.json_path) == "" then
    return false, "Falta elegir el JSON del job."
  end

  if trim(selection.dxf_path) == "" then
    return false, "Falta elegir el DXF de geometria."
  end

  if trim(selection.tools_path) == "" then
    return false, "Falta elegir el catalogo de herramientas."
  end

  if not file_exists(selection.json_path) then
    return false, "No existe el JSON: " .. tostring(selection.json_path)
  end

  if not file_exists(selection.dxf_path) then
    return false, "No existe el DXF: " .. tostring(selection.dxf_path)
  end

  if not file_exists(selection.tools_path) then
    return false, "No existe el catalogo de herramientas: " .. tostring(selection.tools_path)
  end

  local json_text = read_all_text(selection.json_path)
  if json_text == nil or json_text == "" then
    return false, "No se pudo leer el JSON del job."
  end

  local tools_text = read_all_text(selection.tools_path)
  if tools_text == nil or tools_text == "" then
    return false, "No se pudo leer el catalogo de herramientas."
  end

  local job_data = nil
  local ok_job, err_job = pcall(function()
    job_data = parse_job_json(json_text)
  end)
  if not ok_job then
    return false, "JSON del job invalido: " .. tostring(err_job)
  end

  local tool_db = nil
  local ok_tools, err_tools = pcall(function()
    tool_db = parse_tool_database(tools_text)
  end)
  if not ok_tools then
    return false, "JSON de herramientas invalido: " .. tostring(err_tools)
  end

  local op_count = 0
  if type(job_data.operations) == "table" then
    op_count = #job_data.operations
  end

  local tool_count = 0
  if type(tool_db) == "table" then
    tool_count = #tool_db
  end

  return true, "Validacion OK. Operaciones: " .. tostring(op_count) .. " | Herramientas: " .. tostring(tool_count)
end

local function update_validation_status(dialog, ok, text)
  local prefix = ok and "OK: " or "ERROR: "
  dialog:UpdateLabelField("ValidationStatus", prefix .. tostring(text))
end

function OnLuaButton_ValidateSelection(dialog)
  local selection = collect_dialog_selection(dialog)
  local ok, validation_message = validate_selected_inputs(selection)
  update_validation_status(dialog, ok, validation_message)
  return true
end

function OnFilePicker_PickJson(dialog)
  collect_dialog_selection(dialog)
  return true
end

local function choose_input_files(defaults)
  local dialog = HTML_Dialog(true, IMPORT_DIALOG_HTML, 760, 300, "GH Aspire Import")
  dialog:AddTextField("JsonPath", defaults.json_path or "")
  dialog:AddTextField("DxfPath", defaults.dxf_path or "")
  dialog:AddTextField("ToolsPath", defaults.tools_path or "")
  dialog:AddLabelField("ValidationStatus", "Pendiente de validacion.")
  dialog:AddFilePicker(false, "PickJson", "JsonPath", true)
  dialog:AddFilePicker(false, "PickDxf", "DxfPath", true)
  dialog:AddFilePicker(false, "PickTools", "ToolsPath", true)

  local ok = dialog:ShowDialog()
  if not ok then
    return nil
  end

  return collect_dialog_selection(dialog)
end

local function clear_selection(job)
  job.Selection:Clear()
end

local function add_selectable_object_recursive(obj, selection)
  if obj == nil then
    return 0
  end

  local class_name = obj.ClassName

  if class_name == "vcCadContour" or class_name == "vcCadPolyline" then
    selection:Add(obj, true, true)
    return 1
  end

  if class_name == "vcCadObjectGroup" then
    local count = 0
    local pos = obj:GetHeadPosition()

    while pos ~= nil do
      local child
      child, pos = obj:GetNext(pos)
      count = count + add_selectable_object_recursive(child, selection)
    end

    return count
  end

  return 0
end

local function select_all_objects_on_layer(job, layer_name)
  local layer = job.LayerManager:FindLayerWithName(layer_name)
  if layer == nil then
    return false, "No existe la capa: " .. layer_name
  end

  local selection = job.Selection
  selection:Clear()

  local pos = layer:GetHeadPosition()
  local raw_count = 0
  local selected_count = 0

  while pos ~= nil do
    local obj
    obj, pos = layer:GetNext(pos)

    if obj ~= nil then
      raw_count = raw_count + 1
      selected_count = selected_count + add_selectable_object_recursive(obj, selection)
    end
  end

  selection:GroupSelectionFinished()

  if selected_count == 0 then
    return false, "La capa " .. layer_name .. " tiene " .. tostring(raw_count) .. " objetos pero 0 vectores seleccionables"
  end

  return true, tostring(selected_count) .. " vectores"
end

local function list_layers(job)
  local lm = job.LayerManager
  local pos = lm:GetHeadPosition()
  local layers = {}

  while pos ~= nil do
    local layer
    layer, pos = lm:GetNext(pos)
    if layer ~= nil then
      table.insert(layers, layer.Name)
    end
  end

  return layers
end

local function find_stock_layer(job)
  local lm = job.LayerManager
  local pos = lm:GetHeadPosition()

  while pos ~= nil do
    local layer
    layer, pos = lm:GetNext(pos)

    if layer ~= nil then
      local n = upper_trim(layer.Name)
      if n == "STOCK" then
        return layer, layer.Name
      end
    end
  end

  pos = lm:GetHeadPosition()
  while pos ~= nil do
    local layer
    layer, pos = lm:GetNext(pos)

    if layer ~= nil then
      local n = upper_trim(layer.Name)
      if string.find(n, "STOCK", 1, true) then
        return layer, layer.Name
      end
    end
  end

  return nil, nil
end

local function get_stock_bounds(job)
  local layer, real_name = find_stock_layer(job)
  if layer == nil then
    return nil, "No existe la capa STOCK", nil, nil
  end

  local pos = layer:GetHeadPosition()
  local merged_box = nil
  local count = 0

  while pos ~= nil do
    local obj
    obj, pos = layer:GetNext(pos)

    if obj ~= nil then
      local box = obj:GetBoundingBox()
      if box ~= nil then
        if merged_box == nil then
          merged_box = Box2D(box)
        else
          merged_box:Merge(box)
        end
        count = count + 1
      end
    end
  end

  if merged_box == nil then
    return nil, "La capa STOCK no tiene objetos validos", real_name, 0
  end

  return merged_box, nil, real_name, count
end

local function resize_job_to_stock(job, stock_box, thickness)
  local width = stock_box.XLength
  local height = stock_box.YLength

  local sheet_manager = job.SheetManager
  local active_sheet_id = sheet_manager.ActiveSheetId

  local ok = sheet_manager:ResizeSheet(active_sheet_id, width, height, thickness, false)
  return ok, width, height
end

local function apply_xy_origin(job, origin_name)
  local origin_value = MaterialBlock.BLC
  local o = trim(origin_name)

  if o == "BOTTOM_LEFT" or o == "lower_left" then
    origin_value = MaterialBlock.BLC
  elseif o == "BOTTOM_RIGHT" then
    origin_value = MaterialBlock.BRC
  elseif o == "TOP_RIGHT" then
    origin_value = MaterialBlock.TRC
  elseif o == "TOP_LEFT" then
    origin_value = MaterialBlock.TLC
  elseif o == "CENTER" or o == "CENTRE" then
    origin_value = MaterialBlock.CENTRE
  else
    origin_value = MaterialBlock.BLC
  end

  return job:SetXY_Origin(origin_value, 0.0, 0.0)
end

local function default_pos_data(in_mm)
  local pos_data = ToolpathPosData()
  pos_data.SafeZGap = in_mm and 5.0 or 0.2
  pos_data.StartZGap = 0.0
  pos_data:SetHomePosition(0.0, 0.0, in_mm and 20.0 or 1.0)
  pos_data:EnsureHomeZIsSafe()
  return pos_data
end

local function default_geometry_selector()
  return GeometrySelector()
end

local function create_profile_toolpath(job, job_data, op, in_mm, tool_db)
  local ok_sel, sel_info = select_all_objects_on_layer(job, op.layer)
  if not ok_sel then
    return false, sel_info
  end

  local tool, tool_name = build_tool_for_operation(job_data, op, tool_db, in_mm)

  local profile_data = ProfileParameterData()
  profile_data.Name = op.name
  profile_data.StartDepth = op.start_depth
  profile_data.CutDepth = op.cut_depth
  profile_data.Allowance = 0.0
  profile_data.ProjectToolpath = false
  profile_data.CornerSharpen = false
  profile_data.CreateSquareCorners = false
  profile_data.KeepStartPoints = false
  profile_data.UseTabs = false
  profile_data.TabLength = 5.0
  profile_data.TabThickness = 1.0
  profile_data.Use3dTabs = false
  profile_data.CutDirection = ProfileParameterData.CLIMB_DIRECTION

  if op.side == "outside" then
    profile_data.ProfileSide = ProfileParameterData.PROFILE_OUTSIDE
  else
    profile_data.ProfileSide = ProfileParameterData.PROFILE_INSIDE
  end

  local ramp_data = RampingData()
  ramp_data.DoRamping = false

  local lead_data = LeadInOutData()
  lead_data.DoLeadIn = false
  lead_data.DoLeadOut = false

  local pos_data = default_pos_data(in_mm)
  local geometry_selector = default_geometry_selector()

  local toolpath_manager = ToolpathManager()
  local toolpath_id = toolpath_manager:CreateProfilingToolpath(
    op.name,
    tool,
    profile_data,
    ramp_data,
    lead_data,
    pos_data,
    geometry_selector,
    false,
    true
  )

  clear_selection(job)

  if toolpath_id == nil then
    return false, "No se pudo crear profile: " .. op.name
  end

  return true, "OK | Tool: " .. tostring(tool_name)
end

local function create_pocket_toolpath(job, job_data, op, in_mm, tool_db)
  local ok_sel, sel_info = select_all_objects_on_layer(job, op.layer)
  if not ok_sel then
    return false, sel_info
  end

  local tool, tool_name = build_tool_for_operation(job_data, op, tool_db, in_mm)

  local pocket_data = PocketParameterData()
  pocket_data.Name = op.name
  pocket_data.StartDepth = op.start_depth
  pocket_data.CutDepth = op.cut_depth
  pocket_data.Allowance = 0.0
  pocket_data.DoRamping = false
  pocket_data.RampDistance = 10.0
  pocket_data.RasterAngle = 0.0
  pocket_data.RasterAllowance = 0.0
  pocket_data.UseAreaClearTool = false
  pocket_data.DoRasterClearance = false
  pocket_data.ProfilePassType = PocketParameterData.PROFILE_LAST
  pocket_data.ProjectToolpath = false
  pocket_data.CutDirection = ProfileParameterData.CLIMB_DIRECTION

  local pos_data = default_pos_data(in_mm)
  local geometry_selector = default_geometry_selector()

  local toolpath_manager = ToolpathManager()
  local toolpath_id = toolpath_manager:CreatePocketingToolpath(
    op.name,
    tool,
    nil,
    pocket_data,
    pos_data,
    geometry_selector,
    false,
    true
  )

  clear_selection(job)

  if toolpath_id == nil then
    return false, "No se pudo crear pocket: " .. op.name
  end

  return true, "OK | Tool: " .. tostring(tool_name)
end

local function create_drill_toolpath(job, job_data, op, in_mm, tool_db)
  local ok_sel, sel_info = select_all_objects_on_layer(job, op.layer)
  if not ok_sel then
    return false, sel_info
  end

  local tool, tool_name = build_tool_for_operation(job_data, op, tool_db, in_mm)

  local drill_data = DrillParameterData()
  drill_data.Name = op.name
  drill_data.StartDepth = op.start_depth
  drill_data.CutDepth = op.cut_depth
  drill_data.DoPeckDrill = false
  drill_data.PeckRetractGap = 0.0
  drill_data.ProjectToolpath = false

  local pos_data = default_pos_data(in_mm)
  local geometry_selector = default_geometry_selector()

  local toolpath_manager = ToolpathManager()
  local toolpath_id = toolpath_manager:CreateDrillingToolpath(
    op.name,
    tool,
    drill_data,
    pos_data,
    geometry_selector,
    false,
    true
  )

  clear_selection(job)

  if toolpath_id == nil then
    return false, "No se pudo crear drill: " .. op.name
  end

  return true, "OK | Tool: " .. tostring(tool_name)
end

local function create_toolpath_for_operation(job, job_data, op, in_mm, tool_db)
  if upper_trim(op.layer) == "STOCK" then
    return true, "STOCK ignorado"
  end

  if op.type == "profile" then
    return create_profile_toolpath(job, job_data, op, in_mm, tool_db)
  elseif op.type == "pocket" then
    return create_pocket_toolpath(job, job_data, op, in_mm, tool_db)
  elseif op.type == "drill" then
    return create_drill_toolpath(job, job_data, op, in_mm, tool_db)
  else
    return false, "Tipo no soportado: " .. tostring(op.type)
  end
end

function main(script_path)
  local job = VectricJob()
  if not job.Exists then
    message("No hay un job abierto.")
    return false
  end

  local script_dir = get_script_dir(script_path)
  local repo_dir = dirname(script_dir)
  local settings_path = get_settings_path(script_dir)
  local remembered = load_import_settings(settings_path)

  local defaults = {
    json_path = remembered.json_path or join_path(repo_dir, "samples\\pieza_001\\pieza_001.json"),
    dxf_path = remembered.dxf_path or join_path(repo_dir, "samples\\pieza_001\\pieza_001.dxf"),
    tools_path = remembered.tools_path or join_path(repo_dir, "tools\\fetched_from_vtdb.json")
  }

  local selected = choose_input_files(defaults)
  if selected == nil then
    return false
  end

  if not file_exists(selected.json_path) then
    message("No existe el JSON:\n" .. tostring(selected.json_path))
    return false
  end

  if not file_exists(selected.dxf_path) then
    message("No existe el DXF:\n" .. tostring(selected.dxf_path))
    return false
  end

  if not file_exists(selected.tools_path) then
    message("No existe el JSON de herramientas:\n" .. tostring(selected.tools_path))
    return false
  end

  local json_text = read_all_text(selected.json_path)
  if json_text == nil or json_text == "" then
    message("No se pudo leer el JSON:\n" .. tostring(selected.json_path))
    return false
  end

  local tools_text = read_all_text(selected.tools_path)
  if tools_text == nil or tools_text == "" then
    message("No se pudo leer el JSON de herramientas:\n" .. tostring(selected.tools_path))
    return false
  end

  local data
  local ok_data, err_data = pcall(function()
    data = parse_job_json(json_text)
  end)
  if not ok_data then
    message("Error parseando JSON del job:\n" .. tostring(err_data))
    return false
  end

  local tool_db
  local ok_tools, err_tools = pcall(function()
    tool_db = parse_tool_database(tools_text)
  end)
  if not ok_tools then
    message("Error parseando JSON de herramientas:\n" .. tostring(err_tools))
    return false
  end

  save_import_settings(settings_path, selected)

  local dxf_path = selected.dxf_path

  local in_mm = true
  local units_l = trim(string.lower(data.units or "mm"))
  if units_l == "in" or units_l == "inch" or units_l == "inches" then
    in_mm = false
  end

  clear_selection(job)

  if not job:ImportDxfDwg(dxf_path) then
    message("No se pudo importar el DXF:\n" .. dxf_path)
    return false
  end

  job:Refresh2DView()

  local stock_box, stock_err, stock_real_name, stock_count = get_stock_bounds(job)
  if stock_box == nil then
    local layers = list_layers(job)
    message(
      "Error leyendo STOCK:\n" ..
      tostring(stock_err) ..
      "\n\nCapas disponibles:\n" ..
      table.concat(layers, "\n")
    )
    return false
  end

  local ok_resize, stock_w, stock_h = resize_job_to_stock(job, stock_box, data.material.thickness)
  local resize_note = ""
  if not ok_resize then
    resize_note = "ResizeSheet devolvio false/nil, pero se continua"
  else
    resize_note = "ResizeSheet OK"
  end

  local ok_origin = apply_xy_origin(job, data.origin)
  job:Refresh2DView()

  local report = {}
  table.insert(report, "DXF importado: " .. dxf_path)
  table.insert(report, "JSON: " .. selected.json_path)
  table.insert(report, "Tools JSON: " .. selected.tools_path)
  table.insert(report, "")
  table.insert(report, "STOCK layer real: " .. tostring(stock_real_name))
  table.insert(report, "STOCK objetos: " .. tostring(stock_count))
  table.insert(report, "Sheet width: " .. tostring(stock_w))
  table.insert(report, "Sheet height: " .. tostring(stock_h))
  table.insert(report, "Thickness: " .. tostring(data.material.thickness))
  table.insert(report, "Units: " .. tostring(data.units))
  table.insert(report, "Origin aplicada: " .. tostring(ok_origin))
  table.insert(report, "Resize status: " .. resize_note)
  table.insert(report, "")

  local created = 0
  local failed = 0

  for i = 1, #data.operations do
    local op = data.operations[i]

    if upper_trim(op.layer) ~= "STOCK" then
      local ok_tp, msg = create_toolpath_for_operation(job, data, op, in_mm, tool_db)
      if ok_tp then
        created = created + 1
        table.insert(report, "[OK] " .. op.name .. " | " .. op.layer .. " | " .. op.type .. " -> " .. tostring(msg))
      else
        failed = failed + 1
        table.insert(report, "[ERROR] " .. op.name .. " | " .. op.layer .. " | " .. op.type .. " -> " .. tostring(msg))
      end
    end
  end

  clear_selection(job)
  job:Refresh2DView()

  table.insert(report, "")
  table.insert(report, "Toolpaths creados: " .. tostring(created))
  table.insert(report, "Toolpaths con error: " .. tostring(failed))

  message(table.concat(report, "\n"))

  return failed == 0
end