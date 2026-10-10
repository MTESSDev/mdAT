# Test_JsonValue

> Test SystemTextJsonFullConverter - JsonValue

## Case 1

Description

``````yaml
values: 
 - 2.923923
 - "test23"
expected:
  name: null
  generateExpectedData: null
  verify: 
    - type: match
      jsonPath: $
      allowAdditionalProperties: false
      data: 
        - 2.923923
        - "test23"
``````