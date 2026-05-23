-- suppress_output_for_current()

dofile([[MyScripts\tool.lua]])

begin_params()
require_int("duration", 240)
end_params()
println(string.format('duration=%d', duration))

println(motion.ModelName)
println(motion.Bones:get_Item(0).Name)

for i = 0, duration, 1 do
  bonest = BoneState()
  bonest.Position = Vector3(10 * math.cos(math.pi * i / 60), 0, 20 * math.sin(math.pi * i / 60))
  
  bonekf = BoneKeyFrame()
  bonekf.KeyFrameIndex = i
  bonekf.Value = bonest
  
  motion.Bones:get_Item(0).KeyFrames:Add(bonekf)
  motion.Bones:get_Item(find_bone_index('左足ＩＫ')).KeyFrames:Add(bonekf)
  motion.Bones:get_Item(find_bone_index('右足ＩＫ')).KeyFrames:Add(bonekf)
end
