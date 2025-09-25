# Test_StringValues

> Test StringValues

## Case 1

Description

``````yaml
input:
  - un
  - test
expected:
  name: null
  generateExpectedData: null
  verify: 
    - type: match
      jsonPath: $
      allowAdditionalProperties: false
      data: 
        - un
        - test
``````