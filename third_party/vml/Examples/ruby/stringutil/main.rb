def reverse(s)
  result = ""
  i = s.length - 1
  while i >= 0
    result = result + s[i]
    i = i - 1
  end
  return result
end
