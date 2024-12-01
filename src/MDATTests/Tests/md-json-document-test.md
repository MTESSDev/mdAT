# Md_JsonDocument_test

> Test JsonDocument Load\
This test whole document integrity

## Case 1

Description

``````yaml
jsonDocument: {"demo": "json"}
expected:
  name: null
  generateExpectedData: null
  verify: 
    - type: match
      jsonPath: $
      allowAdditionalProperties: false
      data: {"demo": "json"}
``````