# Test_AddYamlTypeConverter

> Test AddYamlTypeConverter

## Case 1

Description

``````yaml
input: un;test
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