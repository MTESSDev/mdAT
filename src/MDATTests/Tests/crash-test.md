# Crash_Test

> Exception test

## Case 1

Description

``````yaml
val1: 1
val2: 2
expected:
  verify:
  - type: match
    jsonPath: $
    allowAdditionalProperties: true
    data: 
      ClassName: System.ArgumentOutOfRangeException
      Message: out of range!
``````