println('From tool.lua')

import('System')
import('System.Numerics')
import('Scallion')
Vector3 = luanet.import_type('System.Numerics.Vector3')
Motion = luanet.import_type('Scallion.DomainModels.Motion')
Bone = luanet.import_type('Scallion.DomainModels.Components.Bone')
IKBone = luanet.import_type('Scallion.DomainModels.Components.IKBone')
BoneState = luanet.import_type('Scallion.DomainModels.Components.BoneState')
BoneInterpolation = luanet.import_type('Scallion.DomainModels.Components.BoneInterpolation')
Interpolation = luanet.import_type('Scallion.DomainModels.Components.Interpolation')
InterpolationParameter = luanet.import_type('Scallion.DomainModels.Components.InterpolationParameter')
BoneKeyFrame = luanet.import_type('Scallion.DomainModels.Components.BoneKeyFrame')

find_bone_index = function(name)
  for i = 0, motion.Bones.Count - 1 do
    if string.find(motion.Bones:get_Item(i).Name, name) ~= nil then
      return i
    end
  end
  return nil
end