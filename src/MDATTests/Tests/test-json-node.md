# Test_JsonNode

> Test SystemTextJsonFullConverter

## Case 1

Description

``````yaml
nodes: 
  - 1.2
  - test
expected:
  name: null
  generateExpectedData: null
  verify: 
    - type: match
      jsonPath: $
      allowAdditionalProperties: false
      data: 
        - 1.2
        - test
``````