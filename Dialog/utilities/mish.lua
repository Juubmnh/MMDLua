--- useful utilities for MMDLua scripts

complex = require [[utilities\complex]]
matrix = require [[utilities\matrix]]

import 'Scallion.DomainModels.Components'

--- round a number to the nearest integer
function math.round(n)
  if n > 0 then return math.floor(n + 0.5)
  else return math.ceil(n - 0.5) end
end

--- generalized permutation number P(n, m)
function permutation(n, m)
  if math.type(m) ~= 'integer' or m < 0 then
    error('m must be a non-negative integer')
  end
  if m == 0 then return 1 end
  local r = 1
  for i = 1, m do
    r = r * (n - i + 1)
  end
  return r
end

--- generalized combination number C(n, m)
function combination(n, m)
  if math.type(m) ~= 'integer' or m < 0 then
    error('m must be a non-negative integer')
  end
  if m == 0 then return 1 end
  local r = 1
  for i = 1, m do
    r = r * (n - i + 1) / i
  end
  return r
end

--- the Bernstein basis function of the binomial distribution B(n,m) with t as the variable
function bernsteinBasis(n, m, t)
  return combination(n, m) * t^m * (1 - t)^(n - m)
end

---
-- @param curve a curve function to be approximated, with the normalized time parameter, returning a 2×1 matrix representing a single point; the curve should pass through the points (0,0) and (1,1)
-- @param samples the count of samples taken from the curve
-- @return a Scallion.DomainModels.Components.Interpolation object
function bezierApproximate(curve, samples)
  local coefficient = matrix(samples, 2)
  local constant = matrix(samples, 2)
  for i = 1, samples do
    local t = i / (samples + 1)
    coefficient[i][1] = bernsteinBasis(3, 1, t)
    coefficient[i][2] = bernsteinBasis(3, 2, t)
    local p = curve(t)
    constant[i][1] = p[1][1] - t^3
    constant[i][2] = p[2][1] - t^3
  end
  local leastSquaresSolution = matrix.invert(matrix.transpose(coefficient) * coefficient) * matrix.transpose(coefficient) * constant
  local first = InterpolationParameter(math.round(leastSquaresSolution[1][1] * 127), math.round(leastSquaresSolution[1][2] * 127))
  local second = InterpolationParameter(math.round(leastSquaresSolution[2][1] * 127), math.round(leastSquaresSolution[2][2] * 127))
  return Interpolation(first, second)
end

---
-- @param name the text contained in the bone name
-- @return the zero-based index of the bone, or nil if not found
function findBoneIndex(name)
  for i = 0, motion.Bones.Count - 1 do
    if string.find(motion.Bones:get_Item(i).Name, name) ~= nil then
      return i
    end
  end
  return nil
end

---
-- @param bones a table of Scallion.DomainModels.Components.Bone objects acquiring the keyframes
-- @param callback the callback function used to set a BoneState object, which accepts an empty BoneState object to be modified and a normalized time parameter
-- @param startFrame the first frame index of the keyframe sequence
-- @param endFrame the last frame index of the keyframe sequence
-- @param segments the number of segments into which this part of the animation is divided; the default is 1
function generateBoneKeyFrames(bones, callback, startFrame, endFrame, segments)
  segments = segments or 1
  for i = 0, segments do
    local t = i / segments
    keyFrame = BoneKeyFrame()
    keyFrame.KeyFrameIndex = startFrame + math.round((endFrame - startFrame) * t)
    local boneState = BoneState()
    callback(boneState, t)
    keyFrame.Value = boneState
    for j = 1, #bones do
      bones[j].KeyFrames:Add(keyFrame)
    end
  end
end
