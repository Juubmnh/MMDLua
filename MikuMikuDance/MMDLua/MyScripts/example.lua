require [[utilities\mish]]

import 'System.Numerics'

mish.beginParams()
mish.requireInt("duration", 120)
mish.endParams()
mish.log(string.format('duration=%d', duration))

mish.log(motion.ModelName)
mish.log(motion.Bones:get_Item(0).Name)

generateBoneKeyFrames({
    motion.Bones:get_Item(0),
    motion.Bones:get_Item(findBoneIndex('左足ＩＫ')),
    motion.Bones:get_Item(findBoneIndex('右足ＩＫ'))
  },
  function(bonest, t)
    bonest.Position = Vector3(10 * math.cos(2 * math.pi * t), 0, 20 * math.sin(2 * math.pi * t))
    curve = function(t) return matrix{ t, t } end
    bonest.Interpolation = BoneInterpolation(
      bezierApproximate(curve, 100),
      bezierApproximate(curve, 100),
      bezierApproximate(curve, 100),
      bezierApproximate(curve, 100)
    )
  end,
0, duration, 120)
