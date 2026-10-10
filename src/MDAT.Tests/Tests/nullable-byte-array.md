# Nullable_byte_array

> Nullable byte[]

## Case 1

Description

``````yaml
form: null
expected:
  name: null
  generateExpectedData: null
  verify: 
    - type: match
      jsonPath: $
      allowAdditionalProperties: false
      data: null
``````