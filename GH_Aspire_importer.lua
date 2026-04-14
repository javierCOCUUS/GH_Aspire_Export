-- VECTRIC LUA SCRIPT

local JSON_PATH = "C:\\My Drive\\pieza_001.json"
local STOCK_LAYER_NAME = "STOCK"

local CONFIG_TOOLS = {
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

local function message(txt)
  DisplayMessageBox(tostring(txt))
end

local function trim(s)
  if s == nil then return "" end
  return (s:gsub("^%s+", ""):gsub("%s+$", ""))
end

local function upper_trim(s)
  return string.upper(trim(s or ""))
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

local function replace_extension(path, new_ext)
  local base = path:gsub("%.[^%.\\/:]+$", "")
  return base .. new_ext
end

local function json_get_string(block, key)
  local pattern = '"' .. key .. '"%s*:%s*"(.-)"'
  return block:match(pattern)
end

local function json_get_number(block, key)
  local pattern = '"' .. key .. '"%s*:%s*([%-]?[%d%.]+)'
  local s = block:match(pattern)
  if s == nil then return nil end
  return tonumber(s)
end

local function parse_operations_array(json_text)
  local ops = {}
  local ops_block = json_text:match('"operations"%s*:%s*%[(.*)%]%s*}')
  if ops_block == nil then
    return ops
  end

  for obj in ops_block:gmatch("{(.-)}") do
    local op = {}
    op.name = json_get_string(obj, "name") or ""
    op.layer = json_get_string(obj, "layer") or ""
    op.type = json_get_string(obj, "type") or ""
    op.start_depth = json_get_number(obj, "start_depth") or 0.0
    op.cut_depth = json_get_number(obj, "cut_depth") or 0.0
    op.side = json_get_string(obj, "side") or ""
    table.insert(ops, op)
  end

  return ops
end

local function parse_job_json(json_text)
  local data = {}

  data.job_name = json_get_string(json_text, "job_name") or "job"
  data.units = json_get_string(json_text, "units") or "mm"
  data.origin = json_get_string(json_text, "origin") or "BOTTOM_LEFT"

  local material_block = json_text:match('"material"%s*:%s*{(.-)}')
  if material_block == nil then
    material_block = ""
  end

  data.material = {
    thickness = json_get_number(material_block, "thickness") or 18.0,
    z_zero = json_get_string(material_block, "z_zero") or "material_top"
  }

  data.operations = parse_operations_array(json_text)

  return data
end

local function build_tool(cfg, in_mm)
  local tool = Tool(cfg.name, cfg.tool_type)
  tool.InMM = in_mm
  tool.ToolNumber = cfg.tool_number
  tool.ToolDia = cfg.dia
  tool.Stepdown = cfg.stepdown
  tool.Stepover = cfg.stepover
  tool.RateUnits = Tool.MM_SEC
  tool.FeedRate = cfg.feed
  tool.PlungeRate = cfg.plunge
  tool.SpindleSpeed = cfg.spindle
  tool:UpdateParameters()
  return tool
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

local function create_profile_toolpath(job, op, in_mm)
  local ok_sel, sel_info = select_all_objects_on_layer(job, op.layer)
  if not ok_sel then
    return false, sel_info
  end

  local tool = build_tool(CONFIG_TOOLS.profile, in_mm)

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

  return true, "OK"
end

local function create_pocket_toolpath(job, op, in_mm)
  local ok_sel, sel_info = select_all_objects_on_layer(job, op.layer)
  if not ok_sel then
    return false, sel_info
  end

  local tool = build_tool(CONFIG_TOOLS.pocket, in_mm)

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

  return true, "OK"
end

local function create_drill_toolpath(job, op, in_mm)
  local ok_sel, sel_info = select_all_objects_on_layer(job, op.layer)
  if not ok_sel then
    return false, sel_info
  end

  local tool = build_tool(CONFIG_TOOLS.drill, in_mm)

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

  return true, "OK"
end

local function create_toolpath_for_operation(job, op, in_mm)
  if upper_trim(op.layer) == "STOCK" then
    return true, "STOCK ignorado"
  end

  if op.type == "profile" then
    return create_profile_toolpath(job, op, in_mm)
  elseif op.type == "pocket" then
    return create_pocket_toolpath(job, op, in_mm)
  elseif op.type == "drill" then
    return create_drill_toolpath(job, op, in_mm)
  else
    return false, "Tipo no soportado: " .. tostring(op.type)
  end
end

function main()
  local job = VectricJob()
  if not job.Exists then
    message("No hay un job abierto.")
    return false
  end

  if not file_exists(JSON_PATH) then
    message("No existe el JSON:\n" .. JSON_PATH)
    return false
  end

  local json_text = read_all_text(JSON_PATH)
  if json_text == nil or json_text == "" then
    message("No se pudo leer el JSON:\n" .. JSON_PATH)
    return false
  end

  local data = parse_job_json(json_text)
  local dxf_path = replace_extension(JSON_PATH, ".dxf")

  if not file_exists(dxf_path) then
    message("No existe el DXF:\n" .. dxf_path)
    return false
  end

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
  table.insert(report, "JSON: " .. JSON_PATH)
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
      local ok_tp, msg = create_toolpath_for_operation(job, op, in_mm)
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